package com.studiogobo.fi.Matcher.Servlet;

import com.studiogobo.fi.Matcher.Model.*;
import com.studiogobo.fi.Matcher.Model.Requirements.Requirement;

import java.util.ArrayList;

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

        final int wantedClients = 2;

        // Start with a list containing only the client we're trying to match
        ArrayList<ServletClientRecord> foundClients = new ArrayList<ServletClientRecord>();
        foundClients.add(primaryClientRecord);

        // Find compatible clients and add them to the list
        for (ServletClientRecord record : clientData.values())
        {
            // Ignore clients already in sessions
            if (record.match_id != 0)
                continue;

            // Ignore the client we're trying to match
            if (record.clientRecord.id == id)
                continue;

            // Ignore incompatible clients
            boolean ok = true;
            for (Requirement req : record.clientRecord.requirements)
            {
                if (!req.Evaluate(record.clientRecord, primaryClientRecord.clientRecord))
                {
                    ok = false;
                    break;
                }
            }
            for (Requirement req : primaryClientRecord.clientRecord.requirements)
            {
                if (!req.Evaluate(primaryClientRecord.clientRecord, record.clientRecord))
                {
                    ok = false;
                    break;
                }
            }
            if (!ok)
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
            MatchRecord match = new MatchRecord(matchData.getNewId(), clientIdList);
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

    public Matchmaker(PerishableCollection<ServletClientRecord> data)
    {
        clientData = data;
    }

    public MatchRecord GetMatchRecord(int id) { return matchData.get(id); }
    public int NumMatches() { return matchData.size(); }

    private PerishableCollection<ServletClientRecord> clientData;
    private PerishableCollection<MatchRecord> matchData = new PerishableCollection<MatchRecord>(5000);
}
