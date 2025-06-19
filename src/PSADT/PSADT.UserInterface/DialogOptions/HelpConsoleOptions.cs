using System;
using System.Collections;
using System.Linq;
using Microsoft.PowerShell;
using Newtonsoft.Json;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for all dialogs.
    /// </summary>
    public sealed record HelpConsoleOptions
    {
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
        /// Initializes a new instance of the <see cref="HelpConsoleOptions"/> class with the specified execution policy
        /// and module paths.
        /// </summary>
        /// <remarks>This constructor is intended for internal use only and is marked as
        /// private.</remarks>
        /// <param name="executionPolicy">The execution policy to be applied. Cannot be <see langword="null"/>.</param>
        /// <param name="modulePaths">An array of module paths to be used. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="executionPolicy"/> or <paramref name="modulePaths"/> is <see langword="null"/>.</exception>
        [JsonConstructor]
        private HelpConsoleOptions(ExecutionPolicy executionPolicy, string[] modulePaths)
        {
            ExecutionPolicy = executionPolicy;
            ModulePaths = modulePaths ?? throw new ArgumentNullException(nameof(modulePaths));
        }

        /// <summary>
        /// Gets the execution policy that determines how operations are executed.
        /// </summary>
        [JsonProperty]
        public readonly ExecutionPolicy ExecutionPolicy;

        /// <summary>
        /// Gets the collection of file paths to the modules associated with the current instance.
        /// </summary>
        [JsonProperty]
        public readonly string[] ModulePaths;
    }
}
