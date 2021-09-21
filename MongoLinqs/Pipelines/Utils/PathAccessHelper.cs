using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MongoLinqs.Pipelines.Utils
{
    public static class PathAccessHelper
    {
        public static string GetPath(Expression path, bool multipleParams)
        {
            var list = new List<string>();
            var current = path;
            Expression pre = null;
            do
            {
                if (GroupHelper.IsGroup(current) && pre == null)
                {
                    list.Insert(0, GroupHelper.GroupElements);
                }
                switch (current)
                {
                    case ParameterExpression leading:
                        if (multipleParams)
                        {
                            list.Insert(0, leading.Name);
                        }
                        else
                        {
                            if (list.Count == 0)
                            {
                                list.Insert(0, "$ROOT");
                            }
                        }

                        pre = current;
                        current = null;
                        break;
                    case MemberExpression member:
                        var memberName = NameHelper.MapMember(member.Member);
                        list.Insert(0, memberName);
                        pre = current;
                        current = member.Expression;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            } while (current != null);
            return string.Join(".", list);
        }
    }
}