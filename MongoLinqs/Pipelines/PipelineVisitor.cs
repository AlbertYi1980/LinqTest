using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using MongoLinqs.Pipelines.Utils;
using Newtonsoft.Json;

namespace MongoLinqs.Pipelines
{
    public class PipelineVisitor
    {
        private readonly StringBuilder _builder;

        private string _collection;

        private readonly ValueExpressionVisitor _valueExpressionVisitor;

        public PipelineVisitor(Expression expression)
        {
            _builder = new StringBuilder();
            _valueExpressionVisitor = new ValueExpressionVisitor(_builder);
     
       
            _builder.Append("[");
            VisitQuery(expression);
            _builder.Append("]");
      
        }


        public string Collection => _collection;

        public string Pipeline => Format(_builder.ToString());

    


        private static string Format(string json)
        {
            using var stringReader = new StringReader(json);
            using var stringWriter = new StringWriter();
            var jsonReader = new JsonTextReader(stringReader);
            var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented , };
            jsonWriter.WriteToken(jsonReader);
            return stringWriter.ToString();
        }

        private static LambdaExpression GetLambda(Expression expression)
        {
            var unary = (UnaryExpression)expression;
            return (LambdaExpression)unary.Operand;
        }

        private void VisitQuery(Expression expression)
        {
            if (expression.Type.GetGenericTypeDefinition() == typeof(MongoDbSet<>))
            {
                _collection = expression.Type.GetGenericArguments().First().Name;
            }
            else if(expression is MethodCallExpression methodCall)
            {
                    VisitLinqMethod(methodCall);
            }
            else
            {
                throw new NotSupportedException();
            }
        
        }

        #region Linq Method

        private void VisitLinqMethod(MethodCallExpression expression)
        {
            switch (expression.Method.Name)
            {
                case "Where":
                    VisitWhere(expression);
                    break;
                case "Select":
                    VisitSelect(expression);
                    break;
                case "SelectMany":
                    VisitSelectMany(expression);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void VisitWhere(MethodCallExpression node)
        {
            VisitQuery(node.Arguments[0]);

         
            _builder.Append("{$match:{$expr:");

            VisitLambda(GetLambda(node.Arguments[1]));

            _builder.Append("}},");
           
        }

        private void VisitSelect(MethodCallExpression node)
        {
            VisitQuery(node.Arguments[0]);
           
            var temp = NameHelper.GetTempField();
            _builder.Append("{$project:{\"" + temp + "\":");
            VisitLambda(GetLambda(node.Arguments[1]));
            _builder.Append("}}");
            _builder.Append(",");
            _builder.Append("{\"$replaceRoot\":{\"newRoot\":\"$" + temp + "\"}},");
        
        }

        private void VisitSelectMany(MethodCallExpression node)
        {
            var source = node.Arguments[0];
            var collectionSelector = GetLambda(node.Arguments[1]);
            var resultSelector = GetLambda(node.Arguments[2]);
            var first = resultSelector!.Parameters[0].Name;
            var second = resultSelector.Parameters[1].Name;
            VisitQuery(source);
       

            var innerVisitor = new PipelineVisitor(collectionSelector.Body);

            var innerCollection = innerVisitor.Collection;
            var @as = NameHelper.GetTempField();
            _builder.Append("{$lookup:{from:\"" + innerCollection + "\",");
            _builder.Append($"pipeline:{innerVisitor.Pipeline},");
            _builder.Append("as:\"" + @as + "\"");
            _builder.Append("}}");
     
            _builder.Append(",");
            _builder.Append("{$unwind:\"" + @as + "\"}");
    
            _builder.Append(",");
            _builder.Append("{$project:{\"" + first + "\":\"$$ROOT\",\"" + second + "\":\"" + @as + "\"}}");
    
            _builder.Append(",");
            _builder.Append($@"  {{
                    ""$project"":{{
                        ""_id"":false,
                        ""{first}.{@as}"": false
                    }}
                }},");

        }

        #endregion

        #region Db Set

        #endregion

        #region Lambda

        private void VisitLambda(LambdaExpression expression)
        {
            _valueExpressionVisitor.Visit(expression.Body, expression.Parameters.Count > 1);
        }

        #endregion
    }
}