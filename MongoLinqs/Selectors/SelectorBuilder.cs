using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoLinqs.MemberPath;
using Newtonsoft.Json;

namespace MongoLinqs.Selectors
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

        private  SelectorResult BuildCore(Expression current)
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