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
        public override ReadOnlyCollection<WindowInfo>? ReadJson(JsonReader reader, Type objectType, ReadOnlyCollection<WindowInfo>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JArray jArray = JArray.Load(reader);
            List<WindowInfo> list = new(jArray.Count);
            foreach (JToken item in jArray)
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
