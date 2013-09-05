/**
 * Created by IntelliJ IDEA.
 * User: gfoot
 * Date: 16/05/11
 * Time: 12:25
 * To change this template use File | Settings | File Templates.
 */

var count = 0;

var baseUrl = "http://localhost:9998";
$.support.cors = true;

function init()
{
    $("body").append($("<div id='header'/>").addClass("header"));
    $("body").append($("<div id='clients'/>").addClass("clients"));

    var newClientButton = $("<a href='javascript:'>New client</a>").addClass("headerButton");
    newClientButton.click(add_client);
    $("#header").append(newClientButton);

    add_client();
}

function add_client()
{
    count += 1;

    var client = new Client("Client " + count);

    $("#clients").append(client.view);
}

function zero_pad(number)
{
    var digits = 2;
    var fullString = "0000" + number.toString();
    return fullString.substr(-digits);
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

function Client(name)
{
    if (!(this instanceof Client))
        return new Client(name);

    var mStatus = "NotConnected";
    var mClientID = null;
    var mSessionID = null;

    this.ctor = function()
    {
        this.name = name;

        this.view = $("#client_template").clone()
                .attr("id", name)
                .fadeIn("slow");

        $(".closeButton", this.view).click(
                delegate(this, this.dtor));

        $(".connectButton", this.view).click(
                delegate(this, this.Connect));

        $(".disconnectButton", this.view).click(
                delegate(this, this.Disconnect));

        $(".clientTitleText", this.view).html(name);

        this.UpdateView();

        this.ScheduleUpdate();
    }

    this.dtor = function()
    {
        this.Disconnect();
        this.view.remove();
    }

    this.Connect = function()
    {
        mStatus = "Connecting";
        this.UpdateView();

        var client = this;

        $.ajax({
            url: baseUrl + "/clients",
            type: "POST",
            dataType: "json",
            success: function(result)
            {
                mStatus = result["state"];
                mClientID = result["@id"];
                client.UpdateView();
            },
            error: function()
            {
                mStatus = "ConnectionFailed";
                client.UpdateView();
            }
        });
    }

    this.Disconnect = function()
    {
        if (mClientID != null)
        {
            $.ajax({
                url: baseUrl + "/clients/" + mClientID,
                type: "DELETE"
            });
        }

        mStatus = "NotConnected";
        mClientID = null;
        this.UpdateView();
    }

    this.UpdateView = function()
    {
        $(".stateText", this.view).text(mStatus);
        $(".clientID", this.view).text(mClientID);
        $(".sessionID", this.view).text(mSessionID);
    }

    this.ScheduleUpdate = function()
    {
        setTimeout(
                delegate(this, this.Update),
                2000);
    }

    this.Update = function()
    {
        var updateDate = new Date();

        var updateDateText =
                zero_pad(updateDate.getHours()) + ":" +
                        zero_pad(updateDate.getMinutes()) + ":" +
                        zero_pad(updateDate.getSeconds());

        var element = $(".updateTime", this.view);
        element.text(updateDateText);

        element.css({"color": "#ff0000"});

        if (mStatus != "Matching")
        {
            this.EndUpdate();
        }
        else
        {
            var client = this;
            $.ajax({
                url: baseUrl + "/matches?client=" + mClientID,
                type: "GET",
                dataType: "json",
                success: function(result)
                {
                    var sessionID = result["@id"];
                    if (sessionID != "0")
                    {
                        mStatus = "Matched";
                        mSessionID = sessionID;
                        console.log(result["clients"]);
                        //$.ajax({url: baseUrl + "/clients/" + mClientID, type: "PUT", ...})
                    }
                },
                error: function(jqXHR, textStatus, errorThrown)
                {
                    if (jqXHR.status != 404)
                        alert("Client " + mClientID + ": poll failed - " + jqXHR.status + " - " + errorThrown);
                },
                complete: function()
                {
                    client.EndUpdate();
                }
            });
        }
    }

    this.EndUpdate = function()
    {
        setTimeout(
                function()
                {
                    var element = $(".updateTime", this.view);
                    element.css({"color": "#000000"});
                },
                200);

        this.UpdateView();

        this.ScheduleUpdate();
    }

    this.ctor();
}

