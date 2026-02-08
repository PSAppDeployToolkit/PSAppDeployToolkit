using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PSADT.Foundation;
using PSADT.ProcessManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSADT.ClientServer.Converters
{
    /// <summary>
    /// Custom JSON converter for <see cref="ProcessLaunchInfo"/> that serializes the serializable
    /// properties, excluding runtime-only objects like <see cref="System.Threading.CancellationToken"/>.
    /// </summary>
    internal sealed class ProcessLaunchInfoConverter : JsonConverter<ProcessLaunchInfo>
    {
        /// <summary>
        /// Reads and converts the JSON to a <see cref="ProcessLaunchInfo"/> instance.
        /// </summary>
        public override ProcessLaunchInfo ReadJson(JsonReader reader, Type objectType, ProcessLaunchInfo? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // Ensure the validity of the JSON structure.
            if (reader.TokenType == JsonToken.Null)
            {
                throw new JsonSerializationException("Cannot deserialize null ProcessLaunchInfo.");
            }
            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonSerializationException($"Expected start of object for ProcessLaunchInfo, got {reader.TokenType}.");
            }

            // Read out all possible properties.
            JObject obj = JObject.Load(reader);
            string? filePath = obj["FilePath"]?.Value<string>();
            List<string>? argumentList = obj["ArgumentList"]?.ToObject<List<string>>(serializer);
            string? workingDirectory = obj["WorkingDirectory"]?.Value<string>();
            RunAsActiveUser? runAsActiveUser = obj["RunAsActiveUser"]?.ToObject<RunAsActiveUser>(serializer);
            bool useLinkedAdminToken = obj["UseLinkedAdminToken"]?.Value<bool>() ?? false;
            bool useHighestAvailableToken = obj["UseHighestAvailableToken"]?.Value<bool>() ?? false;
            bool inheritEnvironmentVariables = obj["InheritEnvironmentVariables"]?.Value<bool>() ?? false;
            bool expandEnvironmentVariables = obj["ExpandEnvironmentVariables"]?.Value<bool>() ?? false;
            bool denyUserTermination = obj["DenyUserTermination"]?.Value<bool>() ?? false;
            bool useUnelevatedToken = obj["UseUnelevatedToken"]?.Value<bool>() ?? false;
            List<string>? standardInput = obj["StandardInput"]?.ToObject<List<string>>(serializer);
            List<IntPtr>? handlesToInherit = obj["HandlesToInherit"]?.ToObject<List<IntPtr>>(serializer);
            bool useShellExecute = obj["UseShellExecute"]?.Value<bool>() ?? false;
            string? verb = obj["Verb"]?.Value<string>();
            bool createNoWindow = obj["CreateNoWindow"]?.Value<bool>() ?? false;
            bool waitForChildProcesses = obj["WaitForChildProcesses"]?.Value<bool>() ?? false;
            bool killChildProcessesWithParent = obj["KillChildProcessesWithParent"]?.Value<bool>() ?? false;
            string? streamEncodingName = obj["StreamEncoding"]?.Value<string>();
            Encoding? streamEncoding = streamEncodingName is not null ? Encoding.GetEncoding(streamEncodingName) : null;
            string? windowStyleStr = obj["ProcessWindowStyle"]?.Value<string>();
            ProcessWindowStyle? windowStyle = windowStyleStr is not null ? (ProcessWindowStyle)Enum.Parse(typeof(ProcessWindowStyle), windowStyleStr) : null;
            string? priorityClassStr = obj["PriorityClass"]?.Value<string>();
            ProcessPriorityClass? priorityClass = priorityClassStr is not null ? (ProcessPriorityClass)Enum.Parse(typeof(ProcessPriorityClass), priorityClassStr) : null;
            bool noTerminateOnTimeout = obj["NoTerminateOnTimeout"]?.Value<bool>() ?? false;

            // Validate required properties.
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new JsonSerializationException("ProcessLaunchInfo requires a non-empty FilePath property.");
            }

            // Return the constructed ProcessLaunchInfo.
            return new(
                filePath!,
                argumentList,
                workingDirectory,
                runAsActiveUser,
                useLinkedAdminToken,
                useHighestAvailableToken,
                inheritEnvironmentVariables,
                expandEnvironmentVariables,
                denyUserTermination,
                useUnelevatedToken,
                standardInput,
                handlesToInherit,
                useShellExecute,
                verb,
                createNoWindow,
                waitForChildProcesses,
                killChildProcessesWithParent,
                streamEncoding,
                windowStyle,
                priorityClass,
                null, // CancellationToken is not serializable.
                noTerminateOnTimeout
            );
        }

        /// <summary>
        /// Writes the <see cref="ProcessLaunchInfo"/> as JSON.
        /// </summary>
        public override void WriteJson(JsonWriter writer, ProcessLaunchInfo? value, JsonSerializer serializer)
        {
            // Ensure the value is not null.
            if (value is null)
            {
                throw new JsonSerializationException("Cannot serialize null ProcessLaunchInfo.");
            }

            // Start writing the JSON object.
            writer.WriteStartObject();

            // Write required property.
            writer.WritePropertyName("FilePath");
            writer.WriteValue(value.FilePath);

            // Write optional collections.
            if (value.ArgumentList is not null)
            {
                writer.WritePropertyName("ArgumentList");
                serializer.Serialize(writer, value.ArgumentList);
            }

            // Write optional strings.
            if (value.WorkingDirectory is not null)
            {
                writer.WritePropertyName("WorkingDirectory");
                writer.WriteValue(value.WorkingDirectory);
            }

            // Write RunAsActiveUser if present.
            if (value.RunAsActiveUser is not null)
            {
                writer.WritePropertyName("RunAsActiveUser");
                serializer.Serialize(writer, value.RunAsActiveUser);
            }

            // Write boolean properties (only if true to reduce payload size).
            if (value.UseLinkedAdminToken)
            {
                writer.WritePropertyName("UseLinkedAdminToken");
                writer.WriteValue(true);
            }
            if (value.UseHighestAvailableToken)
            {
                writer.WritePropertyName("UseHighestAvailableToken");
                writer.WriteValue(true);
            }
            if (value.InheritEnvironmentVariables)
            {
                writer.WritePropertyName("InheritEnvironmentVariables");
                writer.WriteValue(true);
            }
            if (value.ExpandEnvironmentVariables)
            {
                writer.WritePropertyName("ExpandEnvironmentVariables");
                writer.WriteValue(true);
            }
            if (value.DenyUserTermination)
            {
                writer.WritePropertyName("DenyUserTermination");
                writer.WriteValue(true);
            }
            if (value.UseUnelevatedToken)
            {
                writer.WritePropertyName("UseUnelevatedToken");
                writer.WriteValue(true);
            }

            // Write optional collections.
            if (value.StandardInput is not null)
            {
                writer.WritePropertyName("StandardInput");
                serializer.Serialize(writer, value.StandardInput);
            }
            if (value.HandlesToInherit is not null)
            {
                writer.WritePropertyName("HandlesToInherit");
                serializer.Serialize(writer, value.HandlesToInherit);
            }

            // Write more boolean properties.
            if (value.UseShellExecute)
            {
                writer.WritePropertyName("UseShellExecute");
                writer.WriteValue(true);
            }
            if (value.Verb is not null)
            {
                writer.WritePropertyName("Verb");
                writer.WriteValue(value.Verb);
            }
            if (value.CreateNoWindow)
            {
                writer.WritePropertyName("CreateNoWindow");
                writer.WriteValue(true);
            }
            if (value.WaitForChildProcesses)
            {
                writer.WritePropertyName("WaitForChildProcesses");
                writer.WriteValue(true);
            }
            if (value.KillChildProcessesWithParent)
            {
                writer.WritePropertyName("KillChildProcessesWithParent");
                writer.WriteValue(true);
            }

            // Write encoding using WebName for portability.
            writer.WritePropertyName("StreamEncoding");
            writer.WriteValue(value.StreamEncoding.WebName);

            // Write enum properties.
            if (value.WindowStyle is not null)
            {
                writer.WritePropertyName("WindowStyle");
                writer.WriteValue(value.WindowStyle.Value.ToString());
            }
            if (value.ProcessWindowStyle is not null)
            {
                writer.WritePropertyName("ProcessWindowStyle");
                writer.WriteValue(value.ProcessWindowStyle.Value.ToString());
            }
            if (value.PriorityClass is not null)
            {
                writer.WritePropertyName("PriorityClass");
                writer.WriteValue(value.PriorityClass.Value.ToString());
            }

            // Write remaining boolean.
            if (value.NoTerminateOnTimeout)
            {
                writer.WritePropertyName("NoTerminateOnTimeout");
                writer.WriteValue(true);
            }

            // End the JSON object.
            writer.WriteEndObject();
        }
    }
}
