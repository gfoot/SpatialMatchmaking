package com.studiogobo.fi.Matcher.Servlet;

import sun.reflect.generics.reflectiveObjects.NotImplementedException;

import java.util.Iterator;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * This adds automatic ID allocation and a keepalive/expiry mechanism to a ConcurrentHashMap.
 */
public class PerishableCollection<T> //implements Map<Integer, T>
{
    public PerishableCollection(long maxAgeMillis)
    {
        this.maxAgeMillis = maxAgeMillis;
    }

    public void expireIdleEntries()
    {
        for (Map.Entry<Integer, TimestampedT> entry : data.entrySet())
        {
            if (entry.getValue().AgeMillis() > maxAgeMillis)
            {
                data.remove(entry.getKey());
            }
        }
    }

    public T get(int id)
    {
        TimestampedT tt = data.get(id);

        if (tt == null)
            return null;

        tt.KeepAlive();
        return tt.value;
    }

    public T remove(int id)
    {
        TimestampedT tt = data.remove(id);

        if (tt == null)
            return null;

        return tt.value;
    }

    public int getNewId()
    {
        return idCounter.incrementAndGet();
    }

    public void put(int id, T value)
    {
        TimestampedT tt = new TimestampedT(value);
        data.put(id, tt);
    }

    public Iterable<T> values()
    {
        return new Iterable<T>()
        {
            @Override
            public Iterator<T> iterator()
            {
                return new Iterator<T>()
                {
                    private Iterator<TimestampedT> childIterator = data.values().iterator();

                    @Override
                    public boolean hasNext()
                    {
                        return childIterator.hasNext();
                    }

                    @Override
                    public T next()
                    {
                        return childIterator.next().value;
                    }

                    @Override
                    public void remove()
                    {
                        throw new NotImplementedException();
                    }
                };
            }
        };
    }

    private class TimestampedT
    {
        public T value;
        private long updateTimeMillis;

        public TimestampedT(T _value)
        {
            value = _value;
            KeepAlive();
        }

        public void KeepAlive()
        {
            updateTimeMillis = System.currentTimeMillis();
        }

        public long AgeMillis()
        {
            return System.currentTimeMillis() - updateTimeMillis;
        }
    }

    private Map<Integer, TimestampedT> data = new ConcurrentHashMap<Integer, TimestampedT>();
    private AtomicInteger idCounter = new AtomicInteger();

    private long maxAgeMillis;
}
