using System;
using System.Linq;
using MongoLinq.Tests.Common;
using Newtonsoft.Json;
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
        public void GroupByDate()
        {
            var q = from s in StudentSet
                let day = s.CreateAt.Date
                group s.Age by day
                into g
                select new {Day = g.Key, Stats = new {Count = g.Sum()},};
            
            var q1 = StudentSet.Select(s => new {s, day = s.CreateAt.Date})
                .GroupBy(@t => @t.day, @t => @t.s.Age)
                .Select(g => new {Day = g.Key, Stats = new {Count = g.Sum()},});

            var result = q.ToList();
            foreach (var item in result)
            {
                WriteLine(JsonConvert.SerializeObject(item));
            }
        }
    }
}