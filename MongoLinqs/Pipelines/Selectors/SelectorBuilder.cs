using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoLinqs.Pipelines.Grouping;
using MongoLinqs.Pipelines.MemberPath;
using Newtonsoft.Json;

namespace MongoLinqs.Pipelines.Selectors
{
    public class SelectorBuilder
    {
        private readonly Expression _body;
        private readonly Expression _param;

        public SelectorBuilder(LambdaExpression selector)
        {
            _body = selector.Body;
            _param = selector.Parameters[0];
        }

        public SelectorResult Build()
        {
            return BuildCore(_body);
        }

        private SelectorResult BuildCore(Expression current)
        {
            if (_param == current)
            {
                return new SelectorResult
                {
                    Kind = SelectorResultKind.Root,
                    Script = "\"$$ROOT\""
                };
            }

            if (current is MemberExpression member)
            {
                return new SelectorResult
                {
                    Kind = SelectorResultKind.Member,
                    Script = BuildMember(member)
                };
            }

            if (current is NewExpression @new)
            {
                return new SelectorResult
                {
                    Kind = SelectorResultKind.New,
                    Script = BuildNew(@new)
                };
            }

            if (current is MemberInitExpression memberInit)
            {
                return new SelectorResult
                {
                    Kind = SelectorResultKind.New,
                    Script = BuildMemberInit(memberInit)
                };
            }

            if (current is ConstantExpression constant)
            {
                return new SelectorResult
                {
                    Kind = SelectorResultKind.Constant,
                    Script = JsonConvert.SerializeObject(constant.Value)
                };
            }

            if (current is MethodCallExpression call)
            {
                if (!GroupHelper.IsGroupCall(call) && !GroupHelper.IsEnumCall(call))
                {
                    throw new NotSupportedException();
                }

                if (call.Method.Name == nameof(Enumerable.Count))
                {
                    return new SelectorResult()
                    {
                        Kind = SelectorResultKind.Member,
                        Script = $"{{\"$size\":\"${GroupHelper.GroupElements}\"}}"
                    };
                }

                if (call.Method.Name == nameof(Enumerable.Average))
                {
                    if (call.Arguments.Count == 1)
                    {
                        return new SelectorResult()
                        {
                            Kind = SelectorResultKind.Member,
                            Script = $"{{\"$avg\":\"${GroupHelper.GroupElements}\"}}"
                        };
                    }
                    else
                    {
                        var lambda = call.Arguments[1] as LambdaExpression;
                        var path = MemberAccessHelper.GetPath(lambda.Body as MemberExpression, lambda.Parameters[0]);
                        return new SelectorResult()
                        {
                            Kind = SelectorResultKind.Member,
                            Script = $"{{\"$avg\":\"${GroupHelper.GroupElements}.{path}\"}}"
                        };
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            throw new NotSupportedException();
        }


        private string BuildMember(MemberExpression member)
        {
            return $"\"${MemberAccessHelper.GetPath(member, _param)}\"";
            ;
        }

        private string BuildNew(NewExpression @new)
        {
            var length = @new!.Constructor!.GetParameters().Length;
            var members = new List<string>();
            for (var i = 0; i < length; i++)
            {
                var name = NameHelper.ToCamelCase(@new.Members![i].Name);
                var value = BuildCore(@new.Arguments[i]).Script;
                var member = $"\"{name}\":{value}";
                members.Add(member);
            }

            return $"{{{string.Join(",", members)}}}";
        }

        private string BuildMemberInit(MemberInitExpression memberInit)
        {
            var length = memberInit!.Bindings!.Count;
            var members = new List<string>();
            for (var i = 0; i < length; i++)
            {
                var assignment = (MemberAssignment) memberInit.Bindings[i];
                var name = NameHelper.ToCamelCase(assignment.Member.Name);
                var result = BuildCore(assignment.Expression);
                var value = result.Script;
                var member = $"\"{name}\":{value}";
                members.Add(member);
            }

            return $"{{{string.Join(",", members)}}}";
        }
    }
}