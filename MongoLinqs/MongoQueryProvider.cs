using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDbAccessor;
using Newtonsoft.Json;

namespace MongoLinqs
{
    public class MongoQueryProvider : IQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new MongoDbSet<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            if (typeof(TResult).GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = typeof(TResult).GenericTypeArguments.First();
                var pipelineGenerator = new MongoPipelineGenerator();
                pipelineGenerator.Visit(expression);
                var collection = GetCollection(elementType.Name);
          
                var stages = BsonSerializer
                    .Deserialize<BsonArray>(pipelineGenerator.Build(), c =>
                    {
                    
                    })
                    .Select(item => (BsonDocument) item);
  
                var pipelineDefinition =  PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);
                var result = collection.Aggregate(pipelineDefinition).ToList();
               
                return (TResult)Deserialize(elementType, result);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private object Deserialize(Type elementType, List<BsonDocument> documents)
        {
            var method = typeof(MongoQueryProvider).GetMethod(nameof(DeserializeCore),
                BindingFlags.Instance | BindingFlags.NonPublic );
            return method!.MakeGenericMethod(elementType).Invoke(this, new object[] {documents});
        }
        
        private IEnumerable<TElement> DeserializeCore<TElement>( List<BsonDocument> documents)
        {
            foreach (var document in documents)
            {
                if (document.Contains("_id"))
                {
                    var idValue = document["_id"];
                    document.Remove("_id");
                    document["id"] = idValue;
                }

                var json = document.ToString();
                yield return JsonConvert.DeserializeObject<TElement>(json);
            }
            
            
        }

        private IMongoCollection<BsonDocument> GetCollection(string collectionName)
        {
            var client = MongoDbHelper.GetClient();
            var db = client.GetDatabase("linq_test");
            return db.GetCollection<BsonDocument>(collectionName);
        }
    }
}