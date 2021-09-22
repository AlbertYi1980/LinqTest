
using System.Collections.ObjectModel;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoLinqs.Pipelines.Utils;
using MongoLinqs.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MongoLinqs
{
    public class MongoWriter
    {
        private readonly string _connectionString;
        private readonly string _db;

        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new Collection<JsonConverter>() {new DefaultJsonConverter()}
        };

        public MongoWriter(string connectionString, string db)
        {
            _connectionString = connectionString;
            _db = db;
        }
        public void Save<TElement>(TElement element)
        { 
            var collectionName = NameHelper.MapCollection(typeof(TElement).Name);
            var collection = new MongoClient(_connectionString)
                .GetDatabase(_db)
                .GetCollection<BsonDocument>(collectionName);
            var source = element.ToBsonDocument(); 
            var id = source["_id"];
            var dest = new BsonDocument();
            foreach (var p in source)
            {
                if (p.Name == "_id") continue;
                dest[ToCamelCase(p.Name)] = p.Value;
            }
           
            collection.UpdateOne(b => b["_id"] == id, new BsonDocument()
            {
                {"$set", dest}
            }, new UpdateOptions()
            {
                IsUpsert = true
            });
            
            
        }

        public void Delete<TElement>(object id)
        {
            var collectionName = NameHelper.MapCollection(typeof(TElement).Name);
            var collection = new MongoClient(_connectionString)
                .GetDatabase(_db)
                .GetCollection<BsonDocument>(collectionName);
            collection.DeleteOne(b => b["_id"] == id);
        }
        
        private static string ToCamelCase(string s)
        {
            if (s == null) return null;
            if (s == string.Empty) return s;
            return s.Substring(0, 1).ToLower() + s.Substring(1);
        }
    }
}