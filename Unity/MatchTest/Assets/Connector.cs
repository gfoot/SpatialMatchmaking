using System;
using System.Collections;
using UnityEngine;

namespace Assets
{
    public class Connector
    {
        private readonly INetworkInterface _networkInterface;
        public bool Connected { get; private set; }

        public Guid Uuid { get; private set; }
        public JsonObject ClientData { get; private set; }
        public JsonObject SessionData { get; private set; }
        public JsonObject OtherClientData { get; private set; }
        public string NetworkError { get; private set; }

        public Connector(INetworkInterface networkInterface)
        {
            _networkInterface = networkInterface;
         
            Uuid = Guid.NewGuid();
        }

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

        public IEnumerator Register(string baseUrl, string gameName)
        {
            var requirements = new JsonArray();
            requirements.Add(RequireAttribute("gameName", gameName));

            var postData = new JsonObject();
            postData.Set("uuid", Uuid.ToString());
            postData.Set("connectionInfo", _networkInterface.GetConnectionInfo());
            postData.Set("requirements", requirements);

            var headers = new Hashtable();
            headers["Content-Type"] = "application/json";
            var www = new WWW(baseUrl + "/clients", postData.ToByteArray(), headers);
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("WWW error: " + www.error);
                yield break;
            }

            if (www.responseHeaders["CONTENT-TYPE"] != "application/json")
            {
                Debug.LogError("Bad content type received: " + www.responseHeaders["CONTENT-TYPE"]);
                yield break;
            }

            ClientData = new JsonObject(www.text);

            while (true)
            {
                www = new WWW(baseUrl + string.Format("/matches?client={0}", ClientData.GetInteger("id")));
                yield return www;

                if (www.error == null)
                    break;

                if (www.error.StartsWith("404"))
                {
                    Debug.Log("No matches yet");
                    continue;
                }

                Debug.LogError("WWW error: " + www.error);
                yield break;
            }

            var sessionId = new JsonObject(www.text).GetInteger("id");

            www = new WWW(baseUrl + string.Format("/matches/{0}", sessionId));
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("WWW error: " + www.error);
                yield break;
            }

            SessionData = new JsonObject(www.text);
            var clients = SessionData.GetArray("clients");
            var otherClient = clients.GetInteger(0) + clients.GetInteger(1) - ClientData.GetInteger("id");

            www = new WWW(baseUrl + string.Format("/clients/{0}", otherClient));
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("WWW error: " + www.error);
                yield break;
            }

            OtherClientData = new JsonObject(www.text);

            var isHost = (SessionData.GetArray("clients").GetInteger(0) - ClientData.GetInteger("id")) == 0;
            while (!Connected)
            {
                if (isHost)
                {
                    Connected = _networkInterface.PollConnected();
                }
                else
                {
                    NetworkError = _networkInterface.Connect(OtherClientData.GetString("connectionInfo"));

                    if (NetworkError == null)
                        Connected = true;
                    else
                        Debug.LogError(string.Format("Network connection error: {0}", NetworkError));
                }

                yield return null;
            }

            // connected...
        }
    }
}