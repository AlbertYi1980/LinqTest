using System.Linq.Expressions;
using System.Text;

namespace MongoLinqs
{
    public class MongoPipelineGenerator : ExpressionVisitor
    {
        private readonly StringBuilder _builder;

        public MongoPipelineGenerator()
        {
            _builder = new StringBuilder();
        }
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            return base.VisitMethodCall(node);
        }

        public string Build()
        {
            return "[{\n    $match:{\n        name:\"bbb\"\n    }\n}]";
            return _builder.ToString();
        }
    }
}