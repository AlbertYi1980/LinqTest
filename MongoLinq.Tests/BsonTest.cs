using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoLinq.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class BsonTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        static BsonTest()
        {
            ConventionRegistry.Remove("__defaults__");
            ConventionRegistry.Register("__defaults__", MongoDbDefaultConventionPack.Instance, t => true);
            // Hack();

            var classMap = new BsonClassMap(typeof(MyClass));
            classMap.MapProperty("A").SetElementName("aa");
            classMap.MapProperty("B").SetElementName("b");
            classMap.MapProperty("Id").SetElementName("id");
            BsonClassMap.RegisterClassMap(classMap);
        }

        private static void Hack()
        {
            var field = typeof(ConventionRegistry).GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                .First(m => m.Name == "__conventionPacks");
            var container = (IList)field.GetValue(null);
            var convention = container[0];
            var packField = convention.GetType().GetFields().First(f => f.Name == "Pack");
            var pack = (DefaultConventionPack)packField.GetValue(convention);
            var conventionsField = pack.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .First(f => f.Name == "_conventions");
            var conventions = (List<IConvention>)conventionsField.GetValue(pack);
            var foo = conventions.First(c => c.Name == "NamedIdMember");
            conventions.Remove(foo);
        }

        public BsonTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Serialize()
        {
            var obj = new MyClass()
            {
                Id = 2,
                A = DateTime.Now,
                B = 12,
            };

            var myDoc = new BsonDocument();


            using var writer = new BsonDocumentWriter(myDoc);

            BsonSerializer.Serialize(writer, obj);
            _testOutputHelper.WriteLine(myDoc.ToString());

        }

        [Fact]
        public void Deserialize()
        {
            var obj = new MyClass()
            {
                Id = 2,
                A = DateTime.Now,
                B = 12,
            };

            var myDoc = new BsonDocument();


            using var writer = new BsonDocumentWriter(myDoc);
            
            BsonSerializer.Serialize(writer, obj);
            _testOutputHelper.WriteLine(myDoc.ToString());
            var obj2 = BsonSerializer.Deserialize<MyClass>(myDoc);
        }
        
        [Fact]
        public void Anonymous()
        {

            var myDoc = new BsonDocument()
            {
                {"Id", 2},
                {"A", DateTime.Now},
                {"B", 12L},
            };


         


            var k = new
            {
                Id = 2,
                A = DateTime.Now,
                B = 12L,
            };
            var type = k.GetType();
            var obj2 = BsonSerializer.Deserialize(myDoc, type);
        }
    }

    public class MyClass
    {
        public int Id { get; set; }
        public DateTime A { get; set; }
        public long B { get; set; }
    }
    
 
}