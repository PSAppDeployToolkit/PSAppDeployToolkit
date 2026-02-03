using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSADT.ClientServer.Converters
{
    /// <summary>
    /// Custom JSON converter for <see cref="PipeResponse"/> that handles polymorphic result deserialization.
    /// </summary>
    /// <remarks>Since the server knows what command it sent, it can specify the expected result type
    /// when deserializing. This converter handles the basic deserialization and the result can be
    /// cast to the expected type by the caller.</remarks>
    internal sealed class PipeResponseConverter : JsonConverter<PipeResponse>
    {
        /// <summary>
        /// Reads and converts the JSON to a <see cref="PipeResponse"/> instance.
        /// </summary>
        public override PipeResponse? ReadJson(JsonReader reader, Type objectType, PipeResponse? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            return new(jObject["Result"]?.ToObject<object>(serializer), jObject["Error"]?.ToObject<Exception>(serializer));
        }

        /// <summary>
        /// Writes the <see cref="PipeResponse"/> as JSON.
        /// </summary>
        public override void WriteJson(JsonWriter writer, PipeResponse? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Cannot serialize a null PipeResponse.");
            }
            writer.WriteStartObject();
            if (value.Result is not null)
            {
                writer.WritePropertyName("Result");
                serializer.Serialize(writer, value.Result);
            }
            if (value.Error is not null)
            {
                writer.WritePropertyName("Error");
                serializer.Serialize(writer, value.Error);
            }
            writer.WriteEndObject();
        }
    }
}
