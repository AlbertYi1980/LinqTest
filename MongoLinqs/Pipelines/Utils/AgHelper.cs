using System;
using System.Linq;
using System.Linq.Expressions;

namespace MongoLinqs.Pipelines.Utils
{
    public static class AgHelper
    {
        public static bool IsAggregating(MethodCallExpression call)
        {
            return GroupHelper.IsGroupCall(call) || GroupHelper.IsEnumCall(call);
        }

        public static string BuildFunctions(MethodCallExpression call,bool multipleParams)
        {
            if (call.Method.Name == nameof(Enumerable.Count))
            {
                return BuildCount(call, multipleParams);
            }

            if (call.Method.Name == nameof(Enumerable.Average))
            {
                return BuildCommon("$avg",call,multipleParams);
            }
            
            if (call.Method.Name == nameof(Enumerable.Sum))
            {
                return BuildCommon("$sum",  call, multipleParams);
            }

            throw new NotSupportedException();
        }

        private static string BuildCount(MethodCallExpression call, bool multipleParams)
        {
            if (call.Arguments.Count != 1)
            {
                throw new NotSupportedException();
            }

            var path = PathAccessHelper.GetPath(call.Arguments[0], multipleParams);

            return $"{{\"$size\":\"${path}\"}}";
        }
        

        private static string BuildCommon(string @operator, MethodCallExpression call, bool multipleParams)
        {
            var outerPath = PathAccessHelper.GetPath(call.Arguments[0], multipleParams);
            if (call.Arguments.Count == 1)
            {
                return $"{{\"{@operator}\":\"${outerPath}\"}}";
            }
            else
            {
                var lambda = call.Arguments[1] as LambdaExpression;
                var innerPath = PathAccessHelper.GetPath(lambda!.Body, lambda.Parameters.Count > 1);
                return $"{{\"{@operator}\":\"${outerPath}.{innerPath}\"}}";
            }
        }
    }
}