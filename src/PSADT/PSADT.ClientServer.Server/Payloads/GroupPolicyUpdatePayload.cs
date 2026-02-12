using System.Runtime.Serialization;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Represents the payload for a group policy update operation, specifying options for forced reapplication of policy settings.
    /// </summary>
    [DataContract]
    internal sealed record GroupPolicyUpdatePayload : IClientServerPayload
    {
        /// <summary>
        /// Reapplies all policy settings. By default, only policy settings that have changed are applied.
        /// </summary>
        [DataMember]
        internal readonly bool Force;

        /// <summary>
        /// Initializes a new instance of the GroupPolicyUpdatePayload class with the specified synchronization and
        /// force options.
        /// </summary>
        /// <param name="force">Reapplies all policy settings. By default, only policy settings that have changed are applied.</param>
        internal GroupPolicyUpdatePayload(bool force)
        {
            Force = force;
        }
    }
}
