using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MongoLinqs
{
    public class MongoDbSet<TElement> : IQueryable<TElement>
    {

        private readonly MongoWriter _writer;
        public MongoDbSet(string connectionString, string db, ILogger logger)
        {

            _writer = new MongoWriter(connectionString, db);
            Provider = new MongoQueryProvider(connectionString, db, logger);
            Expression = Expression.Constant(this);
        }

        public MongoDbSet(IQueryProvider provider ,Expression expression)
        {
            Provider = provider;
            Expression = expression;
    
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return Provider.Execute<IEnumerable<TElement>>(Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Type ElementType => typeof(TElement);
        public Expression Expression { get; }
        public IQueryProvider Provider { get; }

        public void Save(TElement element)
        {
            _writer.Save<TElement>(element);
        }
        
        public void Delete(object id)
        {
            _writer.Delete<TElement>(id);
        }
    }
}