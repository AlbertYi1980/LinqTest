using System.Linq;
using MongoLinq.Tests.Common;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class HierarchyTest: TestBase
    {
        public HierarchyTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
        
        [Fact]
        public void SelectComplexNew()
        {
            var q = from s in StudentSet
                select new {Id2 = s.Id, s.Name, s.Enabled, s.SchoolId, Age = new {Value = s.Age}};

            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
    }
}