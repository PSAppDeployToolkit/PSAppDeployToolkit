using System;
using System.IO;
using System.Runtime.Serialization;

namespace PSADT.UserInterface.Utilities
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
        /// <remarks>This method uses the <see cref="System.Runtime.Serialization.DataContractSerializer"/> to serialize the object into XML format and then encodes the resulting XML string into a Base64 string. The output can be used for safe transmission or storage of the serialized data.</remarks>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize. Cannot be <see langword="null"/>.</param>
        /// <returns>A Base64-encoded string representation of the serialized XML data.</returns>
        public static string SerializeToString<T>(T obj)
        {
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
        /// <param name="type">The <see cref="Type"/> of the object to serialize. Must not be <see langword="null"/>.</param>
        /// <returns>A Base64-encoded string containing the XML representation of the serialized object.</returns>
        public static string SerializeToString(object obj, Type type)
        {
            using (var ms = new MemoryStream())
            {
                new DataContractSerializer(type).WriteObject(ms, obj);
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
        /// <param name="type">The <see cref="Type"/> of the object to deserialize. Cannot be <see langword="null"/>.</param>
        /// <returns>An object of the specified type, deserialized from the provided string.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the deserialization process results in a <see langword="null"/> object.</exception>
        public static object DeserializeFromString(string str, Type type)
        {
            using (var ms = new MemoryStream(Convert.FromBase64String(str)))
            {
                return new DataContractSerializer(type).ReadObject(ms)! ?? throw new InvalidOperationException("Deserialization returned a null result.");
            }
        }
    }
}
