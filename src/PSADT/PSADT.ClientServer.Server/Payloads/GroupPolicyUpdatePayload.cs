using Newtonsoft.Json;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Represents the payload for a group policy update operation, specifying options for forced reapplication of policy settings.
    /// </summary>
    internal sealed record GroupPolicyUpdatePayload : IPayload
    {
        /// <summary>
        /// Reapplies all policy settings. By default, only policy settings that have changed are applied.
        /// </summary>
        [JsonProperty]
        internal readonly bool Force;

        /// <summary>
        /// Initializes a new instance of the GroupPolicyUpdatePayload class with the specified synchronization and
        /// force options.
        /// </summary>
        /// <param name="force">Reapplies all policy settings. By default, only policy settings that have changed are applied.</param>
        [JsonConstructor]
        internal GroupPolicyUpdatePayload(bool force)
        {
            Force = force;
        }
    }
}
