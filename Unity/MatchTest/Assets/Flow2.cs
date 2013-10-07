using UnityEngine;
using System.Collections;

namespace Assets
{
    public class Flow2 : MonoBehaviour
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
            GUILayout.BeginArea(new Rect(Screen.width*0.1f, Screen.height*0.1f, Screen.width*0.8f, Screen.height*0.8f));
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
                        GuiField("Client ID", _clientData.GetInteger("id"));

                        if (_sessionData != null)
                        {
                            GuiField("Session ID", _sessionData.GetInteger("id"));

                            if ((_sessionData.GetArray("clients").GetInteger(0) - _clientData.GetInteger("id")) == 0)
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
        }

        private JsonObject RequireAttribute(string attribute, params string[] values)
        {
            var valuesArray = new JsonArray();
            foreach (var value in values)
                valuesArray.Add(value);
         
            var result = new JsonObject();
            result.Set("@type", "requireAttribute");
            result.Set("attribute", attribute);
            result.Set("values", valuesArray);
            return result;
        }

        private IEnumerator Register()
        {
            var requirements = new JsonArray();
            requirements.Add(RequireAttribute("gameName", "com.studiogobo.fi.Matcher.Unity.MatchTest"));

            var postData = new JsonObject();
            postData.Set("uuid", _uuid.ToString());
            postData.Set("connectionInfo", Network.player.ipAddress + ":" + Network.player.port);
            postData.Set("requirements", requirements);

            var headers = new Hashtable();
            headers["Content-Type"] = "application/json";
            var www = new WWW(BaseUrl + "/clients", postData.ToByteArray(), headers);
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("WWW error: " + www.error);
                yield break;
            }

            if (www.responseHeaders["CONTENT-TYPE"] != "application/json")
            {
                Debug.LogError("Bad content type received: " + www.responseHeaders["CONTENT-TYPE"]);
                yield break;
            }

            _clientData = new JsonObject(www.text);

            while (true)
            {
                www = new WWW(BaseUrl + string.Format("/matches?client={0}", _clientData.GetInteger("id")));
                yield return www;

                if (www.error == null)
                    break;

                if (www.error.StartsWith("404"))
                {
                    Debug.Log("No matches yet");
                    continue;
                }

                Debug.LogError("WWW error: " + www.error);
                yield break;
            }

            var sessionId = new JsonObject(www.text).GetInteger("id");

            www = new WWW(BaseUrl + string.Format("/matches/{0}", sessionId));
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("WWW error: " + www.error);
                yield break;
            }

            _sessionData = new JsonObject(www.text);
            var clients = _sessionData.GetArray("clients");
            var otherClient = clients.GetInteger(0) + clients.GetInteger(1) - _clientData.GetInteger("id");

            www = new WWW(BaseUrl + string.Format("/clients/{0}", otherClient));
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("WWW error: " + www.error);
                yield break;
            }

            _otherClientData = new JsonObject(www.text);

            var isHost = (_sessionData.GetArray("clients").GetInteger(0) - _clientData.GetInteger("id")) == 0;
            while (!_connected)
            {
                if (isHost)
                {
                    foreach (var connection in Network.connections)
                    {
                        Debug.Log(connection.ipAddress);

                        // check it's all OK?

                        _connected = true;
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
                }

                yield return null;
            }

            // connected...
        }
    }
}
