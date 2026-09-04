using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using PSADT.Collections;
using PSADT.ProcessManagement;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the InitCloseAppsDialog command.
    /// </summary>
    /// <remarks>The definitions are held as a <see cref="ValueList{T}"/> so that this record compares by their
    /// contents. Every collection the framework offers compares by reference, so holding one directly would make two
    /// payloads listing the same applications unequal however alike they were, while the generated <c language="csharp">ToString</c>
    /// rendered them identically.</remarks>
    [DataContract]
    internal sealed record class InitCloseAppsDialogPayload : IClientServerPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InitCloseAppsDialogPayload"/> class.
        /// </summary>
        /// <param name="processDefinitions">The collection of process definitions to monitor, or null if no processes need to be monitored.</param>
        internal InitCloseAppsDialogPayload(IReadOnlyList<ProcessDefinition>? processDefinitions)
        {
            ProcessDefinitionsValue = processDefinitions is not null ? new ValueList<ProcessDefinition>([.. processDefinitions]) : null;
        }

        /// <summary>
        /// The collection of process definitions to monitor, or null if no processes need to be monitored.
        /// </summary>
        /// <remarks>Nothing at all is not the same as an empty collection: the client refuses a later prompt
        /// outright when no definitions were given, where an empty collection is a dialog about no applications.</remarks>
        [IgnoreDataMember]
        internal IReadOnlyList<ProcessDefinition>? ProcessDefinitions => ProcessDefinitionsValue is not null ? new ReadOnlyCollection<ProcessDefinition>([.. ProcessDefinitionsValue]) : null;

        /// <summary>
        /// The definitions recorded for <see cref="ProcessDefinitions"/>.
        /// </summary>
        [DataMember]
        private readonly ValueList<ProcessDefinition>? ProcessDefinitionsValue;
    }
}
