using System;
using System.Collections;
using UnityEngine;

namespace Assets
{
    /// <summary>
    /// Primary interface for communicating with the matchmaking service.
    /// 
    /// Apply this component to a GameObject and initialize the public fields to control the matchmaking process.
    /// </summary>
    public class Connector : MonoBehaviour
    {
        /// <summary>
        /// The INetworkInterface implementation to use for low level networking with other peers
        /// </summary>
        public INetworkInterface NetworkInterface;

        /// <summary>
        /// The base URL of the matchmaking service
        /// </summary>
        public string BaseUrl;

        /// <summary>
        /// A game name, which is converted into a matchmaking requirement so you only match with other instances of the same game
        /// </summary>
        public string GameName;

        /// <summary>
        /// Called after successful connection
        /// </summary>
        public event Action OnSuccess;

        /// <summary>
        /// Called on giving up, when MaxFailures matchmaking attempts have been made and failed
        /// </summary>
        public event Action OnFailure;

        /// <summary>
        /// Indicates whether a connection has been established
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// String representation of the last network error to occur
        /// TODO: replace with an event so the caller can capture all errors that occur
        /// </summary>
        public string NetworkError { get; private set; }

        /// <summary>
        /// Informational string stating what operation is currently in progress
        /// </summary>
        public string Status { get; private set; }

        /// <summary>
        /// Maximum number of failed matchmaking attempts before giving up
        /// </summary>
        public int MaxFailures = 10;
        
        /// <summary>
        /// Delay after unexpected server errors, before reattempting matchmaking
        /// </summary>
        public int UnexpectedServerErrorRetryDelay = 5;

        /// <summary>
        /// Delay after failing to connect to a host, before retrying the connection
        /// </summary>
        public int ConnectToHostFailRetryDelay = 1;

        /// <summary>
        /// Amount of time a host will wait for a client to connect before giving up and requesting a new match
        /// </summary>
        public float HostWaitForClientTimeout = 10;

        /// <summary>
        /// For debug use only - simulate NAT negotiation errors.  Peers can only communicate if they share some bits in common.
        /// </summary>
        public int DebugConnectivityBits = 1;


        private static JsonObject RequireAttribute(string attribute, params string[] values)
        {
            var valuesArray = new JsonArray();
            foreach (var value in values)
                valuesArray.Add(value);
         
            var result = new JsonObject();
            result.Set("@type", "requireAttribute");
            result.Set("attribute", attribute);
            result.Set("values", valuesArray);
            return result;
        }

        private static JsonObject RequireNotUuid(string uuid)
        {
            var result = new JsonObject();
            result.Set("@type", "requireNotUuid");
            result.Set("uuid", uuid);
            return result;
        }

        public IEnumerator Start()
        {
            Status = "registering";

            var requirements = new JsonArray();
            requirements.Add(RequireAttribute("gameName", GameName));

            var postData = new JsonObject();
            postData.Set("uuid", Guid.NewGuid().ToString());
            postData.Set("connectionInfo", NetworkInterface.GetConnectionInfo() + string.Format("!{0}", DebugConnectivityBits));
            postData.Set("requirements", requirements);

            var headers = new Hashtable();
            headers["Content-Type"] = "application/json";
            var www = new WWW(BaseUrl + "/clients", postData.ToByteArray(), headers);
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("WWW error: " + www.error);
                Status = "registration failed";
                yield break;
            }

            if (www.responseHeaders["CONTENT-TYPE"] != "application/json")
            {
                Debug.LogError("Bad content type received: " + www.responseHeaders["CONTENT-TYPE"]);
                Status = "registration failed";
                yield break;
            }

            var clientData = new JsonObject(www.text);

            // "failures" counts the number of times we hit error cases from the server, so we can retry on errors but still give up if it's really broken.
            // It doesn't necessarily increase each time through the loop.
            var failures = 0;

