using UnityEngine;

namespace Assets
{
    public class UnityNetworkInterface : MonoBehaviour, INetworkInterface
    {
        public bool Connected { get; private set; }
        public bool Ready { get; private set; }
        public bool Connecting { get; private set; }
        public string NetworkError { get; private set; }

        public void Start()
        {
            int port = 52202;
            while (true)
            {
                var error = Network.InitializeServer(5, port, false);
                if (error == NetworkConnectionError.NoError)
                    break;

                ++port;
            }
        }

        public void OnServerInitialized()
        {
            Ready = true;
        }

        public string GetConnectionInfo()
        {
            if (!Ready)
                Debug.LogError("Don't call GetConnectionInfo until Ready is true");

            return Network.player.ipAddress + ":" + Network.player.port;
        }

        public string GetBadConnectionInfo()
        {
            return "127.0.0.1:52201";
        }

        public string Listen()
        {
            return null;
        }

        public void OnPlayerConnected(NetworkPlayer player)
        {
            Connected = true;
        }

        public bool Connect(string connectionInfo)
        {
            var addressAndPort = connectionInfo.Split(':');
            var address = addressAndPort[0];
            var port = int.Parse(addressAndPort[1]);
            
            var error = Network.Connect(address, port);
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
            Connecting = false;
            Connected = true;
        }

        public void OnFailedToConnect(NetworkConnectionError error)
        {
            Debug.Log("OnFailedToConnect");
            NetworkError = error.ToString();
            Connecting = false;
        }
    }
}