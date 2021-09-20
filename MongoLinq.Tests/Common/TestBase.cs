using System;
using MongoLinqs;
using Xunit.Abstractions;

namespace MongoLinq.Tests.Common
{
    public abstract class TestBase
    {
        protected  readonly MongoDbSet<Student> StudentSet;
        protected  readonly MongoDbSet<School> SchoolSet;
        protected   readonly TestLogger Logger;

        protected TestBase(ITestOutputHelper testOutputHelper)
        {
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
    }
}