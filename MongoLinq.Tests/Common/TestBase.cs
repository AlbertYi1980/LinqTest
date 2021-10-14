using System;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoLinq.Tests.Entities;
using MongoLinqs;
using MongoLinqs.Pipelines.Utils;
using Xunit.Abstractions;

namespace MongoLinq.Tests.Common
{
    public abstract class TestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        protected  readonly MongoDbSet<Student> StudentSet;
        protected  readonly MongoDbSet<School> SchoolSet;
        protected   readonly TestLogger Logger;

        static TestBase()
        {
         
            ConventionRegistry.Remove("__defaults__");
            ConventionRegistry.Register("__defaults__", MongoDbDefaultConventionPack.Instance, t => true);
            NameHelper.Register(typeof(Student));
            NameHelper.Register(typeof(School));
        }
        
        protected TestBase(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
       
            var password = "3#yab@c";
            var defaultDb = "local";
            var connectionString =
                $"mongodb+srv://albert:{Uri.EscapeDataString(password)}@cluster0.0qbsz.mongodb.net/{defaultDb}?retryWrites=true&w=majority";
            var db = "linq_test";
            Logger = new TestLogger(testOutputHelper);
            var context = new MongoDbContext(connectionString, db, Logger);
            StudentSet = context.Set<Student>();
            SchoolSet = context.Set<School>();
        }

        protected void WriteLine(string s)
        {
            _testOutputHelper.WriteLine(s);
        }
    }
}