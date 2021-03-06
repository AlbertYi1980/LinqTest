using System.Linq;
using MongoLinq.Tests.Common;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class AgTest : TestBase
    {
      
        public AgTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
           
        }
        [Fact]
        public void GroupCount()
        {
            var q = from s in StudentSet
                group s by s.SchoolId
                into g
                select new {SchoolId = g.Key, Stats = new {Count = g.Count()}};
            
            var q1 = StudentSet.GroupBy(s => s.SchoolId)
                .Select(g => new {SchoolId = g.Key, Stats = new {Count = g.Count()}});
            
            var list = q.ToList();
            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
        
        [Fact]
        public void GroupCount2()
        {
            var q = from s in StudentSet
                group s.Name by s.SchoolId into g
                select new {SchoolId = g.Key, Stats = new {Count = g.Count()},  };
          
            var q2 = StudentSet.GroupBy(s => s.SchoolId, s => s.Name)
                .Select(g => new {SchoolId = g.Key, Stats = new {Count = g.Count()},});
            
            var list = q.ToList();
            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
        
        [Fact]
        public void GroupCount3()
        {
            var q = from s in StudentSet
                group s.Name by s.SchoolId into g
                select new {SchoolId = g.Key, Count = g.Count() };
          
      
            
            var list = q.ToList();
            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void GroupAvg()
        {
            var q = from s in StudentSet
                group s by s.SchoolId
                into g
                select new {SchoolId = g.Key, AvgAge = g.Average(i => i.Age)};
            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void GroupAvg2()
        {
            var q = from s in StudentSet
                group s.Age by s.SchoolId
                into g
                select new {SchoolId = g.Key, AvgAge = g.Average()};
            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
        
        
        [Fact]
        public void GroupSum()
        {
            var q = from student in StudentSet
                group student by student.SchoolId into g
                join school in SchoolSet on g.Key equals school.Id 
                select new {SchoolName = school.Name, AvgSum = g.Sum(i => i.Age)};
            
            var q1 = StudentSet.GroupBy(student => student.SchoolId)
                .Join(SchoolSet, g => g.Key, school => school.Id,
                    (g, school) => new {SchoolName = school.Name, AvgSum = g.Sum(i => i.Age)});
           
            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
    }
}