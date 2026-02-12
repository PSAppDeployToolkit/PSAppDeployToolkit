using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for all dialogs.
    /// </summary>
    [DataContract]
    public sealed record HelpConsoleOptions : IDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HelpConsoleOptions"/> class with the specified options.
        /// This accepts a hashtable of parameters to ease construction on the PowerShell side of things.
        /// </summary>
        /// <param name="options"></param>
        public HelpConsoleOptions(Hashtable options) : this((options ?? throw new ArgumentNullException(nameof(options)))["ModuleHelpMap"] as IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ?? null!)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpConsoleOptions"/> class with the specified execution policy
        /// and module data.
        /// </summary>
        /// <param name="moduleHelpMap">A read-only dictionary containing module help information. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="moduleHelpMap"/> is null.</exception>
        private HelpConsoleOptions(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> moduleHelpMap)
        {
            ModuleHelpMap = moduleHelpMap ?? throw new ArgumentNullException(nameof(moduleHelpMap));
        }

        /// <summary>
        /// Gets a read-only dictionary that maps module names to their associated help topics and descriptions.
        /// </summary>
        /// <remarks>Use this property to retrieve help information for specific modules. Each entry in
        /// the dictionary represents a module, with its value being another dictionary that maps help topic names to
        /// their corresponding descriptions. This structure enables efficient access to context-sensitive help content
        /// for different modules within the application.</remarks>
        [DataMember]
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ModuleHelpMap { get; private set; }
    }
}
