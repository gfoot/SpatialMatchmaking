package com.studiogobo.fi.Matcher.Servlet;

import com.studiogobo.fi.Matcher.Model.ClientRecord;
import com.studiogobo.fi.Matcher.Model.MatchRecord;

import javax.ws.rs.*;
import javax.ws.rs.core.Context;
import javax.ws.rs.core.Response;
import javax.xml.bind.JAXBContext;
import javax.xml.bind.JAXBException;
import javax.xml.bind.Marshaller;
import java.net.URI;

@Path("/")
public class Servlet
{
    private void Log(String message)
    {
        System.out.println("SRV: " + message);
    }

    public void preDestroy() throws InterruptedException
    {
        jobQueue.Quit();
    }

    @GET
    @Path("clients/{id}")
    @Produces("application/json")
    public ClientRecord getClient(@PathParam("id") int id)
    {
        Log("GET " + id);
        ServletClientRecord record = getServletClientRecord(id);

        Log("    OK");
        return record.clientRecord;
    }

    @POST
    @Path("clients")
    @Produces("application/json")
    public Response createClient() throws InterruptedException
    {
        final int id = clientData.getNewId();
        ClientRecord record = new ClientRecord(id);
        clientData.put(id, new ServletClientRecord(record));

        jobQueue.Enqueue(new Runnable() {
            public void run()
            {
                matchmaker.NewClient(id);
            }
        });

        Log("POST => " + id);

        return Response.created(URI.create("" + id)).entity(record).build();
    }

    @DELETE
    @Path("clients/{id}")
    public Response deleteClient(@PathParam("id") int id)
    {
        ServletClientRecord client = getServletClientRecord(id);
        client.deleted = true;

        if (client.match_id == 0)
        {
            clientData.remove(id);
        }
        else
        {
            MatchRecord match = matchmaker.GetMatchRecord(client.match_id);
            if (match == null)
            {
                clientData.remove(id);
            }
            else
            {
                boolean okToDelete = true;
                for (int clientId : match.clients)
                {
                    ServletClientRecord otherClient = clientData.get(clientId);
                    if (otherClient != null)
                    {
                        if (!otherClient.deleted)
                            okToDelete = false;
                    }
                }

                if (okToDelete)
                {
                    for (int clientId : match.clients)
                    {
                        clientData.remove(clientId);
                    }
                    matchmaker.RemoveMatchRecord(client.match_id);
                }
            }
        }

        Log("DELETE " + id + " => accepted");
        return Response.status(202).build();
    }

    @GET
    @Path("matches")
    @Produces("application/json")
    public Response seekMatch(@QueryParam("client") int client_id, @Context javax.ws.rs.core.UriInfo uriInfo)
    {
        Log("    path " + uriInfo.getAbsolutePath());
        Log("match query for " + client_id);

        ServletClientRecord client = getServletClientRecord(client_id);

        Log("    match id " + client.match_id);

        if (client.match_id == 0)
        {
            throw new WebApplicationException(Response.Status.NOT_FOUND);
        }


        URI uri = uriInfo.getAbsolutePathBuilder().path("" + client.match_id).build();
        Log("    returning redirect to " + uri.toString());
        Response response = Response.seeOther(uri).entity(client.clientRecord).build();
        return response;
    }

    @GET
    @Path("matches/{id}")
    @Produces("application/json")
    public MatchRecord getMatch(@PathParam("id") int id)
    {
        MatchRecord record = matchmaker.GetMatchRecord(id);
        if (record == null)
        {
            Log("GET " + id + " => not found");

            throw new WebApplicationException(Response.Status.NOT_FOUND);
        }

        Log("GET " + id + " => " + record.clients);

        return record;
    }

    @GET
    @Path("state")
    @Produces("application/json")
    public String getState()
    {
        int numClients = 0;
        int numClientsPendingDelete = 0;
        int numUnmatchedClients = 0;
        int numMatches = matchmaker.NumMatches();

        for (ServletClientRecord client : clientData.values())
        {
            ++numClients;
            if (client.match_id == 0)
                ++numUnmatchedClients;
            if (client.deleted)
                ++numClientsPendingDelete;
        }

        String result = "";
        result += "{\n";
        result += "    \"numClients\": \"" + numClients + "\",\n";
        result += "    \"numClientsPendingDelete\": \"" + numClientsPendingDelete + "\",\n";
        result += "    \"numUnmatchedClients\": \"" + numUnmatchedClients + "\",\n";
        result += "    \"numMatches\": \"" + numMatches + "\"\n";
        result += "}\n";

        return result;
    }

    private ServletClientRecord getServletClientRecord(int client_id) {
        ServletClientRecord client = clientData.get(client_id);

        if (client == null)
        {
            Log("    client " + client_id + " not found");

            throw new WebApplicationException(Response.Status.BAD_REQUEST);
        }
        return client;
    }

    private PerishableCollection<ServletClientRecord> clientData = new PerishableCollection<ServletClientRecord>(5000);

    private JobQueue jobQueue = new JobQueue(10);

    private Matchmaker matchmaker = new Matchmaker(clientData);
}
