package com.studiogobo.fi.spatialmatchmaking.model;

import com.studiogobo.fi.spatialmatchmaking.model.requirements.Requirement;

import java.util.List;
import java.util.UUID;
import java.util.Vector;

public class ClientRecord
{
    public int id;

    public UUID uuid = new UUID(0, 0);

    public List<Requirement> requirements = new Vector<Requirement>();

    public Location location = new Location();

    public String connectionInfo;

    public ClientRecord()
    {
        this(0);
    }

    public ClientRecord(int _id)
    {
        id = _id;
    }

    public boolean RequirementsPass(ClientRecord other)
    {
        for (Requirement req : requirements)
        {
            if (!req.Evaluate(this, other))
            {
                return false;
            }
        }
        return true;
    }
}
