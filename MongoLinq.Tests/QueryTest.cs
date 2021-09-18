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
        private readonly ITestOutputHelper _testOutputHelper;
        readonly MongoDbSet<Student> _studentSet;
        readonly MongoDbSet<School> _schoolSet;

        public QueryTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _studentSet = new MongoDbSet<Student>();
            _schoolSet = new MongoDbSet<School>();
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
                _testOutputHelper.WriteLine(JsonConvert.SerializeObject(item));
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
                _testOutputHelper.WriteLine(JsonConvert.SerializeObject(item));
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
                _testOutputHelper.WriteLine(JsonConvert.SerializeObject(item));
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
                _testOutputHelper.WriteLine(JsonConvert.SerializeObject(item));
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
                _testOutputHelper.WriteLine(JsonConvert.SerializeObject(item));
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
                _testOutputHelper.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

        [Fact]
        public void Group()
        {
            // var q = from s in _studentSet
            //     group s.Name by s.SchoolId
            //     into g
            //     select new {SchoolId = g.Key, Stats = new {Count = g.Count()}};
            // var list = q.ToList();
            // foreach (var item in list)
            // {
            //     _testOutputHelper.WriteLine(JsonConvert.SerializeObject(item));
            // }
        }

        [Fact]
        public void GroupJoin()
        {
        }
    }
}