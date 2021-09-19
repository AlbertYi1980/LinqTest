using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MongoLinqs.Pipelines.Grouping
{
    public static class GroupHelper
    {
        public const string GroupElements = "f_94af22dbce8645b6a8c97cc2f28a9fc7";

        public static bool IsGroupCall(MethodCallExpression call)
        {
            var method = call.Method;
            if (!method.IsStatic) return false;
            var arguments = call.Arguments;
            if (arguments.Count < 1) return false;
            var type = arguments[0].Type;
            if (!type.IsGenericType) return false;
            return type.GetGenericTypeDefinition() == typeof(IGrouping<,>);
        }
        
        public static bool IsEnumCall(MethodCallExpression call)
        {
            var method = call.Method;
            if (!method.IsStatic) return false;
            var arguments = call.Arguments;
            if (arguments.Count < 1) return false;
            var type = arguments[0].Type;
            if (!type.IsGenericType) return false;
            return type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }
        
        public static bool IsGroupMember(MemberExpression member)
        {
            var type = member.Expression!.Type;
            if (!type.IsGenericType) return false;
            return type.GetGenericTypeDefinition() == typeof(IGrouping<,>);
        }
    }
}