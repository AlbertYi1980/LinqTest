using System.Linq;
using MongoLinq.Tests.Common;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class CalcTest : TestBase
    {


        public CalcTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }


        [Fact]
        public void Plus()
        {
            var q = from s in StudentSet
                select new {Id = s.Id + 1, s.Name, s.Enabled, s.SchoolId};

        
            
            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
    }
}