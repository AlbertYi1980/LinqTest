using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Newtonsoft.Json;

namespace MongoLinqs.Pipelines
{
    public class ValueExpressionVisitor
    {
        private readonly StringBuilder _builder;

        public ValueExpressionVisitor(StringBuilder builder)
        {
            _builder = builder;
        }


        public void Visit(Expression expression, bool multipleParams)
        {
            VisitRecursive(expression, multipleParams);
        }

        private void VisitRecursive(Expression expression, bool multipleParams)
        {
            var kind = DetermineKind(expression);
            switch (kind)
            {
                case ValueKind.Chain:
                    _builder.Append("\"$");
                    VisitChain(expression, multipleParams);
                    _builder.Append("\"");
                    break;
                case ValueKind.Constant:
                    var constant = (ConstantExpression)expression;
                    _builder.Append(JsonConvert.SerializeObject(constant.Value));
                    break;
                case ValueKind.Formula:
                    VisitFormula(expression, multipleParams);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void VisitChain(Expression expression, bool multipleParams)
        {
            if (expression is MemberExpression member)
            {
                VisitChain(member.Expression, multipleParams);
                _builder.Append($".{member.Member.Name}");
            }
            else if (expression is ParameterExpression parameter)
            {
                _builder.Append(multipleParams ? $"$ROOT.{parameter.Name}" : "$ROOT");
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private ValueKind DetermineKind(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Constant)
            {
                return ValueKind.Constant;
            }

            var current = expression;
            while (current is MemberExpression member)
            {
                current = member.Expression;
            }

            if (current is ParameterExpression)
            {
                return ValueKind.Chain;
            }

            return ValueKind.Formula;
        }

        private enum ValueKind
        {
            Chain,
            Constant,
            Formula,
        }

        private static readonly Dictionary<ExpressionType, string> BinaryOperators = new()
        {
            { ExpressionType.AndAlso, "$and" },
            { ExpressionType.OrElse, "$or" },
            { ExpressionType.Equal, "$eq" }, 
            { ExpressionType.NotEqual, "$ne" },
        };

        private void VisitFormula(Expression expression, bool multipleParams)
        {
            if (expression is BinaryExpression binary)
            {
                var operatorFound = BinaryOperators.TryGetValue(binary.NodeType, out var @operator);
                if (!operatorFound)
                {
                    throw new NotSupportedException();
                }

                VisitBinary(binary, @operator, multipleParams);
            }
            else if (expression is NewExpression @new)
            {
                VisitNew(@new, multipleParams);
            } else if (expression is MemberInitExpression memberInit)
            {
                VisitMemberInit(memberInit, multipleParams);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private void VisitBinary(BinaryExpression binary, string @operator, bool multipleParams)
        {
            _builder.Append("{");
            _builder.Append($"{@operator}:[");
            VisitRecursive(binary.Left, multipleParams);
            _builder.Append(",");
            VisitRecursive(binary.Right, multipleParams);
            _builder.Append("]");
            _builder.Append("}");
        }

        private void VisitNew(NewExpression @new, bool multipleParams)
        {
            _builder.Append("{");
            var length = @new.Arguments.Count;
            for (var i = 0; i < length; i++)
            {
                if (i > 0)
                {
                    _builder.Append(",");
                }
                var name = @new.Constructor!.GetParameters()[i].Name;
                _builder.Append($"\"{name}\":");
                VisitRecursive(@new.Arguments[i], multipleParams);
            }

            _builder.Append("}");
        }
        
        private void VisitMemberInit(MemberInitExpression memberInit, bool multipleParams)
        {
            _builder.Append("{");
            var length = memberInit.Bindings.Count;
            for (var i = 0; i < length; i++)
            {
                if (i > 0)
                {
                    _builder.Append(",");
                }
                var assignment = (MemberAssignment) memberInit.Bindings[i];
                var name = assignment.Member.Name;
                _builder.Append($"\"{name}\":");
                VisitRecursive(assignment.Expression, multipleParams);
            }

            _builder.Append("}");
        }
    }
}