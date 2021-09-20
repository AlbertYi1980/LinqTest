using System;
using System.Collections.ObjectModel;
using MongoLinqs;
using MongoLinqs.Serialization;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace MongoLinq.Tests
{
    public class SerializationTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SerializationTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void MirrorDeserialize()
        {
            var obj = new
            {
                _id = 1,
                data = new
                {
                    name = "张三",
                },
                createAt = DateTime.Now
            };
            var json = JsonConvert.SerializeObject(obj);
            _testOutputHelper.WriteLine(json);
            var dto = JsonConvert.DeserializeObject<Dto>(json, new JsonSerializerSettings
            {
                Converters = new Collection<JsonConverter> {new MirrorJsonConverter()}
            });
            _testOutputHelper.WriteLine(JsonConvert.SerializeObject(dto));
        }

        [Fact]
        public void MirrorSerialize()
        {
            var dto = new Dto
            {
                Id = 2,
                Name = "kk",
                CreateAt = DateTime.Now
            };
            var json = JsonConvert.SerializeObject(dto, new JsonSerializerSettings
            {
                Converters = new Collection<JsonConverter> {new MirrorJsonConverter()}
            });
            _testOutputHelper.WriteLine(json);
        }

        [Fact]
        public void DefaultDeserialize()
        {
            var obj = new
            {
                _id = 1,
                name = "张三",
                createAt = DateTime.Now
            };
            var json = JsonConvert.SerializeObject(obj);
            _testOutputHelper.WriteLine(json);
            var dto = JsonConvert.DeserializeObject<Dto>(json, new JsonSerializerSettings
            {
                Converters = new Collection<JsonConverter> {new DefaultJsonConverter()}
            });
            _testOutputHelper.WriteLine(JsonConvert.SerializeObject(dto));
        }

        [Fact]
        public void DefaultSerialize()
        {
            var dto = new Dto
            {
                Id = 2,
                Name = "kk",
                CreateAt = DateTime.Now
            };
            var json = JsonConvert.SerializeObject(dto, new JsonSerializerSettings
            {
                Converters = new Collection<JsonConverter> {new DefaultJsonConverter()}
            });
            _testOutputHelper.WriteLine(json);
        }

        [Entity]
        public class Dto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime CreateAt { get; set; }
        }
    }
}