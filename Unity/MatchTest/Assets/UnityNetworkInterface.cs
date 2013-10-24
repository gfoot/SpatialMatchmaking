using UnityEngine;

namespace Assets
{
    public class UnityNetworkInterface : MonoBehaviour, INetworkInterface
    {
        private string _expectedClientUuid;
        private string _localUuid;

        public int PortMin = 52202;
        public int PortMax = 52299;
        private string _connectToGuid;

        public bool Connected { get; private set; }
        public bool Ready { get; private set; }
        public bool Connecting { get; private set; }
        public string NetworkError { get; private set; }

        public void Start()
        {
            Ready = true;
        }

        public string GetConnectionInfo()
        {
            return Network.player.guid;
        }

        public string GetBadConnectionInfo()
        {
            return "1234567890";
        }

        public string Listen(string expectedClientUuid)
        {
            _expectedClientUuid = expectedClientUuid;
            _connectToGuid = null;

            var error = NetworkConnectionError.NoError;
            for (int i = 0; i < 10; ++i)
            {
                error = Network.InitializeServer(5, (int)(PortMin + (PortMax + 1 - PortMin) * Random.value), true);
                if (error == NetworkConnectionError.NoError)
                    return null;
            }

            return error.ToString();
        }

        [RPC]
        public void HelloFrom(string clientUuid, NetworkMessageInfo info)
        {
            if (clientUuid != _expectedClientUuid)
            {
                Network.CloseConnection(info.sender, true);
                return;
            }
            Connected = true;
        }

        public void StopListening()
        {
            Network.Disconnect();
        }

        public bool Connect(string connectionInfo, string localUuid)
        {
            _localUuid = localUuid;
            _connectToGuid = connectionInfo;

            //var addressAndPort = connectionInfo.Split(':');
            //var address = addressAndPort[0];
            //var port = int.Parse(addressAndPort[1]);
            
            var error = Network.Connect(connectionInfo);
            if (error != NetworkConnectionError.NoError)
            {
                NetworkError = error.ToString();
                return false;
            }

            Connecting = true;
            
            return true;
        }

        public void OnConnectedToServer()
        {
            Debug.Log("OnConnectedToServer");
            networkView.RPC("HelloFrom", RPCMode.Server, _localUuid);
            Connecting = false;
            Connected = true;
        }

        public void OnFailedToConnect(NetworkConnectionError error)
        {
            Debug.Log("OnFailedToConnect");
            NetworkError = error.ToString();
            Connecting = false;
        }

        public void OnGUI()
        {
            GUILayout.Label(Network.player.guid);
            if (_connectToGuid != null)
                GUILayout.Label(_connectToGuid);
        }
    }
}