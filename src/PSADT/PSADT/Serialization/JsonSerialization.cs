using System;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PSADT.Serialization
{
    /// <summary>
    /// Provides utility methods for JSON serialization and deserialization, including Base64 encoding and decoding.
    /// </summary>
    /// <remarks>The <see cref="JsonSerialization"/> class offers methods to serialize objects to JSON
    /// strings, encode them as Base64, and deserialize Base64-encoded JSON strings back into objects. It also provides
    /// default settings for JSON serialization using Newtonsoft.Json, which can be used across the application. <para>
    /// Key features include: <list type="bullet"> <item><description>Serialization of objects to Base64-encoded JSON
    /// strings.</description></item> <item><description>Deserialization of Base64-encoded JSON strings into objects or
    /// dynamic types.</description></item> <item><description>Default JSON serializer settings for consistent
    /// behavior.</description></item> </list> </para></remarks>
    public static class JsonSerialization
    {
        /// <summary>
        /// Initializes the <see cref="JsonSerialization"/> class and configures default settings to ensure
        /// compatibility between .NET Core and .NET Framework.
        /// </summary>
        /// <remarks>This static constructor sets the default serialization binder to a compatibility
        /// binder when running on the .NET Framework. This ensures that serialized objects can be properly deserialized
        /// across different .NET runtime environments.</remarks>
        static JsonSerialization()
        {
            // Set the default serialization binder to ensure compatibility between .NET Core and .NET Framework.
            if (RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework"))
            {
                DefaultJsonSerializerSettings.SerializationBinder = new DotNetCompatibleSerializationBinder();
            }
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
        public static string SerializeToString<T>(T obj)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, Formatting.None, DefaultJsonSerializerSettings)));
        }

        /// <summary>
        /// Serializes the specified object to a JSON string and encodes it as a Base64 string.
        /// </summary>
        /// <remarks>This method uses default JSON serialization settings and encodes the resulting JSON
        /// string using UTF-8 before converting it to Base64. The caller can decode the Base64 string and parse the
        /// JSON to reconstruct the original object.</remarks>
        /// <param name="obj">The object to serialize. Must not be <see langword="null"/>.</param>
        /// <returns>A Base64-encoded string containing the JSON representation of the specified object.</returns>
        public static string SerializeToString(object obj)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, Formatting.None, DefaultJsonSerializerSettings)));
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
        public static T DeserializeFromString<T>(string base64Json)
        {
            if (string.IsNullOrEmpty(base64Json))
            {
                throw new ArgumentNullException(nameof(base64Json), "Base64 JSON string cannot be null or empty.");
            }
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(Convert.FromBase64String(base64Json)), DefaultJsonSerializerSettings) ?? throw new JsonSerializationException("Deserialization returned a null result.");
        }

        /// <summary>
        /// Deserializes a Base64-encoded JSON string into a dynamic object.
        /// </summary>
        /// <remarks>This method first decodes the Base64 string into its original JSON format and then
        /// deserializes it using the default JSON serializer settings. Ensure that the input string is a valid
        /// Base64-encoded representation of JSON data.</remarks>
        /// <param name="base64Json">The Base64-encoded JSON string to deserialize. This parameter cannot be null or empty.</param>
        /// <returns>A dynamic object representing the deserialized JSON data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="base64Json"/> is null or empty.</exception>
        /// <exception cref="JsonSerializationException">Thrown if the deserialization process results in a null object.</exception>
        public static dynamic DeserializeFromString(string base64Json)
        {
            if (string.IsNullOrEmpty(base64Json))
            {
                throw new ArgumentNullException(nameof(base64Json), "Base64 JSON string cannot be null or empty.");
            }
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(Convert.FromBase64String(base64Json)), DefaultJsonSerializerSettings) ?? throw new JsonSerializationException("Deserialization returned a null result.");
        }

        /// <summary>
        /// Provides the default settings for JSON serialization and deserialization using Newtonsoft.Json.
        /// </summary>
        /// <remarks>These settings include the following configurations: <list type="bullet">
        /// <item><description>Indented formatting for improved readability.</description></item>
        /// <item><description>Excludes null values and default values from the serialized output.</description></item>
        /// <item><description>Disables type name handling to avoid including type metadata in the
        /// JSON.</description></item> <item><description>Uses ISO 8601 format for date
        /// serialization.</description></item> <item><description>Includes a converter for serializing and
        /// deserializing enums as strings.</description></item> </list> This static field can be used as a standard
        /// configuration for JSON serialization across the application.</remarks>
        private static readonly JsonSerializerSettings DefaultJsonSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
        };

        /// <summary>
        /// Provides a serialization binder that ensures compatibility between .NET Core and .NET Framework by
        /// resolving types with special handling for core library assemblies.
        /// </summary>
        /// <remarks>This binder overrides the default type resolution behavior to replace the .NET
        /// Core-specific core library assembly name ("System.Private.CoreLib") with the .NET Framework equivalent 
        /// ("mscorlib"). This ensures that types serialized in one runtime environment can be correctly deserialized
        /// in another.</remarks>
        private sealed class DotNetCompatibleSerializationBinder : DefaultSerializationBinder
        {
            /// <summary>
            /// Resolves a type from its assembly name and type name, with special handling for core library assemblies.
            /// </summary>
            /// <remarks>If the specified assembly name matches the core library assembly, it is
            /// replaced with the standard mscorlib assembly name. This ensures compatibility when resolving types
            /// across different runtime environments.</remarks>
            /// <param name="assemblyName">The name of the assembly containing the type. Can be <see langword="null"/>.</param>
            /// <param name="typeName">The name of the type to resolve.</param>
            /// <returns>The <see cref="Type"/> object representing the resolved type.</returns>
            public override Type BindToType(string? assemblyName, string typeName)
            {
                if (assemblyName == CoreLibAssembly)
                {
                    assemblyName = MscorlibAssembly;
                    typeName = typeName.Replace(CoreLibAssembly, MscorlibAssembly);
                }
                return base.BindToType(assemblyName, typeName);
            }

            /// <summary>
            /// Represents the name of the core library assembly used by the .NET (Core) runtime.
            /// </summary>
            private const string CoreLibAssembly = "System.Private.CoreLib";

            /// <summary>
            /// Represents the name of the mscorlib assembly.
            /// </summary>
            /// <remarks>This constant is used to reference the mscorlib assembly, which contains
            /// fundamental classes and base types used by the .NET Framework.</remarks>
            private const string MscorlibAssembly = "mscorlib";
        }
    }
}
