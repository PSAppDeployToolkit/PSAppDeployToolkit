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
        /// Sets the environment variable as a REG_EXPAND_SZ type.
        /// </summary>
        [JsonProperty]
        internal readonly bool Expandable;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentVariablePayload"/> class.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="value">The value to set for the environment variable.</param>
        /// <param name="expandable">Indicates whether the value is expandable.</param>
        [JsonConstructor]
        internal EnvironmentVariablePayload(string name, string? value = null, bool expandable = false)
        {
            Name = name;
            Value = value;
            Expandable = expandable;
        }
    }
}
