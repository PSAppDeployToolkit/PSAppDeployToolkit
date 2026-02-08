using System;
using System.IO;
using System.Runtime.Serialization;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides utility methods for serializing and deserializing objects.
    /// </summary>
    /// <remarks>This class contains static methods for working with serialization, such as converting objects to XML strings. It is designed to simplify common serialization tasks and ensure consistent formatting.</remarks>
    public static class SimpleSerializer
    {
        /// <summary>
        /// Serializes the specified object to a Base64-encoded XML string.
        /// </summary>
        /// <remarks>This method uses the <see cref="DataContractSerializer"/> to serialize the object into XML format and then encodes the resulting XML string into a Base64 string. The output can be used for safe transmission or storage of the serialized data.</remarks>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize. Cannot be <see langword="null"/>.</param>
        /// <returns>A Base64-encoded string representation of the serialized XML data.</returns>
        public static string Serialize<T>(T obj)
        {
            if (null == obj || (obj is string str && string.IsNullOrWhiteSpace(str)))
            {
                throw new ArgumentNullException(nameof(obj), "The object to serialize cannot be null.");
            }
            using MemoryStream ms = new();
            new DataContractSerializer(typeof(T)).WriteObject(ms, obj);
            string output = Convert.ToBase64String(ms.ToArray());
            return string.IsNullOrWhiteSpace(output) ? throw new InvalidOperationException("Serialization returned a null result") : output;
        }

        /// <summary>
        /// Deserializes an object of type <typeparamref name="T"/> from a Base64-encoded XML string.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="str">A Base64-encoded string containing the XML representation of the object.</param>
        /// <returns>An instance of type <typeparamref name="T"/> deserialized from the provided XML string, or <see langword="null"/> if the deserialization fails or the XML represents a null value.</returns>
        public static T Deserialize<T>(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentNullException(nameof(str), "The input string cannot be null or empty.");
            }
            using MemoryStream ms = new(Convert.FromBase64String(str));
            return (T)new DataContractSerializer(typeof(T)).ReadObject(ms)! ?? throw new InvalidOperationException("Deserialization returned a null result.");
        }

        /// <summary>
        /// Deserializes a Base64-encoded XML string into an object of the specified type.
        /// </summary>
        /// <remarks>The input string must be a Base64-encoded representation of an XML document. The method uses <see cref="DataContractSerializer"/> for deserialization, which requires the type to be decorated with appropriate data contract attributes.</remarks>
        /// <param name="str">The Base64-encoded XML string to deserialize. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="type">The <see cref="Type"/> of the object to deserialize. Cannot be <see langword="null"/>.</param>
        /// <returns>An object of the specified type, deserialized from the provided string.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the deserialization process results in a <see langword="null"/> object.</exception>
        public static object Deserialize(string str, Type type)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentNullException(nameof(str), "The input string cannot be null or empty.");
            }
            using MemoryStream ms = new(Convert.FromBase64String(str));
            return new DataContractSerializer(type).ReadObject(ms)! ?? throw new InvalidOperationException("Deserialization returned a null result.");
        }
    }
}
