package com.studiogobo.fi.spatialmatchmaking.servlet;

import com.studiogobo.fi.spatialmatchmaking.model.*;

import java.util.ArrayList;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;

public class Matchmaker
{
    private void Log(String message)
    {
        System.out.println(" MM: " + message);
    }

    public void UpdateClient(int id)
    {
        Log("UpdateClient(" + id + ")");

        ServletClientRecord primaryClientRecord = clientData.get(id);
        if (primaryClientRecord == null)
        {
            Log("client id " + id + " not found");
            return;
        }

        if (primaryClientRecord.match_id != 0)
        {
            Log("client id " + id + " already matched");
            return;
        }

        if (!primaryClientRecord.active)
            return;

        final int wantedClients = 2;

        // Start with a list containing only the client we're trying to match
        ArrayList<ServletClientRecord> foundClients = new ArrayList<ServletClientRecord>();
        foundClients.add(primaryClientRecord);

        // Find compatible clients and add them to the list
        for (ServletClientRecord record : clientData.values())
        {
            // Expire clients who have been idle for a long time
            if (record.AgeMillis() > 60000)
            {
                DeleteClient(record.clientRecord.id);
                continue;
            }

            // Ignore clients already in sessions
            if (record.match_id != 0)
                continue;

            // Ignore inactive clients
            if (!record.active)
                continue;

            // Ignore the client we're trying to match
            if (record.clientRecord.id == id)
                continue;

            // Ignore incompatible clients
            if (!primaryClientRecord.RequirementsPass(record) || !record.RequirementsPass(primaryClientRecord))
                continue;

            foundClients.add(record);

            // Stop looking if we've found enough clients now.
            //
            // If finding compatible clients is a slow process, some may have quit by the time we finish.  In that
            // case, we should verify that the foundClients are still valid, in the loop, before breaking out.  So we
            // should be able to be quite confident that the client list is fairly reliable when we leave the loop.
            if (foundClients.size() == wantedClients)
                break;
        }

        if (foundClients.size() == wantedClients)
        {
            Log("   match success");

            // Make a list of client IDs
            int[] clientIdList = new int[foundClients.size()];
            for (int i = 0; i < foundClients.size(); ++i)
                clientIdList[i] = foundClients.get(i).clientRecord.id;

            // Create a MatchRecord
            MatchRecord match = new MatchRecord(lastMatchId.incrementAndGet(), clientIdList);
            matchData.put(match.id, match);

            Log("        new match id " + match.id);

            // Mark these clients as part of the session.
            for (ServletClientRecord record : foundClients)
            {
                Log("        client " + record.clientRecord.id);
                record.match_id = match.id;

                // Signal anybody watching the record to say that it has changed
                record.waitUntilMatched.countDown();
            }
        }
    }

    public void VerifyMatch(int id)
    {
        MatchRecord match = matchData.get(id);
        if (match == null)
            return;

        // Check all the clients are still compatible with the match, and cancel the match if any are unhappy
        for (int clientId : match.clients)
        {
            ServletClientRecord client = clientData.get(clientId);
            if (client == null)
            {
                RemoveMatch(id);
                return;
            }

            for (int otherClientId : match.clients)
            {
                ServletClientRecord otherClient = clientData.get(otherClientId);
                if (otherClient == null || !client.RequirementsPass(otherClient))
                {
                    RemoveMatch(id);
                    return;
                }
            }
        }
    }

    public void RemoveMatch(int id)
    {
        MatchRecord match = matchData.remove(id);
        if (match == null)
            return;

        // Remove the match reference from the client records
        for (int clientId : match.clients)
        {
            ServletClientRecord client = clientData.get(clientId);
            if (client == null) continue;

            if (client.match_id == id) {
                client.ClearMatch();

                if (client.deleted)
                {
                    clientData.remove(clientId);
                }
            }
        }

        // Search again for matches for these clients
        for (int clientId : match.clients)
        {
            UpdateClient(clientId);
        }
    }

    public void DeleteClient(int id)
    {
        final ServletClientRecord client = clientData.get(id);
        client.deleted = true;

        if (client.match_id == 0)
        {
            clientData.remove(id);
        }
        else
        {
            MatchRecord match = GetMatchRecord(client.match_id);
            if (match == null)
            {
                clientData.remove(id);
                return;
            }

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

                RemoveMatch(client.match_id);
            }
        }
    }

    public Matchmaker(ConcurrentHashMap<Integer, ServletClientRecord> data)
    {
        clientData = data;
    }

    public MatchRecord GetMatchRecord(int id) { return matchData.get(id); }
    public int NumMatches() { return matchData.size(); }

    private ConcurrentHashMap<Integer, ServletClientRecord> clientData;
    private ConcurrentHashMap<Integer, MatchRecord> matchData = new ConcurrentHashMap<Integer, MatchRecord>();
    private AtomicInteger lastMatchId = new AtomicInteger();
}
