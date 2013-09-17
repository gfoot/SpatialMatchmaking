using System.Collections.Generic;

namespace Assets
{
    public class JsonObject
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();

        public void Set(string key, object value)
        {
            _data[key] = value;
        }

        public string GetString(string key)
        {
            return (string)_data[key];
        }

        public JsonObject GetObject(string key)
        {
            return (JsonObject)_data[key];
        }

        public JsonArray GetArray(string key)
        {
            return (JsonArray)_data[key];
        }

        public double GetNumber(string key)
        {
            return (double)_data[key];
        }

        public JsonObject()
        {
        }

        public JsonObject(string data)
        {
            var position = 0;
            DoParse(data, ref position);
            Json.ConsumeWhitespace(data, ref position);
            if (position != data.Length)
                throw new Json.ParseException("Expected end of string", data, position);
        }

        public JsonObject(byte[] data)
            : this(Json.BytesToString(data))
        {
        }

        public override string ToString()
        {
            var s = "{ ";
            
            var first = true;
            foreach (var key in _data.Keys)
            {
                if (!first)
                {
                    s += ", ";
                }
                first = false;

                var value = _data[key];
                s += "\"" + key + "\": ";

                s += Json.StringifyObject(value);
            }
            
            s += " }";

            return s;
        }

        public byte[] ToByteArray()
        {
            return Json.StringToBytes(ToString());
        }

        public static JsonObject Parse(string s)
        {
            var position = 0;
            return Json.ParseObject(s, ref position);
        }

        public void DoParse(string s, ref int position)
        {
            var pos = position;

            Json.ConsumeWhitespace(s, ref pos);

            if (s[pos] != '{')
                throw new Json.ParseException("Expected '{'", s, pos);
            ++pos;

            Json.ConsumeWhitespace(s, ref pos);

            while (s[pos] != '}')
            {
                var key = Json.ParseString(s, ref pos);
                
                Json.ConsumeWhitespace(s, ref pos);
                
                if (s[pos] != ':')
                    throw new Json.ParseException("Expected ':'", s, pos);
                ++pos;
                
                Json.ConsumeWhitespace(s, ref pos);

                Set(key, Json.Parse(s, ref pos));

                Json.ConsumeWhitespace(s, ref pos);

                if (s[pos] != ',' && s[pos] != '}')
                    throw new Json.ParseException("Expected ',' or '}'", s, pos);

                if (s[pos] == ',')
                {
                    ++pos;
                    Json.ConsumeWhitespace(s, ref pos);
                }
            }
            ++pos;

            Json.ConsumeWhitespace(s, ref pos);

            position = pos;
        }
    }
}