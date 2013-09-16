using UnityEngine;
using System.Collections;

namespace Assets
{
    public class Flow : MonoBehaviour
    {
        private System.Guid _uuid;
        private int _clientId;

        public void Start()
        {
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            _uuid = System.Guid.NewGuid();
            yield return null;
        }

        private static void GuiField(string label, string value)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(label, GUILayout.Width(200));
                GUILayout.Label(value, GUILayout.ExpandWidth(true));
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
                    GuiField("UUID", _uuid.ToString());

                    if (_clientId == 0)
                    {
                        if (GuiButton("Register"))
                        {
                            Register();
                        }
                    }
                    else
                    {
                        GuiField("Client ID", _clientId.ToString());
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();
        }

        private void Register()
        {
            _clientId = 1;

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
