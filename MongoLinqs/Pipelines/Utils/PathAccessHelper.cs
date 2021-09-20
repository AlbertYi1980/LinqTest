using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MongoLinqs.Pipelines.Utils
{
    public static class PathAccessHelper
    {
        public static string GetPath(Expression path, IList<Expression> @params)
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
                        if (@params.Count > 1)
                        {
                            var segment = NameHelper.Map(leading!.Name);
                            list.Insert(0, segment);
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
                        var memberName = NameHelper.Map(member.Member.Name);
                        if (GroupHelper.IsGroupMember(member) && memberName == "key")
                        {
                            memberName = "_id";
                        }
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