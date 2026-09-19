using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using PSADT.PowerShellTestFixture;
using PSAppDeployToolkit.Foundation;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Foundation
{
    /// <summary>
    /// Tests the set of lists a deployment registers its callbacks into.
    /// </summary>
    /// <remarks>
    /// A property per hookpoint rather than a keyed table, which is what makes the hookpoints checkable at compile
    /// time. What is left to check here is that they are ten separate lists: a caller registering an
    /// <c language="text">OnExit</c> callback and finding it invoked at <c language="text">OnInit</c> would be a
    /// deployment running its cleanup before it started.
    /// </remarks>
    /// <param name="powerShell">The hosted engine, shared across the collection.</param>
    [Collection(PowerShellCollection.Name)]
    public sealed class ModuleCallbacksTests(PowerShellFixture powerShell)
    {
        /// <summary>
        /// Verifies that a fresh set carries a list for every hookpoint and that all of them are empty.
        /// </summary>
        [Fact]
        public void Constructor_StartsEveryHookpointEmpty()
        {
            // Act
            ModuleCallbacks callbacks = new();

            // Assert
            Assert.NotEmpty(Hookpoints(callbacks));
            Assert.All(Hookpoints(callbacks), static hookpoint => Assert.Empty(hookpoint.Value));
        }

        /// <summary>
        /// Verifies that every hookpoint is its own list rather than one shared between them.
        /// </summary>
        /// <remarks>
        /// Asserted by filling each in turn and counting the rest, so a pair sharing an instance is caught wherever in
        /// the set it sits rather than only between the two a case happened to name.
        /// </remarks>
        [Fact]
        public void Constructor_GivesEachHookpointItsOwnList()
        {
            // Arrange
            ModuleCallbacks callbacks = new();
            CommandInfo command = AnyCommand();

            // Act and assert
            foreach (KeyValuePair<string, IList<CommandInfo>> hookpoint in Hookpoints(callbacks))
            {
                hookpoint.Value.Add(command);
                Assert.Equal(
                    [hookpoint.Key],
                    [.. Hookpoints(callbacks).Where(static h => h.Value.Count > 0).Select(static h => h.Key)],
                    StringComparer.Ordinal);
                hookpoint.Value.Clear();
            }
        }

        /// <summary>
        /// Verifies that a hookpoint keeps what it is given, in the order it was given.
        /// </summary>
        /// <remarks>
        /// Order is the point: callbacks run in registration order, so a set that reordered them would change when a
        /// deployment's own steps happened relative to one another.
        /// </remarks>
        [Fact]
        public void Hookpoint_KeepsWhatItWasGivenInOrder()
        {
            // Arrange
            ModuleCallbacks callbacks = new();
            CommandInfo first = Command("Get-PSCallStack");
            CommandInfo second = Command("Get-Command");

            // Act
            callbacks.OnInit.Add(first);
            callbacks.OnInit.Add(second);

            // Assert
            Assert.Equal([first, second], callbacks.OnInit);
        }

        /// <summary>
        /// Reads every hookpoint list off the set by reflection, keyed on the property's name.
        /// </summary>
        /// <remarks>
        /// Read rather than listed so that a hookpoint added later is covered without this file being touched, which
        /// is the same reason the PowerShell side reads them from the enumeration.
        /// </remarks>
        /// <param name="callbacks">The set to read.</param>
        /// <returns>Each hookpoint's name and its list.</returns>
        private static IReadOnlyList<KeyValuePair<string, IList<CommandInfo>>> Hookpoints(ModuleCallbacks callbacks)
        {
            return [.. typeof(ModuleCallbacks)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(static p => p.PropertyType == typeof(IList<CommandInfo>))
                .Select(p => new KeyValuePair<string, IList<CommandInfo>>(p.Name, (IList<CommandInfo>)(p.GetValue(callbacks) ?? throw new InvalidOperationException($"The [{p.Name}] hookpoint holds nothing at all."))))];
        }

        /// <summary>
        /// Resolves a command, for a test that needs something of the right type rather than a particular command.
        /// </summary>
        /// <returns>A command.</returns>
        private CommandInfo AnyCommand()
        {
            return Command("Get-PSCallStack");
        }

        /// <summary>
        /// Resolves the named command from the fixture's runspace.
        /// </summary>
        /// <param name="name">The command to resolve.</param>
        /// <returns>The command.</returns>
        private CommandInfo Command(string name)
        {
            return (CommandInfo)powerShell.InvokeInRunspace($"Get-Command -Name '{name}'")[0].BaseObject;
        }
    }
}
