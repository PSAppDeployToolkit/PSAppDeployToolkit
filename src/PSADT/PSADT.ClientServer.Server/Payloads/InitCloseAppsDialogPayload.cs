using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using PSADT.ProcessManagement;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the InitCloseAppsDialog command.
    /// </summary>
    [DataContract]
    internal sealed record InitCloseAppsDialogPayload : IClientServerPayload
    {
        /// <summary>
        /// The collection of process definitions to monitor, or null if no processes need to be monitored.
        /// </summary>
        [DataMember]
        internal readonly ReadOnlyCollection<ProcessDefinition>? ProcessDefinitions;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitCloseAppsDialogPayload"/> class.
        /// </summary>
        /// <param name="processDefinitions">The collection of process definitions to monitor, or null if no processes need to be monitored.</param>
        internal InitCloseAppsDialogPayload(ReadOnlyCollection<ProcessDefinition>? processDefinitions)
        {
            ProcessDefinitions = processDefinitions;
        }
    }
}
