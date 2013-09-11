package com.studiogobo.fi.Matcher.Model;

import com.studiogobo.fi.Matcher.Model.Requirements.RequireNotUuid;
import com.studiogobo.fi.Matcher.Model.Requirements.Requirement;

import java.util.List;
import java.util.UUID;
import java.util.Vector;

public class ClientRecord
{
    public int id;

    public UUID uuid = new UUID(0, 0);

    public List<Requirement> requirements = new Vector<Requirement>();

    public ClientRecord()
    {
        this(0);
    }

    public ClientRecord(int _id)
    {
        id = _id;
        RequireNotUuid requirement = new RequireNotUuid();
        requirement.uuid = new UUID(0, 0);
        requirements.add(requirement);
        requirements.add(requirement);
    }
}
