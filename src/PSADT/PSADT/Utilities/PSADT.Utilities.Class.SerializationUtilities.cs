using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides utility methods for serializing and deserializing objects.
    /// </summary>
    /// <remarks>This class contains static methods for working with serialization, such as converting objects to XML strings. It is designed to simplify common serialization tasks and ensure consistent formatting.</remarks>
    public static class SerializationUtilities
    {
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
            using (var ms = new MemoryStream())
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
            using (var ms = new MemoryStream())
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
            using (var ms = new MemoryStream(Convert.FromBase64String(str)))
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
            using (var ms = new MemoryStream(Convert.FromBase64String(str)))
            using (var reader = new StreamReader(ms))
            {
                if (typeRegex.Match(reader.ReadToEnd()) is not Match match || !match.Success)
                {
                    throw new InvalidOperationException("The input string does not contain a valid type name in the expected format.");
                }
                ms.Position = 0; reader.DiscardBufferedData();
                string typeName = $"{match.Groups[2].Value}.{match.Groups[1].Value}";
                if (!typesTable.TryGetValue(typeName, out Type? type))
                {
                    throw new InvalidOperationException($"The type [{typeName}] could not be found in the current AppDomain.");
                }
                return new DataContractSerializer(type).ReadObject(ms)! ?? throw new InvalidOperationException("Deserialization returned a null result.");
            }
        }

        /// <summary>
        /// Creates a lookup table of all types available in the current application domain.
        /// </summary>
        /// <remarks>The method iterates through all assemblies loaded in the current application domain
        /// and collects their types into a dictionary, using the fully qualified type name as the key. The resulting
        /// dictionary is returned as a read-only collection.</remarks>
        /// <returns>A <see cref="ReadOnlyDictionary{TKey, TValue}"/> where the keys are fully qualified type names and the
        /// values are the corresponding <see cref="Type"/> objects.</returns>
        private static ReadOnlyDictionary<string, Type> BuildTypesLookupTable()
        {
            // Build out a lookup table of types and return as a ReadOnlyDictionary.
            Dictionary<string, Type> typesLookup = [];
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (null != type && type.IsPublic && null != type.FullName)
                    {
                        // Don't use .Add() here so types can clobber mscorlib if necessary.
                        typesLookup[type.FullName] = type;
                    }
                }
            }
            return new ReadOnlyDictionary<string, Type>(typesLookup);
        }

        /// <summary>
        /// A read-only dictionary that maps string keys to their corresponding <see cref="Type"/> objects.
        /// </summary>
        /// <remarks>This dictionary is initialized with a predefined set of mappings using the <see
        /// cref="BuildTypesLookupTable"/> method. It provides a thread-safe, immutable lookup table for type
        /// associations.</remarks>
        private static readonly ReadOnlyDictionary<string, Type> typesTable = BuildTypesLookupTable();

        /// <summary>
        /// Represents a compiled regular expression used to extract the type name from a DataContract namespace URI.
        /// </summary>
        /// <remarks>The regular expression matches the `xmlns` attribute in a DataContract namespace URI
        /// and captures the type name. This is useful for parsing serialized XML data that includes type
        /// information.</remarks>
        private static readonly Regex typeRegex = new Regex("^<(\\w+) xmlns=\"http://schemas.datacontract.org/2004/07/([^\"]+)\"", RegexOptions.Compiled);
    }
}
