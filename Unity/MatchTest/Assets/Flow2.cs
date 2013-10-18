using UnityEngine;

namespace Assets
{
    public class Flow2 : MonoBehaviour
    {
        public string BaseUrl = "http://fi-cloud:8080";

        private Connector _connector;

        public void Start()
        {
            _connector = new Connector(new UnityNetworkInterface());
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
                    GuiField("UUID", _connector.Uuid);

                    if (_connector.ClientData == null)
                    {
                        if (GuiButton("Register"))
                        {
                            StartCoroutine(_connector.Register(BaseUrl, "com.studiogobo.fi.Matcher.Unity.MatchTest"));
                        }
                    }
                    else
                    {
                        GuiField("Client ID", _connector.ClientData.GetInteger("id"));

                        if (_connector.SessionData != null)
                        {
                            GuiField("Session ID", _connector.SessionData.GetInteger("id"));

                            if ((_connector.SessionData.GetArray("clients").GetInteger(0) - _connector.ClientData.GetInteger("id")) == 0)
                            {
                                GuiField("My role", "Host");
                                if (_connector.OtherClientData != null)
                                {
                                    if (!_connector.Connected)
                                    {
                                        GuiField("Listen for", _connector.OtherClientData.GetString("uuid"));
                                    }
                                    else
                                    {
                                        GuiField("Connected to", _connector.OtherClientData.GetString("uuid"));
                                    }
                                }
                            }
                            else
                            {
                                GuiField("My role", "Client");
                                if (_connector.OtherClientData != null)
                                {
                                    if (!_connector.Connected)
                                    {
                                        GuiField("Connect to", _connector.OtherClientData.GetString("uuid"));
                                        GuiField("at address", _connector.OtherClientData.GetString("connectionInfo"));
                                    }
                                    else
                                    {
                                        GuiField("Connected to", _connector.OtherClientData.GetString("uuid"));
                                    }
                                }
                            }

                            if (_connector.NetworkError != null)
                                GuiField("Network error", _connector.NetworkError);
                        }
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();
        }
    }
}
