using System;
using MongoDB.Driver;

namespace MongoDbAccessor
{
    public static class MongoDbHelper
    {
        public static MongoClient GetClient()
        {
            var password = "3#yab@c";
            var defaultDb = "local";
            var connectionString = $"mongodb+srv://albert:{Uri.EscapeDataString(password)}@cluster0.0qbsz.mongodb.net/{defaultDb}?retryWrites=true&w=majority";
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            var client = new MongoClient(settings);
            return client;
        }
    }
}