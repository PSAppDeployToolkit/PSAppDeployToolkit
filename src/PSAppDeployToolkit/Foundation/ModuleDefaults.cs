using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// Represents the default configuration and string values for a module in the PSAppDeployToolkit.Foundation namespace.
    /// </summary>
    public sealed class ModuleDefaults
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleDefaults"/> class with the specified configuration and string values.
        /// </summary>
        /// <param name="defaults">A dictionary containing the default configuration and string values for the module.</param>
        public ModuleDefaults(IReadOnlyDictionary<string, IReadOnlyDictionary<string, ScriptBlock>> defaults)
        {
            if (defaults?.Count is not 2)
            {
                throw new ArgumentException("Defaults must contain 2 elements.", nameof(defaults));
            }
            if (!defaults.TryGetValue("Config", out IReadOnlyDictionary<string, ScriptBlock>? config))
            {
                throw new ArgumentException("Defaults must contain a 'Config' element.", nameof(defaults));
            }
            if (!defaults.TryGetValue("Strings", out IReadOnlyDictionary<string, ScriptBlock>? strings))
            {
                throw new ArgumentException("Defaults must contain a 'Strings' element.", nameof(defaults));
            }
            if (config?.Count is not 1)
            {
                throw new ArgumentException("Config element must contain 1 element.", nameof(defaults));
            }
            if (!(strings?.Count > 0))
            {
                throw new ArgumentException("Strings element must contain at least 1 element.", nameof(defaults));
            }
            if (!config.ContainsKey(string.Empty))
            {
                throw new ArgumentException("Config element must contain a single element with an empty key.", nameof(defaults));
            }
            if (!strings.ContainsKey(string.Empty))
            {
                throw new ArgumentException("Strings element must contain a single element with an empty key.", nameof(defaults));
            }
            Config = defaults["Config"];
            Strings = defaults["Strings"];
        }

        /// <summary>
        /// Gets the default configuration values for the module.
        /// </summary>
        public IReadOnlyDictionary<string, ScriptBlock> Config { get; }

        /// <summary>
        /// Gets the default string values for the module.
        /// </summary>
        public IReadOnlyDictionary<string, ScriptBlock> Strings { get; }
    }
}
