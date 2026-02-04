using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using PSADT.Foundation;

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
        public override RunAsActiveUser Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Ensure the validity of the JSON structure.
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Cannot deserialize null RunAsActiveUser.");
            }
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected start of object for RunAsActiveUser, got {reader.TokenType}.");
            }

            // Read properties until the end of the object.
            string? ntAccountValue = null; string? sidValue = null; uint sessionId = 0; bool? isLocalAdmin = null;
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
                    case "NTAccount":
                        ntAccountValue = reader.GetString();
                        break;
                    case "SID":
                        sidValue = reader.GetString();
                        break;
                    case "SessionId":
                        sessionId = reader.GetUInt32();
                        break;
                    case "IsLocalAdmin":
                        isLocalAdmin = reader.TokenType != JsonTokenType.Null ? reader.GetBoolean() : null;
                        break;
                    default:
                        // Skip unknown properties for forward compatibility.
                        reader.Skip();
                        break;
                }
            }

            // Validate required properties.
            if (string.IsNullOrWhiteSpace(ntAccountValue) || string.IsNullOrWhiteSpace(sidValue))
            {
                throw new JsonException("RunAsActiveUser requires NTAccount and SID properties.");
            }
            return new(new(ntAccountValue), new(sidValue), sessionId, isLocalAdmin);
        }

        /// <summary>
        /// Writes the <see cref="RunAsActiveUser"/> as JSON.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, RunAsActiveUser value, JsonSerializerOptions options)
        {
            // Ensure the value is not null.
            if (value is null)
            {
                throw new JsonException("Cannot serialize null RunAsActiveUser.");
            }

            // Write the JSON object.
            writer.WriteStartObject();
            writer.WriteString("NTAccount", value.NTAccount.Value);
            writer.WriteString("SID", value.SID.Value);
            writer.WriteNumber("SessionId", value.SessionId);
            if (value.IsLocalAdmin is not null)
            {
                writer.WriteBoolean("IsLocalAdmin", value.IsLocalAdmin.Value);
            }
            writer.WriteEndObject();
        }
    }
}
