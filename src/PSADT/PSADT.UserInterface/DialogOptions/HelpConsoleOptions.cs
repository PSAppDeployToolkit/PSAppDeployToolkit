using System;
using System.Collections;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.PowerShell;
using PSADT.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for all dialogs.
    /// </summary>
    [DataContract]
    public sealed record HelpConsoleOptions
    {
        /// <summary>
        /// Initializes the <see cref="HelpConsoleOptions"/> class and registers it as a serializable type.
        /// </summary>
        /// <remarks>This static constructor ensures that the <see cref="HelpConsoleOptions"/> type is added
        /// to the list of serializable types for data contract serialization. This allows instances of <see
        /// cref="ClientException"/> to be serialized and deserialized using data contract serializers.</remarks>
        static HelpConsoleOptions()
        {
            DataContractSerialization.AddSerializableType(typeof(HelpConsoleOptions));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpConsoleOptions"/> class with the specified options.
        /// This accepts a hashtable of parameters to ease construction on the PowerShell side of things.
        /// </summary>
        /// <param name="options"></param>
        public HelpConsoleOptions(Hashtable options)
        {
            // Nothing here is allowed to be null.
            if (options["ExecutionPolicy"] is not ExecutionPolicy executionPolicy)
            {
                throw new ArgumentNullException("ExecutionPolicy value is null or invalid.", (Exception?)null);
            }
            if (options["ModulePaths"] is not string[] modulePaths || modulePaths.Length == 0 || modulePaths.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentNullException("ModulePaths value is null or invalid.", (Exception?)null);
            }

            // The hashtable was correctly defined, assign the remaining values.
            ExecutionPolicy = executionPolicy;
            ModulePaths = modulePaths;
        }

        /// <summary>
        /// Gets the execution policy that determines how operations are executed.
        /// </summary>
        [DataMember]
        public readonly ExecutionPolicy ExecutionPolicy;

        /// <summary>
        /// Gets the collection of file paths to the modules associated with the current instance.
        /// </summary>
        [DataMember]
        public readonly string[] ModulePaths;
    }
}
