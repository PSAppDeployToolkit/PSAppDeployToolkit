using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using PSADT.ProcessManagement;

namespace PSADT.ClientServer.Converters
{
    /// <summary>
    /// Custom JSON converter for <see cref="ProcessResult"/> that serializes only the serializable
    /// properties, excluding runtime objects like <see cref="System.Diagnostics.Process"/>.
    /// </summary>
    internal sealed class ProcessResultConverter : JsonConverter<ProcessResult>
    {
        /// <summary>
        /// Reads and converts the JSON to a <see cref="ProcessResult"/> instance.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Implementing this here will just make a mess.")]
        public override ProcessResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Ensure the validity of the JSON structure.
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Cannot deserialize null ProcessResult.");
            }
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected start of object for ProcessResult, got {reader.TokenType}.");
            }

            // Read properties until the end of the object.
            IReadOnlyList<string>? interleaved = null;
            IReadOnlyList<string>? stdOut = null;
            IReadOnlyList<string>? stdErr = null;
            ProcessLaunchInfo? launchInfo = null;
            string? commandLine = null;
            int exitCode = 0;
            while (reader.Read())
            {
                // Break on end of object.
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Expected property name, got {reader.TokenType}.");
                }

                // Handle each property accordingly.
                string propertyName = reader.GetString() ?? throw new JsonException("Property name cannot be null."); _ = reader.Read();
                switch (propertyName)
                {
                    case "ExitCode":
                        exitCode = reader.GetInt32();
                        break;
                    case "CommandLine":
                        commandLine = reader.TokenType != JsonTokenType.Null ? reader.GetString() : null;
                        break;
                    case "LaunchInfo":
                        launchInfo = reader.TokenType != JsonTokenType.Null ? JsonSerializer.Deserialize<ProcessLaunchInfo>(ref reader, options) : null;
                        break;
                    case "StdOut":
                        stdOut = ReadStringList(ref reader, options);
                        break;
                    case "StdErr":
                        stdErr = ReadStringList(ref reader, options);
                        break;
                    case "Interleaved":
                        interleaved = ReadStringList(ref reader, options);
                        break;
                    default:
                        // Skip unknown properties for forward compatibility.
                        reader.Skip();
                        break;
                }
            }

            // Return the constructed ProcessResult using the appropriate constructor.
            if (launchInfo is not null && commandLine is not null && stdOut is not null && stdErr is not null && interleaved is not null)
            {
                return new ProcessResult(launchInfo, commandLine, exitCode, stdOut, stdErr, interleaved);
            }
            if (stdOut is not null && stdErr is not null && interleaved is not null)
            {
                return new ProcessResult(exitCode, stdOut, stdErr, interleaved);
            }
            return new ProcessResult(exitCode);
        }

        /// <summary>
        /// Writes the <see cref="ProcessResult"/> as JSON, excluding non-serializable properties.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, ProcessResult value, JsonSerializerOptions options)
        {
            // Ensure the value is not null.
            if (value is null)
            {
                throw new JsonException("Cannot serialize null ProcessResult.");
            }

            // Start writing the JSON object.
            writer.WriteStartObject();
            writer.WriteNumber("ExitCode", value.ExitCode);

            // Write the command line if present.
            if (value.CommandLine is not null)
            {
                writer.WriteString("CommandLine", value.CommandLine);
            }

            // Write the launch info if present.
            if (value.LaunchInfo is not null)
            {
                writer.WritePropertyName("LaunchInfo");
                JsonSerializer.Serialize(writer, value.LaunchInfo, options);
            }

            // Write the string lists.
            writer.WritePropertyName("StdOut");
            WriteStringList(writer, value.StdOut, options);
            writer.WritePropertyName("StdErr");
            WriteStringList(writer, value.StdErr, options);
            writer.WritePropertyName("Interleaved");
            WriteStringList(writer, value.Interleaved, options);

            // End the JSON object.
            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads a JSON array as a list of strings.
        /// </summary>
        private static List<string>? ReadStringList(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            return reader.TokenType != JsonTokenType.Null ? JsonSerializer.Deserialize<List<string>>(ref reader, options) : null;
        }

        /// <summary>
        /// Writes a string list to JSON.
        /// </summary>
        private static void WriteStringList(Utf8JsonWriter writer, IReadOnlyList<string>? list, JsonSerializerOptions options)
        {
            if (list is not null)
            {
                JsonSerializer.Serialize(writer, list, options);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
