using System;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using MongoLinqs.MemberPath;
using Newtonsoft.Json;

namespace MongoLinqs.Conditions
{
    public class ConditionBuilder
    {

        private readonly Expression _param;
        private readonly Expression _body;
        public ConditionBuilder(LambdaExpression lambda)
        {
            _param = lambda.Parameters[0];
            _body = lambda.Body;
        }

        public string Build()
        {
            return BuildCore(_body);
        }

        private string BuildCore(Expression current)
        {
            var binary = current as BinaryExpression;
            var constant = current as ConstantExpression;
            var unary = current as UnaryExpression;

            var builder = new StringBuilder();

            switch (current.NodeType)
            {
                case ExpressionType.AndAlso:
                    builder.Append("{\"$and\":[");
                    builder.Append(BuildCore(binary!.Left));
                    builder.Append(",");
                    builder.Append(BuildCore(binary!.Right));
                    builder.Append("]}");
                    break;
                case ExpressionType.OrElse:
                    builder.Append("{\"$or\":[");
                    builder.Append(BuildCore(binary!.Left));
                    builder.Append(",");
                    builder.Append(BuildCore(binary!.Right));
                    builder.Append("]}");
                    break;
                case ExpressionType.Equal:
                    builder.Append("{");
                    builder.Append(VisitProperty(binary!.Left));
                    builder.Append(":{\"$eq\":");
                    builder.Append(BuildCore(binary!.Right));
                    builder.Append("}}");
                    break;
                case ExpressionType.NotEqual:
                    builder.Append("{");
                    builder.Append(VisitProperty(binary!.Left));
                    builder.Append(":{\"$ne\":");
                    builder.Append(BuildCore(binary!.Right));
                    builder.Append("}}");
                    break;
                case ExpressionType.Not:
                    builder.Append("{");
                    builder.Append(VisitProperty(unary!.Operand));
                    builder.Append(":{\"$ne\":");
                    builder.Append("true");
                    builder.Append("}}");
                    break;
                case ExpressionType.MemberAccess:
                    builder.Append("{");
                    builder.Append(VisitProperty(current));
                    builder.Append(":{\"$eq\":");
                    builder.Append("true");
                    builder.Append("}}");
                    break;

                case ExpressionType.Constant:
                    var value = constant!.Value;
                    if (current.Type == typeof(int))
                    {
                        builder.Append(value);
                    }
                    else if (current.Type == typeof(int?))
                    {
                        var s = value == null ? "null" : value.ToString();
                        builder.Append(s);
                    }
                    else if (current.Type == typeof(string))
                    {
                        var s = value == null ? "null" : JsonConvert.ToString(value);
                        builder.Append(s);
                    }
                    else
                    {
                        throw new NotSupportedException($"not support const type {current.Type.Name}");
                    }


                    break;
                case ExpressionType.Call:
                    builder.Append(VisitSpecialCondition(current));
                    break;
                default:

                    throw new NotSupportedException();
            }

            return builder.ToString();
        }

        private string VisitProperty(Expression node)
        {
            var member = node as MemberExpression;
            return $"\"{MemberAccessHelper.GetPath(member, _param)}\"";
        }

        private string VisitSpecialCondition(Expression node)
        {
            var call = node as MethodCallExpression;
            if (call!.Method.Name != nameof(string.Contains)) throw new NotSupportedException();
            var left = call.Object as MemberExpression;
            var right = call.Arguments[0] as ConstantExpression;
            if (right!.Value == null) throw new NotSupportedException();
            if (left!.Type != typeof(string) || right!.Type != typeof(string)) throw new NotSupportedException();
            if (left.NodeType != ExpressionType.MemberAccess) throw new NotSupportedException();
            if (right.NodeType != ExpressionType.Constant) throw new NotSupportedException();

            var builder = new StringBuilder();

            builder.Append("{");
            builder.Append(VisitProperty(left));
            builder.Append(":{\"$regex\":");
            builder.Append(JsonConvert.ToString($".*{Regex.Escape(right.Value!.ToString()!)}.*"));
            builder.Append("}");
            builder.Append("}");
            return builder.ToString();
        }
    }
}