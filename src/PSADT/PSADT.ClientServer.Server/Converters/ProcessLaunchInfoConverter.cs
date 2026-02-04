using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PSADT.Foundation;
using PSADT.ProcessManagement;

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
        public override ProcessLaunchInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Ensure the validity of the JSON structure.
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Cannot deserialize null ProcessLaunchInfo.");
            }
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected start of object for ProcessLaunchInfo, got {reader.TokenType}.");
            }

            // Initialize all properties with defaults.
            string? filePath = null;
            List<string>? argumentList = null;
            string? workingDirectory = null;
            RunAsActiveUser? runAsActiveUser = null;
            bool useLinkedAdminToken = false;
            bool useHighestAvailableToken = false;
            bool inheritEnvironmentVariables = false;
            bool expandEnvironmentVariables = false;
            bool denyUserTermination = false;
            bool useUnelevatedToken = false;
            List<string>? standardInput = null;
            List<IntPtr>? handlesToInherit = null;
            bool useShellExecute = false;
            string? verb = null;
            bool createNoWindow = false;
            bool waitForChildProcesses = false;
            bool killChildProcessesWithParent = false;
            Encoding? streamEncoding = null;
            ProcessWindowStyle? windowStyle = null;
            ProcessPriorityClass? priorityClass = null;
            bool noTerminateOnTimeout = false;

            // Read properties until the end of the object.
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
                    case "FilePath":
                        filePath = reader.GetString();
                        break;
                    case "ArgumentList":
                        argumentList = ReadStringList(ref reader, options);
                        break;
                    case "WorkingDirectory":
                        workingDirectory = reader.TokenType != JsonTokenType.Null ? reader.GetString() : null;
                        break;
                    case "RunAsActiveUser":
                        runAsActiveUser = reader.TokenType != JsonTokenType.Null ? JsonSerializer.Deserialize<RunAsActiveUser>(ref reader, options) : null;
                        break;
                    case "UseLinkedAdminToken":
                        useLinkedAdminToken = reader.GetBoolean();
                        break;
                    case "UseHighestAvailableToken":
                        useHighestAvailableToken = reader.GetBoolean();
                        break;
                    case "InheritEnvironmentVariables":
                        inheritEnvironmentVariables = reader.GetBoolean();
                        break;
                    case "ExpandEnvironmentVariables":
                        expandEnvironmentVariables = reader.GetBoolean();
                        break;
                    case "DenyUserTermination":
                        denyUserTermination = reader.GetBoolean();
                        break;
                    case "UseUnelevatedToken":
                        useUnelevatedToken = reader.GetBoolean();
                        break;
                    case "StandardInput":
                        standardInput = ReadStringList(ref reader, options);
                        break;
                    case "HandlesToInherit":
                        handlesToInherit = reader.TokenType != JsonTokenType.Null ? JsonSerializer.Deserialize<List<IntPtr>>(ref reader, options) : null;
                        break;
                    case "UseShellExecute":
                        useShellExecute = reader.GetBoolean();
                        break;
                    case "Verb":
                        verb = reader.TokenType != JsonTokenType.Null ? reader.GetString() : null;
                        break;
                    case "CreateNoWindow":
                        createNoWindow = reader.GetBoolean();
                        break;
                    case "WaitForChildProcesses":
                        waitForChildProcesses = reader.GetBoolean();
                        break;
                    case "KillChildProcessesWithParent":
                        killChildProcessesWithParent = reader.GetBoolean();
                        break;
                    case "StreamEncoding":
                        streamEncoding = reader.TokenType != JsonTokenType.Null ? Encoding.GetEncoding(reader.GetString()!) : null;
                        break;
                    case "WindowStyle":
                        // WindowStyle (SHOW_WINDOW_CMD) is computed from ProcessWindowStyle; skip during deserialization.
                        reader.Skip();
                        break;
                    case "ProcessWindowStyle":
                        windowStyle = reader.TokenType != JsonTokenType.Null ? (ProcessWindowStyle)Enum.Parse(typeof(ProcessWindowStyle), reader.GetString()!) : null;
                        break;
                    case "PriorityClass":
                        priorityClass = reader.TokenType != JsonTokenType.Null ? (ProcessPriorityClass)Enum.Parse(typeof(ProcessPriorityClass), reader.GetString()!) : null;
                        break;
                    case "NoTerminateOnTimeout":
                        noTerminateOnTimeout = reader.GetBoolean();
                        break;
                    default:
                        // Skip unknown properties for forward compatibility.
                        reader.Skip();
                        break;
                }
            }

            // Validate required properties.
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new JsonException("ProcessLaunchInfo requires a non-empty FilePath property.");
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
        public override void Write(Utf8JsonWriter writer, ProcessLaunchInfo value, JsonSerializerOptions options)
        {
            // Ensure the value is not null.
            if (value is null)
            {
                throw new JsonException("Cannot serialize null ProcessLaunchInfo.");
            }

            // Start writing the JSON object.
            writer.WriteStartObject();

            // Write required property.
            writer.WriteString("FilePath", value.FilePath);

            // Write optional collections.
            if (value.ArgumentList is not null)
            {
                writer.WritePropertyName("ArgumentList");
                JsonSerializer.Serialize(writer, value.ArgumentList, options);
            }

            // Write optional strings.
            if (value.WorkingDirectory is not null)
            {
                writer.WriteString("WorkingDirectory", value.WorkingDirectory);
            }

            // Write RunAsActiveUser if present.
            if (value.RunAsActiveUser is not null)
            {
                writer.WritePropertyName("RunAsActiveUser");
                JsonSerializer.Serialize(writer, value.RunAsActiveUser, options);
            }

            // Write boolean properties (only if true to reduce payload size).
            if (value.UseLinkedAdminToken)
            {
                writer.WriteBoolean("UseLinkedAdminToken", true);
            }
            if (value.UseHighestAvailableToken)
            {
                writer.WriteBoolean("UseHighestAvailableToken", true);
            }
            if (value.InheritEnvironmentVariables)
            {
                writer.WriteBoolean("InheritEnvironmentVariables", true);
            }
            if (value.ExpandEnvironmentVariables)
            {
                writer.WriteBoolean("ExpandEnvironmentVariables", true);
            }
            if (value.DenyUserTermination)
            {
                writer.WriteBoolean("DenyUserTermination", true);
            }
            if (value.UseUnelevatedToken)
            {
                writer.WriteBoolean("UseUnelevatedToken", true);
            }

            // Write optional collections.
            if (value.StandardInput is not null)
            {
                writer.WritePropertyName("StandardInput");
                JsonSerializer.Serialize(writer, value.StandardInput, options);
            }
            if (value.HandlesToInherit is not null)
            {
                writer.WritePropertyName("HandlesToInherit");
                JsonSerializer.Serialize(writer, value.HandlesToInherit, options);
            }

            // Write more boolean properties.
            if (value.UseShellExecute)
            {
                writer.WriteBoolean("UseShellExecute", true);
            }
            if (value.Verb is not null)
            {
                writer.WriteString("Verb", value.Verb);
            }
            if (value.CreateNoWindow)
            {
                writer.WriteBoolean("CreateNoWindow", true);
            }
            if (value.WaitForChildProcesses)
            {
                writer.WriteBoolean("WaitForChildProcesses", true);
            }
            if (value.KillChildProcessesWithParent)
            {
                writer.WriteBoolean("KillChildProcessesWithParent", true);
            }

            // Write encoding using WebName for portability.
            writer.WriteString("StreamEncoding", value.StreamEncoding.WebName);

            // Write enum properties.
            if (value.WindowStyle is not null)
            {
                writer.WriteString("WindowStyle", value.WindowStyle.Value.ToString());
            }
            if (value.ProcessWindowStyle is not null)
            {
                writer.WriteString("ProcessWindowStyle", value.ProcessWindowStyle.Value.ToString());
            }
            if (value.PriorityClass is not null)
            {
                writer.WriteString("PriorityClass", value.PriorityClass.Value.ToString());
            }

            // Write remaining boolean.
            if (value.NoTerminateOnTimeout)
            {
                writer.WriteBoolean("NoTerminateOnTimeout", true);
            }

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
    }
}
