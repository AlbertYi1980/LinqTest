using System;

namespace MongoLinqs.Pipelines.Utils
{
    public static class NameHelper
    {
        public static string Map(string s)
        {
            if (s == "Id") return "_id";
            return ToCamelCase(s);
        }

        public static string InverseMap(string s)
        {
            if (s == "_id") return "Id";
            return ToPascalCase(s);
        }

        private static string ToCamelCase(string s)
        {
            if (s == null) return null;
            if (s == string.Empty) return s;
            return s.Substring(0, 1).ToLower() + s.Substring(1);
        }
        
        private static string ToPascalCase(string s)
        {
            if (s == null) return null;
            if (s == string.Empty) return s;
            return s.Substring(0, 1).ToUpper() + s.Substring(1);
        }
        
        public static string GetTempField()
        {
            return $"f_{Guid.NewGuid():n}";
        }
        
    }
}