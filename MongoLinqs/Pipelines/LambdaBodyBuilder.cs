using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoLinqs.Pipelines.Utils;
using Newtonsoft.Json;

namespace MongoLinqs.Pipelines
{
    public class LambdaBodyBuilder
    {
        private readonly bool _isRef;
        private readonly Expression _body;
        private readonly bool _multipleParams;

        public LambdaBodyBuilder(Expression body, bool multipleParams, bool isRef = true)
        {
            _isRef = isRef;
            _body = body;
            _multipleParams = multipleParams;
        }

        public string Build()
        {
            return BuildRecursive(_body);
        }

        private string BuildRecursive(Expression current)
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
                case BinaryExpression binary:
                    switch (binary.NodeType)
                    {
                        case ExpressionType.Add:
                            return BuildBinary(binary, "$add");
                        case ExpressionType.Subtract:
                            return BuildBinary(binary, "$subtract");
                        case ExpressionType.Multiply:
                            return BuildBinary(binary, "$multiply");
                        case ExpressionType.Divide:
                            if (binary.Left.Type == typeof(int) && binary.Right.Type == typeof(int))
                            {
                                return BuildIntegerDivide(binary);
                            }
                            else
                            {
                                return BuildBinary(binary, "$divide");
                            }
                            
                        case ExpressionType.Modulo:
                            return BuildBinary(binary, "$mod");
                        default:
                            throw new NotSupportedException();
                    }
                case UnaryExpression unary:
                    switch (unary.NodeType)
                    {
                        case ExpressionType.Convert:
                            return BuildRecursive(unary.Operand);
                        default:
                            throw new NotSupportedException();
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        private string BuildBinary(BinaryExpression binary, string @operator)
        {
            var left = BuildRecursive(binary.Left);
            var right = BuildRecursive(binary.Right);
            return $"{{{@operator}:[{left},{right}]}}";
        }
        
        private string BuildIntegerDivide(BinaryExpression binary)
        {
            var left = BuildRecursive(binary.Left);
            var right = BuildRecursive(binary.Right);
            return $"{{$toInt:{{$divide:[{{$subtract:[{left},{{$mod:[{left},{right}]}}]}},{right}]}}}}";
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
                    value = BuildRecursive(@new.Arguments[i]);
                }
                else if (expression is MemberInitExpression memberInit)
                {
                    var assignment = (MemberAssignment) memberInit.Bindings[i];

                    name = NameHelper.MapMember(assignment.Member);

                    value = BuildRecursive(assignment.Expression);
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