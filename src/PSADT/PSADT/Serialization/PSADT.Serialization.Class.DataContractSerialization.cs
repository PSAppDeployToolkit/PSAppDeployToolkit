using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace PSADT.Serialization
{
    /// <summary>
    /// Provides utility methods for serializing and deserializing objects.
    /// </summary>
    /// <remarks>This class contains static methods for working with serialization, such as converting objects to XML strings. It is designed to simplify common serialization tasks and ensure consistent formatting.</remarks>
    public static class DataContractSerialization
    {
        /// <summary>
        /// Initializes static data for the <see cref="DataContractSerialization"/> class.
        /// </summary>
        /// <remarks>This static constructor builds a lookup table of serializable types from the loaded
        /// assemblies in the current application domain. It also includes mappings for common primitive types. The
        /// lookup table is used to facilitate serialization and deserialization of objects by their type
        /// names.</remarks>
        static DataContractSerialization()
        {
            // Build out a lookup table of types from loaded assemblies.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Skip assemblies that have a null name.
                if (null == assembly.FullName)
                {
                    continue;
                }
                foreach (var type in assembly.GetTypes())
                {
                    // Skip any invalid type that aren't deserialisable.
                    if (!type.IsPublic || null == type.FullName || null == type.AssemblyQualifiedName || !type.IsSerializable)
                    {
                        continue;
                    }
                    typesTable[type.FullName] = type;
                }
            }

            // Inject types for primitives also.
            typesTable["int"] = typeof(int);
            typesTable["long"] = typeof(long);
            typesTable["string"] = typeof(string);
            typesTable["boolean"] = typeof(bool);
            typesTable["double"] = typeof(double);
            typesTable["float"] = typeof(float);
            typesTable["decimal"] = typeof(decimal);
            typesTable["char"] = typeof(char);
            typesTable["byte"] = typeof(sbyte);
            typesTable["unsignedByte"] = typeof(byte);
            typesTable["unsignedShort"] = typeof(ushort);
            typesTable["unsignedInt"] = typeof(uint);
            typesTable["unsignedLong"] = typeof(ulong);
        }

        /// <summary>
        /// Serializes the specified object to a Base64-encoded XML string.
        /// </summary>
        /// <remarks>This method uses the <see cref="DataContractSerializer"/> to serialize the object into XML format and then encodes the resulting XML string into a Base64 string. The output can be used for safe transmission or storage of the serialized data.</remarks>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize. Cannot be <see langword="null"/>.</param>
        /// <returns>A Base64-encoded string representation of the serialized XML data.</returns>
        public static string SerializeToString<T>(T obj)
        {
            if (null == obj)
            {
                throw new ArgumentNullException(nameof(obj), "The object to serialize cannot be null.");
            }
            using (MemoryStream ms = new())
            {
                new DataContractSerializer(typeof(T)).WriteObject(ms, obj);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <summary>
        /// Serializes the specified object to a Base64-encoded string representation of its XML format.
        /// </summary>
        /// <remarks>The method uses <see cref="DataContractSerializer"/> to serialize the object into XML format, and then encodes the resulting XML string into a Base64 string. The output can be used for storage or transmission in scenarios where Base64 encoding is required.</remarks>
        /// <param name="obj">The object to serialize. Must not be <see langword="null"/>.</param>
        /// <returns>A Base64-encoded string containing the XML representation of the serialized object.</returns>
        public static string SerializeToString(object obj)
        {
            if (null == obj)
            {
                throw new ArgumentNullException(nameof(obj), "The object to serialize cannot be null.");
            }
            using (MemoryStream ms = new())
            {
                new DataContractSerializer(obj.GetType()).WriteObject(ms, obj);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <summary>
        /// Deserializes an object of type <typeparamref name="T"/> from a Base64-encoded XML string.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="str">A Base64-encoded string containing the XML representation of the object.</param>
        /// <returns>An instance of type <typeparamref name="T"/> deserialized from the provided XML string, or <see langword="null"/> if the deserialization fails or the XML represents a null value.</returns>
        public static T DeserializeFromString<T>(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentNullException(nameof(str), "The input string cannot be null or empty.");
            }
            using (MemoryStream ms = new(Convert.FromBase64String(str)))
            {
                return (T)new DataContractSerializer(typeof(T)).ReadObject(ms)! ?? throw new InvalidOperationException("Deserialization returned a null result.");
            }
        }

        /// <summary>
        /// Deserializes a Base64-encoded XML string into an object of the specified type.
        /// </summary>
        /// <remarks>The input string must be a Base64-encoded representation of an XML document. The method uses <see cref="DataContractSerializer"/> for deserialization, which requires the type to be decorated with appropriate data contract attributes.</remarks>
        /// <param name="str">The Base64-encoded XML string to deserialize. Cannot be <see langword="null"/> or empty.</param>
        /// <returns>An object of the specified type, deserialized from the provided string.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the deserialization process results in a <see langword="null"/> object.</exception>
        public static dynamic DeserializeFromString(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentNullException(nameof(str), "The input string cannot be null or empty.");
            }
            using (MemoryStream ms = new(Convert.FromBase64String(str)))
            using (StreamReader reader = new(ms))
            {
                // Work out the type name via regex if we can.
                string dcsData = reader.ReadToEnd(); string typeName = string.Empty;
                if (primitiveRegex.Match(dcsData) is Match primitive && primitive.Success)
                {
                    typeName = primitive.Groups[1].Value;
                }
                else if (typeRegex.Match(dcsData) is Match match && match.Success)
                {
                    typeName = $"{match.Groups[2].Value}.{match.Groups[1].Value}";
                }
                else
                {
                    throw new InvalidOperationException("The input string does not contain a valid type name in the expected format.");
                }
                ms.Position = 0; reader.DiscardBufferedData();

                // Get the type object from the lookup table and return a deserialised object.
                if (!typesTable.TryGetValue(typeName, out Type? type))
                {
                    throw new InvalidOperationException($"The type [{typeName}] could not be found in the current AppDomain.");
                }
                return new DataContractSerializer(type).ReadObject(ms)! ?? throw new InvalidOperationException("Deserialization returned a null result.");
            }
        }

        /// <summary>
        /// Adds the specified type to the lookup table if it is not already present.
        /// </summary>
        /// <remarks>If the type's fully qualified name is already present in the lookup table,  this
        /// method does nothing.</remarks>
        /// <param name="type">The type to add to the lookup table. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is <see langword="null"/>.</exception>
        internal static void AddSerializableType(Type type)
        {
            if (null == type)
            {
                throw new ArgumentNullException(nameof(type), "Input cannot be null or empty.");
            }
            if (!typesTable.ContainsKey(type.FullName!))
            {
                typesTable[type.FullName!] = type;
            }
        }

        /// <summary>
        /// Represents a mapping between type names and their corresponding <see cref="Type"/> objects.
        /// </summary>
        /// <remarks>This dictionary is used to store and retrieve type information based on string keys.
        /// It is intended for internal use and should not be accessed directly by external code.</remarks>
        private static readonly Dictionary<string, Type> typesTable = [];

        /// <summary>
        /// Represents a compiled regular expression used to match primitive type names in a specific format.
        /// </summary>
        /// <remarks>The regular expression checks for type names enclosed in angle brackets, such as
        /// `<int>` or `<string>`. Supported types include common primitives like int, long, string, boolean, and
        /// various numeric types.</remarks>
        private static readonly Regex primitiveRegex = new Regex(@"^<(int|long|string|boolean|double|float|decimal|char|byte|unsignedByte|unsignedShort|unsignedInt|unsignedLong)", RegexOptions.Compiled);

        /// <summary>
        /// Represents a compiled regular expression used to extract the type name from a DataContract namespace URI.
        /// </summary>
        /// <remarks>The regular expression matches the `xmlns` attribute in a DataContract namespace URI
        /// and captures the type name. This is useful for parsing serialized XML data that includes type
        /// information.</remarks>
        private static readonly Regex typeRegex = new Regex("^<(\\w+) xmlns=\"http://schemas.datacontract.org/2004/07/([^\"]+)\"", RegexOptions.Compiled);
    }
}
