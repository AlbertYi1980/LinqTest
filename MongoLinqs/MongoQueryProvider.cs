﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoLinqs.Pipelines;
using Newtonsoft.Json;

namespace MongoLinqs
{
    public class MongoQueryProvider : IQueryProvider
    {
        private readonly string _connectionString;
        private readonly string _db;
        private readonly ILogger _logger;

        public MongoQueryProvider(string connectionString, string db, ILogger logger)
        {
            _connectionString = connectionString;
            _db = db;
            _logger = logger;
        }
        
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
                var pipelineGenerator = new MongoPipelineGenerator(_logger);
                pipelineGenerator.Visit(expression);   
                var pipelineResult = pipelineGenerator.Build();
                var collection = GetCollection(pipelineResult.StartAt);
                var stages = BsonSerializer
                    .Deserialize<BsonArray>(pipelineResult.Pipeline)
                    .Select(item => (BsonDocument) item);
  
                var pipelineDefinition =  PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);
                var result = collection.Aggregate(pipelineDefinition).ToList();
                var json = result.ToJson();
                _logger.WriteLine();
                _logger.WriteLine("Raw json:");
                _logger.WriteLine(json);
                _logger.WriteLine();
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
            var db = GetDb(_connectionString, _db);
            return db.GetCollection<BsonDocument>(collectionName);
        }
        
        private static IMongoDatabase GetDb(string connectionString, string db)
        {
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            return new MongoClient(settings).GetDatabase(db);
        }
    }
}