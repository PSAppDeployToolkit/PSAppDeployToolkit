using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PSADT.ClientServer.Payloads;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Dialogs;
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
        public override ShowModalDialogPayload? ReadJson(JsonReader reader, Type objectType, ShowModalDialogPayload? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // Deserialize Options based on DialogType.
            JObject jObject = JObject.Load(reader);
            if (jObject["DialogType"]?.ToObject<DialogType>() is not DialogType dialogType || !DialogTypeToOptionsType.TryGetValue(dialogType, out Type? optionsType))
            {
                throw new JsonSerializationException("Missing or invalid DialogType.");
            }
            if (jObject["DialogStyle"]?.ToObject<DialogStyle>() is not DialogStyle dialogStyle)
            {
                throw new JsonSerializationException("Missing or invalid DialogStyle.");
            }
            if (jObject["Options"]?.ToObject(optionsType, serializer) is not object options)
            {
                throw new JsonSerializationException("Missing or invalid Options.");
            }

            // Create and return the ShowModalDialogPayload.
            return new(dialogType, dialogStyle, options);
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
            serializer.Serialize(writer, value.DialogType);
            writer.WritePropertyName("DialogStyle");
            serializer.Serialize(writer, value.DialogStyle);
            writer.WritePropertyName("Options");
            serializer.Serialize(writer, value.Options);
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
