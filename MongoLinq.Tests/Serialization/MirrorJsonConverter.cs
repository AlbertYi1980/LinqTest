using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualBasic;
using MongoLinq.Tests.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MongoLinq.Tests.Serialization
{
    public class MirrorJsonConverter : JsonConverter
    {
        private const string IdPropertyName = "Id";
        private static readonly string[] OtherSystemPropertyNames = {"CreateAt"};

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var source = JObject.FromObject(value);
            var properties = source.Properties();
            var destination = new JObject();
            var data = new JObject();
            foreach (var property in properties)
            {
                var propertyName = property.Name;
                if (propertyName == IdPropertyName)
                {
                    destination.Add("_id", property.Value);
                }
                else if (OtherSystemPropertyNames.Contains(propertyName))
                {
                    destination.Add(ToCamelCase(propertyName), property.Value);
                }
                else
                {
                    data.Add(ToCamelCase(propertyName), property.Value);
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
                if (propertyName == "_id")
                {
                    destination.Add(IdPropertyName, property.Value);
                }
                else if (OtherSystemPropertyNames.Contains(ToPascalCase( propertyName)))
                {
                    destination.Add(ToPascalCase(propertyName), property.Value);
                }
                else if (propertyName == "data")
                {
                    var innerProperties = data!.Properties();
                    foreach (var innerProperty in innerProperties)
                    {
                         destination.Add(ToPascalCase(innerProperty.Name), innerProperty.Value);
                    }
                }
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