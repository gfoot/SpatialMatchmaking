using System.Collections.Generic;
using UnityEngine;

namespace Assets
{
    public class Flow2 : MonoBehaviour
    {
        public string BaseUrl = "http://fi-cloud:8080";

        private Connector _connector;
        private int _connectivityBits;
        private string _key;
        private readonly List<string> _log = new List<string>();

        public void Start()
        {
            _connectivityBits = 0;
            for (int i = 0; i < 3; ++i)
            {
                if (Random.value > 0.5f)
                {
                    _connectivityBits |= 1 << i;
                }
            }

            Camera.main.backgroundColor = new Color(
                (_connectivityBits & 1) > 0 ? 0.5f : 0.0f,
                (_connectivityBits & 2) > 0 ? 0.5f : 0.0f,
                (_connectivityBits & 4) > 0 ? 0.5f : 0.0f
            );
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
            GUILayout.BeginArea(new Rect(20, 40, Screen.width - 40, Screen.height - 80));
            {
                GUILayout.BeginVertical();
                {
                    if (_connector == null)
                    {
                        if (GuiButton("Go"))
                        {
                            var unityNetworkInterface = gameObject.AddComponent<UnityNetworkInterface>();
                            unityNetworkInterface.DisplayDebugUI = true;
                            unityNetworkInterface.DebugConnectivityBits = _connectivityBits;

                            _connector = gameObject.AddComponent<Connector>();
                            _connector.NetworkInterface = unityNetworkInterface;
                            _connector.BaseUrl = BaseUrl;
                            _connector.GameName = "com.studiogobo.fi.Matcher.Unity.MatchTest";
                            _connector.OnSuccess += Success;
                            //_connector.OnFailure += ...;
                            _connector.OnLogEvent += ProcessLogEvent;
                        }
                    }
                    else
                    {
                        for (int i = _log.Count - 8; i < _log.Count; ++i)
                            if (i >= 0)
                                GUILayout.Label(_log[i]);
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();

            if (_key != null)
                GUI.Label(new Rect(0, Screen.height - 30, Screen.width, Screen.height - 20), _key);
        }

        private void Success()
        {
            if (Network.isServer)
            {
                _key = ((int)(10000*Random.value)).ToString();
                networkView.RPC("RpcSetKey", RPCMode.Others, _key);
            }
        }

        [RPC]
        public void RpcSetKey(string key, NetworkMessageInfo info)
        {
            _key = key;
        }

        private void ProcessLogEvent(bool isError, string message)
        {
            if (isError)
                _log.Add("    ERROR: " + message);
            else
                _log.Add(message);
        }
    }
}
