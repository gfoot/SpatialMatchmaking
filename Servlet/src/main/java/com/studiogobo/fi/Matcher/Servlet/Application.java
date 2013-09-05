package com.studiogobo.fi.Matcher.Servlet;

import javax.annotation.PreDestroy;
import javax.ws.rs.ApplicationPath;
import java.util.HashSet;
import java.util.Set;

@ApplicationPath("Matcher")
public class Application extends javax.ws.rs.core.Application
{
    private static final Set<Object> singletons = new HashSet<Object>();
    private Set<Class<?>> classes = new HashSet<Class<?>>();

    private Servlet servlet = new Servlet();

    public Application()
    {
        singletons.add(servlet);
    }

    @Override
    public Set<Class<?>> getClasses()
    {
        return classes;
    }

    @Override
    public Set<Object> getSingletons()
    {
        return singletons;
    }

    @PreDestroy
    public void preDestroy() throws InterruptedException
    {
        servlet.preDestroy();
    }
}
