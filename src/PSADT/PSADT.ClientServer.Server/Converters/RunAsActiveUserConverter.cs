using System;
using PSADT.Foundation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSADT.ClientServer.Converters
{
    /// <summary>
    /// Custom JSON converter for <see cref="RunAsActiveUser"/> that serializes the user's
    /// NT account, security identifier, session ID, and local admin status.
    /// </summary>
    internal sealed class RunAsActiveUserConverter : JsonConverter<RunAsActiveUser>
    {
        /// <summary>
        /// Reads and converts the JSON to a <see cref="RunAsActiveUser"/> instance.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public override RunAsActiveUser ReadJson(JsonReader reader, Type objectType, RunAsActiveUser? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // Ensure the validity of the JSON structure.
            if (reader.TokenType == JsonToken.Null)
            {
                throw new JsonSerializationException("Cannot deserialize null RunAsActiveUser.");
            }
            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonSerializationException($"Expected start of object for RunAsActiveUser, got {reader.TokenType}.");
            }

            // Read properties from the JObject.
            JObject obj = JObject.Load(reader);
            string? ntAccountValue = obj["NTAccount"]?.Value<string>();
            string? sidValue = obj["SID"]?.Value<string>();
            uint sessionId = obj["SessionId"]?.Value<uint>() ?? 0;
            bool? isLocalAdmin = obj["IsLocalAdmin"]?.Value<bool>();

            // Validate required properties.
            if (string.IsNullOrWhiteSpace(ntAccountValue) || string.IsNullOrWhiteSpace(sidValue))
            {
                throw new JsonSerializationException("RunAsActiveUser requires NTAccount and SID properties.");
            }
            return new(new(ntAccountValue), new(sidValue), sessionId, isLocalAdmin);
        }

        /// <summary>
        /// Writes the <see cref="RunAsActiveUser"/> as JSON.
        /// </summary>
        public override void WriteJson(JsonWriter writer, RunAsActiveUser? value, JsonSerializer serializer)
        {
            // Ensure the value is not null.
            if (value is null)
            {
                throw new JsonSerializationException("Cannot serialize null RunAsActiveUser.");
            }

            // Write the JSON object.
            writer.WriteStartObject();
            writer.WritePropertyName("NTAccount");
            writer.WriteValue(value.NTAccount.Value);
            writer.WritePropertyName("SID");
            writer.WriteValue(value.SID.Value);
            writer.WritePropertyName("SessionId");
            writer.WriteValue(value.SessionId);
            if (value.IsLocalAdmin is not null)
            {
                writer.WritePropertyName("IsLocalAdmin");
                writer.WriteValue(value.IsLocalAdmin.Value);
            }
            writer.WriteEndObject();
        }
    }
}
