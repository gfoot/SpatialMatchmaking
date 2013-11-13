package com.studiogobo.fi.Matcher.Model.Requirements;

import com.studiogobo.fi.Matcher.Model.ClientRecord;
import org.codehaus.jackson.annotate.JsonTypeName;

import java.util.UUID;

@JsonTypeName("requireNotUuid")
public class RequireNotUuid extends Requirement
{
    public UUID uuid;

    @Override
    public boolean Evaluate(ClientRecord us, ClientRecord them)
    {
        return !them.uuid.equals(uuid);
    }
}
