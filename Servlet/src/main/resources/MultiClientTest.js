
var count = 0;

var baseUrl = "http://localhost:9998";
$.support.cors = true;

var clients = {};

function init()
{
    var newClientButton = $("#newClientButton");//<a href='javascript:'>New client</a>").addClass("headerButton");
    newClientButton.click(add_client);

    update_status();

    add_client();
}

function update_status()
{
    function scheduleRefresh()
    {
        setTimeout(
            update_status,
            2000);
    }

    $.ajax({
        url: baseUrl + "/state",
        type: "GET",
        dataType: "json",
        success: function(result)
        {
            $("#status").text("C: " + result["numClients"] + " (UM: " + result["numUnmatchedClients"] + ", PD: " + result["numClientsPendingDelete"] + "), M: " + result["numMatches"]);
            scheduleRefresh();
        },
        error: function(jqXHR, textStatus, errorThrown)
        {
            console.log("Status update failed - " + jqXHR.status + " - " + errorThrown);
            $("#status").text("Status query failed");
            scheduleRefresh();
        }
    });
}

function add_client()
{
    count += 1;

    var client = new Client("Client " + count);
    clients[client.name] = client;
    $("#clients").append(client.view);
}

function delegate(obj, method)
{
    return function()
    {
        if (typeof(method) == 'string')
            method = obj[method];
        method.apply(obj);
    };
}

/**
 * @return {string}
 */
function UUID()
{
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        var r = Math.random()*16|0, v = c == 'x' ? r : (r&0x3|0x8);
        return v.toString(16);
    });
}

