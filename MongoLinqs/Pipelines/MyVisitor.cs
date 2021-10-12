﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using Newtonsoft.Json;

namespace MongoLinqs.Pipelines
{
    public class MyVisitor
    {
        private readonly StringBuilder _builder;
        private readonly List<int> _indexes;
        private string _mainCollection;
        private bool _inMemberChain;

        public MyVisitor(Expression expression)
        {
            _inMemberChain = false;
            _builder = new StringBuilder();
            _indexes = new List<int>();
            Push();
            _builder.Append("[");
            VisitQuery(expression);
            _builder.Append("]");
            Pop();
        }

        public string MainCollection => _mainCollection;

        public string Pipeline => Format(_builder.ToString());

        private void Push()
        {
            _indexes.Add(0);
        }

        private void Pop()
        {
            _indexes.RemoveAt(_indexes.Count - 1);
        }

        private void Next()
        {
            _indexes[^1] += 1;
        }


        private static string Format(string json)
        {
            using var stringReader = new StringReader(json);
            using var stringWriter = new StringWriter();
            var jsonReader = new JsonTextReader(stringReader);
            var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
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
                    VisitWhere(expression);
                    break;
                case "Select":
                    VisitSelect(expression);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void VisitWhere(MethodCallExpression node)
        {
            VisitQuery(node.Arguments[0]);

            if (_indexes[^1] != 0)
            {
                _builder.Append(",");
            }

            _builder.Append("{$match:{$expr:");

            VisitLambda(GetLambda(node.Arguments[1]));

            _builder.Append("}}");
            Next();
        }

        private void VisitSelect(MethodCallExpression node)
        {
            VisitQuery(node.Arguments[0]);
            if (_indexes[^1] != 0)
            {
                _builder.Append(",");
            }

            _builder.Append("{$project:{");
            _builder.Append("}}");
            Next();
        }

        #endregion

        #region Db Set

        private void VisitDbSet(ConstantExpression expression)
        {
            if (_indexes.Count == 1)
            {
                _mainCollection = "abc";
            }
        }

        #endregion

        #region Lambda

        private void VisitLambda(LambdaExpression expression)
        {
            var @params = expression.Parameters;
            VisitExpression(expression.Body);
        }

        private void VisitExpression(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                    VisitAndAlso((BinaryExpression)expression);
                    break;
                case ExpressionType.Equal:
                    VisitEqual((BinaryExpression)expression);
                    break;
                case ExpressionType.Constant:
                    VisitConstant((ConstantExpression)expression);
                    break;
                case ExpressionType.MemberAccess:
                    VisitMember((MemberExpression)expression);
                    break;
                case ExpressionType.Parameter:
                    VisitParameter((ParameterExpression)expression);
                    break;
                default:
                    _builder.Append("\"*\"");
                    break;
            }
        }

        private void VisitParameter(ParameterExpression expression)
        {
            _builder.Append("*");
        }

        private void VisitMember(MemberExpression expression)
        {
            var inMemberChain = _inMemberChain;
            _inMemberChain = true;
            if (!inMemberChain)
            {
                _builder.Append("\"$");
            }

            if (expression.Expression!.NodeType == ExpressionType.MemberAccess)
            {
                VisitMember((MemberExpression)expression.Expression);
            }
            else
            {
                _inMemberChain = false;
                VisitExpression(expression.Expression);
            }
            _builder.Append(".");
            _builder.Append(expression.Member.Name);

            if (!inMemberChain)
            {
                _builder.Append("\"");
            }
        }


        private void VisitConstant(ConstantExpression expression)
        {
            _builder.Append(JsonConvert.SerializeObject(expression.Value));
        }

        private void VisitAndAlso(BinaryExpression expression)
        {
            _builder.Append("{");
            _builder.Append("$and:[");
            VisitExpression(expression.Left);
            _builder.Append(",");
            VisitExpression(expression.Right);
            _builder.Append("]");
            _builder.Append("}");
        }

        private void VisitEqual(BinaryExpression expression)
        {
            _builder.Append("{");
            _builder.Append("$eq:[");
            VisitExpression(expression.Left);
            _builder.Append(",");
            VisitExpression(expression.Right);
            _builder.Append("]");
            _builder.Append("}");
        }

        #endregion
    }
}