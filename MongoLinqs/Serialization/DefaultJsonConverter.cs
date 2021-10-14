using System;
using System.Collections.Generic;
using System.Reflection;
using MongoLinqs.Pipelines.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace MongoLinqs.Serialization
{
    
    public class CustomContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
           return NameHelper.MapEntity(propertyName);
        }
    }
    
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
                destination.Add(NameHelper.MapEntity( propertyName), property.Value);
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
                destination.Add(NameHelper.InverseMapEntity( propertyName), property.Value);
            }
            return destination.ToObject(objectType);
        }
        

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetCustomAttribute<EntityAttribute>() != null;
        }

       
    }
}