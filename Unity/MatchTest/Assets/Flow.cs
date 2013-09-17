using UnityEngine;
using System.Collections;

namespace Assets
{
    public class Flow : MonoBehaviour
    {
        private System.Guid _uuid;
        private int _clientId;

        private JsonObject _clientData;

        public string BaseUrl = "http://localhost:9998";
        private JsonObject _sessionData;

        public void Start()
        {
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            _uuid = System.Guid.NewGuid();
            yield return null;
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

                        if (_sessionData == null)
                        {
                            
                        }
                        else
                        {
                            GuiField("Session ID", _sessionData.GetNumber("id"));
                        }
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();

            if (GUI.Button(new Rect(10, 10, 200, 20), "JsonTest"))
                JsonTest();
        }

        private IEnumerator Register()
        {
            var postData = new JsonObject();
            postData.Set("uuid", _uuid.ToString());

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
            }
            else
            {
                Debug.LogError("WWW error: " + www.error);
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
