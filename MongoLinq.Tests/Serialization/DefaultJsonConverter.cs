using System;
using System.Linq;
using System.Reflection;
using MongoLinq.Tests.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MongoLinq.Tests.Serialization
{
    public class DefaultJsonConverter : JsonConverter
    {
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var source = JObject.FromObject(value);
            var properties = source.Properties();
            var destination = new JObject();
            foreach (var property in properties)
            {
                var propertyName = property.Name;
                var destinationPropertyName = propertyName == "Id" ? "_id" : ToCamelCase(propertyName);
                destination.Add(destinationPropertyName, property.Value);
            }
     
            destination.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var source = JObject.Load(reader);
            var destination = new JObject();
            var properties = source.Properties();
            foreach (var property in properties)
            {
                var propertyName = property.Name;
                var destinationPropertyName = propertyName == "_id" ? "Id" : ToPascalCase(propertyName);
                destination.Add(destinationPropertyName, property.Value);
            }
            return destination.ToObject(objectType);
        }
        

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetCustomAttribute<EntityAttribute>() != null;
        }

        private static string ToCamelCase(string s)
        {
            if (s == null) return null;
            if (s == string.Empty) return s;
            return s.Substring(0, 1).ToLower() + s.Substring(1);
        }
        
        private static string ToPascalCase(string s)
        {
            if (s == null) return null;
            if (s == string.Empty) return s;
            return s.Substring(0, 1).ToUpper() + s.Substring(1);
        }
    }
}