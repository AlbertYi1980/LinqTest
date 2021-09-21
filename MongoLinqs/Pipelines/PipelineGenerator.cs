using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using MongoLinqs.Pipelines.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MongoLinqs.Pipelines
{
    public class PipelineGenerator : ExpressionVisitor
    {
        private readonly ILogger _logger;
        private string _startAt;
        private readonly List<string> _steps;

        public PipelineGenerator(ILogger logger)
        {
            _logger = logger;
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
            _startAt ??= NameHelper.MapCollection(node.Type.GenericTypeArguments[0].Name);
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

            if (node.Method.Name == nameof(Enumerable.GroupJoin))
            {
                VisitGroupJoin(node);
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


                var attached = NameHelper.MapCollection(secondarySetEx.Type.GenericTypeArguments[0].Name);
                var resultSelector = (node.Arguments[2] as UnaryExpression)!.Operand as LambdaExpression;
                var first = resultSelector!.Parameters[0].Name;
                var second = resultSelector.Parameters[1].Name;
                var temp = NameHelper.GetTempField();
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

                var attached = NameHelper.MapCollection(inner.Type.GenericTypeArguments[0].Name);
                var outerKeySelector = (node.Arguments[2] as UnaryExpression)!.Operand as LambdaExpression;
         
                var outerKey =new LambdaBodyBuilder(outerKeySelector!.Body,outerKeySelector.Parameters.Count > 1,false).Build();
                  
                var innerKeySelector = (node.Arguments[3] as UnaryExpression)!.Operand as LambdaExpression;
                var innerKey = new LambdaBodyBuilder(innerKeySelector!.Body, innerKeySelector.Parameters.Count > 1,false).Build();
          
                var resultSelector = (node.Arguments[4] as UnaryExpression)!.Operand as LambdaExpression;
                var resultSelectorScript = new LambdaBodyBuilder(resultSelector!.Body, resultSelector.Parameters.Count > 1).Build();
                var first = resultSelector!.Parameters[0].Name;
                var second = resultSelector.Parameters[1].Name;
                var temp = NameHelper.GetTempField();

                _steps.Add($@"
                {{
                    ""$lookup"": {{
                        ""from"": ""{attached}"",
                        ""localField"": {outerKey},
                        ""foreignField"": {innerKey},  
                        ""as"": ""{temp}""
                    }}
                }}
                ");
                _steps.Add($@"
                {{
                    ""$unwind"": ""${temp}""
                }}
                ");
                
                _steps.Add($@"
                {{
                    ""$project"":{{
                        ""_id"":false,
                        ""{first}"" : ""$$ROOT"",
                        ""{second}"":""${temp}""
                    }}
                }}
                "); 
                _steps.Add($@"
                {{
                    ""$project"":{resultSelectorScript}
                }}
                ");
          
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
            else if (pc == 2)
            {
                var source = node.Arguments[0];
                Visit(source);
                var keySelector = (node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression;
                var keySelectorScript = new LambdaBodyBuilder(keySelector!.Body, keySelector.Parameters.Count > 1).Build();


                var script = $@"
                {{
                    ""$group"": {{
                        ""_id"": {keySelectorScript},
                        ""{GroupHelper.GroupElements}"":{{""$push"":""$$ROOT""}},
                    }}
                }}
                ";
                _steps.Add(script);
            }
            else if (pc == 3)
            {
                var source = node.Arguments[0];
                Visit(source);
                var keySelector = (node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression;
                var elementSelector = (node.Arguments[2] as UnaryExpression)!.Operand as LambdaExpression;
                var keySelectorScript = new LambdaBodyBuilder(keySelector!.Body, keySelector.Parameters.Count > 1).Build();
                var elementSelectorScript = new LambdaBodyBuilder(elementSelector!.Body, elementSelector.Parameters.Count > 1).Build();

                var script = $@"
                {{
                    ""$group"": {{
                        ""_id"": {keySelectorScript},
                        ""{GroupHelper.GroupElements}"":{{""$push"":{elementSelectorScript}}},
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


        private void VisitGroupJoin(MethodCallExpression node)
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

                var attached = NameHelper.MapCollection(inner.Type.GenericTypeArguments[0].Name);
                var outerKeySelector = (node.Arguments[2] as UnaryExpression)!.Operand as LambdaExpression;
                var outerKey =
                    NameHelper.MapMember((outerKeySelector!.Body as MemberExpression)!.Member);
                var innerKeySelector = (node.Arguments[3] as UnaryExpression)!.Operand as LambdaExpression;
                var innerKey =
                    NameHelper.MapMember((innerKeySelector!.Body as MemberExpression)!.Member);
                var resultSelector = (node.Arguments[4] as UnaryExpression)!.Operand as LambdaExpression;
                var result = new LambdaBodyBuilder(resultSelector!.Body, resultSelector.Parameters.Count > 1).Build();
                var first = resultSelector!.Parameters[0].Name;
                var second = resultSelector.Parameters[1].Name;
                var temp = NameHelper.GetTempField();
                _steps.Add($@"
                {{
                    ""$lookup"": {{
                        ""from"": ""{attached}"",
                        ""localField"": ""{outerKey}"",
                        ""foreignField"": ""{innerKey}"",  
                        ""as"": ""{temp}""
                    }}
                }}
                ");
                _steps.Add($@"
                {{
                    ""$project"": {{
                        ""_id"": false,
                        ""{first}"": ""$$ROOT"",
                        ""{second}"": ""${temp}""
                    }}
                }}
                ");
                _steps.Add($@"
                {{
                    ""$project"": {{
                        ""{first}.{temp}"": false                     
                    }}
                }}
                ");
                _steps.Add($"{{\"$project\":{result}}}");
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
            var selectorBuilder = new LambdaBodyBuilder(selector!.Body, selector.Parameters.Count > 1);
            var result = selectorBuilder.Build();
            if (result == "$$ROOT")
            {
                return;
            }

            if (result.StartsWith("{"))
            {
                _steps.Add($"{{\"$project\":{result}}}");
            }
            else
            {
                var temp = NameHelper.GetTempField();
                _steps.Add($"{{\"$project\":{{\"{temp}\":{result}}}}}");
                _steps.Add($"{{\"$replaceRoot\":{{\"newRoot\":\"${temp}\"}}}}");
            }
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


        public PipelineResult Build()
        {
            var pipeline = $"[{string.Join(",", _steps)}]";
            pipeline = FormatPipeline(pipeline);
            _logger.WriteLine();
            _logger.WriteLine("Pipe line:");
            _logger.WriteLine(pipeline);
            _logger.WriteLine();
            return new PipelineResult()
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