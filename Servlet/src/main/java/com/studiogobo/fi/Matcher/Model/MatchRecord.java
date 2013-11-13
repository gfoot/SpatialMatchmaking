package com.studiogobo.fi.Matcher.Model;

public class MatchRecord {
    public int id;

    public int[] clients;

    public MatchRecord()
    {
        this(0, null);
    }

    public MatchRecord(int _id, int[] _clients)
    {
        id = _id;
        clients = _clients;
    }
}