            while (failures < MaxFailures)
            {
                NetworkError = null;
                Status = "waiting for match";
                while (true)
                {
                    www = new WWW(BaseUrl + string.Format("/matches?client={0}", clientData.GetInteger("id")));
                    yield return www;

                    if (www.error == null)
                        break;

                    if (www.error.StartsWith("404"))
                    {
                        Debug.Log("No matches yet");
                        Status = "still waiting for match";
                        continue;
                    }

                    Debug.LogError("WWW error: " + www.error);
                    yield break;
                }
                if (www.error != null)
                {
                    Status = "wait-for-match failure, trying again in a while";
                    ++failures;
                    yield return new WaitForSeconds(UnexpectedServerErrorRetryDelay);
                    continue;
                }

                Status = "fetching match data";
                var sessionId = new JsonObject(www.text).GetInteger("id");

                www = new WWW(BaseUrl + string.Format("/matches/{0}", sessionId));
                yield return www;

                if (www.error != null)
                {
                    Status = "failed to fetch match data, trying again in a while";
                    Debug.LogError("WWW error: " + www.error);
                    ++failures;
                    yield return new WaitForSeconds(UnexpectedServerErrorRetryDelay);
                    continue;
                }

                var clients = new JsonObject(www.text).GetArray("clients");
                var otherClient = clients.GetInteger(0) + clients.GetInteger(1) - clientData.GetInteger("id");

                Status = "fetching other client data";

                www = new WWW(BaseUrl + string.Format("/clients/{0}", otherClient));
                yield return www;

                if (www.error != null)
                {
                    Status = "failed to fetch other client data, trying again in a while";
                    Debug.LogError("WWW error: " + www.error);
                    ++failures;
                    yield return new WaitForSeconds(UnexpectedServerErrorRetryDelay);
                    continue;
                }

                var otherClientData = new JsonObject(www.text);

                var isHost = clients.GetInteger(0) == clientData.GetInteger("id");
                if (isHost)
                {
                    Status = "hosting - waiting for other client to join";
                    NetworkError = null;

                    var startTime = Time.realtimeSinceStartup;

                    NetworkInterface.StartListening(otherClientData.GetString("uuid"));

                    while (!NetworkInterface.Connected)
                    {
                        if (Time.realtimeSinceStartup - startTime > HostWaitForClientTimeout)
                        {
                            NetworkInterface.StopListening();
                            Status = "Timeout waiting for client to connect";
                            yield return new WaitForSeconds(1);
                            break;
                        }
                        yield return null;
                    }
                }
                else
                {
                    Status = "connecting to host";
                    var attempts = 0;
                    while (!NetworkInterface.Connected)
                    {
                        var splitConnectionInfo = otherClientData.GetString("connectionInfo").Split('!');
                        var connectionInfo = splitConnectionInfo[0];

                        var connectivityBits = int.Parse(splitConnectionInfo[1]);
                        if ((connectivityBits & DebugConnectivityBits) == 0)
                            connectionInfo = NetworkInterface.GetBadConnectionInfo();

                        NetworkError = null;
                        if (NetworkInterface.StartConnecting(connectionInfo, clientData.GetString("uuid")))
                        {
                            while (NetworkInterface.Connecting)
                                yield return null;
                        }

                        if (NetworkInterface.Connected)
                            continue;

                        NetworkError = NetworkInterface.NetworkError;
                        Status = "error connecting to host - trying again";
                        Debug.LogError(string.Format("Network connection error: {0}", NetworkInterface.NetworkError));
                        ++attempts;
                        if (attempts >= 3) break;
                        yield return new WaitForSeconds(ConnectToHostFailRetryDelay);
                    }
                }

                if (!NetworkInterface.Connected)
                {
                    Status = "giving up connecting, will find another match";

                    // We failed to connect to the peer, so explicitly ask the server not to match us with the same peer again
                    requirements.Add(RequireNotUuid(otherClientData.GetString("uuid")));
                    postData.Set("requirements", requirements);
                    www = new WWW(BaseUrl + string.Format("/clients/{0}/update", clientData.GetInteger("id")), postData.ToByteArray(), headers);
                    yield return www;

                    if (www.error != null)
                    {
                        Status = "error while updating requirements to exclude this partner";
                        Debug.LogError("WWW error: " + www.error);
                    }

                    ++failures;
                    yield return new WaitForSeconds(1);
                    continue;
                }

                // connected
                Status = "Connected";
                Connected = true;

                break;
            }

            // either connected or given up connecting

            // tidy up
            yield return new WWW(BaseUrl + string.Format("/clients/{0}/delete", clientData.GetInteger("id")), new JsonObject().ToByteArray());

            if (Connected && OnSuccess != null)
                OnSuccess();
            else if (!Connected && OnFailure != null)
                OnFailure();
        }
    }
}