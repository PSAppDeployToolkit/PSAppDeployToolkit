using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PSADT.ClientServer.Converters
{
    /// <summary>
    /// Custom JSON converter for <see cref="IntPtr"/> that serializes as a 64-bit integer.
    /// </summary>
    internal sealed class IntPtrConverter : JsonConverter<IntPtr>
    {
        /// <summary>
        /// Reads and converts the JSON to an <see cref="IntPtr"/>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Populate switch", Justification = "Default case handles all other token types.")]
        public override IntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => new IntPtr(reader.GetInt64()),
                _ => throw new JsonException($"Expected number token for IntPtr, got {reader.TokenType}.")
            };
        }

        /// <summary>
        /// Writes the <see cref="IntPtr"/> as a JSON number.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, IntPtr value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.ToInt64());
        }
    }
}
