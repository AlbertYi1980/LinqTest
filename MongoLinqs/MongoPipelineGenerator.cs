using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MongoLinqs
{
    public class MongoPipelineGenerator : ExpressionVisitor
    {
        private readonly StringBuilder _builder;
        private string _startAt;
        private bool _firstStep = true;

        public MongoPipelineGenerator()
        {
            _builder = new StringBuilder();
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type.GetGenericTypeDefinition() == typeof(MongoDbSet<>))
            {
                _startAt ??= node.Type.GenericTypeArguments[0].Name;
                return node;
            }

            throw BuildException(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == nameof(Enumerable.Where))
            {
                VisitWhere(node);
                return node;
            }

            if (node.Method.Name == nameof(Enumerable.Select))
            {
                VisitSelect(node);
                return node;
            }

            throw new NotSupportedException("only support where");
        }

        private void VisitSelect(MethodCallExpression node)
        {
            var source = node.Arguments[0];
            Visit(source);
            var selector = (node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression;
            var param = selector!.Parameters[0];
            var body = selector.Body;
            if (param == body)
            {
                return;
            }

            if (!_firstStep)
            {
                _firstStep = true;
                _builder.Append(",");
            }

            _builder.Append("{\"$project\":{");
            var newExp = body as NewExpression;
            var length = newExp!.Constructor!.GetParameters().Length;
            _builder.Append("\"_id\":false");
            for (int i = 0; i < length; i++)
            {
                _builder.Append(",");

                _builder.Append('"' + ToCamelCase(newExp.Members![i].Name) + '"');
                _builder.Append(":");
                var arg = newExp.Arguments[i] as MemberExpression;

                VisitProperty(arg, param, true);
            }

            _builder.Append("}}");
        }

        private void VisitCondition(Expression node, Expression param)
        {
            var binary = node as BinaryExpression;
            var constant = node as ConstantExpression;
            var unary = node as UnaryExpression;


            switch (node.NodeType)
            {
                case ExpressionType.AndAlso:
                    _builder.Append("{\"$and\":[");
                    VisitCondition(binary!.Left, param);
                    _builder.Append(",");
                    VisitCondition(binary!.Right, param);
                    _builder.Append("]}");
                    break;
                case ExpressionType.OrElse:
                    _builder.Append("{\"$or\":[");
                    VisitCondition(binary!.Left, param);
                    _builder.Append(",");
                    VisitCondition(binary!.Right, param);
                    _builder.Append("]}");
                    break;
                case ExpressionType.Equal:
                    _builder.Append("{");
                    VisitProperty(binary!.Left, param);
                    _builder.Append(":{\"$eq\":");
                    VisitCondition(binary!.Right, param);
                    _builder.Append("}}");
                    break;
                case ExpressionType.NotEqual:
                    _builder.Append("{");
                    VisitProperty(binary!.Left, param);
                    _builder.Append(":{\"$ne\":");
                    VisitCondition(binary!.Right, param);
                    _builder.Append("}}");
                    break;
                case ExpressionType.Not:
                    _builder.Append("{");
                    VisitProperty(unary!.Operand, param);
                    _builder.Append(":{\"$ne\":");
                    _builder.Append("true");
                    _builder.Append("}}");
                    break;
                case ExpressionType.MemberAccess:
                    _builder.Append("{");
                    VisitProperty(node, param);
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
                    VisitSpecialCondition(node, param);
                    break;
                default:

                    throw BuildException(node);
            }
        }

        private void VisitProperty(Expression node, Expression param, bool isPlaceHolder = false)
        {
            var member = node as MemberExpression;
            if (member!.Expression != param)
            {
                throw new NotSupportedException("property should belong to lambda parameter");
            }

            if (isPlaceHolder)
            {
                _builder.Append($"\"${GetMemberName(member)}\"");
            }
            else
            {
                _builder.Append($"\"{GetMemberName(member)}\"");
            }
        }

        private void VisitSpecialCondition(Expression node, Expression param)
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
            VisitProperty(left, param);
            _builder.Append(":{\"$regex\":");
            _builder.Append(JsonConvert.ToString($".*{Regex.Escape(right.Value!.ToString()!)}.*"));
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

        private void VisitWhere(MethodCallExpression node)
        {
            var source = node.Arguments[0];
            Visit(source);
            if (_firstStep)
            {
                _firstStep = false;
            }
            else
            {
                _builder.Append(",");
            }

            _builder.Append("{");
            _builder.Append("\"$match\":");
            var lambda = (node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression;
            var body = lambda!.Body;
            var parameter = lambda.Parameters[0];
            VisitCondition(body, parameter);
            _builder.Append("}");
        }

        private static string ToCamelCase(string s)
        {
            if (s == null) return null;
            if (s == string.Empty) return s;
            return s.Substring(0, 1).ToLower() + s.Substring(1);
        }

        public MongoPipelineResult Build()
        {
            var pipeline = $"[{_builder}]";
            Console.WriteLine();
            Console.WriteLine("Pipe line:");
            Console.WriteLine(FormatPipeline(pipeline));
            Console.WriteLine();
            return new MongoPipelineResult()
            {
                StartAt = _startAt,
                Pipeline = pipeline,
            };
        }

        private string FormatPipeline(string pipeline)
        {
            var jArray = JsonConvert.DeserializeObject<JArray>(pipeline);
            return JsonConvert.SerializeObject(jArray, new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented
            });
        }
    }

    public class MongoPipelineResult
    {
        public string StartAt { get; set; }
        public string Pipeline { get; set; }
    }
}