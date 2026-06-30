using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using PSADT.ProcessManagement;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the InitCloseAppsDialog command.
    /// </summary>
    /// <param name="ProcessDefinitions">The collection of process definitions to monitor, or null if no processes need to be monitored.</param>
    [DataContract]
    internal sealed record class InitCloseAppsDialogPayload(ReadOnlyCollection<ProcessDefinition>? ProcessDefinitions) : IClientServerPayload
    {
        /// <summary>
        /// The collection of process definitions to monitor, or null if no processes need to be monitored.
        /// </summary>
        [DataMember]
        internal readonly ReadOnlyCollection<ProcessDefinition>? ProcessDefinitions = ProcessDefinitions;
    }
}
