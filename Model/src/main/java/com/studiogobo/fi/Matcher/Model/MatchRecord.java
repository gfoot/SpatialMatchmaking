package com.studiogobo.fi.Matcher.Model;

import javax.xml.bind.annotation.XmlAttribute;
import javax.xml.bind.annotation.XmlElement;
import javax.xml.bind.annotation.XmlRootElement;

@XmlRootElement(name="match")
public class MatchRecord {
    @XmlAttribute
    public int id;

    @XmlElement
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

