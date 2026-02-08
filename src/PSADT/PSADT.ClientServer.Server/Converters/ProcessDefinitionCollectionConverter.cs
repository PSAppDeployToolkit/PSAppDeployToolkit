using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PSADT.ProcessManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSADT.ClientServer.Converters
{
    /// <summary>
    /// Custom JSON converter for collections of <see cref="ProcessDefinition"/> objects.
    /// </summary>
    internal sealed class ProcessDefinitionCollectionConverter : JsonConverter<ReadOnlyCollection<ProcessDefinition>>
    {
        /// <summary>
        /// Reads and converts the JSON to a <see cref="ReadOnlyCollection{ProcessDefinition}"/>.
        /// </summary>
        public override ReadOnlyCollection<ProcessDefinition> ReadJson(JsonReader reader, Type objectType, ReadOnlyCollection<ProcessDefinition>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                throw new JsonSerializationException("Cannot deserialize null ProcessDefinition collection.");
            }
            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new JsonSerializationException("Expected start of array.");
            }
            List<ProcessDefinition> list = []; foreach (JToken item in JArray.Load(reader))
            {
                list.Add(item.ToObject<ProcessDefinition>(serializer) ?? throw new JsonSerializationException("Failed to deserialize ProcessDefinition item."));
            }
            return list.Count == 0 ? throw new JsonSerializationException("ProcessDefinition collection cannot be empty.") : new(list);
        }

        /// <summary>
        /// Writes the collection as JSON.
        /// </summary>
        public override void WriteJson(JsonWriter writer, ReadOnlyCollection<ProcessDefinition>? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Cannot serialize a null ProcessDefinition collection.");
            }
            writer.WriteStartArray();
            foreach (ProcessDefinition item in value)
            {
                serializer.Serialize(writer, item);
            }
            writer.WriteEndArray();
        }
    }
}
