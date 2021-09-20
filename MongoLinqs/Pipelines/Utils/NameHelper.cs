using System;
using System.Reflection;

namespace MongoLinqs.Pipelines.Utils
{
    public static class NameHelper
    {
        public static string MapEntity(string s)
        {
            if (s == "Id") return "_id";
            return ToCamelCase(s);
        }
        
        public static string MapMember(MemberInfo member)
        {
            var memberName = member.Name;
            var isEntityMember = member.ReflectedType!.GetCustomAttribute<EntityAttribute>() != null;
            var isGroupMember = GroupHelper.IsGroupMember(member);
            if (isEntityMember && memberName == "Id") return "_id";
            if (isGroupMember && memberName == "Key") return "_id";
            return ToCamelCase(memberName);
        }
        
        public static string MapCollection(string s)
        {
            return ToCamelCase(s);
        }

        public static string InverseMapEntity(string s)
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