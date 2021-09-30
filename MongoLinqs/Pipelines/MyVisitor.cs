using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MongoLinqs.Pipelines
{
    public class MyVisitor
    {
        private readonly StringBuilder _builder;
        private List<string> _indexes;

        public MyVisitor(Expression expression)
        {
            _builder = new StringBuilder();
            _indexes = new List<string>();
            MainCollection = null;
            VisitQuery(expression);
        }

        public string MainCollection { get; }

        public string Pipeline => _builder.ToString();

        private void VisitQuery(Expression expression)
        {
            switch (expression)
            {
                case MethodCallExpression methodCall:
                    VisitLinqMethod(methodCall);
                    break;
                case ConstantExpression constant:
                    VisitDbSet(constant);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        #region Linq Method

        private void VisitLinqMethod(MethodCallExpression expression)
        {
            switch (expression.Method.Name)
            {
                case "Where":
                    break;
                case "Select":
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
        
        private void VisitWhere(MethodCallExpression node)
        {
            var source = node.Arguments[0];
            VisitQuery(source);

            var builder = new StringBuilder();

            builder.Append("{");
            builder.Append("\"$match\":");
            var lambda = (node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression;

            var result = new LambdaBodyBuilder(lambda!.Body, lambda.Parameters.Count > 1).Build(true, true);
            builder.Append(result);
            builder.Append("}");
            _builder.Append(builder.ToString());
        }

        #endregion

        public void VisitDbSet(ConstantExpression expression)
        {
            
        }
    }
}