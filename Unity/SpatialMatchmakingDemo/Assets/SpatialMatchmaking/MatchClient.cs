using System;
using System.Collections;
using UnityEngine;

namespace Assets.SpatialMatchmaking
{
    /// <summary>
    /// Primary interface for communicating with the matchmaking service.
    /// 
    /// Apply this component to a GameObject and initialize the public fields to control the matchmaking process.
    /// </summary>
    public class MatchClient : MonoBehaviour
    {
        /// <summary>
        /// The INetworkInterface implementation to use for low level networking with other peers
        /// </summary>
        public INetworkInterface NetworkInterface;

        /// <summary>
        /// The ILocationInterface implementation to use when sending location data to the server
        /// </summary>
        public ILocationInterface LocationInterface;

        /// <summary>
        /// The base URL of the matchmaking service
        /// </summary>
        public string BaseUrl;

        /// <summary>
        /// A game name, which is converted into a matchmaking requirement so you only match with other instances of the same game
        /// </summary>
        public string GameName;

        /// <summary>
        /// Maximum radius for location-based matching
        /// </summary>
        public int MaxMatchRadius;

        /// <summary>
        /// Called after successful connection
        /// </summary>
        public event Action OnSuccess;

        /// <summary>
        /// Called on giving up, when MaxFailures matchmaking attempts have been made and failed
        /// </summary>
        public event Action OnFailure;

        /// <summary>
        /// Called during the connection process to report status changes and errors
        /// </summary>
        public event Action<bool, string> OnLogEvent;

        /// <summary>
        /// Indicates whether a connection has been established
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// Maximum number of failed matchmaking attempts before giving up
        /// </summary>
        public int MaxFailures = 10;
        
        /// <summary>
        /// Maximum number of seconds to wait for location service initialization
        /// </summary>
        public int LocationInitTimeout = 20;

        /// <summary>
        /// Delay after unexpected server errors, before reattempting matchmaking
        /// </summary>
        public int UnexpectedServerErrorRetryDelay = 5;

        /// <summary>
        /// Delay after failing to connect to a host, before retrying the connection
        /// </summary>
        public int ConnectToHostFailRetryDelay = 1;

        /// <summary>
        /// Duration a host will wait for a client to connect before giving up and requesting a new match
        /// </summary>
        public float HostWaitForClientTimeout = 20;

        /// <summary>
        /// Duration a client will wait while attempting to connect to a host before abandoning the connection
        /// </summary>
        public int ConnectToHostTimeout = 9;


        private void Log(string message)
        {
            if (OnLogEvent != null) OnLogEvent(false, message);
        }
        
        private void LogError(string message)
        {
            if (OnLogEvent != null) OnLogEvent(true, message);
        }

        private static JsonObject RequireAttribute(string attribute, params string[] values)
        {
            return new JsonObject {
                                      { "@type", "requireAttribute" }, 
                                      { "attribute", attribute }, 
                                      { "values", new JsonArray(values) }
                                  };
        }

        private static JsonObject RequireNotUuid(string uuid)
        {
            return new JsonObject {
                                      { "@type", "requireNotUuid" }, 
                                      { "uuid", uuid }
                                  };
        }

        private static JsonObject RequireLocationWithin(int radius)
        {
            return new JsonObject {
                                      { "@type", "requireLocationWithin" },
                                      { "radius", radius }
                                  };
        }

        public IEnumerator Start()
        {
            Log("waiting for location");
            yield return StartCoroutine(LocationInterface.Init(LocationInitTimeout));
            if (!LocationInterface.Ready)
            {
                LogError("Location service failed");
                if (OnFailure != null)
                    OnFailure();
                yield break;
            }

            Log("registering");

            var requirements = new JsonArray
                                   {
                                       RequireAttribute("gameName", GameName),
                                       RequireLocationWithin(MaxMatchRadius)
                                   };

            var location = LocationInterface.Location;

            var postData = new JsonObject {
                { "uuid", Guid.NewGuid().ToString() },
                { "connectionInfo", NetworkInterface.GetConnectionInfo() },
                { "location", new JsonObject {
                    {"longitude", location.Longitude},
                    {"latitude", location.Latitude},
                }},
                { "requirements", requirements },
            };

            var headers = new Hashtable();
            headers["Content-Type"] = "application/json";
            var www = new WWW(BaseUrl + "/clients", postData.ToByteArray(), headers);
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("WWW error: " + www.error);
                LogError("registration failed");
                yield break;
            }

            if (www.responseHeaders["CONTENT-TYPE"] != "application/json")
            {
                Debug.LogError("Bad content type received: " + www.responseHeaders["CONTENT-TYPE"]);
                LogError("registration failed");
                yield break;
            }

            var clientData = new JsonObject(www.text);

            // "failures" counts the number of times we hit error cases from the server, so we can retry on errors but still give up if it's really broken.
            // It doesn't necessarily increase each time through the loop.
            var failures = 0;

