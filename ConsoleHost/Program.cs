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
            var dbSet = new MongoDbSet<Student>();
            var q = from s in dbSet
                where  s.Id == 2 && s.Name.Contains("bb") && s.Name == "bbb" 
                select new {Id2 = s.Id, s.Name, s.Enabled}; 
               
            var e = q.Expression;
            var students = q.ToList();
        
            foreach (var student in students)
            {
                Console.WriteLine(JsonConvert.SerializeObject(student));
            }
        }

    }
}