using System;
using System.Data;
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


        public override Expression? Visit(Expression? node)
        {
            return base.Visit(node);
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
             
                _builder.AppendLine("{");
                _builder.AppendLine("$match:{");
                dynamic condition = (node.Arguments[1] as UnaryExpression)!.Operand;
                string leftProperty = condition.Body.Left.Member.Name;
                leftProperty = ToCamelCase(leftProperty);
                if (leftProperty == "id") leftProperty = "_id";
                ConstantExpression rightConst = condition.Body.Right;
                if (rightConst.Type == typeof(string))
                {
                    _builder.Append($"{leftProperty}:\"{rightConst.Value}\"");
                }
                else if (rightConst.Type == typeof(int))
                {
                    _builder.Append($"{leftProperty}:{rightConst.Value}");
                }
                else
                {
                    throw new NotSupportedException();
                }
                _builder.AppendLine("}");
                _builder.AppendLine("}");
                return node;
            }
            else
            {
                throw new NotSupportedException("only support where");
            }
         
        }
        
        private string ToCamelCase(string s)
        {
            if (s == null) return null;
            if (s == string.Empty) return s;
            return s.Substring(0, 1).ToLower() + s.Substring(1);
        }

        public string Build()
        {
            // return "[{\n    $match:{\n        name:\"bbb\"\n    }\n}]";
            var pipeline = $"[{_builder}]";
            return pipeline;
        }
    }
}