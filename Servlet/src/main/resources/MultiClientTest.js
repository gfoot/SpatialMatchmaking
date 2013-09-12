
var count = 0;

var baseUrl = "http://localhost:9998";
$.support.cors = true;

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

    var mErrors = [];

    this.constructor = function()
    {
        this.name = name;

        this.info = {}
        this.info.uuid = UUID();
        this.info.requirements = [];

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

    this.Register = function()
    {
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
                alert("Client '" + name + "': registration failed - " + jqXHR.status + " - " + errorThrown);
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
        var client = this;
        $.ajax({
            url: baseUrl + "/matches?client=" + mClientID,
            type: "GET",
            dataType: "json",
            success: function (result) {
                var sessionID = result["id"];
                if (sessionID != "0") {
                    mSessionID = sessionID;
                    client.GetMatchInfo();
                }
                else
                {
                    alert("Client '" + client.name + "' received zero match ID");
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                if (jqXHR.status == 404)
                {
                    // Try again
                    setTimeout(delegate(client, client.GetMatch), 500);
                }
                else
                {
                    alert("Client " + mClientID + ": poll failed - " + jqXHR.status + " - " + errorThrown);
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
                alert("Match query failed for client " + mClientID + " getting match " + mSessionID + " - " +
                    jqXHR.status + " - " + errorThrown);
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
                client.mOtherClientInfo = result;
                client.UpdateView();
            },
            error: function(jqXHR, textStatus, errorThrown)
            {
                alert("Match query failed for client " + mClientID + " getting match " + mSessionID + " - " +
                    jqXHR.status + " - " + errorThrown);
            }
        });
    }

    this.RejectMatch = function()
    {
        this.info.requirements.push({
            "@type": "requireNotUuid",
            "uuid": this.mOtherClientInfo["uuid"]
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

    this.UpdateView = function()
    {
        var clientBody = $("#clientBody", this.view)

        function field(label, value)
        {
            var element = $("#field", "#clientBodyTemplates").clone();
            element.find(".label").text(label);
            element.find(".value").text(value);
            clientBody.append(element);
        }

        function button(label, action) {
            var element = $("#button", "#clientBodyTemplates").clone();
            element.find(".button").text(label);
            element.click(action);
            clientBody.append(element);
        }

        clientBody.html("");

        clientBody.append($("#stateText", "#clientBodyTemplates").clone());
        var stateText = clientBody.find("#stateText").find(".value");
        stateText.text(mClientID == null ? "Unregistered" : mSessionID == null ? "Matching" : "Matched");

        field("UUID", this.info.uuid);

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
                button("Reject match", delegate(this, this.RejectMatch));
            }
        }
    }

    this.constructor();
}

