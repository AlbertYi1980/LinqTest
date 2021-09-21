using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoLinqs.Pipelines.AgMethods;
using MongoLinqs.Pipelines.Utils;
using Newtonsoft.Json;

namespace MongoLinqs.Pipelines.Selectors
{
    public class SelectorBuilder
    {
        private readonly bool _isRef;
        private readonly Expression _body;
        private readonly bool _multipleParams;

        public SelectorBuilder(LambdaExpression selector, bool isRef = true)
        {
            _isRef = isRef;
            _body = selector.Body;
            _multipleParams = selector.Parameters.Count > 1;
        }

        public string Build()
        {
            return BuildCore(_body);
        }

        private string BuildCore(Expression current)
        {
            switch (current)
            {
                case ParameterExpression:
                case MemberExpression:
                    var prefix = _isRef ? "$" : string.Empty;
                    return $"\"{prefix}{PathAccessHelper.GetPath(current, _multipleParams)}\"";
                case NewExpression:
                case MemberInitExpression:
                    return BuildNew(current);
                case ConstantExpression constant:
                    return JsonConvert.SerializeObject(constant.Value);
                case MethodCallExpression call:
                    if (!AgHelper.IsAggregating(call)) throw new NotSupportedException();
                    return AgHelper.BuildFunctions(call, _multipleParams);
                 default:
                     throw new NotSupportedException();
            }
        }

        private int GetNewLength(Expression expression)
        {
            if (expression is NewExpression @new)
            {
                return @new.Constructor!.GetParameters().Length;
            }

            if (expression is MemberInitExpression memberInit)
            {
                return memberInit.Bindings.Count;
            }

            throw new NotSupportedException();
        }

        private string BuildNew(Expression expression)
        {
            var length = GetNewLength(expression);
            var members = new List<string>();
            for (var i = 0; i < length; i++)
            {
                string name;
                string value;

                if (expression is NewExpression @new)
                {
                    name = NameHelper.MapMember(@new.Members![i]);
                    value = BuildCore(@new.Arguments[i]);
                }
                else if (expression is MemberInitExpression memberInit)
                {
                    var assignment = (MemberAssignment) memberInit.Bindings[i];

                    name = NameHelper.MapMember(assignment.Member);

                    value = BuildCore(assignment.Expression);
                }
                else
                {
                    throw new NotSupportedException();
                }

                var member = $"\"{name}\":{value}";
                members.Add(member);
            }

            return $"{{{string.Join(",", members)}}}";
        }
    }
}