using System.Collections.ObjectModel;
using PSADT.ProcessManagement;
using Newtonsoft.Json;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the InitCloseAppsDialog command.
    /// </summary>
    internal sealed record InitCloseAppsDialogPayload : IPayload
    {
        /// <summary>
        /// The collection of process definitions to monitor.
        /// </summary>
        [JsonProperty]
        internal readonly ReadOnlyCollection<ProcessDefinition> ProcessDefinitions;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitCloseAppsDialogPayload"/> class.
        /// </summary>
        /// <param name="processDefinitions">The collection of process definitions to monitor.</param>
        [JsonConstructor]
        internal InitCloseAppsDialogPayload(ReadOnlyCollection<ProcessDefinition> processDefinitions)
        {
            ProcessDefinitions = processDefinitions;
        }
    }
}
