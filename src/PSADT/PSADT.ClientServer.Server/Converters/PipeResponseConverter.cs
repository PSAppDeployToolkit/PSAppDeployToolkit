using System;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        public override PipeResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Validate the token type.
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Cannot deserialize null PipeResponse.");
            }

            // Get the document from from the reader.
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;

            // Get the members from the document and construct the object.
            object? result = null; Exception? error = null;
            if (root.TryGetProperty("Result", out JsonElement resultElement) && resultElement.ValueKind != JsonValueKind.Null)
            {
                result = DeserializeResult(resultElement);
            }
            if (root.TryGetProperty("Error", out JsonElement errorElement) && errorElement.ValueKind != JsonValueKind.Null)
            {
                error = JsonSerializer.Deserialize<Exception>(errorElement.GetRawText(), options);
            }
            return new PipeResponse(result, error);
        }

        /// <summary>
        /// Writes the <see cref="PipeResponse"/> as JSON.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, PipeResponse value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Cannot serialize a null PipeResponse.");
            }
            writer.WriteStartObject();
            if (value.Result is not null)
            {
                writer.WritePropertyName("Result");
                JsonSerializer.Serialize(writer, value.Result, value.Result.GetType(), options);
            }
            if (value.Error is not null)
            {
                writer.WritePropertyName("Error");
                JsonSerializer.Serialize(writer, value.Error, options);
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// Deserializes the result based on its JSON type.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Populate switch", Justification = "All known JsonValueKind values are handled.")]
        private static object? DeserializeResult(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when element.TryGetInt64(out long l) => l,
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Array => JsonSerializer.Deserialize<object[]>(element.GetRawText()),
                JsonValueKind.Object => JsonSerializer.Deserialize<object>(element.GetRawText()),
                _ => throw new JsonException($"Unexpected JsonValueKind [{element.ValueKind}] encountered while deserializing result."),
            };
        }
    }
}
