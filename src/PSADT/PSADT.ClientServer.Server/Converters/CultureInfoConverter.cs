using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PSADT.ClientServer.Converters
{
    /// <summary>
    /// Custom JSON converter for <see cref="CultureInfo"/> that serializes only the culture name
    /// to avoid circular references from the Parent property chain.
    /// </summary>
    internal sealed class CultureInfoConverter : JsonConverter<CultureInfo>
    {
        /// <summary>
        /// Reads and converts the JSON to a <see cref="CultureInfo"/> instance.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public override CultureInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Cannot deserialize null CultureInfo.");
            }
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected string token for CultureInfo, got {reader.TokenType}.");
            }
            if (reader.GetString() is not string cultureName || string.IsNullOrWhiteSpace(cultureName))
            {
                throw new JsonException("Culture name cannot be null or empty.");
            }
            return CultureInfo.GetCultureInfo(cultureName);
        }

        /// <summary>
        /// Writes the <see cref="CultureInfo"/> as a JSON string containing only the culture name.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, CultureInfo value, JsonSerializerOptions options)
        {
            // Serialize only the culture name to avoid circular references.
            if (value is null)
            {
                throw new JsonException("Cannot serialize null CultureInfo.");
            }
            writer.WriteStringValue(value.Name);
        }
    }
}
