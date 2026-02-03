using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PSADT.ClientServer.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public override PipeRequest? ReadJson(JsonReader reader, Type objectType, PipeRequest? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // Get the command to determine payload type.
            JObject jObject = JObject.Load(reader);
            if (jObject["Command"]?.ToObject<PipeCommand>() is not PipeCommand command)
            {
                throw new JsonSerializationException("Missing or invalid Command.");
            }

            // Deserialize the payload based on the command.
            return jObject["Payload"] is JToken payloadToken && payloadToken.Type != JTokenType.Null && CommandToPayloadType.TryGetValue(command, out Type? payloadType) && payloadType is not null
                ? new(command, (IPayload?)payloadToken.ToObject(payloadType, serializer))
                : (PipeRequest)new(command, null);
        }

        /// <summary>
        /// Writes the <see cref="PipeRequest"/> as JSON.
        /// </summary>
        public override void WriteJson(JsonWriter writer, PipeRequest? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Cannot serialize a null PipeRequest.");
            }
            writer.WriteStartObject();
            writer.WritePropertyName("Command");
            serializer.Serialize(writer, value.Command);
            if (value.Payload is not null)
            {
                writer.WritePropertyName("Payload");
                serializer.Serialize(writer, value.Payload);
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
