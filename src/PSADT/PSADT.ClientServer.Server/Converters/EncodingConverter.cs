using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        public override Encoding Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Cannot deserialize null Encoding.");
            }
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected string token for Encoding, got {reader.TokenType}.");
            }
            if (reader.GetString() is not string encodingName || string.IsNullOrWhiteSpace(encodingName))
            {
                throw new JsonException("Encoding name cannot be null or empty.");
            }
            return Encoding.GetEncoding(encodingName);
        }

        /// <summary>
        /// Writes the <see cref="Encoding"/> as a JSON string containing only the encoding name.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, Encoding value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                throw new JsonException("Cannot serialize null Encoding.");
            }
            writer.WriteStringValue(value.WebName);
        }
    }
}