            while (failures < MaxFailures)
            {
                Log("waiting for match");
                while (true)
                {
                    www = new WWW(BaseUrl + string.Format("/matches?client={0}", clientData.GetInteger("id")));
                    yield return www;

                    if (www.error == null)
                        break;

                    if (www.error.StartsWith("404"))
                    {
                        Log("still waiting for match");
                        continue;
                    }

                    LogError("WWW error: " + www.error);
                    yield break;
                }
                if (www.error != null)
                {
                    Log("wait-for-match failure, trying again in a while");
                    ++failures;
                    yield return new WaitForSeconds(UnexpectedServerErrorRetryDelay);
                    continue;
                }

                Log("fetching match data");
                var sessionId = new JsonObject(www.text).GetInteger("id");

                www = new WWW(BaseUrl + string.Format("/matches/{0}", sessionId));
                yield return www;

                if (www.error != null)
                {
                    LogError("WWW error: " + www.error);
                    Log("failed to fetch match data, trying again in a while");
                    ++failures;
                    yield return new WaitForSeconds(UnexpectedServerErrorRetryDelay);
                    continue;
                }

                var clients = new JsonObject(www.text).GetArray("clients");
                var otherClient = clients.GetInteger(0) + clients.GetInteger(1) - clientData.GetInteger("id");

                Log("fetching other client data");

                www = new WWW(BaseUrl + string.Format("/clients/{0}", otherClient));
                yield return www;

                if (www.error != null)
                {
                    LogError("WWW error: " + www.error);
                    Log("failed to fetch other client data, trying again in a while");
                    ++failures;
                    yield return new WaitForSeconds(UnexpectedServerErrorRetryDelay);
                    continue;
                }

                var otherClientData = new JsonObject(www.text);

                var isHost = clients.GetInteger(0) == clientData.GetInteger("id");
                if (isHost)
                {
                    Log("hosting - waiting for other client to join");

                    var startTime = Time.realtimeSinceStartup;

                    if (NetworkInterface.StartListening(otherClientData.GetString("uuid")))
                    {
                        while (!NetworkInterface.Connected)
                        {
                            if (Time.realtimeSinceStartup - startTime > HostWaitForClientTimeout)
                            {
                                NetworkInterface.StopListening();
                                Log("Timeout waiting for client to connect");
                                yield return new WaitForSeconds(1);
                                break;
                            }
                            yield return null;
                        }
                    }
                    else
                    {
                        LogError(NetworkInterface.NetworkError);
                        Log("failed to initialize as host");
                        yield return new WaitForSeconds(1);
                    }
                }
                else
                {
                    // This really shouldn't be here.  We probably need a way for the host to not fill in the connectionInfo until 
                    // it is ready to accept connections, and this delay should be replaced by the client polling for that info to 
                    // become available.  (The barrier to this is that currently updating client info clears all matches...)
                    Log("waiting for a few seconds to let the host start");
                    yield return new WaitForSeconds(4);

                    var attempts = 0;
                    while (!NetworkInterface.Connected)
                    {
                        Log("connecting to host");

                        var startTime = Time.realtimeSinceStartup;

                        var startConnectingOk = NetworkInterface.StartConnecting(otherClientData.GetString("connectionInfo"), clientData.GetString("uuid"));
                        var timedOut = false;

                        if (startConnectingOk)
                        {
                            while (NetworkInterface.Connecting && !timedOut)
                            {
                                timedOut = Time.realtimeSinceStartup - startTime > ConnectToHostTimeout;
                                yield return null;
                            }
                        }

                        if (NetworkInterface.Connected)
                            continue;

                        if (!startConnectingOk)
                        {
                            LogError(NetworkInterface.NetworkError);
                            Log("error connecting to host - trying again");
                        }
                        else if (timedOut)
                        {
                            Log("timeout connecting to host - trying again");
                        }
                        else
                        {
                            LogError(NetworkInterface.NetworkError);
                            Log("error connecting to host - trying again");
                        }

                        ++attempts;
                        if (attempts >= 3) break;
                        yield return new WaitForSeconds(ConnectToHostFailRetryDelay);
                    }
                }

                if (!NetworkInterface.Connected)
                {
                    Log("giving up connecting, will find another match");

                    // We failed to connect to the peer, so explicitly ask the server not to match us with the same peer again
                    requirements.Add(RequireNotUuid(otherClientData.GetString("uuid")));
                    postData.Set("requirements", requirements);
                    www = new WWW(BaseUrl + string.Format("/clients/{0}/update", clientData.GetInteger("id")), postData.ToByteArray(), headers);
                    yield return www;

                    if (www.error != null)
                    {
                        LogError("WWW error: " + www.error);
                        Log("error while updating requirements to exclude this partner");
                    }

                    ++failures;
                    yield return new WaitForSeconds(1);
                    continue;
                }

                // connected
                Log("Connected");
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