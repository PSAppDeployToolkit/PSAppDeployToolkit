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
        public override ReadOnlyCollection<ProcessDefinition>? ReadJson(JsonReader reader, Type objectType, ReadOnlyCollection<ProcessDefinition>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JArray jArray = JArray.Load(reader);
            if (jArray.Count == 0)
            {
                throw new JsonSerializationException("ProcessDefinition collection cannot be empty.");
            }
            List<ProcessDefinition> list = new(jArray.Count);
            foreach (JToken item in jArray)
            {
                list.Add(item.ToObject<ProcessDefinition>(serializer) ?? throw new JsonSerializationException("Failed to deserialize ProcessDefinition item."));
            }
            return new(list);
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
