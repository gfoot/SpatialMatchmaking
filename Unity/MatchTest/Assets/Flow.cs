using System;
using UnityEngine;
using System.Collections;

namespace Assets
{
    public class Flow : MonoBehaviour
    {
        public string BaseUrl = "http://localhost:9998";

        private System.Guid _uuid;
        private JsonObject _clientData;
        private JsonObject _sessionData;
        private JsonObject _otherClientData;
        private bool _connected;
        private NetworkConnectionError _networkError;

        public void Start()
        {
            _uuid = System.Guid.NewGuid();

            int port = 52202;
            while (true)
            {
                var error = Network.InitializeServer(5, port, false);
                if (error == NetworkConnectionError.NoError)
                    break;

                ++port;
            }
        }

        private static void GuiField<T>(string label, T value)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(label, GUILayout.Width(200));
                GUILayout.Label(value.ToString(), GUILayout.ExpandWidth(true));
            }
            GUILayout.EndHorizontal();
        }

        private static bool GuiButton(string label)
        {
            bool value;

            GUILayout.BeginHorizontal();
            {
                value = GUILayout.Button(label, GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            return value;
        }

        public void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width * 0.1f, Screen.height * 0.1f, Screen.width * 0.8f, Screen.height * 0.8f));
            {
                GUILayout.BeginVertical();
                {
                    GuiField("UUID", _uuid);

                    if (_clientData == null)
                    {
                        if (GuiButton("Register"))
                        {
                            StartCoroutine(Register());
                        }
                    }
                    else
                    {
                        GuiField("Client ID", _clientData.GetNumber("id"));

                        if (_sessionData != null)
                        {
                            GuiField("Session ID", _sessionData.GetNumber("id"));

                            if ((int)(_sessionData.GetArray("clients").GetNumber(0) - _clientData.GetNumber("id") + 0.5) == 0)
                            {
                                GuiField("My role", "Host");
                                if (_otherClientData != null)
                                {
                                    if (!_connected)
                                    {
                                        GuiField("Listen for", _otherClientData.GetString("uuid"));
                                    }
                                    else
                                    {
                                        GuiField("Connected to", _otherClientData.GetString("uuid"));
                                    }
                                }
                            }
                            else
                            {
                                GuiField("My role", "Client");
                                if (_otherClientData != null)
                                {
                                    if (!_connected)
                                    {
                                        GuiField("Connect to", _otherClientData.GetString("uuid"));
                                        GuiField("at address", _otherClientData.GetString("connectionInfo"));
                                    }
                                    else
                                    {
                                        GuiField("Connected to", _otherClientData.GetString("uuid"));
                                    }
                                }
                            }

                            if (_networkError != NetworkConnectionError.NoError)
                                GuiField("Network error", _networkError);
                        }
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();

            if (GUI.Button(new Rect(10, 10, 100, 20), "JsonTest"))
                JsonTest();
        }

        private IEnumerator Register()
        {
            var postData = new JsonObject();
            postData.Set("uuid", _uuid.ToString());
            postData.Set("connectionInfo", Network.player.ipAddress + ":" + Network.player.port);

            var headers = new Hashtable();
            headers["Content-Type"] = "application/json";
            var www = new WWW(BaseUrl + "/clients", postData.ToByteArray(), headers);
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("WWW error: " + www.error);
                yield break;
            }
            
            //foreach (var key in www.responseHeaders.Keys)
            //{
            //    Debug.Log(key + ": " + www.responseHeaders[key]);
            //}
            //Debug.Log(www.text);

            if (www.responseHeaders["CONTENT-TYPE"] != "application/json")
            {
                Debug.LogError("Bad content type received: " + www.responseHeaders["CONTENT-TYPE"]);
                yield break;
            }

            _clientData = new JsonObject(www.text);

            StartCoroutine(GetMatch());
        }

        private IEnumerator GetMatch()
        {
            if (_clientData == null)
                yield break;

            var www = new WWW(BaseUrl + string.Format("/matches?client={0}", _clientData.GetNumber("id")));
            yield return www;

            if (www.error == null)
            {
                var sessionId = (int)(new JsonObject(www.text).GetNumber("id") + 0.5);
                StartCoroutine(GetMatchInfo(sessionId));
            }
            else
            {
                Debug.LogError("WWW error: " + www.error);
            }
        }

        private IEnumerator GetMatchInfo(int sessionId)
        {
            var www = new WWW(BaseUrl + string.Format("/matches/{0}", sessionId));
            yield return www;

            if (www.error == null)
            {
                _sessionData = new JsonObject(www.text);
                var clients = _sessionData.GetArray("clients");
                var otherClient = (int)(clients.GetNumber(0) + clients.GetNumber(1) - _clientData.GetNumber("id") + 0.5);
                StartCoroutine(GetOtherClientInfo(otherClient));
            }
            else
            {
                Debug.LogError("WWW error: " + www.error);
            }
        }

        private IEnumerator GetOtherClientInfo(int clientId)
        {
            var www = new WWW(BaseUrl + string.Format("/clients/{0}", clientId));
            yield return www;

            if (www.error == null)
            {
                _otherClientData = new JsonObject(www.text);
                StartCoroutine(MakeConnection());
            }
            else
            {
                Debug.LogError("WWW error: " + www.error);
            }
        }

        private IEnumerator MakeConnection()
        {
            var isHost = (int)(_sessionData.GetArray("clients").GetNumber(0) - _clientData.GetNumber("id") + 0.5) == 0;
            while (true)
            {
                if (isHost)
                {
                    foreach (var connection in Network.connections)
                    {
                        Debug.Log(connection.ipAddress);
                        
                        // check it's all OK?

                        _connected = true;
                        yield break;
                    }
                }
                else
                {
                    var addressAndPort = _otherClientData.GetString("connectionInfo").Split(':');
                    var address = addressAndPort[0];
                    var port = int.Parse(addressAndPort[1]);
                    _networkError = Network.Connect(address, port);
                    
                    // check it's all OK?
                    
                    _connected = true;
                    yield break;
                }

                yield return null;
            }
        }

        private static void JsonTest()
        {
            var obj = new JsonObject();
            obj.Set("hello", "world");
            obj.Set("another", "string");
            Debug.Log(obj.ToString());

            var array = new JsonArray();
            array.Add("hello");
            var index = array.Add(new JsonObject());
            array.GetObject(index).Set("inner", "object");
            obj.Set("array", array);
            Debug.Log(obj);

            TestJsonParser("{  }");
            TestJsonParser("{ \"a\": \"b\" }");
            TestJsonParser("{ \"a\": \"b\", }", "{ \"a\": \"b\" }");
            TestJsonParser("{ \"a\": \"b\", \"c\": \"d\" }");
            TestJsonParser("{ \"a\": { \"b\": \"c\" }, \"d\": { \"e\": \"f\" } }");
            TestJsonParser("{ \"a\": [ \"b\", [ { \"c\": \"d\" } ] ] }");
            TestJsonParser("{ \"a\": 1 }");
            TestJsonParser("{ \"a\": -1 }");
            TestJsonParser("{ \"a\": 0.5 }");
            TestJsonParser("{ \"a\": [ -0.5, 1, 3 ] }");
        }

        private static void TestJsonParser(string s)
        {
            TestJsonParser(s, s);
        }
            
        private static void TestJsonParser(string s, string expected)
        {
            var obj = JsonObject.Parse(s);
            var t = obj.ToString();
            if (t != expected)
                Debug.LogError(string.Format("Json parser failed - {0} => {1}, expected {2}", s, t, expected));
            else
                Debug.Log("Json parsed OK - " + s);
        }
    }
}
