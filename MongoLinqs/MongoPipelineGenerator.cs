using System;
using System.Collections.Generic;
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
   
        private string _startAt;
        private List<string> _steps;

        public MongoPipelineGenerator()
        {
         
            _steps = new List<string>();
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type.GetGenericTypeDefinition() == typeof(MongoDbSet<>))
            {
                VisitStartCollection(node);
                return node;
            }

            throw BuildException(node);
        }

        private void VisitStartCollection(ConstantExpression node)
        {
            _startAt ??= ToCamelCase(node.Type.GenericTypeArguments[0].Name);
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

            if (node.Method.Name == nameof(Enumerable.SelectMany))
            {
                VisitSelectMany(node);
                return node;
            }

            throw new NotSupportedException($"method {node.Method.Name} is not supported.");
        }

        private void VisitSelectMany(MethodCallExpression node)
        {
            var pc = node.Method.GetParameters().Count();
            if (pc == 2)
            {
                throw BuildException(node);
            }
            else if (pc == 3)
            {
                var source = node.Arguments[0];
                Visit(source);
                var collectionSelector = (node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression;
                var secondarySetEx = collectionSelector!.Body;

                if (secondarySetEx.Type.GetGenericTypeDefinition() != typeof(MongoDbSet<>))
                {
                    throw BuildException(node);
                }


                var attached = ToCamelCase(secondarySetEx.Type.GenericTypeArguments[0].Name);
                var resultSelector = (node.Arguments[2] as UnaryExpression)!.Operand as LambdaExpression;
                var first = resultSelector!.Parameters[0].Name;
                var second = resultSelector.Parameters[1].Name;
                var temp = $"f_{Guid.NewGuid():n}";
                var script = $@"
                {{
                    ""$lookup"": {{
                        ""from"": ""{attached}"",
                        ""pipeline"": [

                        ],
                        ""as"": ""{temp}""
                    }}
                }},
                {{
                    ""$unwind"": ""${temp}""
                }},
                {{
                    ""$project"":{{
                        ""{first}"" : ""$$ROOT"",
                        ""{second}"":""${temp}""
                    }}
                }},
                {{
                    ""$project"":{{
                        ""_id"":false,
                        ""{first}.{temp}"": false
                    }}
                }}
                ";
                _steps.Add(script);
            }
            else
            {
                throw BuildException(node);
            }
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

            


            if (body is NewExpression)
            {
                var builder = new StringBuilder();
                builder.Append("{\"$project\":{");
                builder.Append( ProcessSelectNew(body));
                builder.Append("}}");
                _steps.Add(builder.ToString());
            }
            else if (body is MemberExpression)
            {
               _steps.Add( ProcessSelectMember(body));
            }
            else
            {
                throw BuildException(node);
            }
        }

        private string ProcessSelectMember(Expression body)
        {
            var builder = new StringBuilder();
            var member = body as MemberExpression;
            var memberPath = GetMemberPath(member);
            var script = @" {{
                ""$replaceRoot"":{{
                ""newRoot"":""${0}""
            }}
            }}";
            builder.AppendFormat(script, memberPath);
            return builder.ToString();
        }

        private string ProcessSelectNew(Expression body)
        {
            var sb = new StringBuilder();
            var newExp = body as NewExpression;
            var length = newExp!.Constructor!.GetParameters().Length;
            sb.Append("\"_id\":false");
            for (int i = 0; i < length; i++)
            {
                sb.Append(",");

                sb.Append('"' + ToCamelCase(newExp.Members![i].Name) + '"');
                sb.Append(":");
                var arg = newExp.Arguments[i] as MemberExpression;

              sb.Append(  VisitProperty(arg, true)) ;
            }

            return sb.ToString();
        }

        private string VisitCondition(Expression node)
        {
            var binary = node as BinaryExpression;
            var constant = node as ConstantExpression;
            var unary = node as UnaryExpression;
            
            var builder = new StringBuilder();

            switch (node.NodeType)
            {
                case ExpressionType.AndAlso:
                    builder.Append("{\"$and\":[");
                    builder.Append( VisitCondition(binary!.Left));
                    builder.Append(",");
                    builder.Append(VisitCondition(binary!.Right));
                    builder.Append("]}");
                    break;
                case ExpressionType.OrElse:
                    builder.Append("{\"$or\":[");
                    builder.Append( VisitCondition(binary!.Left));
                    builder.Append(",");
                    builder.Append(VisitCondition(binary!.Right));
                    builder.Append("]}");
                    break;
                case ExpressionType.Equal:
                    builder.Append("{");
                    builder.Append(VisitProperty(binary!.Left));
                    builder.Append(":{\"$eq\":");
                    builder.Append(VisitCondition(binary!.Right));
                    builder.Append("}}");
                    break;
                case ExpressionType.NotEqual:
                    builder.Append("{");
                    builder.Append(VisitProperty(binary!.Left));
                    builder.Append(":{\"$ne\":");
                    builder.Append(VisitCondition(binary!.Right));
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
                    builder.Append(VisitProperty(node));
                    builder.Append(":{\"$eq\":");
                    builder.Append("true");
                    builder.Append("}}");
                    break;

                case ExpressionType.Constant:
                    var value = constant!.Value;
                    if (node.Type == typeof(int))
                    {
                        builder.Append(value);
                    }
                    else if (node.Type == typeof(int?))
                    {
                        var s = value == null ? "null" : value.ToString();
                        builder.Append(s);
                    }
                    else if (node.Type == typeof(string))
                    {
                        var s = value == null ? "null" : JsonConvert.ToString(value);
                        builder.Append(s);
                    }
                    else
                    {
                        throw new NotSupportedException($"not support const type {node.Type.Name}");
                    }


                    break;
                case ExpressionType.Call:
                   builder.Append(  VisitSpecialCondition(node));
                    break;
                default:

                    throw BuildException(node);
            }

            return builder.ToString();
        }

        private string VisitProperty(Expression node, bool isPlaceHolder = false)
        {
            var member = node as MemberExpression;
            var builder = new StringBuilder();
            if (isPlaceHolder)
            {
                builder.Append($"\"${GetMemberPath(member)}\"");
            }
            else
            {
                builder.Append($"\"{GetMemberPath(member)}\"");
            }
            return builder.ToString();
        }

        private string VisitSpecialCondition(Expression node)
        {
            var call = node as MethodCallExpression;
            if (call!.Method.Name != nameof(string.Contains)) throw BuildException(node);
            var left = call.Object as MemberExpression;
            var right = call.Arguments[0] as ConstantExpression;
            if (right!.Value == null) throw BuildException(node);
            if (left!.Type != typeof(string) || right!.Type != typeof(string)) throw BuildException(node);
            if (left.NodeType != ExpressionType.MemberAccess) throw BuildException(node);
            if (right.NodeType != ExpressionType.Constant) throw BuildException(node);

            var builder = new StringBuilder();

            builder.Append("{");
            VisitProperty(left);
            builder.Append(":{\"$regex\":");
            builder.Append(JsonConvert.ToString($".*{Regex.Escape(right.Value!.ToString()!)}.*"));
            builder.Append("}");
            builder.Append("}");
            return builder.ToString();
        }

        private static NotSupportedException BuildException(Expression node)
        {
            return new NotSupportedException($"{node} is not supported.");
        }

        private static string GetMemberPath(MemberExpression member)
        {
            var list = new List<string>();
            var current = member;
            do
            {
                list.Insert(0, FixMemberName(ToCamelCase(current.Member.Name)));
                current = current.Expression as MemberExpression;
            } while (current != null);

            var path = string.Join(".", list);
            return path;
        }

        private static string FixMemberName(string memberName)
        {
            return memberName == "id" ? "_id" : memberName;
        }

        private void VisitWhere(MethodCallExpression node)
        {
            var source = node.Arguments[0];
            Visit(source);
           
            var builder = new StringBuilder();

            builder.Append("{");
            builder.Append("\"$match\":");
            var lambda = (node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression;
            var body = lambda!.Body;
            builder.Append( VisitCondition(body));
            builder.Append("}");
            _steps.Add(builder.ToString());
        }

        private static string ToCamelCase(string s)
        {
            if (s == null) return null;
            if (s == string.Empty) return s;
            return s.Substring(0, 1).ToLower() + s.Substring(1);
        }

        public MongoPipelineResult Build()
        {
            var pipeline = $"[{string.Join(",", _steps)}]"; 
            pipeline = FormatPipeline(pipeline);
            Console.WriteLine();
            Console.WriteLine("Pipe line:");
            Console.WriteLine(pipeline);
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