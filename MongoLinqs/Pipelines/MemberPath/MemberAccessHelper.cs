using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoLinqs.Pipelines.Grouping;

namespace MongoLinqs.Pipelines.MemberPath
{
    public static class MemberAccessHelper
    {
        public static string GetPath(MemberExpression member, Expression param)
        {
            var list = new List<string>();
            var current = member;
            do
            {
                var memberName = NameHelper.FixMemberName(NameHelper.ToCamelCase(current.Member.Name));
                if (GroupHelper.IsGroupMember(current) && current.Member.Name == "Key")
                {
                    memberName = "_id";
                }

              
                list.Insert(0, memberName);
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
    }
}