using System.Linq;
using MongoLinq.Tests.Common;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class TempTest: TestBase
    {
        public TempTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
        
        [Fact]
        public void Temp1()
        {
            var q = from s in StudentSet
                select new {Id2 = s.Id, s.Name, s.Enabled, s.SchoolId, Age = new {Value = s.Age}};

            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
        
        [Fact]
        public void Temp2()
        {
            var q = from s in StudentSet
                select new {Id2 = s.Id, s.Name, s.Enabled, s.SchoolId, Age = new {Value = s.Age}};

            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
        
        [Fact]
        public void Temp3()
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