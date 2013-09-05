package com.studiogobo.fi.Matcher.Servlet;

import com.studiogobo.fi.Matcher.Model.ClientRecord;
import com.studiogobo.fi.Matcher.Model.MatchRecord;

import javax.ws.rs.*;
import javax.ws.rs.core.Context;
import javax.ws.rs.core.Response;
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
        ServletClientRecord record = clientData.get(id);
        if (record == null)
        {
            Log("GET " + id + " => not found");

            throw new WebApplicationException(Response.Status.NOT_FOUND);
        }

        Log("GET " + id + " => " + record.clientRecord.state);

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
        if (clientData.remove(id) == null)
        {
            Log("DELETE " + id + " => not found");

            throw new WebApplicationException(Response.Status.NOT_FOUND);
        }

        Log("DELETE " + id + " => ok");

        return Response.ok().build();
    }

    @GET
    @Path("matches")
    @Produces("application/json")
    public Response seekMatch(@QueryParam("client") int client_id, @Context javax.ws.rs.core.UriInfo uriInfo)
    {
        Log("    path " + uriInfo.getAbsolutePath());
        Log("match query for " + client_id);

        ServletClientRecord client = clientData.get(client_id);

        if (client == null)
        {
            Log("    client " + client_id + " not found");

            throw new WebApplicationException(Response.Status.BAD_REQUEST);
        }

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

    private PerishableCollection<ServletClientRecord> clientData = new PerishableCollection<ServletClientRecord>(5000);

    private JobQueue jobQueue = new JobQueue(10);

    private Matchmaker matchmaker = new Matchmaker(clientData);
}
