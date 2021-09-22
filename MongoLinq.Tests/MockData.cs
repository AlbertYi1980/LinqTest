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
            StudentSet.Save(new Student()
            {
                Id = 5,
                Name = "eee",
                CreateAt = DateTime.Now.AddDays(-10)
            });
        }
        
        [Fact]
        public void DeleteStudent()
        {
            StudentSet.Delete(5);
        }
    }
}