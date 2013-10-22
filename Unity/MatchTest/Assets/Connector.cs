using System;
using System.Collections;
using UnityEngine;

namespace Assets
{
    public class Connector : MonoBehaviour
    {
        public INetworkInterface NetworkInterface;
        public string BaseUrl;
        public string GameName;

        public bool Connected { get; private set; }
        public string NetworkError { get; private set; }
        public string Status { get; private set; }

        private JsonObject ClientData { get; set; }
        private JsonObject SessionData { get; set; }
        private JsonObject OtherClientData { get; set; }

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
            Status = "waiting for network interface";
            while (!NetworkInterface.Ready)
                yield return null;

            Status = "registering";

            var requirements = new JsonArray();
            requirements.Add(RequireAttribute("gameName", GameName));

            var postData = new JsonObject();
            postData.Set("uuid", Guid.NewGuid().ToString());
            postData.Set("connectionInfo", NetworkInterface.GetConnectionInfo());
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

            ClientData = new JsonObject(www.text);

            // "failures" counts the number of times we hit error cases from the server, so we can retry on errors but still give up if it's really broken.
            // It doesn't necessarily increase each time through the loop.
            var failures = 0;

            while (failures < 10)
            {
                Status = "waiting for match";
                while (true)
                {
                    www = new WWW(BaseUrl + string.Format("/matches?client={0}", ClientData.GetInteger("id")));
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
                    yield return new WaitForSeconds(5);
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
                    yield return new WaitForSeconds(5);
                    continue;
                }

                SessionData = new JsonObject(www.text);
                var clients = SessionData.GetArray("clients");
                var otherClient = clients.GetInteger(0) + clients.GetInteger(1) - ClientData.GetInteger("id");

                Status = "fetching other client data";

                www = new WWW(BaseUrl + string.Format("/clients/{0}", otherClient));
                yield return www;

                if (www.error != null)
                {
                    Status = "failed to fetch other client data, trying again in a while";
                    Debug.LogError("WWW error: " + www.error);
                    ++failures;
                    yield return new WaitForSeconds(5);
                    continue;
                }

                OtherClientData = new JsonObject(www.text);

                var isHost = clients.GetInteger(1) == ClientData.GetInteger("id");
                if (isHost)
                {
                    Status = "hosting - waiting for other client to join";
                    while (!NetworkInterface.Connected)
                        yield return null;

                    Status = "hosting - other client present";
                    Connected = true;
                }
                else
                {
                    Status = "connecting to host";
                    var attempts = 0;
                    while (!Connected)
                    {
                        if (NetworkInterface.Connect(OtherClientData.GetString("connectionInfo")))
                        {
                            while (NetworkInterface.Connecting)
                                yield return null;
                        }

                        if (NetworkInterface.Connected) continue;

                        NetworkError = NetworkInterface.NetworkError;
                        Status = "error connecting to host - trying again in a while";
                        Debug.LogError(string.Format("Network connection error: {0}", NetworkInterface.NetworkError));
                        ++attempts;
                        if (attempts >= 3) break;
                        yield return new WaitForSeconds(1);
                    }
                }

                if (!Connected)
                {
                    Status = "giving up connecting to host, will find another match in a while";

                    // We failed to connect to the peer, so explicitly ask the server not to match us with the same peer again
                    requirements.Add(RequireNotUuid(OtherClientData.GetString("uuid")));
                    postData.Set("requirements", requirements);
                    www = new WWW(BaseUrl + string.Format("/clients/{0}/update", ClientData.GetInteger("id")), postData.ToByteArray(), headers);
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
                break;
            }

            // either connected or given up connecting

            // tidy up
            yield return new WWW(BaseUrl + string.Format("/clients/{0}/delete", ClientData.GetInteger("id")));
        }
    }
}