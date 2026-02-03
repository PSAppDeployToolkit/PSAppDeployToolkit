using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using PSADT.ClientServer.Converters;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides utility methods for JSON serialization and deserialization, including Base64 encoding and decoding.
    /// </summary>
    /// <remarks>The <see cref="DataSerialization"/> class offers methods to serialize objects to JSON
    /// strings, encode them as Base64, and deserialize Base64-encoded JSON strings back into objects. It also provides
    /// default settings for JSON serialization using Newtonsoft.Json, which can be used across the application. <para>
    /// Key features include: <list type="bullet"> <item><description>Serialization of objects to Base64-encoded JSON
    /// strings.</description></item> <item><description>Deserialization of Base64-encoded JSON strings into objects.
    /// </description></item> <item><description>Default JSON serializer settings for consistent
    /// behavior.</description></item> </list> </para> <para>
    /// This implementation uses <c>TypeNameHandling.None</c> for security, with custom <see cref="JsonConverter"/>
    /// classes to handle polymorphic deserialization based on discriminator fields (like <c>PipeCommand</c> and
    /// <c>DialogType</c>). This approach is fully compliant with CA2326 and CA2327 security rules.
    /// </para></remarks>
    public static class DataSerialization
    {
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
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj), "Object to serialize cannot be null.");
            }
            if (JsonConvert.SerializeObject(obj, DefaultJsonSerializerSettings) is not string res || string.IsNullOrWhiteSpace(JsonControlCharacters.Replace(res, string.Empty).Trim()))
            {
                throw new JsonSerializationException("Serialization returned an empty string.");
            }
            return Convert.ToBase64String(ServerInstance.DefaultEncoding.GetBytes(res));
        }

        /// <summary>
        /// Serializes the specified object to a JSON string and encodes it as a Base64 string.
        /// </summary>
        /// <remarks>This method uses default JSON serialization settings and encodes the resulting JSON
        /// string using UTF-8 before converting it to Base64. The caller can decode the Base64 string and parse the
        /// JSON to reconstruct the original object.</remarks>
        /// <param name="obj">The object to serialize. Must not be <see langword="null"/>.</param>
        /// <returns>A Base64-encoded string containing the JSON representation of the specified object.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public static string SerializeToString(object obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj), "Object to serialize cannot be null.");
            }
            if (JsonConvert.SerializeObject(obj, DefaultJsonSerializerSettings) is not string res || string.IsNullOrWhiteSpace(JsonControlCharacters.Replace(res, string.Empty).Trim()))
            {
                throw new JsonSerializationException("Serialization returned an empty string.");
            }
            return Convert.ToBase64String(ServerInstance.DefaultEncoding.GetBytes(res));
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
        /// <exception cref="JsonSerializationException">Thrown if the deserialization process results in a null object.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public static T DeserializeFromString<T>(string base64Json)
        {
            if (string.IsNullOrWhiteSpace(base64Json))
            {
                throw new ArgumentNullException(nameof(base64Json), "Base64 JSON string cannot be null or empty.");
            }
            if (JsonConvert.DeserializeObject<T>(ServerInstance.DefaultEncoding.GetString(Convert.FromBase64String(base64Json)), DefaultJsonSerializerSettings) is not T res)
            {
                throw new JsonSerializationException("Deserialization returned a null result.");
            }
            return res;
        }

        /// <summary>
        /// Deserializes a Base64-encoded JSON string into an object.
        /// </summary>
        /// <remarks>This method first decodes the Base64 string into its original JSON format and then
        /// deserializes it using the default JSON serializer settings. Ensure that the input string is a valid
        /// Base64-encoded representation of JSON data.</remarks>
        /// <param name="base64Json">The Base64-encoded JSON string to deserialize. This parameter cannot be null or empty.</param>
        /// <returns>An object representing the deserialized JSON data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="base64Json"/> is null or empty.</exception>
        /// <exception cref="JsonSerializationException">Thrown if the deserialization process results in a null object.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public static object DeserializeFromString(string base64Json)
        {
            if (string.IsNullOrWhiteSpace(base64Json))
            {
                throw new ArgumentNullException(nameof(base64Json), "Base64 JSON string cannot be null or empty.");
            }
            if (JsonConvert.DeserializeObject(ServerInstance.DefaultEncoding.GetString(Convert.FromBase64String(base64Json)), DefaultJsonSerializerSettings) is not object res)
            {
                throw new JsonSerializationException("Deserialization returned a null result.");
            }
            return res;
        }

        /// <summary>
        /// Provides the default settings for JSON serialization and deserialization using Newtonsoft.Json.
        /// </summary>
        /// <remarks>These settings include the following configurations: <list type="bullet">
        /// <item><description><c>TypeNameHandling.None</c> for security (no type information in JSON).</description></item>
        /// <item><description>Excludes null values and default values from the serialized output.</description></item>
        /// <item><description>Custom converters for polymorphic types using discriminator fields.</description></item>
        /// <item><description>Custom exception converter that validates types against a secure allowlist.</description></item>
        /// </list> This static field can be used as a standard configuration for JSON serialization across the application.
        /// <para>
        /// The custom converters handle polymorphic deserialization securely by using known discriminator fields
        /// (like <c>PipeCommand</c> and <c>DialogType</c>) to determine the concrete type, rather than relying on
        /// type information embedded in the JSON.
        /// </para></remarks>
        private static readonly JsonSerializerSettings DefaultJsonSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.None,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Formatting = Formatting.None,
            Converters =
            [
                new ExceptionConverter(),
                new PipeRequestConverter(),
                new PipeResponseConverter(),
                new ShowModalDialogPayloadConverter(),
                new WindowInfoCollectionConverter(),
                new ProcessDefinitionCollectionConverter(),
            ],
        };

        /// <summary>
        /// Represents a regular expression that matches common JSON control characters, including brackets, braces,
        /// commas, single quotes, and double quotes.
        /// </summary>
        /// <remarks>This regular expression uses culture-invariant and case-insensitive matching to
        /// identify JSON structural characters. It can be used to detect or process control characters in JSON strings
        /// for validation or parsing purposes.</remarks>
        private static readonly Regex JsonControlCharacters = new(@"null|\[|\]|\{|\}|\,|""|\'", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }
}
