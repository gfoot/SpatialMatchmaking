using System;
using System.Text;

namespace Assets
{
    public static class Json
    {
        public static string StringifyObject(object value)
        {
            if (value is JsonObject)
                return (value as JsonObject).ToString();

            if (value is JsonArray)
                return (value as JsonArray).ToString();
            
            return "\"" + value + "\"";
        }

        public static void ConsumeWhitespace(string s, ref int position)
        {
            while (position < s.Length && Char.IsWhiteSpace(s[position]))
                ++position;
        }

        public static JsonObject ParseObject(string s, ref int position)
        {
            var obj = new JsonObject();
            obj.DoParse(s, ref position);
            return obj;
        }

        public static JsonArray ParseArray(string s, ref int position)
        {
            var array = new JsonArray();
            var pos = position;

            ConsumeWhitespace(s, ref pos);

            if (s[pos] != '[')
                throw new ParseException("Expected '['", s, pos);
            ++pos;

            ConsumeWhitespace(s, ref pos);

            while (s[pos] != ']')
            {
                ConsumeWhitespace(s, ref pos);
                
                if (s[pos] == '"')
                {
                    array.Add(ParseString(s, ref pos));
                }
                else if (s[pos] == '{')
                {
                    array.Add(ParseObject(s, ref pos));
                }
                else if (s[pos] == '[')
                {
                    array.Add(ParseArray(s, ref pos));
                }
                else
                {
                    throw new ParseException("Expected '\"', '{' or '['", s, pos);
                }

                ConsumeWhitespace(s, ref pos);

                if (s[pos] != ',' && s[pos] != ']')
                    throw new ParseException("Expected ',' or ']'", s, pos);

                if (s[pos] == ',')
                {
                    ++pos;
                    ConsumeWhitespace(s, ref pos);
                }
            }
            ++pos;

            ConsumeWhitespace(s, ref pos);

            position = pos;
            return array;
        }

        public static string ParseString(string s, ref int position)
        {
            var result = "";
            var pos = position;

            if (s[pos] != '"')
                throw new ParseException("Expected '\"'", s, pos);
            ++pos;

            while (s[pos] != '"')
            {
                if (s[pos] == '\\')
                    ++pos;
                result += s[pos];
                ++pos;
            }

            ++pos;

            position = pos;
            return result;
        }

        public class ParseException : Exception
        {
            public ParseException(string error, string data, int position)
                : base(String.Format("Json parse error: {0} at position {1} in \"{2}\"", error, position, data))
            {
            }
        }

        public static string BytesToString(byte[] data)
        {
            var decoder = Encoding.UTF8.GetDecoder();
            var chars = new char[decoder.GetCharCount(data, 0, data.Length)];
            var count = decoder.GetChars(data, 0, data.Length, chars, 0);
            return new string(chars, 0, count);
        }
    }
}
