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
        /// Serializes the specified object to a byte array using the DataContractSerializer.
        /// </summary>
        /// <remarks>The method uses DataContractSerializer to perform serialization. The object type must
        /// be compatible with DataContractSerializer, which requires appropriate data contract attributes. If the
        /// object cannot be serialized, an exception may be thrown by the serializer.</remarks>
        /// <typeparam name="T">The type of the object to serialize. Must be serializable by DataContractSerializer.</typeparam>
        /// <param name="obj">The object to serialize. Cannot be null or, if a string, empty or whitespace.</param>
        /// <returns>A byte array containing the serialized representation of the object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null, or if <paramref name="obj"/> is a string that is empty or consists
        /// only of whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown if serialization results in an empty byte array.</exception>
        public static byte[] SerializeToBytes<T>(T obj)
        {
            if (null == obj || (obj is string str && string.IsNullOrWhiteSpace(str)))
            {
                throw new ArgumentNullException(nameof(obj), "The object to serialize cannot be null.");
            }
            using MemoryStream ms = new();
            new DataContractSerializer(typeof(T)).WriteObject(ms, obj);
            byte[] output = ms.ToArray();
            return output.Length == 0 ? throw new InvalidOperationException("Serialization returned a null result") : output;
        }

        /// <summary>
        /// Deserializes an object of type T from the specified byte array using the DataContractSerializer.
        /// </summary>
        /// <remarks>The byte array must represent a valid DataContractSerializer serialization of type T.
        /// This method does not perform schema validation and will throw an exception if the data is not compatible
        /// with the specified type.</remarks>
        /// <typeparam name="T">The type of the object to deserialize from the byte array.</typeparam>
        /// <param name="bytes">The byte array containing the serialized representation of the object. Cannot be null or empty.</param>
        /// <returns>The deserialized object of type T.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="bytes"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if deserialization returns a null result.</exception>
        public static T DeserializeFromBytes<T>(byte[] bytes)
        {
            return (T)DeserializeFromBytes(bytes, typeof(T));
        }

        /// <summary>
        /// Serializes the specified object to a Base64-encoded string representation.
        /// </summary>
        /// <remarks>Use this method to obtain a string representation suitable for storage or
        /// transmission. The resulting string can be deserialized using a corresponding method that accepts a
        /// Base64-encoded input. The serialization format is determined by the implementation of the underlying
        /// serialization method.</remarks>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize. Cannot be null.</param>
        /// <returns>A Base64-encoded string containing the serialized form of the object.</returns>
        public static string SerializeToString<T>(T obj)
        {
            return Convert.ToBase64String(SerializeToBytes(obj));
        }

        /// <summary>
        /// Deserializes an object of type T from a Base64-encoded string representation.
        /// </summary>
        /// <remarks>The input string must be a valid Base64-encoded representation of the serialized
        /// object. If the string is not properly formatted or does not represent a valid serialized object,
        /// deserialization may fail and throw an exception.</remarks>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="str">The Base64-encoded string containing the serialized object data. Cannot be null or empty.</param>
        /// <returns>An instance of type T deserialized from the specified string.</returns>
        public static T DeserializeFromString<T>(string str)
        {
            return DeserializeFromBytes<T>(Convert.FromBase64String(str));
        }

        /// <summary>
        /// Deserializes an object from a byte array using the specified type and the DataContractSerializer.
        /// </summary>
        /// <remarks>The deserialization uses the DataContractSerializer, which requires that the target
        /// type and its members are properly attributed for serialization. Ensure that the byte array was produced by a
        /// compatible serializer to avoid errors.</remarks>
        /// <param name="bytes">The byte array containing the serialized object data. Cannot be null or empty.</param>
        /// <param name="type">The type of the object to deserialize. Specifies the target type for the deserialization operation.</param>
        /// <returns>An object instance of the specified type that was deserialized from the byte array.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="bytes"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the deserialization process returns a null result.</exception>
        public static object DeserializeFromBytes(byte[] bytes, Type type)
        {
            if (bytes is null || bytes.Length == 0)
            {
                throw new ArgumentNullException(nameof(bytes), "The input byte array cannot be null or empty.");
            }
            using MemoryStream ms = new(bytes, false);
            return new DataContractSerializer(type).ReadObject(ms)! ?? throw new InvalidOperationException("Deserialization returned a null result.");
        }

        /// <summary>
        /// Deserializes an object of the specified type from a base64-encoded string representation.
        /// </summary>
        /// <remarks>The input string must be a valid base64-encoded serialization produced by
        /// DataContractSerializer for the specified type. This method does not perform type validation beyond what
        /// DataContractSerializer supports.</remarks>
        /// <param name="str">The base64-encoded string containing the serialized object data. Cannot be null or empty.</param>
        /// <param name="type">The type of the object to deserialize from the string. Must be a type supported by DataContractSerializer.</param>
        /// <returns>An object instance of the specified type deserialized from the input string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if deserialization fails and returns a null result.</exception>
        public static object DeserializeFromString(string str, Type type)
        {
            return DeserializeFromBytes(Convert.FromBase64String(str), type);
        }
    }
}
