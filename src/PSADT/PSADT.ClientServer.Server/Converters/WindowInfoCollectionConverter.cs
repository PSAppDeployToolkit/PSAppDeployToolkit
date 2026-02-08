using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PSADT.WindowManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public override ReadOnlyCollection<WindowInfo> ReadJson(JsonReader reader, Type objectType, ReadOnlyCollection<WindowInfo>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                throw new JsonSerializationException("Cannot deserialize null WindowInfo collection.");
            }
            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new JsonSerializationException("Expected start of array.");
            }
            List<WindowInfo> list = []; foreach (JToken item in JArray.Load(reader))
            {
                list.Add(item.ToObject<WindowInfo>(serializer) ?? throw new JsonSerializationException("Failed to deserialize WindowInfo item."));
            }
            return new(list);
        }

        /// <summary>
        /// Writes the collection as JSON.
        /// </summary>
        public override void WriteJson(JsonWriter writer, ReadOnlyCollection<WindowInfo>? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Cannot serialize a null WindowInfo collection.");
            }
            writer.WriteStartArray();
            foreach (WindowInfo item in value)
            {
                serializer.Serialize(writer, item);
            }
            writer.WriteEndArray();
        }
    }
}
