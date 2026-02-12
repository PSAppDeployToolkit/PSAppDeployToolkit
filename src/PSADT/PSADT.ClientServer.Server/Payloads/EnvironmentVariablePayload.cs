using System.Runtime.Serialization;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for environment variable operations.
    /// </summary>
    [DataContract]
    internal sealed record EnvironmentVariablePayload : IClientServerPayload
    {
        /// <summary>
        /// The name of the environment variable.
        /// </summary>
        [DataMember]
        internal readonly string Name;

        /// <summary>
        /// The value to set for the environment variable.
        /// </summary>
        /// <remarks>This field is only used for the SetEnvironmentVariable command and is null for Get/Remove operations.</remarks>
        [DataMember]
        internal readonly string? Value;

        /// <summary>
        /// Sets the environment variable as a REG_EXPAND_SZ type.
        /// </summary>
        [DataMember]
        internal readonly bool Expandable;

        /// <summary>
        /// Gets a value indicating whether data should be appended to an existing resource rather than overwriting it.
        /// </summary>
        [DataMember]
        internal readonly bool Append;

        /// <summary>
        /// Gets a value indicating whether the associated item should be removed.
        /// </summary>
        [DataMember]
        internal readonly bool Remove;

        /// <summary>
        /// Initializes a new instance of the EnvironmentVariablePayload class with the specified environment variable
        /// name, value, and options for expansion, appending, or removal.
        /// </summary>
        /// <param name="name">The name of the environment variable. Cannot be null.</param>
        /// <param name="value">The value to assign to the environment variable, or null to indicate no value.</param>
        /// <param name="expandable">true to mark the value as expandable (e.g., to allow variable substitution); otherwise, false.</param>
        /// <param name="append">true to append the value to the existing environment variable; otherwise, false.</param>
        /// <param name="remove">true to indicate that the environment variable should be removed; otherwise, false.</param>
        internal EnvironmentVariablePayload(string name, string? value = null, bool expandable = false, bool append = false, bool remove = false)
        {
            Name = name;
            Value = value;
            Expandable = expandable;
            Append = append;
            Remove = remove;
        }
    }
}
