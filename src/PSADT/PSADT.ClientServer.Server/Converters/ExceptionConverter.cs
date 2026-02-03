using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSADT.ClientServer.Converters
{
    /// <summary>
    /// Provides custom JSON serialization and deserialization for <see cref="Exception"/> objects, enabling exceptions
    /// to be accurately represented in JSON format and reconstructed from JSON data.
    /// </summary>
    /// <remarks>This converter uses the <see cref="ISerializable"/> interface and exception serialization
    /// constructors to preserve exception details, including type, message, stack trace, inner exceptions, and custom
    /// data. It supports round-trip serialization for most exception types that implement the required serialization
    /// constructor. When deserializing, the converter requires the exception type to be available and to derive from
    /// <see cref="Exception"/>. If the exception type or required constructor is missing, a <see
    /// cref="JsonSerializationException"/> is thrown. This converter is intended for advanced scenarios such as
    /// logging, diagnostics, or distributed error handling where exception fidelity is important.</remarks>
    internal sealed class ExceptionConverter : JsonConverter<Exception>
    {
        /// <summary>
        /// Reads and converts the JSON to an <see cref="Exception"/> instance.
        /// </summary>
        public override Exception? ReadJson(JsonReader reader, Type objectType, Exception? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return reader.TokenType == JsonToken.Null
                ? throw new JsonSerializationException("Cannot deserialize null to Exception.")
                : DeserializeException(JObject.Load(reader));
        }

        /// <summary>
        /// Writes the <see cref="Exception"/> as JSON using <see cref="ISerializable"/>.
        /// </summary>
        public override void WriteJson(JsonWriter writer, Exception? value, JsonSerializer serializer)
        {
            // Validate input.
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Cannot serialize a null Exception.");
            }

            // Get SerializationInfo for the exception.
            (SerializationInfo info, StreamingContext context) = GetSerializationInfo(value.GetType());
            GetObjectData(value, info, context);

            // Write the JSON.
            writer.WriteStartObject();
            writer.WritePropertyName("$type");
            writer.WriteValue(value.GetType().FullName);

            // Write all SerializationInfo entries.
            foreach (SerializationEntry entry in info)
            {
                writer.WritePropertyName(entry.Name);
                if (entry.Value is null)
                {
                    writer.WriteNull();
                }
                else if (entry.Value is Exception innerException)
                {
                    // Recursively serialize inner exceptions
                    WriteJson(writer, innerException, serializer);
                }
                else
                {
                    // Use the serializer for other types
                    serializer.Serialize(writer, entry.Value);
                }
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// Deserializes a JObject to an Exception using the serialization constructor.
        /// </summary>
        private static Exception DeserializeException(JObject jObject)
        {
            // Get the exception type and validate.
            if (jObject["$type"]?.Value<string>() is not string typeName)
            {
                throw new JsonSerializationException("Exception JSON is missing required [$type] property.");
            }
            if (Type.GetType(typeName, true) is not Type exceptionType)
            {
                throw new JsonSerializationException($"Exception type [{typeName}] could not be resolved. Ensure the assembly is loaded.");
            }
            if (!typeof(Exception).IsAssignableFrom(exceptionType))
            {
                throw new JsonSerializationException($"Type [{typeName}] does not derive from System.Exception.");
            }

            // Find the serialization constructor.
            if (exceptionType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [typeof(SerializationInfo), typeof(StreamingContext)], null) is not ConstructorInfo serializationCtor)
            {
                throw new JsonSerializationException($"Exception type [{typeName}] does not have a serialization constructor (SerializationInfo, StreamingContext).");
            }

            // Build SerializationInfo from the JSON and reconstruct the exception.
            (SerializationInfo info, StreamingContext context) = GetSerializationInfo(exceptionType);
            foreach (KeyValuePair<string, JToken?> property in jObject)
            {
                if (property.Key == "$type" || property.Value is null)
                {
                    continue;
                }
                object? value = ConvertJTokenToSerializationValue(property.Key, property.Value);
                info.AddValue(property.Key, value, value?.GetType() ?? GetExpectedTypeForNullField(property.Key));
            }
            return (Exception)serializationCtor.Invoke([info, context]);
        }

        /// <summary>
        /// Gets the expected CLR type for a serialization field when the value is null.
        /// </summary>
        /// <remarks>
        /// When deserializing null values, SerializationInfo.AddValue requires a type parameter.
        /// This method returns the expected type based on known exception serialization field names.
        /// </remarks>
        private static Type GetExpectedTypeForNullField(string fieldName)
        {
            return fieldName switch
            {
                "ClassName" or "Message" or "StackTraceString" or "RemoteStackTraceString" or "Source" or "HelpURL" or "ExceptionMethod" => typeof(string),
                "InnerException" => typeof(Exception),
                "Data" => typeof(IDictionary),
                "HResult" or "RemoteStackIndex" => typeof(int),
                "WatsonBuckets" => typeof(byte[]),
                _ => typeof(object)
            };
        }

        /// <summary>
        /// Converts a JToken to the appropriate CLR type for SerializationInfo.
        /// </summary>
        private static object? ConvertJTokenToSerializationValue(string fieldName, JToken token)
        {
            return token.Type switch
            {
                // Null/undefined
                JTokenType.Null or JTokenType.Undefined => null,

                // Primitives
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.Integer => token.Value<long>(),
                JTokenType.Float => token.Value<double>(),
                JTokenType.String => token.Value<string>(),
                JTokenType.Bytes => token.Value<byte[]>(),

                // Date/Time types
                JTokenType.Date => token.Value<DateTime>(),
                JTokenType.TimeSpan => token.Value<TimeSpan>(),

                // Other value types
                JTokenType.Guid => token.Value<Guid>(),
                JTokenType.Uri => token.Value<Uri>(),

                // Complex types - handle specially based on field name
                JTokenType.Object when fieldName is "InnerException" => DeserializeException((JObject)token),
                JTokenType.Object when fieldName is "Data" => ConvertJObjectToDictionary((JObject)token),
                JTokenType.Object => token.ToObject<object>(),

                JTokenType.Array when fieldName is "InnerExceptions" => DeserializeInnerExceptions((JArray)token),
                JTokenType.Array => token.ToObject<object[]>(),

                // Raw JSON string
                JTokenType.Raw => token.ToString(),

                // Structural tokens (shouldn't appear as values in serialization data)
                JTokenType.None or JTokenType.Constructor or JTokenType.Property or JTokenType.Comment => throw new NotSupportedException($"Unexpected token type: {token.Type}"),

                // Fallback for any future enum values
                _ => token.ToObject<object>(),
            };
        }

        /// <summary>
        /// Converts a JObject to a dictionary for the Data property.
        /// </summary>
        private static ListDictionary? ConvertJObjectToDictionary(JObject jObject)
        {
            if (jObject.Count == 0)
            {
                return null;
            }
            ListDictionary dict = [];
            foreach (KeyValuePair<string, JToken?> kvp in jObject)
            {
                if (kvp.Value is not null)
                {
                    dict.Add(kvp.Key, kvp.Value.Type == JTokenType.String ? kvp.Value.Value<string>() : kvp.Value.ToString());
                }
            }
            return dict;
        }

        /// <summary>
        /// Deserializes an array of inner exceptions (for AggregateException).
        /// </summary>
        private static Exception[] DeserializeInnerExceptions(JArray jArray)
        {
            List<Exception> exceptions = new(jArray.Count);
            foreach (JToken token in jArray)
            {
                if (token is JObject exObj)
                {
                    exceptions.Add(DeserializeException(exObj));
                }
            }
            return [.. exceptions];
        }

        /// <summary>
        /// Creates a new SerializationInfo and StreamingContext pair for the specified type.
        /// </summary>
        /// <param name="type">The type for which to create the SerializationInfo instance. Cannot be null.</param>
        /// <returns>A tuple containing the SerializationInfo initialized for the specified type and a StreamingContext with all
        /// states.</returns>
        private static (SerializationInfo info, StreamingContext context) GetSerializationInfo(Type type)
        {
#pragma warning disable SYSLIB0050, SYSLIB0051
            SerializationInfo info = new(type, new FormatterConverter());
            StreamingContext context = new(StreamingContextStates.All);
#pragma warning restore SYSLIB0050, SYSLIB0051
            return (info, context);
        }

        /// <summary>
        /// Populates a SerializationInfo object with the data needed to serialize the specified exception.
        /// </summary>
        /// <param name="ex">The exception to serialize. Cannot be null.</param>
        /// <param name="info">The SerializationInfo object to populate with data. Cannot be null.</param>
        /// <param name="context">The destination for this serialization. Provides contextual information about the source or destination.</param>
        private static void GetObjectData(Exception ex, SerializationInfo info, StreamingContext context)
        {
#pragma warning disable SYSLIB0050, SYSLIB0051
            ex.GetObjectData(info, context);
#pragma warning restore SYSLIB0050, SYSLIB0051
        }
    }
}
