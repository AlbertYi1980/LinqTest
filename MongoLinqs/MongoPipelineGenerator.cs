using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using MongoLinqs.Conditions;
using MongoLinqs.Grouping;
using MongoLinqs.Selectors;
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

       

        private void VisitGroupBy(MethodCallExpression node)
        {
            var pc = node.Method.GetParameters().Count();
            if (pc < 2)
            {
                throw BuildException(node);
            }
            else if ( pc == 2)
            {
                var source = node.Arguments[0];
                Visit(source);
                var keySelector = (node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression;
                var keySelectorScript = new SelectorBuilder(keySelector).Build();
               
                
                var script = $@"
                {{
                    ""$group"": {{
                        ""_id"": {keySelectorScript.Script},
                        ""{GroupHelper.GroupElements}"":{{""$push"":""$$ROOT""}},
                    }}
                }}
                ";
                _steps.Add(script);
            }
            else if ( pc == 3)
            {
                var source = node.Arguments[0];
                Visit(source);
                var keySelector = (node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression;
                var elementSelector =  (node.Arguments[2] as UnaryExpression)!.Operand as LambdaExpression;
                var keySelectorScript = new SelectorBuilder(keySelector).Build();
                var elementSelectorScript = new SelectorBuilder(elementSelector).Build();
                
                var script = $@"
                {{
                    ""$group"": {{
                        ""_id"": {keySelectorScript.Script},
                        ""{GroupHelper.GroupElements}"":{{""$push"":{elementSelectorScript.Script}}},
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
            var selectorBuilder = new SelectorBuilder(selector);
            var result = selectorBuilder.Build();
            if (result.Kind == SelectorResultKind.Root)
            {
                return;
            }
            
            if (result.Kind == SelectorResultKind.Member )
            {
                var temp = $"f_{Guid.NewGuid():n}";
                _steps.Add($"{{\"$project\":{{\"{temp}\":{result.Script}}}}}");
                _steps.Add($"{{\"$replaceRoot\":{{\"newRoot\":\"${temp}\"}}}}");
                return;
            }
            
            if (result.Kind == SelectorResultKind.Constant )
            {
                var temp = $"f_{Guid.NewGuid():n}";
                _steps.Add($"{{\"$project\":{{\"{temp}\":{result.Script}}}}}");
                _steps.Add($"{{\"$replaceRoot\":{{\"newRoot\":\"${temp}\"}}}}");
                return;
            }

            if (result.Kind == SelectorResultKind.New)
            {
                _steps.Add($"{{\"$project\":{result.Script}}}");
                return;
            }
            throw BuildException(node);
        }


        private static NotSupportedException BuildException(Expression node)
        {
            return new NotSupportedException($"{node} is not supported.");
        }


        private void VisitWhere(MethodCallExpression node)
        {
            var source = node.Arguments[0];
            Visit(source);

            var builder = new StringBuilder();

            builder.Append("{");
            builder.Append("\"$match\":");
            var lambda = (node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression;
            
            var result = new ConditionBuilder(lambda).Build();
            builder.Append(result);
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