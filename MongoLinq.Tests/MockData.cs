using System;
using MongoLinq.Tests.Common;
using MongoLinq.Tests.Entities;
using Xunit;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class MockData : TestBase
    {
        public MockData(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }


        [Fact]
        public void CreateStudent()
        {
            var students = new Student[]
            {
                new Student()
                {
                    Id = 1,
                    Name = "aaa",
                    CreateAt = DateTime.Now.AddDays(-5),
                    Enabled = false,
                    Age = 12,
                    SchoolId = 1,
                    
                },
                new Student()
                {
                    Id = 2,
                    Name = "bbb",
                    CreateAt = DateTime.Now.AddDays(-7),
                    Enabled = true,
                    Age = 15,
                    SchoolId = 2,
                    
                },
                new Student()
                {
                    Id = 3,
                    Name = "ccc",
                    CreateAt = DateTime.Now.AddDays(-10),
                    Enabled = true,
                    Age = 18,
                    SchoolId = 3,
                    
                },
                new Student()
                {
                    Id = 4,
                    Name = "ddd",
                    CreateAt = DateTime.Now.AddDays(-7),
                    Enabled = false,
                    Age = 11,
                    SchoolId = 2,
                    
                },
                new Student()
                {
                    Id = 5,
                    Name = "eee",
                    CreateAt = DateTime.Now.AddDays(-5),
                    Enabled = false,
                    Age = 16,
                    SchoolId = 2,
                    
                },
                new Student()
                {
                    Id = 6,
                    Name = "fff",
                    CreateAt = DateTime.Now.AddDays(-10),
                    Enabled = true,
                    Age = 11,
                    SchoolId = 1,
                    
                },
                new Student()
                {
                    Id = 7,
                    Name = "ggg",
                    CreateAt = DateTime.Now.AddDays(-11),
                    Enabled = false,
                    Age = 15,
                    SchoolId = 3,
                    
                },
             
               
            };

            foreach (var student in students)
            {
                   StudentSet.Save(student);
            }
         
        }
        
        [Fact]
        public void DeleteStudent()
        {
            StudentSet.Delete(5);
        }
        
           [Fact]
        public void CreateSchool()
        {
            var schools = new School[]
            {
                new School()
                {
                    Id = 1,
                    Name = "XXX",
                  
                },
                new School()
                {
                    Id = 2,
                    Name = "YYY",
                  
                    
                },
                new School()
                {
                    Id = 3,
                    Name = "ZZ",
                   
                    
                },
             
             
               
            };

            foreach (var school in schools)
            {
                   SchoolSet.Save(school);
            }
         
        }
    }
}