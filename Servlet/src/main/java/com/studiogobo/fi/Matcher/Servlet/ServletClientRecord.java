package com.studiogobo.fi.Matcher.Servlet;

import com.studiogobo.fi.Matcher.Model.ClientRecord;

/**
 * Created with IntelliJ IDEA.
 * User: george
 * Date: 05/09/13
 * Time: 13:54
 * To change this template use File | Settings | File Templates.
 */
public class ServletClientRecord
{
    public ClientRecord clientRecord;
    public int match_id = 0;
    public boolean deleted = false;

    public ServletClientRecord(ClientRecord _clientRecord)
    {
        clientRecord = _clientRecord;
    }
}
