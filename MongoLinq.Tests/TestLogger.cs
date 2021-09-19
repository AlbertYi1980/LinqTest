using MongoLinqs;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class TestLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TestLogger(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public void WriteLine(string message = null)
        {
            _testOutputHelper.WriteLine(message?? string.Empty);
        }
    }
}