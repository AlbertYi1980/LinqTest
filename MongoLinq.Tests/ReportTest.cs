using System.Linq;
using MongoLinq.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class ReportTest : TestBase
    {

        public ReportTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }


        [Fact]
        public void Foo()
        {
            var q = from s in StudentSet
                group s.Name by s.SchoolId
                into g
                select new {SchoolId = g.Key, Stats = new {Count = g.Count()},};

        }
    }
}