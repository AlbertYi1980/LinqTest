using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MongoLinqs.MemberPath
{
    public static class MemberAccessHelper
    {
        public static string GetPath(MemberExpression member, Expression param)
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
    }
}