using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using PSADT.ClientServer.Payloads;

namespace PSADT.ClientServer.Converters
{
    /// <summary>
    /// Custom JSON converter for <see cref="PipeRequest"/> that handles polymorphic payload deserialization
    /// without using TypeNameHandling.
    /// </summary>
    /// <remarks>This converter uses the <see cref="PipeCommand"/> value as a discriminator to determine
    /// the correct payload type during deserialization, avoiding the security risks associated with
    /// TypeNameHandling.</remarks>
    internal sealed class PipeRequestConverter : JsonConverter<PipeRequest>
    {
        /// <summary>
        /// Reads and converts the JSON to a <see cref="PipeRequest"/> instance.
        /// </summary>
        public override PipeRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Validate the token type.
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Cannot deserialize null PipeRequest.");
            }

            // Get the document from from the reader.
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;

            // Get the command to determine payload type.
            if (!root.TryGetProperty("Command", out JsonElement commandElement))
            {
                throw new JsonException("Missing or invalid Command.");
            }
            PipeCommand command = (PipeCommand)commandElement.GetInt32();

            // Deserialize the payload based on the command.
            IPayload? payload = null;
            if (root.TryGetProperty("Payload", out JsonElement payloadElement) && payloadElement.ValueKind != JsonValueKind.Null)
            {
                if (!CommandToPayloadType.TryGetValue(command, out Type? payloadType) || payloadType is null)
                {
                    throw new JsonException($"Command [{command}] has a payload but no payload type is registered.");
                }
                payload = (IPayload?)JsonSerializer.Deserialize(payloadElement.GetRawText(), payloadType, options);
                if (payload is null)
                {
                    throw new JsonException($"Failed to deserialize payload for command [{command}].");
                }
            }
            return new(command, payload);
        }

        /// <summary>
        /// Writes the <see cref="PipeRequest"/> as JSON.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, PipeRequest value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Cannot serialize a null PipeRequest.");
            }
            writer.WriteStartObject();
            writer.WriteNumber("Command", (int)value.Command);
            if (value.Payload is not null)
            {
                writer.WritePropertyName("Payload");
                JsonSerializer.Serialize(writer, value.Payload, value.Payload.GetType(), options);
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// Maps <see cref="PipeCommand"/> values to their corresponding payload types.
        /// </summary>
        private static readonly ReadOnlyDictionary<PipeCommand, Type?> CommandToPayloadType = new(new Dictionary<PipeCommand, Type?>
        {
            [PipeCommand.Open] = null,
            [PipeCommand.Close] = null,
            [PipeCommand.InitCloseAppsDialog] = typeof(InitCloseAppsDialogPayload),
            [PipeCommand.PromptToCloseApps] = typeof(PromptToCloseAppsPayload),
            [PipeCommand.ShowModalDialog] = typeof(ShowModalDialogPayload),
            [PipeCommand.ShowProgressDialog] = typeof(ShowProgressDialogPayload),
            [PipeCommand.ProgressDialogOpen] = null,
            [PipeCommand.UpdateProgressDialog] = typeof(UpdateProgressDialogPayload),
            [PipeCommand.CloseProgressDialog] = null,
            [PipeCommand.ShowBalloonTip] = typeof(ShowBalloonTipPayload),
            [PipeCommand.MinimizeAllWindows] = null,
            [PipeCommand.RestoreAllWindows] = null,
            [PipeCommand.SendKeys] = typeof(SendKeysPayload),
            [PipeCommand.GetProcessWindowInfo] = typeof(GetProcessWindowInfoPayload),
            [PipeCommand.RefreshDesktopAndEnvironmentVariables] = null,
            [PipeCommand.GetUserNotificationState] = null,
            [PipeCommand.GetForegroundWindowProcessId] = null,
            [PipeCommand.GetEnvironmentVariable] = typeof(EnvironmentVariablePayload),
            [PipeCommand.SetEnvironmentVariable] = typeof(EnvironmentVariablePayload),
            [PipeCommand.RemoveEnvironmentVariable] = typeof(EnvironmentVariablePayload),
        });
    }
}
