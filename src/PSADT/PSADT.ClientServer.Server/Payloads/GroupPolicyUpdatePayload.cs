using System.Runtime.Serialization;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Represents the payload for a group policy update operation, specifying options for forced reapplication of policy settings.
    /// </summary>
    /// <param name="Force">Reapplies all policy settings. By default, only policy settings that have changed are applied.</param>
    [DataContract]
    internal sealed record class GroupPolicyUpdatePayload(bool Force) : IClientServerPayload
    {
        /// <summary>
        /// Reapplies all policy settings. By default, only policy settings that have changed are applied.
        /// </summary>
        [DataMember]
        internal readonly bool Force = Force;
    }
}
