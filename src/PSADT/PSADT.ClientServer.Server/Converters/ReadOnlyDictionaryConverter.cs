using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PSADT.ClientServer.Converters
{
    /// <summary>
    /// Custom JSON converter for <see cref="ReadOnlyDictionary{TKey, TValue}"/> that deserializes
    /// by first creating a regular dictionary and then wrapping it.
    /// </summary>
    internal sealed class ReadOnlyDictionaryConverter : JsonConverterFactory
    {
        /// <summary>
        /// Determines whether the converter can convert the specified type.
        /// </summary>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ReadOnlyDictionary<,>);
        }

        /// <summary>
        /// Creates a converter for the specified type.
        /// </summary>
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Type[] typeArguments = typeToConvert.GetGenericArguments();
            Type keyType = typeArguments[0]; Type valueType = typeArguments[1];
            Type converterType = typeof(ReadOnlyDictionaryConverterInner<,>).MakeGenericType(keyType, valueType);
            return (JsonConverter?)Activator.CreateInstance(converterType);
        }

        /// <summary>
        /// Inner converter that handles the actual serialization/deserialization.
        /// </summary>
        private sealed class ReadOnlyDictionaryConverterInner<TKey, TValue> : JsonConverter<ReadOnlyDictionary<TKey, TValue>> where TKey : notnull
        {
            /// <summary>
            /// Reads and converts the JSON to a <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
            /// </summary>
            public override ReadOnlyDictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                // Deserialize as a regular dictionary first.
                if (reader.TokenType == JsonTokenType.Null)
                {
                    throw new JsonException("Cannot deserialize null ReadOnlyDictionary.");
                }
                if (JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(ref reader, options) is not Dictionary<TKey, TValue> dict)
                {
                    throw new JsonException("Deserialization returned a null dictionary.");
                }

                // Return as read-only dictionary.
                return new(dict);
            }

            /// <summary>
            /// Writes the <see cref="ReadOnlyDictionary{TKey, TValue}"/> as JSON.
            /// </summary>
            public override void Write(Utf8JsonWriter writer, ReadOnlyDictionary<TKey, TValue> value, JsonSerializerOptions options)
            {
                // Serialize as a regular dictionary.
                JsonSerializer.Serialize(writer, (IDictionary<TKey, TValue>)value, options);
            }
        }
    }
}
