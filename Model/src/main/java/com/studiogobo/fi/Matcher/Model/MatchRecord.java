package com.studiogobo.fi.Matcher.Model;

import javax.xml.bind.annotation.XmlAttribute;
import javax.xml.bind.annotation.XmlElement;
import javax.xml.bind.annotation.XmlRootElement;
import java.util.List;

/**
 * Created with IntelliJ IDEA.
 * User: george
 * Date: 05/09/13
 * Time: 12:53
 * To change this template use File | Settings | File Templates.
 */
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

