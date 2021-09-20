using MongoLinq.Tests.Common;
using MongoLinqs;

namespace MongoLinq.Tests.Entities
{
    [Entity]
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public bool Enabled { get; set; }

        public int Age { get; set; }

        public int SchoolId { get; set; }
    }
}