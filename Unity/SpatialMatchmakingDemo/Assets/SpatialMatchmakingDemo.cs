using System.Collections.Generic;
using Assets.SpatialMatchmaking;
using UnityEngine;

namespace Assets
{
    public class SpatialMatchmakingDemo : MonoBehaviour
    {
        public string BaseUrl = "http://fi-cloud:8080";
        public int MaxMatchRadius = 500;
        public GUISkin LargeGuiSkin;

        private MatchClient _matchClient;
        private int _connectivityBits;
        private string _key;
        private readonly List<string> _log = new List<string>();

        private TestLocationInterface _testLocationInterface;
        private string _testLatitude = "50.83946";
        private string _testLongitude = "-0.1729644";

        private bool _useTestLocationInterface;
        private int _guiScale;

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

            //Network.natFacilitatorIP = "130.206.83.114";
            //Network.natFacilitatorPort = 50005;

            _guiScale = Screen.width / 300;
            if (_guiScale < 1) _guiScale = 1;
            LargeGuiSkin.label.fontSize = 10 * _guiScale;
            LargeGuiSkin.button.fontSize = 13 * _guiScale;
            LargeGuiSkin.toggle.padding.left = 20 * _guiScale;
            LargeGuiSkin.toggle.padding.top = 20 * _guiScale;
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

        public void OnGUI()
        {
            GUI.skin = LargeGuiSkin;
            int quarterWidth = Screen.width / 4;

            GUILayout.BeginArea(new Rect(20, 40, Screen.width - 40, Screen.height - 80));
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Fake location", GUILayout.Width(quarterWidth));
                    _useTestLocationInterface = GUILayout.Toggle(_useTestLocationInterface, "");
                    GUILayout.EndHorizontal();
                 
                    if (!_useTestLocationInterface)
                    {
                        if (Input.location.status == LocationServiceStatus.Stopped)
                            Input.location.Start();
                        if (Input.location.status == LocationServiceStatus.Running)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("lat/long", GUILayout.Width(quarterWidth));
                            GUILayout.Label(string.Format("{0} / {1}", Input.location.lastData.latitude,
                                                          Input.location.lastData.longitude));
                            GUILayout.EndHorizontal();
                        }
                    }

                    if (_useTestLocationInterface)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("lat/long", GUILayout.Width(quarterWidth));

                        _testLatitude = GUILayout.TextField(_testLatitude, GUILayout.Width(quarterWidth));
                        _testLongitude = GUILayout.TextField(_testLongitude, GUILayout.Width(quarterWidth));
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
                        GUILayout.Label("", GUILayout.Width(quarterWidth));
                        GUILayout.Label(string.Format("{0}", _testLocationInterface.Location.Latitude), GUILayout.Width(quarterWidth));
                        GUILayout.Label(string.Format("{0}", _testLocationInterface.Location.Longitude), GUILayout.Width(quarterWidth));
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }

                    if (_matchClient == null)
                    {
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("conn bits", GUILayout.Width(quarterWidth));
                            var r = GUILayout.Toggle((_connectivityBits & 1) != 0, "");
                            var g = GUILayout.Toggle((_connectivityBits & 2) != 0, "");
                            var b = GUILayout.Toggle((_connectivityBits & 4) != 0, "");
                            _connectivityBits = (r ? 1 : 0) | (g ? 2 : 0) | (b ? 4 : 0);
                            UpdateBackgroundColor();
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Go", GUILayout.ExpandWidth(false)))
                            Go();
                        GUILayout.EndHorizontal();

                        GUILayout.FlexibleSpace();
                    }
                    else
                    {
                        var s = "";
                        for (int i = 0; i < _log.Count; ++i)
                            s += _log[i] + "\n";

                        GUILayout.TextArea(s, GUILayout.ExpandHeight(true));
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Quit", GUILayout.ExpandWidth(false)))
                        Application.Quit();
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();

            if (_key != null)
                GUI.Label(new Rect(0, Screen.height - 30, Screen.width, Screen.height - 20), _key);
        }

        private void Go()
        {
            var unityNetworkInterface = gameObject.AddComponent<UnityNetworkInterface>();
            unityNetworkInterface.DisplayDebugUI = true;
            unityNetworkInterface.DebugConnectivityBits = _connectivityBits;

            _matchClient = gameObject.AddComponent<MatchClient>();
            _matchClient.NetworkInterface = unityNetworkInterface;
            _matchClient.LocationInterface = _testLocationInterface;
            _matchClient.BaseUrl = BaseUrl;
            _matchClient.GameName = "com.studiogobo.fi.SpatialMatchmaking.Unity.SpatialMatchmakingDemo";
            _matchClient.MaxMatchRadius = MaxMatchRadius;
            _matchClient.OnSuccess += Success;
            //_matchClient.OnFailure += ...;
            _matchClient.OnLogEvent += ProcessLogEvent;

            if (!_useTestLocationInterface)
            {
                var locationInterface = new UnityInputLocationInterface();
                _matchClient.LocationInterface = locationInterface;
                _matchClient.OnSuccess += locationInterface.Dispose;
                _matchClient.OnFailure += locationInterface.Dispose;
            }
        }

        private void Success()
        {
            if (Network.isServer)
            {
                _key = string.Format("{0}", (int)(10000*Random.value));
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
