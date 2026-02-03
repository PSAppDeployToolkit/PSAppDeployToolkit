using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using PSADT.ClientServer.Converters;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides utility methods for JSON serialization and deserialization, including Base64 encoding and decoding.
    /// </summary>
    /// <remarks>The <see cref="DataSerialization"/> class offers methods to serialize objects to JSON
    /// strings, encode them as Base64, and deserialize Base64-encoded JSON strings back into objects. It also provides
    /// default settings for JSON serialization using System.Text.Json, which can be used across the application. <para>
    /// Key features include: <list type="bullet"> <item><description>Serialization of objects to Base64-encoded JSON
    /// strings.</description></item> <item><description>Deserialization of Base64-encoded JSON strings into objects.
    /// </description></item> <item><description>Default JSON serializer settings for consistent
    /// behavior.</description></item> </list> </para> <para>
    /// This implementation uses custom <see cref="JsonConverter"/> classes to handle polymorphic deserialization 
    /// based on discriminator fields (like <c>PipeCommand</c> and <c>DialogType</c>).
    /// </para></remarks>
    public static class DataSerialization
    {
        /// <summary>
        /// Serializes the specified object directly to a UTF-8 encoded byte array.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize. Cannot be null.</param>
        /// <returns>A UTF-8 encoded byte array containing the JSON representation of the object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null.</exception>
        /// <exception cref="JsonException">Thrown if serialization fails.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public static byte[] SerializeToBytes<T>(T obj)
        {
            if (obj is null || (obj is string str && string.IsNullOrWhiteSpace(str)))
            {
                throw new ArgumentNullException(nameof(obj), "Object to serialize cannot be null.");
            }
            if (JsonSerializer.SerializeToUtf8Bytes(obj, typeof(T), DefaultJsonSerializerOptions) is { Length: > 0 } result)
            {
                return result;
            }
            throw new JsonException("Serialization returned an empty result.");
        }

        /// <summary>
        /// Deserializes the specified UTF-8 encoded byte array to an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <param name="json">A UTF-8 encoded byte array containing the JSON to deserialize. Cannot be null or empty.</param>
        /// <returns>An instance of type T deserialized from the specified JSON bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null or empty.</exception>
        /// <exception cref="JsonException">Thrown if deserialization fails or results in a null object.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public static T DeserializeFromBytes<T>(byte[] json)
        {
            if (json is null || json.Length == 0)
            {
                throw new ArgumentNullException(nameof(json), "Input bytes cannot be null or empty.");
            }
            if (JsonSerializer.Deserialize<T>(json, DefaultJsonSerializerOptions) is T result)
            {
                return result;
            }
            throw new JsonException("Deserialization returned a null result.");
        }

        /// <summary>
        /// Serializes the specified object to a JSON string and encodes it as a Base64 string.
        /// </summary>
        /// <remarks>This method uses default JSON serialization settings and encodes the resulting JSON
        /// string using UTF-8 before converting it to Base64. The caller can decode the Base64 string and parse the
        /// JSON to reconstruct the original object.</remarks>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize. Cannot be <see langword="null"/>.</param>
        /// <returns>A Base64-encoded string containing the JSON representation of the specified object.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public static string SerializeToString<T>(T obj)
        {
            if (obj is null || (obj is string str && string.IsNullOrWhiteSpace(str)))
            {
                throw new ArgumentNullException(nameof(obj), "Object to serialize cannot be null.");
            }
            return Convert.ToBase64String(SerializeToBytes(obj));
        }

        /// <summary>
        /// Deserializes a Base64-encoded JSON string into an object of the specified type.
        /// </summary>
        /// <remarks>This method expects the input string to be a valid Base64-encoded representation of a
        /// JSON object. Ensure that the input string is properly encoded and matches the expected structure of the
        /// target type.</remarks>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="base64Json">The Base64-encoded JSON string to deserialize. Cannot be null or empty.</param>
        /// <returns>An object of type <typeparamref name="T"/> deserialized from the provided JSON string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="base64Json"/> is null or empty.</exception>
        /// <exception cref="JsonException">Thrown if the deserialization process results in a null object.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public static T DeserializeFromString<T>(string base64Json)
        {
            if (string.IsNullOrWhiteSpace(base64Json))
            {
                throw new ArgumentNullException(nameof(base64Json), "Base64 JSON string cannot be null or empty.");
            }
            return DeserializeFromBytes<T>(Convert.FromBase64String(base64Json));
        }

        /// <summary>
        /// Deserializes a Base64-encoded JSON string into an object of the specified type.
        /// </summary>
        /// <remarks>This method first decodes the Base64 string into its original JSON format and then
        /// deserializes it using the default JSON serializer settings.</remarks>
        /// <param name="base64Json">The Base64-encoded JSON string to deserialize. This parameter cannot be null or empty.</param>
        /// <param name="type">The type to deserialize the JSON into.</param>
        /// <returns>An object representing the deserialized JSON data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="base64Json"/> is null or empty, or if <paramref name="type"/> is null.</exception>
        /// <exception cref="JsonException">Thrown if the deserialization process results in a null object.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public static object DeserializeFromString(string base64Json, Type type)
        {
            if (string.IsNullOrWhiteSpace(base64Json))
            {
                throw new ArgumentNullException(nameof(base64Json), "Base64 JSON string cannot be null or empty.");
            }
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type), "Type cannot be null.");
            }
            if (JsonSerializer.Deserialize(Convert.FromBase64String(base64Json), type, DefaultJsonSerializerOptions) is object result)
            {
                return result;
            }
            throw new JsonException("Deserialization returned a null result.");
        }

        /// <summary>
        /// The cached JsonSerializerOptions instance configured with the default settings.
        /// </summary>
        private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null,
            WriteIndented = false,
            Converters =
            {
                new ExceptionConverter(),
                new PipeRequestConverter(),
                new PipeResponseConverter(),
                new ShowModalDialogPayloadConverter(),
                new WindowInfoCollectionConverter(),
                new ProcessDefinitionCollectionConverter(),
                new CultureInfoConverter(),
                new ReadOnlyDictionaryConverter(),
                new IntPtrConverter(),
            },
        };
    }
}
