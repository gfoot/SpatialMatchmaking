using UnityEngine;

namespace Assets
{
    public class UnityNetworkInterface : INetworkInterface
    {
        public UnityNetworkInterface()
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

        public string GetConnectionInfo()
        {
            return Network.player.ipAddress + ":" + Network.player.port;
        }

        public bool PollConnected()
        {
            foreach (var connection in Network.connections)
            {
                Debug.Log(connection.ipAddress);

                // check it's the right person/people?

                return true;
            }

            return false;
        }

        public string Connect(string connectionInfo)
        {
            var addressAndPort = connectionInfo.Split(':');
            var address = addressAndPort[0];
            var port = int.Parse(addressAndPort[1]);
            
            var error = Network.Connect(address, port);
            if (error != NetworkConnectionError.NoError)
                return error.ToString();
            
            return null;
        }
    }
}