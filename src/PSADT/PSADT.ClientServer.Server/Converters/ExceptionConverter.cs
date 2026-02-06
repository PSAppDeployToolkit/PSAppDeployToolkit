using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    /// cref="JsonException"/> is thrown. This converter is intended for advanced scenarios such as
    /// logging, diagnostics, or distributed error handling where exception fidelity is important.</remarks>
    internal sealed class ExceptionConverter : JsonConverter<Exception>
    {
        /// <summary>
        /// Reads and converts the JSON to an <see cref="Exception"/> instance.
        /// </summary>
        public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Cannot deserialize null to Exception.");
            }
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            return DeserializeException(doc.RootElement);
        }

        /// <summary>
        /// Writes the <see cref="Exception"/> as JSON using <see cref="ISerializable"/>.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
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
            writer.WriteString("$type", value.GetType().AssemblyQualifiedName);

            // Write all SerializationInfo entries.
            foreach (SerializationEntry entry in info)
            {
                writer.WritePropertyName(entry.Name);
                if (entry.Value is null)
                {
                    writer.WriteNullValue();
                }
                else if (entry.Value is Exception innerException)
                {
                    // Recursively serialize inner exceptions
                    Write(writer, innerException, options);
                }
                else
                {
                    // Use the serializer for other types
                    JsonSerializer.Serialize(writer, entry.Value, entry.Value.GetType(), options);
                }
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// Deserializes a JsonElement to an Exception using the serialization constructor.
        /// </summary>
        private static Exception DeserializeException(JsonElement element)
        {
            // Get the exception type and validate.
            if (!element.TryGetProperty("$type", out JsonElement typeElement) || typeElement.GetString() is not string typeName)
            {
                throw new JsonException("Exception JSON is missing required [$type] property.");
            }
            if (!IsTrustedExceptionType(typeName))
            {
                throw new JsonException($"Exception type [{typeName}] is not from a trusted assembly. Only exceptions from System, Microsoft, and PSADT assemblies are allowed.");
            }
            if (Type.GetType(typeName, true) is not Type exceptionType)
            {
                throw new JsonException($"Exception type [{typeName}] could not be resolved. Ensure the assembly is loaded.");
            }
            if (!typeof(Exception).IsAssignableFrom(exceptionType))
            {
                throw new JsonException($"Type [{typeName}] does not derive from System.Exception.");
            }

            // Find the serialization constructor.
            if (exceptionType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [typeof(SerializationInfo), typeof(StreamingContext)], null) is not ConstructorInfo serializationCtor)
            {
                throw new JsonException($"Exception type [{typeName}] does not have a serialization constructor (SerializationInfo, StreamingContext).");
            }

            // Build SerializationInfo from the JSON and reconstruct the exception.
            (SerializationInfo info, StreamingContext context) = GetSerializationInfo(exceptionType);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (property.Name == "$type")
                {
                    continue;
                }
                object? value = ConvertJsonElementToSerializationValue(property.Name, property.Value);
                info.AddValue(property.Name, value, value?.GetType() ?? GetExpectedTypeForNullField(property.Name));
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
        /// Converts a JsonElement to the appropriate CLR type for SerializationInfo.
        /// </summary>
        private static object? ConvertJsonElementToSerializationValue(string fieldName, JsonElement element)
        {
            return element.ValueKind switch
            {
                // Null/undefined
                JsonValueKind.Null => null,

                // Primitives
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when element.TryGetInt64(out long l) => l,
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.String when element.TryGetDateTime(out DateTime dt) => dt,
                JsonValueKind.String => element.GetString(),

                // Complex types - handle specially based on field name
                JsonValueKind.Object when fieldName is "InnerException" => DeserializeException(element),
                JsonValueKind.Object when fieldName is "Data" => ConvertJsonElementToDictionary(element),
                JsonValueKind.Object => JsonSerializer.Deserialize<object>(element.GetRawText()),

                JsonValueKind.Array when fieldName is "InnerExceptions" => DeserializeInnerExceptions(element),
                JsonValueKind.Array => JsonSerializer.Deserialize<object[]>(element.GetRawText()),

                // Fallback
                JsonValueKind.Undefined => throw new JsonException($"Undefined value cannot be deserialized for field [{fieldName}]."),
                _ => JsonSerializer.Deserialize<object>(element.GetRawText()),
            };
        }

        /// <summary>
        /// Converts a JsonElement to a dictionary for the Data property.
        /// </summary>
        private static ListDictionary? ConvertJsonElementToDictionary(JsonElement element)
        {
            ListDictionary dict = [];
            foreach (JsonProperty prop in element.EnumerateObject())
            {
                dict.Add(prop.Name, prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop.Value.GetRawText());
            }
            return dict.Count == 0 ? null : dict;
        }

        /// <summary>
        /// Deserializes an array of inner exceptions (for AggregateException).
        /// </summary>
        private static Exception[] DeserializeInnerExceptions(JsonElement element)
        {
            List<Exception> exceptions = [];
            foreach (JsonElement item in element.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException($"Expected object in InnerExceptions array, got {item.ValueKind}.");
                }
                exceptions.Add(DeserializeException(item));
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
