package com.studiogobo.fi.Matcher.Servlet;

import com.studiogobo.fi.Matcher.Model.ClientRecord;

import java.util.concurrent.CountDownLatch;

public class ServletClientRecord
{
    public ClientRecord clientRecord;
    public int match_id = 0;
    public boolean deleted = false;
    public CountDownLatch waitUntilMatched = new CountDownLatch(1);

    public ServletClientRecord(ClientRecord _clientRecord)
    {
        clientRecord = _clientRecord;
    }
}
