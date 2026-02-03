using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using PSADT.WindowManagement;

namespace PSADT.ClientServer.Converters
{
    /// <summary>
    /// Custom JSON converter for collections of <see cref="WindowInfo"/> objects.
    /// </summary>
    internal sealed class WindowInfoCollectionConverter : JsonConverter<ReadOnlyCollection<WindowInfo>>
    {
        /// <summary>
        /// Reads and converts the JSON to a <see cref="ReadOnlyCollection{WindowInfo}"/>.
        /// </summary>
        public override ReadOnlyCollection<WindowInfo> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Cannot deserialize null WindowInfo collection.");
            }
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected start of array.");
            }
            List<WindowInfo> list = []; while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                list.Add(JsonSerializer.Deserialize<WindowInfo>(ref reader, options) ?? throw new JsonException("Failed to deserialize WindowInfo item."));
            }
            return new(list);
        }

        /// <summary>
        /// Writes the collection as JSON.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, ReadOnlyCollection<WindowInfo> value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Cannot serialize a null WindowInfo collection.");
            }
            writer.WriteStartArray();
            foreach (WindowInfo item in value)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
            writer.WriteEndArray();
        }
    }
}
