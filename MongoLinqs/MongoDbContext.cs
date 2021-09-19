namespace MongoLinqs
{
    public class MongoDbContext
    {
        private readonly string _connectionString;
        private readonly string _db;

        public MongoDbContext(string connectionString, string db)
        {
            _connectionString = connectionString;
            _db = db;
        }
        public MongoDbSet<TElement> Set<TElement>() => new MongoDbSet<TElement>(_connectionString, _db);
    }
}