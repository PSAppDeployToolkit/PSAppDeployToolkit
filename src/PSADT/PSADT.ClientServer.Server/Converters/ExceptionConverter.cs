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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public override Exception? ReadJson(JsonReader reader, Type objectType, Exception? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                throw new JsonSerializationException("Cannot deserialize null to Exception.");
            }
            return DeserializeException(JObject.Load(reader), serializer);
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
            writer.WriteValue(value.GetType().AssemblyQualifiedName);

            // Write all SerializationInfo entries.
            foreach (SerializationEntry entry in info)
            {
                writer.WritePropertyName(entry.Name);
                if (entry.Value is null)
                {
                    // Send a null so all mandatory fields are present.
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
        private static Exception DeserializeException(JObject obj, JsonSerializer serializer)
        {
            // Get the exception type and validate.
            if (obj["$type"]?.Value<string>() is not string typeName)
            {
                throw new JsonSerializationException("Exception JSON is missing required [$type] property.");
            }
            if (!IsTrustedExceptionType(typeName))
            {
                throw new JsonSerializationException($"Exception type [{typeName}] is not from a trusted assembly. Only exceptions from System, Microsoft, and PSADT assemblies are allowed.");
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
            foreach (KeyValuePair<string, JToken?> property in obj)
            {
                if (property.Key == "$type")
                {
                    continue;
                }
                object? value = ConvertJTokenToSerializationValue(property.Key, property.Value, serializer);
                info.AddValue(property.Key, value, value?.GetType() ?? GetExpectedTypeForNullField(property.Key));
            }
            return (Exception)serializationCtor.Invoke([info, context]);
        }

        /// <summary>
        /// Validates that the exception type comes from a trusted assembly.
        /// </summary>
        /// <param name="assemblyQualifiedTypeName">The assembly-qualified type name to validate.</param>
        /// <returns>true if the type is from a trusted assembly; otherwise, false.</returns>
        private static bool IsTrustedExceptionType(string assemblyQualifiedTypeName)
        {
            // Find the assembly part of the name (after the first comma).
            int commaIndex = assemblyQualifiedTypeName.IndexOf(',');
            if (commaIndex < 0)
            {
                return false;
            }

            // Get the assembly name portion (skip the comma and space) and check against trusted prefixes.
            string assemblyPart = assemblyQualifiedTypeName.Substring(commaIndex + 1).TrimStart();
            foreach (string prefix in TrustedAssemblyPrefixes)
            {
                if (assemblyPart.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Populate switch", Justification = "Default case handles all other token types.")]
        private static object? ConvertJTokenToSerializationValue(string fieldName, JToken? token, JsonSerializer serializer)
        {
            if (token is null || token.Type == JTokenType.Null)
            {
                return null;
            }

            return token.Type switch
            {
                // Primitives
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.Integer => token.Value<long>(),
                JTokenType.Float => token.Value<double>(),
                JTokenType.String => token.Value<string>(),
                JTokenType.Date => token.Value<DateTime>(),

                // Complex types - handle specially based on field name
                JTokenType.Object when fieldName is "InnerException" => DeserializeException((JObject)token, serializer),
                JTokenType.Object when fieldName is "Data" => ConvertJTokenToDictionary((JObject)token),
                JTokenType.Object => token.ToObject<object>(serializer),

                JTokenType.Array when fieldName is "InnerExceptions" => DeserializeInnerExceptions((JArray)token, serializer),
                JTokenType.Array => token.ToObject<object[]>(serializer),

                // Fallback
                JTokenType.Undefined => throw new JsonSerializationException($"Undefined value cannot be deserialized for field [{fieldName}]."),
                _ => token.ToObject<object>(serializer),
            };
        }

        /// <summary>
        /// Converts a JObject to a dictionary for the Data property.
        /// </summary>
        private static ListDictionary? ConvertJTokenToDictionary(JObject obj)
        {
            ListDictionary dict = [];
            foreach (KeyValuePair<string, JToken?> prop in obj)
            {
                dict.Add(prop.Key, prop.Value?.Type == JTokenType.String ? prop.Value.Value<string>() : prop.Value?.ToString());
            }
            return dict.Count == 0 ? null : dict;
        }

        /// <summary>
        /// Deserializes an array of inner exceptions (for AggregateException).
        /// </summary>
        private static Exception[] DeserializeInnerExceptions(JArray array, JsonSerializer serializer)
        {
            List<Exception> exceptions = [];
            foreach (JToken item in array)
            {
                if (item.Type != JTokenType.Object)
                {
                    throw new JsonSerializationException($"Expected object in InnerExceptions array, got {item.Type}.");
                }
                exceptions.Add(DeserializeException((JObject)item, serializer));
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

        /// <summary>
        /// Trusted assemblies from which exception types may be loaded during deserialization.
        /// This is a security measure to prevent arbitrary type instantiation (CA2326/CA2327 mitigation).
        /// </summary>
        private static readonly HashSet<string> TrustedAssemblyPrefixes =
        [
            "mscorlib,",
            "System,",
            "System.",
            "Microsoft.",
            "PSADT.",
            "PSAppDeployToolkit,",
        ];
    }
}
