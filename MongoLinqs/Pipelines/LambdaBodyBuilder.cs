using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using MongoLinqs.Pipelines.Utils;
using Newtonsoft.Json;

namespace MongoLinqs.Pipelines
{
    public class LambdaBodyBuilder
    {
        private readonly Expression _body;
        private readonly bool _multipleParams;

        public LambdaBodyBuilder(Expression body, bool multipleParams)
        {
            _body = body;
            _multipleParams = multipleParams;
        }

        public string Build(bool isRef = true, bool fieldFirst = false)
        {
            return BuildRecursive(_body, isRef, fieldFirst);
        }

        private string BuildRecursive(Expression current, bool isRef, bool fieldFirst)
        {
            switch (current)
            {
                case ParameterExpression:
                case MemberExpression:
                    var prefix = isRef ? "$" : string.Empty;
                    return $"\"{prefix}{PathAccessHelper.GetPath(current, _multipleParams)}\"";
                case NewExpression:
                case MemberInitExpression:
                    return BuildNew(current);
                case ConstantExpression constant:
                    return JsonConvert.SerializeObject(constant.Value);
                case MethodCallExpression call:
                    if (call.Method.Name == nameof(string.Contains))
                    {
                        return BuildStringContains(call);
                    }

                    if (!AgHelper.IsAggregating(call)) throw new NotSupportedException();
                    return AgHelper.BuildFunctions(call, _multipleParams);
                case BinaryExpression binary:
                    switch (binary.NodeType)
                    {
                        case ExpressionType.Add:
                            return BuildOperatorFirstBinary(binary, "$add", fieldFirst);
                        case ExpressionType.Subtract:
                            return BuildOperatorFirstBinary(binary, "$subtract", fieldFirst);
                        case ExpressionType.Multiply:
                            return BuildOperatorFirstBinary(binary, "$multiply", fieldFirst);
                        case ExpressionType.Divide:
                            if (binary.Left.Type == typeof(int) && binary.Right.Type == typeof(int))
                            {
                                return BuildIntegerDivide(binary);
                            }
                            else
                            {
                                return BuildOperatorFirstBinary(binary, "$divide", fieldFirst);
                            }

                        case ExpressionType.Modulo:
                            return BuildOperatorFirstBinary(binary, "$mod", fieldFirst);

                        case ExpressionType.Equal:
                            if (fieldFirst)
                            {
                                return BuildLeftFirstBinary(binary, "$eq");
                            }
                            else
                            {
                                return BuildOperatorFirstBinary(binary, "$eq", fieldFirst);
                            }

                        case ExpressionType.NotEqual:
                            if (fieldFirst)
                            {
                                return BuildLeftFirstBinary(binary, "$ne");
                            }
                            else
                            {
                                return BuildOperatorFirstBinary(binary, "$ne", fieldFirst);
                            }

                        case ExpressionType.OrElse:
                            return BuildOperatorFirstBinary(binary, "$or", fieldFirst);
                        case ExpressionType.AndAlso:
                            return BuildOperatorFirstBinary(binary, "$and", fieldFirst);

                        default:
                            throw new NotSupportedException();
                    }
                case UnaryExpression unary:
                    switch (unary.NodeType)
                    {
                        case ExpressionType.Not:
                            return BuildUnary(unary, "$not");
                        case ExpressionType.Convert:
                            return BuildRecursive(unary.Operand, true, false);
                        default:
                            throw new NotSupportedException();
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        private string BuildOperatorFirstBinary(BinaryExpression binary, string @operator, bool fieldFirst)
        {
            fieldFirst = (@operator == "$and" || @operator == "$or") && fieldFirst;
            var left = BuildRecursive(binary.Left, true, fieldFirst);
            var right = BuildRecursive(binary.Right, true, fieldFirst);
            return $"{{{@operator}:[{left},{right}]}}";
        }

        private string BuildLeftFirstBinary(BinaryExpression binary, string @operator)
        {
            var left = BuildRecursive(binary.Left, false, true);
            var right = BuildRecursive(binary.Right, true, false);
            return $"{{{left}:{{{@operator}:{right}}}}}";
        }

        private string BuildUnary(UnaryExpression unary, string @operator)
        {
            var operand = BuildRecursive(unary.Operand, true, false);
            return $"{{{@operator}:{operand}}}";
        }

        private string BuildIntegerDivide(BinaryExpression binary)
        {
            var left = BuildRecursive(binary.Left, true, false);
            var right = BuildRecursive(binary.Right, true, false);
            return $"{{$toInt:{{$divide:[{{$subtract:[{left},{{$mod:[{left},{right}]}}]}},{right}]}}}}";
        }

        private string BuildStringContains(MethodCallExpression call)
        {
            var left = call.Object as MemberExpression;
            var right = call.Arguments[0] as ConstantExpression;
            if (right!.Value == null) throw new NotSupportedException();
            if (left!.Type != typeof(string) || right!.Type != typeof(string)) throw new NotSupportedException();
            if (left.NodeType != ExpressionType.MemberAccess) throw new NotSupportedException();
            if (right.NodeType != ExpressionType.Constant) throw new NotSupportedException();

            var builder = new StringBuilder();

            builder.Append("{");
            builder.Append(BuildRecursive(left, false, false));
            builder.Append(":{\"$regex\":");
            builder.Append(JsonConvert.ToString($".*{Regex.Escape(right.Value!.ToString()!)}.*"));
            builder.Append("}");
            builder.Append("}");
            return builder.ToString();
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
                    value = BuildRecursive(@new.Arguments[i], true, false);
                }
                else if (expression is MemberInitExpression memberInit)
                {
                    var assignment = (MemberAssignment) memberInit.Bindings[i];

                    name = NameHelper.MapMember(assignment.Member);

                    value = BuildRecursive(assignment.Expression, true, false);
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