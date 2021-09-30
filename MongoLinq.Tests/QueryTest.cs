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
        public void WhereEqual()
        {
            var q = from s in StudentSet
                where s.Id == 2 
                select new {Id2 = s.Id, s.Name, s.Enabled, s.SchoolId};

            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
        
        [Fact]
        public void WhereNotEqual()
        {
            var q = from s in StudentSet
                where  s.Name != "bbb"
                select new {Id2 = s.Id, s.Name, s.Enabled, s.SchoolId};

            var list = q.ToList();

            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
        
        [Fact]
        public void WhereStringContains()
        {
            var q = from s in StudentSet
                where  s.Name.Contains("bb")
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
        public void SelectId()
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
            var q2 = SchoolSet.SelectMany(s1 => StudentSet, (s1, s2) => new { s1, s2 })
                .Where(@t => @t.s2.Name != "a")
                .Select(@t => @t.s2);
            var list = q.ToList();
            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }

       
        [Fact]
        public void SelectMany2()
        {
            var q = from s1 in SchoolSet
                where s1.Name == "kk"
                from s2 in StudentSet
                where s2.Name != "a"
                select s2;
            var q2 = SchoolSet.Where(s1 => s1.Name == "kk")
                .SelectMany(s1 => StudentSet, (s1, s2) => new { s1, s2 })
                .Where(@t => @t.s2.Name != "a")
                .Select(@t => @t.s2);
            var list = q.ToList();
            foreach (var item in list)
            {
                Logger.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
       
       
      
    }
}