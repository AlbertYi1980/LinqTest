using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace MongoLinqs
{
    public class MongoPipelineGenerator : ExpressionVisitor
    {
        private readonly StringBuilder _builder;

        public MongoPipelineGenerator()
        {
            _builder = new StringBuilder();
        }


        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type.IsGenericType && node.Type.GetGenericTypeDefinition() == typeof(MongoDbSet<>))
            {
                var elementType = node.Type.GenericTypeArguments.First();
            }

            return base.VisitConstant(node);
        }


        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == nameof(Enumerable.Where))
            {
                ProcessWhere(node);
                return node;
            }
            else
            {
                throw new NotSupportedException("only support where");
            }
        }

        private void ProcessCondition(Expression node, Expression param)
        {
            var binary = node as BinaryExpression;
            var constant = node as ConstantExpression;
            var unary = node as UnaryExpression;
     
            
            switch (node.NodeType)
            {
                case ExpressionType.AndAlso:
                    _builder.Append("{\"$and\":[");
                    ProcessCondition(binary!.Left, param);
                    _builder.Append(",");
                    ProcessCondition(binary!.Right, param);
                    _builder.Append("]}");
                    break;
                case ExpressionType.OrElse:
                    _builder.Append("{\"$or\":[");
                    ProcessCondition(binary!.Left, param);
                    _builder.Append(",");
                    ProcessCondition(binary!.Right, param);
                    _builder.Append("]}");
                    break;
                case ExpressionType.Equal:
                    _builder.Append("{");
                    ProcessProperty(binary!.Left, param);
                    _builder.Append(":{\"$eq\":");
                    ProcessCondition(binary!.Right, param);
                    _builder.Append("}}");
                    break;
                case ExpressionType.NotEqual:
                    _builder.Append("{");
                    ProcessProperty(binary!.Left, param);
                    _builder.Append(":{\"$ne\":");
                    ProcessCondition(binary!.Right, param);
                    _builder.Append("}}");
                    break;
                case ExpressionType.Not:
                    _builder.Append("{");
                    ProcessProperty(unary!.Operand, param);
                    _builder.Append(":{\"$ne\":");
                    _builder.Append("true");
                    _builder.Append("}}");
                    break;
                case ExpressionType.MemberAccess:
                    _builder.Append("{");
                    ProcessProperty(node, param);
                    _builder.Append(":{\"$eq\":");
                    _builder.Append("true");
                    _builder.Append("}}");
                    break;
                    
                case ExpressionType.Constant:
                    var value = constant!.Value;
                    if (node.Type == typeof(int))
                    {
                        _builder.Append(value);
                    }
                    else if (node.Type == typeof(int?))
                    {
                        var s = value == null ? "null" : value.ToString();
                        _builder.Append(s);
                    }
                    else if (node.Type == typeof(string))
                    {
                        var s = value == null ? "null" : JsonConvert.ToString(value);
                        _builder.Append(s);
                    }
                    else
                    {
                        throw new NotSupportedException($"not support const type {node.Type.Name}");
                    }
                    
             
             
                    break;
                case ExpressionType.Call:
                    ProcessSpecialCondition(node, param);
                    break;
                default:

                    throw BuildException(node);
            }
        }

        private void ProcessProperty(Expression node, Expression param)
        {
            var member = node as MemberExpression;
            if (member!.Expression != param)
            {
                throw new NotSupportedException("property should belong to lambda parameter");
            }

            _builder.Append($"\"{GetMemberName(member)}\"");
        }

        private void ProcessSpecialCondition(Expression node, Expression param)
        {
            var call = node as MethodCallExpression;
            if (call!.Method.Name != nameof(string.Contains)) throw BuildException(node);
            var left = call.Object as MemberExpression;
            var right = call.Arguments[0] as ConstantExpression;
            if (right!.Value == null) throw BuildException(node);
            if (left!.Type != typeof(string) || right!.Type != typeof(string)) throw BuildException(node);
            if (left.NodeType != ExpressionType.MemberAccess) throw BuildException(node);
            if (right.NodeType != ExpressionType.Constant) throw BuildException(node);
        

            _builder.Append("{");
            ProcessProperty(left, param);
            _builder.Append(":{\"$regex\":");
            _builder.Append(JsonConvert.ToString( $".*{Regex.Escape( right.Value!.ToString()!)}.*"));
            _builder.Append("}");
            _builder.Append("}");
        }

        private static NotSupportedException BuildException(Expression node)
        {
            return new NotSupportedException($"{node} is not supported.");
        }

        private static string GetMemberName(MemberExpression member)
        {
            var memberName = member.Member.Name;
            memberName = ToCamelCase(memberName);
            return memberName == "id" ? "_id" : memberName;
        }

        private void ProcessWhere(MethodCallExpression node)
        {
            _builder.Append("{");
            _builder.Append("\"$match\":");
            var lambda = (node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression;
            var body = lambda!.Body;
            var parameter = lambda.Parameters[0];
            ProcessCondition(body, parameter);
            _builder.Append("}");
        }

        private static string ToCamelCase(string s)
        {
            if (s == null) return null;
            if (s == string.Empty) return s;
            return s.Substring(0, 1).ToLower() + s.Substring(1);
        }

        public string Build()
        {
            var pipeline = $"[{_builder}]";
            return pipeline;
        }
    }
}