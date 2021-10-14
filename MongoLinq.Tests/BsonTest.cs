using System;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class BsonTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public BsonTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        
        [Fact]
        public void Temp1()
        {
        
            var obj = new MyClass()
            {
                Id = 2,
                A = DateTime.Now,
                B = 12,
            };

            var myDoc = new BsonDocument();


            using var writer = new BsonDocumentWriter(myDoc);
            var classMap = new BsonClassMap(typeof(MyClass));
            classMap.MapProperty("A").SetElementName("aa");
            classMap.MapProperty("B").SetElementName("b");
            classMap.MapProperty("Id").SetElementName("id");
         
            BsonClassMap.RegisterClassMap(classMap);
            BsonSerializer.Serialize(writer, obj);
            _testOutputHelper.WriteLine(myDoc.ToString());
            var obj2 = BsonSerializer.Deserialize<MyClass>(myDoc);

        }
        
      
    }
    
    public class MyClass
    {
        public int Id { get; set; }
        public DateTime A { get; set; }
        public long B { get; set; }
    }
}