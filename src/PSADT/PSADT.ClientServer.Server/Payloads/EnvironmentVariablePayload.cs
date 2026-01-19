using Newtonsoft.Json;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for environment variable operations.
    /// </summary>
    internal sealed record EnvironmentVariablePayload : IPayload
    {
        /// <summary>
        /// The name of the environment variable.
        /// </summary>
        [JsonProperty]
        internal readonly string Name;

        /// <summary>
        /// The value to set for the environment variable.
        /// </summary>
        /// <remarks>This field is only used for the SetEnvironmentVariable command and is null for Get/Remove operations.</remarks>
        [JsonProperty]
        internal readonly string? Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentVariablePayload"/> class.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="value">The value to set for the environment variable.</param>
        [JsonConstructor]
        internal EnvironmentVariablePayload(string name, string? value = null)
        {
            Name = name;
            Value = value;
        }
    }
}
