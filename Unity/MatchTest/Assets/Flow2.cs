using UnityEngine;

namespace Assets
{
    public class Flow2 : MonoBehaviour
    {
        public string BaseUrl = "http://fi-cloud:8080";

        private Connector _connector;

        public void Start()
        {
        }

        private static void GuiField<T>(string label, T value)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(label, GUILayout.Width(100));
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
                    if (_connector == null)
                    {
                        if (GuiButton("Go"))
                        {
                            _connector = gameObject.AddComponent<Connector>();
                            _connector.NetworkInterface = gameObject.AddComponent<UnityNetworkInterface>();
                            _connector.BaseUrl = BaseUrl;
                            _connector.GameName = "com.studiogobo.fi.Matcher.Unity.MatchTest";
                            //_connector.OnConnected += ...;
                            //_connector.OnConnectFailed += ...;
                        }
                    }
                    else
                    {
                        GuiField("Status", _connector.Status);

                        if (_connector.NetworkError != null)
                            GuiField("Network error", _connector.NetworkError);
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();
        }
    }
}
