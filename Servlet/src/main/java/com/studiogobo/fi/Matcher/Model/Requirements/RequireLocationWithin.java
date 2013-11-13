package com.studiogobo.fi.Matcher.Model.Requirements;

import com.studiogobo.fi.Matcher.Model.ClientRecord;
import org.codehaus.jackson.annotate.JsonTypeName;

@JsonTypeName("requireLocationWithin")
public class RequireLocationWithin extends Requirement {
    public int radius;

    @Override
    public boolean Evaluate(ClientRecord us, ClientRecord them) {
        return us.location.Distance(them.location) < radius;
    }
}
