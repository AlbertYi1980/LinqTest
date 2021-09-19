using System;
using System.Linq;
using MongoLinqs;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class QueryTest
    {
        readonly MongoDbSet<Student> _studentSet;
        readonly MongoDbSet<School> _schoolSet;
        private readonly TestLogger _logger;

        public QueryTest(ITestOutputHelper testOutputHelper)
        {
            var password = "3#yab@c";
            var defaultDb = "local";
            var connectionString = $"mongodb+srv://albert:{Uri.EscapeDataString(password)}@cluster0.0qbsz.mongodb.net/{defaultDb}?retryWrites=true&w=majority";
            var db = "linq_test";
            _logger = new TestLogger(testOutputHelper);
            var context = new MongoDbContext(connectionString, db, _logger);
            _studentSet = context.Set<Student>();
            _schoolSet = context.Set<School>();
        }


        [Fact]
        public void Where()
        {
            var q = from s in _studentSet
                where s.Id == 2 && s.Name.Contains("bb") && s.Name == "bbb"
                select new {Id2 = s.Id, s.Name, s.Enabled, s.SchoolId};

            var list = q.ToList();

            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void SelectNew()
        {
            var q = from s in _studentSet
                // where  s.Id == 2 && s.Name.Contains("bb") && s.Name == "bbb" 
                select new {Id2 = s.Id, s.Name, s.Enabled, s.SchoolId};

            var list = q.ToList();

            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void SelectNewInit()
        {
            var q = from s in _studentSet
                select new Student() {Id = s.Id, Name = "dd", Enabled = s.Enabled,};

            var list = q.ToList();

            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void SelectNewNull()
        {
            var q = from s in _studentSet
                select new Student() {Id = s.Id, Name = null, Enabled = s.Enabled,};

            var list = q.ToList();

            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void SelectComplexNew()
        {
            var q = from s in _studentSet
                select new {Id2 = s.Id, s.Name, s.Enabled, s.SchoolId, Age = new {Value = s.Age}};

            var list = q.ToList();

            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }


        [Fact]
        public void SelectConst()
        {
            var q = from s in _studentSet
                select 1;

            var list = q.ToList();

            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void SelectMember()
        {
            var q = from s in _studentSet
                select s.Name;

            var list = q.ToList();

            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }


        [Fact]
        public void SelectDirect()
        {
            var q = from s in _schoolSet
                select s;
            var list = q.ToList();

            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void SelectMany()
        {
            var q = from s1 in _schoolSet
                from s2 in _studentSet
                where s2.Name != "a"
                select s2;
            var list = q.ToList();
            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void Join()
        {
            var q = from s1 in _schoolSet
                join s2 in _studentSet on s1.Id equals s2.SchoolId
                where s2.Name != "a"
                select new {SchoolName = s1.Name, StudentName = s2.Name};
            var list = q.ToList();
            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void GroupCount()
        {
            var q = from s in _studentSet
                group s by s.SchoolId
                into g
                select new {SchoolId = g.Key, Stats = new {Count = g.Count()}};
            var list = q.ToList();
            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void GroupAvg()
        {
            var q = from s in _studentSet
                group s by s.SchoolId
                into g
                select new {SchoolId = g.Key, Stats = new {AvgAge = g.Average(i => i.Age)}};
            var list = q.ToList();

            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void GroupAvg2()
        {
            var q = from s in _studentSet
                group s.Age by s.SchoolId
                into g
                select new {SchoolId = g.Key, AvgAge = g.Average()};
            var list = q.ToList();

            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
        
        [Fact]
        public void GroupJoin()
        {
            var q =
                from school in _schoolSet
                join student in _studentSet on school.Id equals student.SchoolId into g
                select new {SchoolName = school.Name, StudentCount = g.Count()};
            var list = q.ToList();

            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void GroupJoin2()
        {
            var q =
                from student in _studentSet
                join school in _schoolSet on student.SchoolId equals school.Id into g
                select new {StudentName = student.Name, SchoolCount = g.Count()};
            var list = q.ToList();

            foreach (var item in list)
            {
                _logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
    }
}