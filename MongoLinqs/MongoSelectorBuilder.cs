using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace MongoLinqs
{
    public class MongoSelectorBuilder
    {
        private readonly LambdaExpression _selector;

        public MongoSelectorBuilder(LambdaExpression selector)
        {
            _selector = selector;
        }

        public MongoSelectorResult Build()
        {
            var param = _selector.Parameters[0];
            var body = _selector.Body;
            return BuildCore(body, param);
        }

        private static MongoSelectorResult BuildCore(Expression body, Expression param)
        {
            if (param == body)
            {
                return new MongoSelectorResult
                {
                    Kind = MongoSelectorResultKind.Root,
                    Script = "$$ROOT"
                };
            }

            if (body is MemberExpression member)
            {
                return new MongoSelectorResult
                {
                    Kind = MongoSelectorResultKind.Member,
                    Script = "$" + BuildMember(member, param)
                };
            }

            if (body is NewExpression @new)
            {
                return new MongoSelectorResult
                {
                    Kind = MongoSelectorResultKind.New,
                    Script = BuildNew(@new, param)
                };
            }

            if (body is ConstantExpression constant)
            {
                return new MongoSelectorResult
                {
                    Kind = MongoSelectorResultKind.Constant,
                    Script = JsonConvert.SerializeObject(constant.Value)
                };
            }

            throw new NotSupportedException();
        }

        private static string BuildMember(MemberExpression member, Expression param)
        {
            var list = new List<string>();
            var current = member;
            do
            {
                list.Insert(0, NameHelper.FixMemberName(NameHelper.ToCamelCase(current.Member.Name)));
                if (current.Expression is MemberExpression expression)
                {
                    current = expression;
                }
                else
                {
                    if (current.Expression == param)
                    {
                        current = null;
                    }
                    else
                    {
                        throw new NotSupportedException("Member path should start with param.");
                    }
                }
            } while (current != null);

            return string.Join(".", list);
        }

        private static string BuildNew(NewExpression @new, Expression param)
        {
            var length = @new!.Constructor!.GetParameters().Length;
            var members = new List<string>();
            for (var i = 0; i < length; i++)
            {
                var name = NameHelper.ToCamelCase(@new.Members![i].Name);
                var value = BuildCore(@new.Arguments[i], param).Script;
                var member = $"\"{name}\":\"{value}\"";
                members.Add(member);
            }

            return string.Join(",", members);
        }
    }
}