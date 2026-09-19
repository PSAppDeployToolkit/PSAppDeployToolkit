using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using PSAppDeployToolkit.Utilities;

namespace PSAppDeployToolkit.Extensions
{
    internal static class SessionStateExtensions
    {
        /// <summary>
        /// Invokes a script block in the context of the specified session state with the provided arguments.
        /// </summary>
        /// <param name="sessionState">The session state in which to invoke the script block.</param>
        /// <param name="scriptBlock">The script block to invoke.</param>
        /// <param name="args">The arguments to pass to the script block.</param>
        internal static void InvokeScript(this SessionState sessionState, ScriptBlock scriptBlock, params object[] args)
        {
            _ = sessionState.InvokeCommand.InvokeScript(sessionState, scriptBlock, args);
        }

        /// <summary>
        /// Invokes a script block in the context of the specified session state with the provided arguments and returns the results.
        /// </summary>
        /// <typeparam name="T">The type of the results.</typeparam>
        /// <param name="sessionState">The session state in which to invoke the script block.</param>
        /// <param name="scriptBlock">The script block to invoke.</param>
        /// <param name="args">The arguments to pass to the script block.</param>
        /// <returns>An enumerable of the results of the script block invocation.</returns>
        internal static IEnumerable<T> InvokeScript<T>(this SessionState sessionState, ScriptBlock scriptBlock, params object[] args)
        {
            return sessionState.InvokeCommand.InvokeScript(sessionState, scriptBlock, args).Select(PowerShellUtilities.GetBaseObject<T>);
        }
    }
}
