using System.Linq;
using MongoLinq.Tests.Common;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class GroupTest : TestBase
    {
        
        public GroupTest(ITestOutputHelper testOutputHelper): base(testOutputHelper)
        {
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
                group s.Name by s.SchoolId
                into g
                select new {SchoolId = g.Key, Stats = new {Count = g.Count()},};

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


    }
}