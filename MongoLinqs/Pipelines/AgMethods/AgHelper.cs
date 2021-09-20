using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoLinqs.Pipelines.Utils;

namespace MongoLinqs.Pipelines.AgMethods
{
    public static class AgHelper
    {
        public static bool IsAggregating(MethodCallExpression call)
        {
            return GroupHelper.IsGroupCall(call) || GroupHelper.IsEnumCall(call);
        }

        public static string BuildFunctions(MethodCallExpression call, IList<Expression> @params)
        {
            if (call.Method.Name == nameof(Enumerable.Count))
            {
                return BuildCount(call, @params);
            }

            if (call.Method.Name == nameof(Enumerable.Average))
            {
                return BuildAverage(call, @params);
            }

            throw new NotSupportedException();
        }

        private static string BuildCount(MethodCallExpression call, IList<Expression> @params)
        {
            if (call.Arguments.Count != 1)
            {
                throw new NotSupportedException();
            }

            var path = PathAccessHelper.GetPath(call.Arguments[0], @params);

            return $"{{\"$size\":\"${path}\"}}";
        }

        private static string BuildAverage(MethodCallExpression call, IList<Expression> @params)
        {
            return BuildCommon("$avg",call, @params);
        }
        
        private static string BuildSum(MethodCallExpression call, IList<Expression> @params)
        {
            return BuildCommon("$sum",call, @params);
        }

        private static string BuildCommon(string @operator, MethodCallExpression call, IList<Expression> @params)
        {
            var outerPath = PathAccessHelper.GetPath(call.Arguments[0], @params);
            if (call.Arguments.Count == 1)
            {
                return $"{{\"{@operator}\":\"${outerPath}\"}}";
            }
            else
            {
                var lambda = call.Arguments[1] as LambdaExpression;
                var innerPath = PathAccessHelper.GetPath(lambda!.Body, lambda.Parameters.Cast<Expression>().ToList());
                return $"{{\"{@operator}\":\"${outerPath}.{innerPath}\"}}";
            }
        }
    }
}