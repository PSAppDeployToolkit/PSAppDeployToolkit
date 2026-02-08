using System;
using System.Collections.Generic;
using PSADT.ProcessManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public override ProcessResult ReadJson(JsonReader reader, Type objectType, ProcessResult? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // Ensure the validity of the JSON structure.
            if (reader.TokenType == JsonToken.Null)
            {
                throw new JsonSerializationException("Cannot deserialize null ProcessResult.");
            }
            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonSerializationException($"Expected start of object for ProcessResult, got {reader.TokenType}.");
            }

            // Read properties from the JObject.
            JObject obj = JObject.Load(reader);
            IReadOnlyList<string>? interleaved = obj["Interleaved"]?.ToObject<List<string>>(serializer);
            IReadOnlyList<string>? stdOut = obj["StdOut"]?.ToObject<List<string>>(serializer);
            IReadOnlyList<string>? stdErr = obj["StdErr"]?.ToObject<List<string>>(serializer);
            ProcessLaunchInfo? launchInfo = obj["LaunchInfo"]?.ToObject<ProcessLaunchInfo>(serializer);
            string? commandLine = obj["CommandLine"]?.Value<string>();
            int exitCode = obj["ExitCode"]?.Value<int>() ?? 0;

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
        public override void WriteJson(JsonWriter writer, ProcessResult? value, JsonSerializer serializer)
        {
            // Ensure the value is not null.
            if (value is null)
            {
                throw new JsonSerializationException("Cannot serialize null ProcessResult.");
            }

            // Start writing the JSON object.
            writer.WriteStartObject();
            writer.WritePropertyName("ExitCode");
            writer.WriteValue(value.ExitCode);

            // Write the command line if present.
            if (value.CommandLine is not null)
            {
                writer.WritePropertyName("CommandLine");
                writer.WriteValue(value.CommandLine);
            }

            // Write the launch info if present.
            if (value.LaunchInfo is not null)
            {
                writer.WritePropertyName("LaunchInfo");
                serializer.Serialize(writer, value.LaunchInfo);
            }

            // Write the string lists.
            writer.WritePropertyName("StdOut");
            WriteStringList(writer, value.StdOut, serializer);
            writer.WritePropertyName("StdErr");
            WriteStringList(writer, value.StdErr, serializer);
            writer.WritePropertyName("Interleaved");
            WriteStringList(writer, value.Interleaved, serializer);

            // End the JSON object.
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes a string list to JSON.
        /// </summary>
        private static void WriteStringList(JsonWriter writer, IReadOnlyList<string>? list, JsonSerializer serializer)
        {
            if (list is not null)
            {
                serializer.Serialize(writer, list);
            }
            else
            {
                writer.WriteNull();
            }
        }
    }
}
