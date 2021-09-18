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
        private readonly List<string> _steps;

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
            _startAt ??= NameHelper.ToCamelCase(node.Type.GenericTypeArguments[0].Name);
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

            if (node.Method.Name == nameof(Enumerable.Join))
            {
                VisitJoin(node);
                return node;
            }
            
            if (node.Method.Name == nameof(Enumerable.GroupBy))
            {
                VisitGroupBy(node);
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


                var attached = NameHelper.ToCamelCase(secondarySetEx.Type.GenericTypeArguments[0].Name);
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

        private void VisitJoin(MethodCallExpression node)
        {
            var pc = node.Method.GetParameters().Count();
            if (pc < 5)
            {
                throw BuildException(node);
            }
            else if (pc == 5)
            {
                var outer = node.Arguments[0];
                Visit(outer);
                var inner = node.Arguments[1];
                if (inner!.Type.GetGenericTypeDefinition() != typeof(MongoDbSet<>))
                {
                    throw BuildException(node);
                }


                var attached = NameHelper.ToCamelCase(inner.Type.GenericTypeArguments[0].Name);
                var outerKeySelector = (node.Arguments[2] as UnaryExpression)!.Operand as LambdaExpression;
                var outerKey = NameHelper.FixMemberName(NameHelper.ToCamelCase((outerKeySelector!.Body as MemberExpression)!.Member.Name));
                var innerKeySelector = (node.Arguments[3] as UnaryExpression)!.Operand as LambdaExpression;
                var innerKey = NameHelper.FixMemberName(NameHelper.ToCamelCase((innerKeySelector!.Body as MemberExpression)!.Member.Name));
                var resultSelector = (node.Arguments[4] as UnaryExpression)!.Operand as LambdaExpression;
                var first = resultSelector!.Parameters[0].Name;
                var second = resultSelector.Parameters[1].Name;
                var temp = $"f_{Guid.NewGuid():n}";
                var script = $@"
                {{
                    ""$lookup"": {{
                        ""from"": ""{attached}"",
                        ""localField"": ""{outerKey}"",
                        ""foreignField"": ""{innerKey}"",  
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

        private const string GroupElements = "f_94af22dbce8645b6a8c97cc2f28a9fc7";

        private void VisitGroupBy(MethodCallExpression node)
        {
            var pc = node.Method.GetParameters().Count();
            if (pc < 3)
            {
                throw BuildException(node);
            }
            else if (pc == 3)
            {
                var source = node.Arguments[0];
                Visit(source);
                var keySelector = (node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression;
                var elementSelector = (node.Arguments[2] as UnaryExpression)!.Operand as LambdaExpression;

                var attached = NameHelper.ToCamelCase(keySelector.Type.GenericTypeArguments[0].Name);
                var outerKeySelector = (node.Arguments[2] as UnaryExpression)!.Operand as LambdaExpression;
                var outerKey = NameHelper.FixMemberName(NameHelper.ToCamelCase((outerKeySelector!.Body as MemberExpression)!.Member.Name));
                var innerKeySelector = (node.Arguments[3] as UnaryExpression)!.Operand as LambdaExpression;
                var innerKey = NameHelper.FixMemberName(NameHelper.ToCamelCase((innerKeySelector!.Body as MemberExpression)!.Member.Name));
                var resultSelector = (node.Arguments[4] as UnaryExpression)!.Operand as LambdaExpression;
                var first = resultSelector!.Parameters[0].Name;
                var second = resultSelector.Parameters[1].Name;
                var temp = $"f_{Guid.NewGuid():n}";
                var script = $@"
                {{
                    ""$lookup"": {{
                        ""from"": ""{attached}"",
                        ""localField"": ""{outerKey}"",
                        ""foreignField"": ""{innerKey}"",  
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
            var selectorBuilder = new MongoSelectorBuilder(selector);
            var result = selectorBuilder.Build();
            if (result.Kind == MongoSelectorResultKind.Root)
            {
                return;
            }
            
            if (result.Kind == MongoSelectorResultKind.Member )
            {
                var temp = $"f_{Guid.NewGuid():n}";
                _steps.Add($"{{\"$project\":{{\"{temp}\":\"{result.Script}\"}}}}");
                _steps.Add($"{{\"$replaceRoot\":{{\"newRoot\":\"${temp}\"}}}}");
                return;
            }
            //
            // if (result.Kind == MongoSelectorResultKind.Constant )
            // {
            //     var temp = $"f_{Guid.NewGuid():n}";
            //     _steps.Add($"{{\"$project\":{{\"{temp}\":{result.Script}}}}}");
            //     _steps.Add($"{{\"$replaceRoot\":{{\"newRoot\":\"${temp}\"}}}}");
            //     return;
            // }

            if (result.Kind == MongoSelectorResultKind.New)
            {
                _steps.Add($"{{\"$project\":{{{result.Script}}}}}");
                return;
            }
            throw BuildException(node);
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
                    builder.Append(VisitCondition(binary!.Left));
                    builder.Append(",");
                    builder.Append(VisitCondition(binary!.Right));
                    builder.Append("]}");
                    break;
                case ExpressionType.OrElse:
                    builder.Append("{\"$or\":[");
                    builder.Append(VisitCondition(binary!.Left));
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
                    builder.Append(VisitSpecialCondition(node));
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
                list.Insert(0, NameHelper.FixMemberName(NameHelper.ToCamelCase(current.Member.Name)));
                current = current.Expression as MemberExpression;
            } while (current != null);

            var path = string.Join(".", list);
            return path;
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
            builder.Append(VisitCondition(body));
            builder.Append("}");
            _steps.Add(builder.ToString());
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

  
}