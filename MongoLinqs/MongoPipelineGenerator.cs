using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

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

        private  void ProcessCondition(Expression node, Expression param)
        {
            var binary = node as BinaryExpression;
            var constant = node as ConstantExpression;
            var member = node as MemberExpression;
            switch (node.NodeType)
            {
                case ExpressionType.AndAlso:
                    _builder.Append("{\"$and\":[");
                    ProcessCondition( binary!.Left, param);
                    _builder.Append(",");
                    ProcessCondition( binary!.Right, param);
                    _builder.Append("]}");
                    break;
                case ExpressionType.OrElse:
                    _builder.Append("{\"$or\":[");
                    ProcessCondition( binary!.Left, param);
                    _builder.Append(",");
                    ProcessCondition( binary!.Right, param);
                    _builder.Append("]}");
                    break;
                case ExpressionType.Not:
                    break;
                case ExpressionType.Equal:
                    _builder.Append("{");
                    ProcessCondition( binary!.Left, param);
                    _builder.Append(":{\"$eq\":");
                    ProcessCondition( binary!.Right, param);
                    _builder.Append("}}");
                    break;
                case ExpressionType.NotEqual:
                    _builder.Append("{");
                    ProcessCondition( binary!.Left, param);
                    _builder.Append(":{\"$ne\":");
                    ProcessCondition( binary!.Right, param);
                    _builder.Append("}}");
                    break;
                case ExpressionType.Constant:
                    if (node.Type == typeof(int))
                    {
                        _builder.Append(constant!.Value);
                    }
                    else if (node.Type == typeof(string))
                    {
                          _builder.Append($"\"{constant!.Value}\"");
                    }
                    else
                    {
                        throw new NotSupportedException($"not support const type {node.Type.Name}");
                    }
                    break;
                case ExpressionType.MemberAccess:
                    if (member!.Expression != param)
                    {
                        throw new NotSupportedException("property should belong to lambda parameter");
                    }

                    _builder.Append($"\"{ToCamelCase( member.Member.Name)}\"");
                    break;
                default:
             
                    throw new NotSupportedException($"{node} is not supported." );
                    break;
            }
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

        private string ToCamelCase(string s)
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