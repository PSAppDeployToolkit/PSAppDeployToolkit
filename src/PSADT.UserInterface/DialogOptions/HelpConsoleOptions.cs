using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using PSADT.Collections;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for all dialogs.
    /// </summary>
    [DataContract]
    [KnownType(typeof(EquatableDictionary<string, string>))]
    public sealed record class HelpConsoleOptions : IDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the HelpConsoleOptions class using the specified options dictionary.
        /// </summary>
        /// <remarks>The constructor extracts the 'ModuleHelpMap' entry from the provided options
        /// dictionary. This entry is expected to contain help information for modules, which is used to configure the
        /// help console.</remarks>
        /// <param name="options">A dictionary containing configuration options for the help console. Must not be null and must include a key
        /// named 'ModuleHelpMap' that maps to a read-only dictionary of module help information.</param>
        /// <exception cref="ArgumentNullException">Thrown if the options parameter is null.</exception>
        public HelpConsoleOptions(IDictionary options) : this((IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>?)(options ?? throw new ArgumentNullException(nameof(options)))["ModuleHelpMap"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'ModuleHelpMap' is missing."))
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
            ArgumentNullException.ThrowIfNull(moduleHelpMap);
            ModuleHelpMapValue = new(moduleHelpMap.Select(static module => new KeyValuePair<string, IReadOnlyDictionary<string, string>>(module.Key, new EquatableDictionary<string, string>(module.Value))));
        }

        /// <summary>
        /// Gets a read-only dictionary that maps module names to their associated help topics and descriptions.
        /// </summary>
        /// <remarks>Use this property to retrieve help information for specific modules. Each entry in
        /// the dictionary represents a module, with its value being another dictionary that maps help topic names to
        /// their corresponding descriptions. This structure enables efficient access to context-sensitive help content
        /// for different modules within the application.
        /// <para>Held as a <see cref="EquatableDictionary{TKey, TValue}"/>, inner dictionaries included, so that this
        /// record compares by the entries all the way down. Every dictionary the framework offers compares by
        /// reference, so holding one directly would make two consoles offering the same help unequal however alike
        /// they were. The inner dictionaries are declared as the interface rather than as what they are, which is what
        /// lets the whole thing be handed back as it is held: a dictionary of a concrete type is not a dictionary of
        /// that type's interface, so declaring them concretely would leave no way to return this without rebuilding
        /// both levels on every read.</para></remarks>
        [IgnoreDataMember]
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ModuleHelpMap => ModuleHelpMapValue;

        /// <summary>
        /// The help recorded for <see cref="ModuleHelpMap"/>.
        /// </summary>
        [DataMember]
        private readonly EquatableDictionary<string, IReadOnlyDictionary<string, string>> ModuleHelpMapValue;
    }
}
