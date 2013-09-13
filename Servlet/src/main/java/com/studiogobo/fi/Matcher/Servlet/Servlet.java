package com.studiogobo.fi.Matcher.Servlet;

import com.studiogobo.fi.Matcher.Model.ClientRecord;
import com.studiogobo.fi.Matcher.Model.MatchRecord;
import com.studiogobo.fi.Matcher.Model.Requirements.Requirement;
import org.codehaus.jackson.map.ObjectMapper;
import org.codehaus.jackson.map.type.CollectionType;
import org.codehaus.jackson.map.type.TypeFactory;
import org.codehaus.jettison.json.JSONException;
import org.codehaus.jettison.json.JSONObject;

import javax.ws.rs.*;
import javax.ws.rs.core.Context;
import javax.ws.rs.core.Response;
import java.io.IOException;
import java.net.URI;
import java.util.Arrays;
import java.util.Iterator;
import java.util.UUID;
import java.util.Vector;
import java.util.concurrent.TimeUnit;

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
    @Consumes("application/json")
    public Response createClient(JSONObject data) throws IOException, JSONException, InterruptedException {
        final int id = clientData.getNewId();
        ClientRecord record = new ClientRecord(id);

        updateClient(record, data);

        clientData.put(id, new ServletClientRecord(record));

        jobQueue.Enqueue(new Runnable() {
            public void run()
            {
                matchmaker.UpdateClient(id);
            }
        });

        Log("POST => " + id);

        return Response.created(URI.create("" + id)).entity(record).build();
    }

    void updateClient(ClientRecord client, JSONObject data) throws JSONException {

        // TODO: lock

        // If we return an error state then we shouldn't modify any data, so we gather up the changes
        // here and only apply them when the request is fully parsed
        UUID newUuid = null;
        Vector<Requirement> newRequirements = null;
        String newConnectionInfo = null;

        Iterator it = data.keys();
        while (it.hasNext())
        {
            String key = (String)it.next();

            if (key.equals("uuid"))
            {
                // The value should be a string representing an UUID
                // It must be unique.

                UUID uuid;

                try {
                    uuid = UUID.fromString(data.getString(key));
                } catch (JSONException e) {
                    Log("    error: " + e.toString());
                    e.printStackTrace();
                    throw new WebApplicationException(Response.Status.BAD_REQUEST);
                }

                boolean ok = true;
                for (ServletClientRecord scr : clientData.values())
                {
                    if (scr.clientRecord != client && scr.clientRecord.uuid.equals(uuid))
                    {
                        ok = false;
                        break;
                    }
                }
                if (!ok)
                    throw new WebApplicationException(Response.Status.CONFLICT);

                newUuid = uuid;
            }
            else if (key.equals("requirements"))
            {
                // The value should be a JSON array of Requirement objects.
                //
                // Each object should contain a "@type" field, e.g. "requireNotUuid",
                // along with whatever other data the specific requirement class
                // accepts.
                ObjectMapper mapper = new ObjectMapper();
                TypeFactory factory = mapper.getTypeFactory();
                CollectionType type = factory.constructCollectionType(Vector.class, Requirement.class);
                try {
                    newRequirements = mapper.readValue(data.getJSONArray(key).toString(), type);
                } catch (IOException e) {
                    Log("    error: " + e.toString());
                    e.printStackTrace();
                    throw new WebApplicationException(Response.Status.BAD_REQUEST);
                } catch (JSONException e) {
                    Log("    error: " + e.toString());
                    e.printStackTrace();
                    throw new WebApplicationException(Response.Status.BAD_REQUEST);
                }
            }
            else if (key.equals("connectionInfo"))
            {
                newConnectionInfo = data.getString(key);
            }
            else
            {
                Log("    unknown field: " + key);
                throw new WebApplicationException(Response.Status.BAD_REQUEST);
            }
        }

        if (newUuid != null)
            client.uuid = newUuid;
        if (newRequirements != null)
            client.requirements = newRequirements;
        if (newConnectionInfo != null)
            client.connectionInfo = newConnectionInfo;

        // TODO: unlock
    }

    @PUT
    @Path("clients/{id}")
    public Response updateClient(@PathParam("id") final int id, JSONObject data) throws IOException, JSONException, InterruptedException {
        ServletClientRecord client = getServletClientRecord(id);

        // update client from request
        updateClient(client.clientRecord, data);

        if (client.match_id != 0)
        {
            final int match_id = client.match_id;
            client.ClearMatch();

            jobQueue.Enqueue(new Runnable() {
                @Override
                public void run() {
                    matchmaker.RemoveMatch(match_id);
                }
            });
        }

        jobQueue.Enqueue(new Runnable() {
            public void run()
            {
                matchmaker.UpdateClient(id);
            }
        });

        return Response.noContent().build();
    }

    @DELETE
    @Path("clients/{id}")
    public Response deleteClient(@PathParam("id") int id) throws InterruptedException {
        final ServletClientRecord client = getServletClientRecord(id);
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

                    jobQueue.Enqueue(new Runnable() {
                        @Override
                        public void run() {
                            matchmaker.RemoveMatch(client.match_id);
                        }
                    });
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
            throws InterruptedException
    {
        Log("    path " + uriInfo.getAbsolutePath());
        Log("match query for " + client_id);

        ServletClientRecord client = getServletClientRecord(client_id);

        if (client.waitUntilMatched.getCount() > 0)
        {
            Log("    waiting for match...");
        }

        boolean awaitResult = client.waitUntilMatched.await(30, TimeUnit.SECONDS);

        if (!awaitResult)
        {
            return Response.status(404).header("Retry-After", "1").build();
        }

        Log("    match id " + client.match_id);

        URI uri = uriInfo.getAbsolutePathBuilder().path("" + client.match_id).build();
        Log("    returning redirect to " + uri.toString());
        return Response.seeOther(uri).entity(client.clientRecord).build();
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

        Log("GET " + id + " => " + Arrays.toString(record.clients));

        return record;
    }

    @GET
    @Path("state")
    @Produces("application/json")
    public JSONObject getState() throws JSONException {
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

        JSONObject result = new JSONObject();
        result.put("numClients", numClients);
        result.put("numClientsPendingDelete", numClientsPendingDelete);
        result.put("numUnmatchedClients", numUnmatchedClients);
        result.put("numMatches", numMatches);
        return result;
    }

    @POST
    @Path("test")
    @Produces("application/json")
    @Consumes("application/json")
    public JSONObject test(JSONObject object) throws JSONException {
        JSONObject result = new JSONObject();
        result.put("hello", object.optString("first"));
        result.put("world", object.optString("second"));
        return result;
    }

    @POST
    @Path("clients/{id}/update")
    public Response updateClient2(@PathParam("id") int id, JSONObject data) throws IOException, JSONException, InterruptedException {
        return updateClient(id, data);
    }

    @POST
    @Path("clients/{id}/delete")
    public Response deleteClient2(@PathParam("id") int id) throws InterruptedException {
        return deleteClient(id);
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