function Client(name)
{
    if (!(this instanceof Client))
        return new Client(name);

    var mClientID = null;
    var mSessionID = null;
    var mOtherClientInfo = null;
    var mConnecting = false;
    var mConnected = false;

    var mErrors = [];

    this.constructor = function()
    {
        this.name = name;

        this.info = {}
        this.info.uuid = UUID();
        this.info.requirements = [];
        this.info.connectionInfo = name;

        this.inbox = [];

        this.view = $("#client_template").clone()
                .attr("id", name)
                .fadeIn("slow");

        $(".closeButton", this.view).click(
                delegate(this, this.destructor));

        $(".clientTitleText", this.view).html(name);

        this.UpdateView();
    }

    this.destructor = function()
    {
        this.Unregister();
        this.view.remove();
    }

    var mDoLaterMethod = null;
    this.DoLater = function(method, time)
    {
        this.ClearDoLater();

        mDoLaterMethod = setTimeout(delegate(this, method), time);
    }

    this.ClearDoLater = function()
    {
        if (mDoLaterMethod != null)
            clearTimeout(mDoLaterMethod);
    }

    this.Register = function()
    {
        this.ClearDoLater();

        var client = this;

        $.ajax({
            url: baseUrl + "/clients",
            type: "POST",
            dataType: "json",
            contentType: 'application/json',
            data: JSON.stringify(this.info),
            success: function(result)
            {
                mClientID = result["id"];
                client.UpdateView();
                client.GetMatch();
            },
            error: function(jqXHR, textStatus, errorThrown)
            {
                alert(name + ": registration failed - " + jqXHR.status + " - " + errorThrown);
                mErrors.add("Registration failed");
                client.UpdateView();
            }
        });
    }

    this.Unregister = function()
    {
        if (mClientID != null)
        {
            $.ajax({
                url: baseUrl + "/clients/" + mClientID,
                type: "DELETE"
            });
        }

        mClientID = null;
        mSessionID = null;
        this.UpdateView();
    }

    this.GetMatch = function()
    {
        if (mClientID == null)
        return;

        var client = this;
        $.ajax({
            url: baseUrl + "/matches?client=" + mClientID,
            type: "GET",
            dataType: "json",
            success: function (result) {
                var sessionID = result["id"];
                if (sessionID != "0") {
                    mSessionID = sessionID;
                    client.UpdateView();
                    client.GetMatchInfo();
                }
                else
                {
                    alert(name + ": client received zero match ID");
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                if (jqXHR.status == 404)
                {
                    // Try again
                    client.DoLater(client.GetMatch, 500);
                }
                else if (jqXHR.status == 400)
                {
                    console.log(name + ": poll failed, bad client ID (" + mClientID + ")? - " + jqXHR.status + " - " + errorThrown + " - " + textStatus);

                    // Reregister
                    mClientID = null;
                    client.UpdateView();
                }
                else
                {
                    alert(name + ": poll failed for client ID " + mClientID + " - " + jqXHR.status + " - " + errorThrown + " - " + textStatus);

                    // Try again
                    client.DoLater(client.GetMatch, 500);
                }
            }
        });
    }

    this.GetMatchInfo = function()
    {
        var client = this;
        $.ajax({
            url: baseUrl + "/matches/" + mSessionID,
            type: "GET",
            dataType: "json",
            success: function(result)
            {
                var clients = result["clients"];
                client.GetOtherClientInfo(clients[0] + clients[1] - mClientID);
            },
            error: function(jqXHR, textStatus, errorThrown)
            {
                alert(name + ": match query failed for match " + mSessionID + " - " +
                    jqXHR.status + " - " + errorThrown);

                // Go back to waiting for matches
                mSessionID = null;
                client.UpdateView();
                client.GetMatch();
            }
        });
    }

    this.GetOtherClientInfo = function(otherClientId)
    {
        var client = this;
        $.ajax({
            url: baseUrl + "/clients/" + otherClientId,
            type: "GET",
            dataType: "json",
            success: function(result)
            {
                mOtherClientInfo = result;
                console.log(mOtherClientInfo);
                client.UpdateView();
                client.Matched();
            },
            error: function(jqXHR, textStatus, errorThrown)
            {
                if (jqXHR.status == 404)
                {
                    console.log(name + ": other client lost");
                }
                else
                {
                    alert(name + ": other client " + otherClientId + " query failed - " +
                        jqXHR.status + " - " + errorThrown);
                }

                // Go back to waiting for matches
                mSessionID = null;
                client.UpdateView();
                client.GetMatch();
            }
        });
    }

    this.Matched = function()
    {
        if (!mConnecting)
        {
            // Clear out the inbox - we're not ready to connect yet
            while (this.inbox.pop()) {}
        }
        else
        {
            this.PollConnect();
            if (mConnected)
                return;
        }

        var client = this;
        var otherClientId = mOtherClientInfo["id"];
        // Check the match record still exists
        $.ajax({
            url: baseUrl + "/matches/" + mSessionID,
            type: "GET",
            dataType: "json",
            success: function(result) {
                // Check the other client record still exists
                $.ajax({
                    url: baseUrl + "/clients/" + otherClientId,
                    type: "GET",
                    dataType: "json",
                    success: function(result)
                    {
                        // repeat
                        client.DoLater(client.Matched, 500);
                    },
                    error: function(jqXHR, textStatus, errorThrown)
                    {
                        if (jqXHR.status == 404)
                        {
                            console.log(name + ": other client lost");
                        }
                        else
                        {
                            alert(name + ": other client " + otherClientId + " query failed - " +
                                jqXHR.status + " - " + errorThrown);
                        }

                        // Go back to waiting for matches
                        mSessionID = null;
                        client.UpdateView();
                        client.GetMatch();
                    }
                })
            },
            error: function(jqXHR, textStatus, errorThrown)
            {
                if (jqXHR.status == 404)
                {
                    console.log(name + ": match lost");
                }
                else
                {
                    alert(name + ": match " + mSessionID + " query failed - " +
                        jqXHR.status + " - " + errorThrown);
                }

                // Go back to waiting for matches
                mSessionID = null;
                client.UpdateView();
                client.GetMatch();
            }
        });
    }

    this.RejectMatch = function()
    {
        this.ClearDoLater();

        this.info.requirements.push({
            "@type": "requireNotUuid",
            "uuid": mOtherClientInfo["uuid"]
        });

        var client = this;
        $.ajax({
            url: baseUrl + "/clients/" + mClientID,
            type: "PUT",
            dataType: "json",
            contentType: "application/json",
            data: JSON.stringify(this.info),
            success: function (result) {
                mSessionID = null;
                client.UpdateView();
                client.GetMatch();
            }
        })
    }

    this.Send = function(otherClientName, message)
    {
        var otherClient = clients[otherClientName];
        if (otherClient == null)
        {
            console.log(name + ": Send: client " + otherClientName + " not found");
            return false;
        }

        otherClient.inbox.push([name, message]);

        return true;
    }

    this.Connect = function()
    {
        this.Send(mOtherClientInfo.connectionInfo, "ping");

        mConnecting = true;
        this.UpdateView();
    }

    this.PollConnect = function()
    {
        var inboxItem;
        while (inboxItem = this.inbox.pop())
        {
            var otherClient = inboxItem[0];
            var message = inboxItem[1];
            if (otherClient == mOtherClientInfo.connectionInfo)
            {
                if (message == "ping")
                {
                    this.Send(mOtherClientInfo.connectionInfo, "pong");
                    this.Connected();
                    return;
                }
                else if (message == "pong")
                {
                    this.Connected();
                    return;
                }
                else
                {
                    console.log(name + ": PollConnect: unrecognized message: " + message);
                }
            }
        }
    }

    this.Connected = function()
    {
        mConnecting = false;
        mConnected = true;
        this.UpdateView();

        $.ajax({
            url: baseUrl + "/clients/" + mClientID,
            type: "DELETE"
        });
    }

    this.CancelConnect = function()
    {
        if (mConnecting && !mConnected)
        {
            mConnecting = false;
            this.UpdateView();
        }
    }

    this.UpdateView = function()
    {
        var clientBody = $("#clientBody", this.view)

        function field(label, value)
        {
            var indent = 0;
            while (label.substring(0, 4) == "    ")
            {
                ++indent;
                label = label.substring(4);
            }
            var element = $("#field", "#clientBodyTemplates").clone();
            element.find(".label").text(label);
            element.find(".value").text(value);
            if (indent > 0)
            {
                element.css({"padding-left": indent*20 + "px"});
            }
            clientBody.append(element);
        }

        function button(label, action) {
            var element = $("#button", "#clientBodyTemplates").clone();
            element.find(".button").text(label);
            element.click(action);
            clientBody.append(element);
        }

        clientBody.html("");

        var stateText = "Bad state";
        if (mClientID == null)
            stateText = "Unregistered";
        else if (mSessionID == null)
            stateText = "Matching";
        else if (!mConnecting && !mConnected)
            stateText = "Matched";
        else if (!mConnected)
            stateText = "Connecting";
        else
            stateText = "Connected";

        field("State", stateText);

        field("UUID", this.info.uuid);

        field("Location", "?");
        field("Requirements", this.info.requirements.length);

        for (i = 0; i < this.info.requirements.length; i++)
        {
            var requirement = this.info.requirements[i];
            var type = requirement["@type"];
            var reqString = "";
            if (type == "requireNotUuid")
                reqString = requirement["uuid"].substring(0, 8) + "...";
            field("    " + type, reqString);
        }

        if (mClientID == null)
        {
            var label = button("Register", delegate(this, this.Register));
        }
        else
        {
            field("Client ID", mClientID);

            if (mSessionID == null)
            {

            }
            else
            {
                field("Session ID", mSessionID);
                if (!mConnected)
                {
                    button("Reject match", delegate(this, this.RejectMatch));

                    if (!mConnecting)
                    {
                        button("Connect", delegate(this, this.Connect));
                    }
                    else
                    {
                        button("Cancel Connect", delegate(this, this.CancelConnect));
                    }
                }
            }
        }
    }

    this.constructor();
}

