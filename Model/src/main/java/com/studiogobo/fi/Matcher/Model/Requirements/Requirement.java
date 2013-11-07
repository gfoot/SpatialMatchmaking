package com.studiogobo.fi.Matcher.Model.Requirements;

import com.studiogobo.fi.Matcher.Model.ClientRecord;
import org.codehaus.jackson.annotate.JsonSubTypes;
import org.codehaus.jackson.annotate.JsonTypeInfo;

@JsonTypeInfo(use= JsonTypeInfo.Id.NAME, include= JsonTypeInfo.As.PROPERTY, property="@type")
@JsonSubTypes({
        @JsonSubTypes.Type(RequireNotUuid.class),
        @JsonSubTypes.Type(RequireAttribute.class),
        @JsonSubTypes.Type(RequireLocationWithin.class),
        @JsonSubTypes.Type(RequireLocationOverlap.class)
})
public abstract class Requirement
{
    public abstract boolean Evaluate(ClientRecord us, ClientRecord them);
}
