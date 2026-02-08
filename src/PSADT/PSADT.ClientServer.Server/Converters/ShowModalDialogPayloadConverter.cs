using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PSADT.ClientServer.Payloads;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public override ShowModalDialogPayload ReadJson(JsonReader reader, Type objectType, ShowModalDialogPayload? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // Validate the token type.
            if (reader.TokenType == JsonToken.Null)
            {
                throw new JsonSerializationException("Cannot deserialize null ShowModalDialogPayload.");
            }

            // Get the DialogType value.
            JObject obj = JObject.Load(reader);
            if (obj["DialogType"]?.Value<int>() is not int dialogTypeInt)
            {
                throw new JsonSerializationException("Missing or invalid DialogType.");
            }
            DialogType dialogType = (DialogType)dialogTypeInt;

            // Get the DialogStyle value.
            if (obj["DialogStyle"]?.Value<int>() is not int dialogStyleInt)
            {
                throw new JsonSerializationException("Missing or invalid DialogStyle.");
            }
            DialogStyle dialogStyle = (DialogStyle)dialogStyleInt;

            // Get the Options type for the given dialog.
            if (!DialogTypeToOptionsType.TryGetValue(dialogType, out Type? optionsType))
            {
                throw new JsonSerializationException($"Unknown DialogType: {dialogType}");
            }
            if (obj["Options"] is not JToken optionsToken)
            {
                throw new JsonSerializationException("Missing or invalid Options.");
            }
            if (optionsToken.ToObject(optionsType, serializer) is not IDialogOptions optionsValue)
            {
                throw new JsonSerializationException("Failed to deserialize Options.");
            }

            // Create and return the ShowModalDialogPayload.
            return new ShowModalDialogPayload(dialogType, dialogStyle, optionsValue);
        }

        /// <summary>
        /// Writes the <see cref="ShowModalDialogPayload"/> as JSON.
        /// </summary>
        public override void WriteJson(JsonWriter writer, ShowModalDialogPayload? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Cannot serialize a null ShowModalDialogPayload.");
            }
            writer.WriteStartObject();
            writer.WritePropertyName("DialogType");
            writer.WriteValue((int)value.DialogType);
            writer.WritePropertyName("DialogStyle");
            writer.WriteValue((int)value.DialogStyle);
            writer.WritePropertyName("Options");
            serializer.Serialize(writer, value.Options, value.Options.GetType());
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
