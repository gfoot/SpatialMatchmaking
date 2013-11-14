using UnityEngine;

namespace Assets.SpatialMatchmaking
{
    /// <summary>
    /// An INetworkInterface for Unity's built-in networking API.  It should be attached to a GameObject which has a NetworkView.  It's not 
    /// necessary for the NetworkView to be observing anything in particular, we just need it for sending and receiving RPCs.
    /// 
    /// No configuration is required, but you can reconfigure the listen port range using 'ListenPortMin' and 'ListenPortMax' if you need to.
    /// </summary>
    public class UnityNetworkInterface : MonoBehaviour, INetworkInterface
    {
        /// <summary>
        /// Minimum port number to listen on, when listening
        /// </summary>
        public int ListenPortMin = 52202;
        
        /// <summary>
        /// Maximum port number to listen on, when listening
        /// </summary>
        public int ListenPortMax = 52299;

        /// <summary>
        /// Enable the display of debugging information via OnGUI
        /// </summary>
        public bool DisplayDebugUI = false;

        /// <summary>
        /// For debug use only - simulate NAT negotiation errors.  Peers can only communicate if they share some bits in common.
        /// </summary>
        public int DebugConnectivityBits = 1;


        public bool Connected { get; private set; }
        public bool Connecting { get; private set; }
        public string NetworkError { get; private set; }

        public string GetConnectionInfo()
        {
            return Network.player.guid;
        }

        public string GetBadConnectionInfo()
        {
            // This isn't great, because it causes the facilitator to reject the connection attempt sooner than you'd get from a NAT failure, but it's OK for now
            return "1234567890";
        }

        public bool StartListening(string expectedClientUuid)
        {
            _expectedClientUuid = expectedClientUuid;
            _connectToGuid = null;

            var error = NetworkConnectionError.NoError;
            NetworkError = "";
            for (int i = 0; i < 10; ++i)
            {
                error = Network.InitializeServer(5, (int)(ListenPortMin + (ListenPortMax + 1 - ListenPortMin) * Random.value), true);
                if (error == NetworkConnectionError.NoError)
                    return true;
            }

            NetworkError = error.ToString();
            return false;
        }

        [RPC]
        public void RpcHelloFrom(string clientUuid, NetworkMessageInfo info)
        {
            var split = clientUuid.Split('!');
            
            clientUuid = split[0];
            var remoteConnectivityBits = int.Parse(split[1]);
            
            if ((remoteConnectivityBits & DebugConnectivityBits) == 0)
            {
                Network.CloseConnection(info.sender, false);
                return;
            }

            if (clientUuid != _expectedClientUuid)
            {
                Network.CloseConnection(info.sender, true);
                return;
            }

            Connected = true;
            networkView.RPC("RpcWelcome", info.sender);
        }

        public void StopListening()
        {
            Network.Disconnect();
        }

        public bool StartConnecting(string connectionInfo, string localUuid)
        {
            _localUuid = localUuid;
            _connectToGuid = connectionInfo;

            var error = Network.Connect(connectionInfo);
            if (error != NetworkConnectionError.NoError)
            {
                NetworkError = error.ToString();
                return false;
            }

            Connecting = true;
            
            return true;
        }

        public void StopConnecting()
        {
            Network.Disconnect();
            Connecting = false;
            Connected = false;
        }

        public void OnConnectedToServer()
        {
            networkView.RPC("RpcHelloFrom", RPCMode.Server, _localUuid + "!" + DebugConnectivityBits);
        }

        [RPC]
        public void RpcWelcome(NetworkMessageInfo info)
        {
            Connecting = false;
            Connected = true;
        }

        public void OnFailedToConnect(NetworkConnectionError error)
        {
            NetworkError = error.ToString();
            Connecting = false;
        }

        public void OnGUI()
        {
            if (!DisplayDebugUI) return;

            GUI.Label(new Rect(0, 0, 0.5f * Screen.width, Screen.height), Network.player.guid);

            if (_connectToGuid != null)
                GUI.Label(new Rect(0.5f * Screen.width, 0, 0.5f * Screen.width, Screen.height), _connectToGuid);
        }

        private string _connectToGuid;
        private string _expectedClientUuid;
        private string _localUuid;
    }
}