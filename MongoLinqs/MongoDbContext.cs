namespace MongoLinqs
{
    public class MongoDbContext
    {
        private readonly string _connectionString;
        private readonly string _db;
        private readonly ILogger _logger;

        public MongoDbContext(string connectionString, string db, ILogger logger = null)
        {
            _connectionString = connectionString;
            _db = db;
            _logger = logger ?? new DummyLogger();
        }
        public MongoDbSet<TElement> Set<TElement>() => new(_connectionString, _db, _logger);
        
        private class DummyLogger : ILogger
        {
            public void WriteLine(string message = null)
            {
            }
        }
    }
}