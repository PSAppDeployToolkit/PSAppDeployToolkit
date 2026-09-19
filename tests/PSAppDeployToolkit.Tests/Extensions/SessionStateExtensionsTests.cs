using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using PSADT.PowerShellTestFixture;
using PSAppDeployToolkit.Extensions;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Extensions
{
    /// <summary>
    /// Tests the two ways the toolkit runs a script block against a session state.
    /// </summary>
    /// <remarks>
    /// Which session state a script runs against is the whole point. The script blocks the toolkit builds reach for
    /// <c language="powershell">$Script:CommandTable</c>, which only the module's own scope can resolve, so running
    /// them anywhere else fails on the first command they name.
    /// </remarks>
    /// <param name="powerShell">The hosted engine, shared across the collection.</param>
    [Collection(PowerShellCollection.Name)]
    public sealed class SessionStateExtensionsTests(PowerShellFixture powerShell)
    {
        /// <summary>
        /// Verifies that a script runs against the session state it was handed.
        /// </summary>
        [Fact]
        public void InvokeScript_RunsAgainstTheSessionStateItWasGiven()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();

            // Act
            powerShell.ModuleSessionState.InvokeScript(ScriptBlock.Create("$Script:SessionStateExtensionsProbe = 'ran'"));

            // Assert
            Assert.Equal("ran", powerShell.ModuleSessionState.PSVariable.GetValue("SessionStateExtensionsProbe"));
        }

        /// <summary>
        /// Verifies that arguments reach the script block.
        /// </summary>
        [Fact]
        public void InvokeScript_PassesItsArgumentsThrough()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();

            // Act
            powerShell.ModuleSessionState.InvokeScript(ScriptBlock.Create("$Script:SessionStateExtensionsArgs = $args -join ','"), "first", "second");

            // Assert
            Assert.Equal("first,second", powerShell.ModuleSessionState.PSVariable.GetValue("SessionStateExtensionsArgs"));
        }

        /// <summary>
        /// Verifies that the void form discards whatever the script wrote.
        /// </summary>
        /// <remarks>
        /// Worth stating because the two overloads differ only in their return, so a caller choosing the wrong one
        /// would otherwise be silently choosing whether output is kept.
        /// </remarks>
        [Fact]
        public void InvokeScript_DiscardsOutputWhenNothingIsAskedFor()
        {
            using IDisposable scope = powerShell.Enter();
            Assert.Null(Record.Exception(() => powerShell.ModuleSessionState.InvokeScript(ScriptBlock.Create("1; 2; 3"))));
        }

        /// <summary>
        /// Verifies that the generic form hands back each result unwrapped to the type asked for.
        /// </summary>
        [Fact]
        public void InvokeScriptOfT_UnwrapsEachResultToTheTypeAskedFor()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();

            // Assert
            Assert.Equal([1, 2, 3], powerShell.ModuleSessionState.InvokeScript<int>(ScriptBlock.Create("1; 2; 3")));
            Assert.Equal(["one"], powerShell.ModuleSessionState.InvokeScript<string>(ScriptBlock.Create("'one'")), StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that the generic form reaches the module's own variables.
        /// </summary>
        /// <remarks>
        /// The reason this extension exists rather than any of the simpler ways to run a script block: the module's
        /// command table is only resolvable from the module's scope.
        /// </remarks>
        [Fact]
        public void InvokeScriptOfT_ReachesTheModulesCommandTable()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();

            // Act
            IEnumerable<string> names = powerShell.ModuleSessionState.InvokeScript<string>(ScriptBlock.Create("$Script:CommandTable.Keys"));

            // Assert
            Assert.Contains("Get-PSCallStack", names, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that the generic form fails rather than skipping a result of the wrong type.
        /// </summary>
        /// <remarks>
        /// It casts rather than filters, so a script returning something unexpected is a fault at the call site rather
        /// than a quietly short result.
        /// </remarks>
        [Fact]
        public void InvokeScriptOfT_FailsOnAResultOfTheWrongType()
        {
            using IDisposable scope = powerShell.Enter();
            _ = Assert.Throws<InvalidCastException>(() => powerShell.ModuleSessionState.InvokeScript<int>(ScriptBlock.Create("'not a number'")).ToList());
        }
    }
}
