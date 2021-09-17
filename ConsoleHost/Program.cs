using System;
using System.Linq;
using MongoLinqs;
using Newtonsoft.Json;

namespace ConsoleHost
{
    class Program
    {
        static void Main()
        {
            var studentSet = new MongoDbSet<Student>();
            var schoolSet = new MongoDbSet<School>();

            // var q = from s in studentSet
            //     // where  s.Id == 2 && s.Name.Contains("bb") && s.Name == "bbb" 
            //     select new {Id2 = s.Id, s.Name, s.Enabled, s.SchoolId};
            //
            // var e = q.Expression;
            // var students = q.ToList();
            //
            // foreach (var student in students)
            // {
            //     Console.WriteLine(JsonConvert.SerializeObject(student));
            // }
            //
            //
            // var q2 = from s in schoolSet
            //     select s;
            //
            //
            // var schools = q2.ToList();
            //
            // foreach (var school in schools)
            // {
            //     Console.WriteLine(JsonConvert.SerializeObject(school));
            // }

            // var q3 = from s1 in schoolSet
            //     from s2 in studentSet
            //     where s2.Name != "a"
            //     select s2;


            // var q4 = from s1 in schoolSet
            //     join s2 in studentSet on s1.Id equals s2.SchoolId  
            //     where s2.Name != "a"
            //     select new { SchoolName = s1.Name, StudentName = s2.Name};


            var q5 = from s in studentSet
                group s.Name by s.SchoolId
                into g
                select new {SchoolId = g.Key, Count = g.Count()};
            var list = q5.ToList();

            foreach (var student in list)
            {
                Console.WriteLine(JsonConvert.SerializeObject(student));
            }
        }
    }
}