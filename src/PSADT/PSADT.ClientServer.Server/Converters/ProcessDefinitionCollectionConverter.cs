using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using PSADT.ProcessManagement;

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
        public override ReadOnlyCollection<ProcessDefinition> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Cannot deserialize null ProcessDefinition collection.");
            }
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected start of array.");
            }
            List<ProcessDefinition> list = []; while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                list.Add(JsonSerializer.Deserialize<ProcessDefinition>(ref reader, options) ?? throw new JsonException("Failed to deserialize ProcessDefinition item."));
            }
            return list.Count == 0 ? throw new JsonException("ProcessDefinition collection cannot be empty.") : new(list);
        }

        /// <summary>
        /// Writes the collection as JSON.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, ReadOnlyCollection<ProcessDefinition> value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Cannot serialize a null ProcessDefinition collection.");
            }
            writer.WriteStartArray();
            foreach (ProcessDefinition item in value)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
            writer.WriteEndArray();
        }
    }
}
