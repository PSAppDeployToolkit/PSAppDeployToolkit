using System;
using System.Text;
using Newtonsoft.Json;

namespace PSADT.ClientServer.Converters
{
    /// <summary>
    /// Custom JSON converter for <see cref="Encoding"/> that serializes only the encoding name
    /// to allow reconstruction via <see cref="Encoding.GetEncoding(string)"/>.
    /// </summary>
    internal sealed class EncodingConverter : JsonConverter<Encoding>
    {
        /// <summary>
        /// Reads and converts the JSON to an <see cref="Encoding"/> instance.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public override Encoding ReadJson(JsonReader reader, Type objectType, Encoding? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                throw new JsonSerializationException("Cannot deserialize null Encoding.");
            }
            if (reader.TokenType != JsonToken.String)
            {
                throw new JsonSerializationException($"Expected string token for Encoding, got {reader.TokenType}.");
            }
            if (reader.Value is not string encodingName || string.IsNullOrWhiteSpace(encodingName))
            {
                throw new JsonSerializationException("Encoding name cannot be null or empty.");
            }
            return Encoding.GetEncoding(encodingName);
        }

        /// <summary>
        /// Writes the <see cref="Encoding"/> as a JSON string containing only the encoding name.
        /// </summary>
        public override void WriteJson(JsonWriter writer, Encoding? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                throw new JsonSerializationException("Cannot serialize null Encoding.");
            }
            writer.WriteValue(value.WebName);
        }
    }
}
