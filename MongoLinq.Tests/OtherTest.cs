using System.Linq;
using MongoLinq.Tests.Common;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class OtherTest: TestBase
    {
        public OtherTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
        
        // [Fact]
        // public void SelectMember()
        // {
        //     var q = from s in _studentSet
        //         select s.Name;
        //
        //     var list = q.ToList();
        //
        //     foreach (var item in list)
        //     {
        //         _logger.WriteLine(JsonConvert.SerializeObject(item));
        //     }
        // }
        //
        // [Fact]
        // public void SelectConst()
        // {
        //     var q = from s in _studentSet
        //         select 1;
        //
        //     var list = q.ToList();
        //
        //     foreach (var item in list)
        //     {
        //         _logger.WriteLine(JsonConvert.SerializeObject(item));
        //     }
        // }
    }
}