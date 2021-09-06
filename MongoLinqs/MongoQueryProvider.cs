using System;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDbAccessor;

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
            throw new NotImplementedException();
        }

        private IMongoCollection<BsonDocument> GetCollection(string collectionName)
        {
            var client = MongoDbHelper.GetClient();
            var db = client.GetDatabase("linq_test");
            return db.GetCollection<BsonDocument>(collectionName);
        }
    }
}