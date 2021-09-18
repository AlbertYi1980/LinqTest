namespace MongoLinqs
{
    public static class NameHelper
    {
        public static string FixMemberName(string memberName)
        {
            return memberName == "id" ? "_id" : memberName;
        }
        
        public static string ToCamelCase(string s)
        {
            if (s == null) return null;
            if (s == string.Empty) return s;
            return s.Substring(0, 1).ToLower() + s.Substring(1);
        }
    }
}