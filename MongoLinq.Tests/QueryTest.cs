using System.Linq;
using MongoLinq.Tests.Common;
using MongoLinq.Tests.Entities;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class QueryTest : TestBase
    {
       

        public QueryTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }


        [Fact]
        public void Where()
        {
            var q = from s in StudentSet
                where s.Id == 2 && s.Name.Contains("bb") && s.Name == "bbb"
                select new {Id2 = s.Id, s.Name, s.Enabled, s.SchoolId};

            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void SelectNew()
        {
            var q = from s in StudentSet
                // where  s.Id == 2 && s.Name.Contains("bb") && s.Name == "bbb" 
                select new {Id2 = s.Id, s.Name, s.Enabled, s.SchoolId};

            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void SelectNewInit()
        {
            var q = from s in StudentSet
                select new Student() {Id = s.Id, Name = "dd", Enabled = s.Enabled,};

            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void SelectNewNull()
        {
            var q = from s in StudentSet
                select new Student() {Id = s.Id, Name = null, Enabled = s.Enabled,};

            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

      


      

     


        [Fact]
        public void SelectDirect()
        {
            var q = from s in SchoolSet
                select s;
            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void SelectMany()
        {
            var q = from s1 in SchoolSet
                from s2 in StudentSet
                where s2.Name != "a"
                select s2;
            var list = q.ToList();
            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void Join()
        {
            var q = from s1 in SchoolSet
                join s2 in StudentSet on s1.Id equals s2.SchoolId
                where s2.Name != "a"
                select new {SchoolName = s1.Name, StudentName = s2.Name};
            var list = q.ToList();
            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
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
        public void GroupJoin()
        {
            var q =
                from school in SchoolSet
                join student in StudentSet on school.Id equals student.SchoolId into g
                select new {SchoolName = school.Name, StudentCount = g.Count()};
            
            var q1 = SchoolSet
                .GroupJoin(StudentSet, school => school.Id, student => student.SchoolId,
                (school, g) => new {SchoolName = school.Name, StudentCount = g.Count()});
            
            
            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void GroupJoin2()
        {
            var q =
                from student in StudentSet
                join school in SchoolSet on student.SchoolId equals school.Id into g
                select new {StudentName = student.Name, SchoolCount = g.Count()};
            
            var q2 = StudentSet.GroupJoin(SchoolSet, student => student.SchoolId, school => school.Id,
                (student, g) => new {StudentName = student.Name, SchoolCount = g.Count()});
            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
        
        
        [Fact]
        public void GroupJoin3()
        {
            var q = SchoolSet
                .GroupJoin(StudentSet, school => school.Id, student => student.SchoolId,
                    (school, g) => new {school, g})
                .SelectMany(@t => StudentSet,
                    (@t, student1) => new {SchoolName = @t.school.Name, StudentCount = @t.g.Count()});
            
     
            
            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
    }
}