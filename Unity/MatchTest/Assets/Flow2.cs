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

        private TestLocationInterface _testLocationInterface;
        private string _testLatitude = "50.83946";
        private string _testLongitude = "-0.1729644";

        private bool _useTestLocationInterface;

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

            UpdateBackgroundColor();

            _testLocationInterface = new TestLocationInterface();

            SetTestLocation(0.0, 0.0);
        }

        private void UpdateBackgroundColor()
        {
            Camera.main.backgroundColor = new Color(
                                                     (_connectivityBits & 1) > 0 ? 0.5f : 0.0f,
                                                     (_connectivityBits & 2) > 0 ? 0.5f : 0.0f,
                                                     (_connectivityBits & 4) > 0 ? 0.5f : 0.0f
                                                     );
        }

        private void SetTestLocation(double latitude, double longitude)
        {
            _testLocationInterface.SetLocation(new Location { Latitude = latitude, Longitude = longitude });
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
                    _useTestLocationInterface = GUILayout.Toggle(_useTestLocationInterface, "Use test LocationInterface");
                    if (!_useTestLocationInterface)
                    {
                        if (Input.location.status == LocationServiceStatus.Stopped)
                            Input.location.Start();
                        if (Input.location.status == LocationServiceStatus.Running)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("lat/long", GUILayout.Width(70));
                            GUILayout.Label(string.Format("{0} / {1}", Input.location.lastData.latitude,
                                                          Input.location.lastData.longitude));
                            GUILayout.EndHorizontal();
                        }
                    }

                    if (_useTestLocationInterface)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("lat/long", GUILayout.Width(70));

                        _testLatitude = GUILayout.TextField(_testLatitude, GUILayout.Width(100));
                        _testLongitude = GUILayout.TextField(_testLongitude, GUILayout.Width(100));
                        if (GUILayout.Button("Set"))
                        {
                            double latitude;
                            double longitude;
                            if (double.TryParse(_testLatitude, out latitude) && double.TryParse(_testLongitude, out longitude))
                                SetTestLocation(latitude, longitude);
                        }
                        GUILayout.FlexibleSpace();

                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("", GUILayout.Width(70));
                        GUILayout.Label(_testLocationInterface.Location.Latitude.ToString(), GUILayout.Width(100));
                        GUILayout.Label(_testLocationInterface.Location.Longitude.ToString(), GUILayout.Width(100));
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }

                    if (_connector == null)
                    {
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("conn bits", GUILayout.Width(70));
                            bool r = GUILayout.Toggle((_connectivityBits & 1) != 0, "");
                            bool g = GUILayout.Toggle((_connectivityBits & 2) != 0, "");
                            bool b = GUILayout.Toggle((_connectivityBits & 4) != 0, "");
                            _connectivityBits = (r ? 1 : 0) | (g ? 2 : 0) | (b ? 4 : 0);
                            UpdateBackgroundColor();
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                        if (GuiButton("Go"))
                        {
                            var unityNetworkInterface = gameObject.AddComponent<UnityNetworkInterface>();
                            unityNetworkInterface.DisplayDebugUI = true;
                            unityNetworkInterface.DebugConnectivityBits = _connectivityBits;

                            var locationInterface = _testLocationInterface;
                            //var locationInterface = new UnityInputLocationInterface();

                            _connector = gameObject.AddComponent<Connector>();
                            _connector.NetworkInterface = unityNetworkInterface;
                            _connector.LocationInterface = locationInterface;
                            _connector.BaseUrl = BaseUrl;
                            _connector.GameName = "com.studiogobo.fi.Matcher.Unity.MatchTest";
                            _connector.MaxMatchRadius = 500;
                            _connector.OnSuccess += Success;
                            //_connector.OnFailure += ...;
                            _connector.OnLogEvent += ProcessLogEvent;

                            //_connector.OnSuccess += locationInterface.Dispose;
                            //_connector.OnFailure += locationInterface.Dispose;
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
