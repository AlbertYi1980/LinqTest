using System;
using System.Linq;
using System.Reflection;
using MongoLinqs.Pipelines.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MongoLinqs.Serialization
{
    public class MirrorJsonConverter : JsonConverter
    {
  
        private static readonly string[] SystemPropertyNames = {"Id", "CreateAt"};

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var source = JObject.FromObject(value);
            var properties = source.Properties();
            var destination = new JObject();
            var data = new JObject();
            foreach (var property in properties)
            {
                var propertyName = property.Name;
                 if (SystemPropertyNames.Contains(propertyName))
                {
                    destination.Add(NameHelper.MapEntity(propertyName), property.Value);
                }
                else
                {
                    data.Add(NameHelper.MapEntity(propertyName), property.Value);
                }
            }
            destination.Add("data", data);
            destination.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var source = JObject.Load(reader);
            var data = (JObject)source["data"] ;
            var destination = new JObject();
            var properties = source.Properties();
            foreach (var property in properties)
            {
                var propertyName = property.Name;
                if (SystemPropertyNames.Contains(NameHelper.InverseMapEntity( propertyName)))
                {
                    destination.Add(NameHelper.InverseMapEntity(propertyName), property.Value);
                }
                else if (propertyName == "data")
                {
                    var innerProperties = data!.Properties();
                    foreach (var innerProperty in innerProperties)
                    {
                         destination.Add(NameHelper.InverseMapEntity(innerProperty.Name), innerProperty.Value);
                    }
                }
            }
            return destination.ToObject(objectType);
        }
        

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetCustomAttribute<EntityAttribute>() != null;
        }
    }
}