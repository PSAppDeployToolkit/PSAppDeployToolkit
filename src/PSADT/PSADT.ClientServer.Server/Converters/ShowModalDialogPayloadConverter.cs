using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using PSADT.ClientServer.Payloads;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Dialogs;

namespace PSADT.ClientServer.Converters
{
    /// <summary>
    /// Custom JSON converter for <see cref="ShowModalDialogPayload"/> that handles polymorphic Options deserialization.
    /// </summary>
    /// <remarks>This converter uses the <see cref="DialogType"/> value as a discriminator to determine
    /// the correct options type during deserialization.</remarks>
    internal sealed class ShowModalDialogPayloadConverter : JsonConverter<ShowModalDialogPayload>
    {
        /// <summary>
        /// Reads and converts the JSON to a <see cref="ShowModalDialogPayload"/> instance.
        /// </summary>
        public override ShowModalDialogPayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Validate the token type.
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Cannot deserialize null ShowModalDialogPayload.");
            }

            // Get the document from from the reader.
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;

            // Get the DialogType value.
            if (!root.TryGetProperty("DialogType", out JsonElement dialogTypeElement))
            {
                throw new JsonException("Missing or invalid DialogType.");
            }
            DialogType dialogType = (DialogType)dialogTypeElement.GetInt32();

            // Get the DialogStyle value.
            if (!root.TryGetProperty("DialogStyle", out JsonElement dialogStyleElement))
            {
                throw new JsonException("Missing or invalid DialogStyle.");
            }
            DialogStyle dialogStyle = (DialogStyle)dialogStyleElement.GetInt32();

            // Get the Options type for the given dialog.
            if (!DialogTypeToOptionsType.TryGetValue(dialogType, out Type? optionsType))
            {
                throw new JsonException($"Unknown DialogType: {dialogType}");
            }
            if (!root.TryGetProperty("Options", out JsonElement optionsElement))
            {
                throw new JsonException("Missing or invalid Options.");
            }
            if (JsonSerializer.Deserialize(optionsElement.GetRawText(), optionsType, options) is not IDialogOptions optionsValue)
            {
                throw new JsonException("Failed to deserialize Options.");
            }

            // Create and return the ShowModalDialogPayload.
            return new ShowModalDialogPayload(dialogType, dialogStyle, optionsValue);
        }

        /// <summary>
        /// Writes the <see cref="ShowModalDialogPayload"/> as JSON.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, ShowModalDialogPayload value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Cannot serialize a null ShowModalDialogPayload.");
            }
            writer.WriteStartObject();
            writer.WriteNumber("DialogType", (int)value.DialogType);
            writer.WriteNumber("DialogStyle", (int)value.DialogStyle);
            writer.WritePropertyName("Options");
            JsonSerializer.Serialize(writer, value.Options, value.Options.GetType(), options);
            writer.WriteEndObject();
        }

        /// <summary>
        /// Maps <see cref="DialogType"/> values to their corresponding options types.
        /// </summary>
        private static readonly ReadOnlyDictionary<DialogType, Type> DialogTypeToOptionsType = new(new Dictionary<DialogType, Type>
        {
            [DialogType.CloseAppsDialog] = typeof(CloseAppsDialogOptions),
            [DialogType.CustomDialog] = typeof(CustomDialogOptions),
            [DialogType.DialogBox] = typeof(DialogBoxOptions),
            [DialogType.HelpConsole] = typeof(HelpConsoleOptions),
            [DialogType.InputDialog] = typeof(InputDialogOptions),
            [DialogType.ProgressDialog] = typeof(ProgressDialogOptions),
            [DialogType.RestartDialog] = typeof(RestartDialogOptions),
        });
    }
}
