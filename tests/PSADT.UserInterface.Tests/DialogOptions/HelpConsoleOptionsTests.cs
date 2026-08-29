using System;
using System.Collections;
using System.Collections.Generic;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogOptions
{
    /// <summary>
    /// Tests the options for the help console.
    /// </summary>
    /// <remarks>
    /// A single nested dictionary of module name to command name to help text. Like the list selection
    /// dialog, the interesting part is that a dictionary held by reference would defeat the record, so
    /// the map is copied into nested <c>ValueDictionary</c> instances and rebuilt as a read-only view on
    /// each read.
    /// </remarks>
    public sealed class HelpConsoleOptionsTests
    {
        /// <summary>
        /// Verifies that the nested map survives being copied in and rebuilt on the way out.
        /// </summary>
        [Fact]
        public void Constructor_KeepsTheWholeNestedMap()
        {
            // Arrange
            Hashtable table = new()
            {
                ["ModuleHelpMap"] = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
                {
                    ["PSAppDeployToolkit"] = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["Show-ADTInstallationWelcome"] = "closes applications",
                        ["Show-ADTInstallationProgress"] = "shows progress",
                    },
                    ["PSAppDeployToolkit.Extensions"] = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["Invoke-ADTExtension"] = "does something else",
                    },
                },
            };

            // Act
            HelpConsoleOptions options = new(table);

            // Assert
            Assert.Equal(2, options.ModuleHelpMap.Count);
            Assert.Equal("closes applications", options.ModuleHelpMap["PSAppDeployToolkit"]["Show-ADTInstallationWelcome"]);
            Assert.Equal("shows progress", options.ModuleHelpMap["PSAppDeployToolkit"]["Show-ADTInstallationProgress"]);
            Assert.Equal("does something else", options.ModuleHelpMap["PSAppDeployToolkit.Extensions"]["Invoke-ADTExtension"]);
        }

        /// <summary>
        /// Verifies that the map is required.
        /// </summary>
        [Fact]
        public void Constructor_RefusesADictionaryMissingTheMap()
        {
            // Arrange
            Hashtable table = SampleOptions.HelpConsole();
            table.Remove("ModuleHelpMap");

            // Act & Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new HelpConsoleOptions(table));
            Assert.Contains("ModuleHelpMap", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a map with no modules in it is accepted.
        /// </summary>
        /// <remarks>
        /// Empty is not the same as absent here. A help console with nothing to show is a console listing
        /// no commands, which is a coherent thing to render, so only a missing map is refused.
        /// </remarks>
        [Fact]
        public void Constructor_AcceptsAnEmptyMap()
        {
            // Arrange
            Hashtable table = new()
            {
                ["ModuleHelpMap"] = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal),
            };

            // Act & Assert
            Assert.Empty(new HelpConsoleOptions(table).ModuleHelpMap);
        }

        /// <summary>
        /// Verifies that the map is copied rather than referenced.
        /// </summary>
        [Fact]
        public void ModuleHelpMap_IsCopiedFromWhatTheCallerHandedIn()
        {
            // Arrange
            Dictionary<string, string> commands = new(StringComparer.Ordinal) { ["a function"] = "what it does" };
            Dictionary<string, IReadOnlyDictionary<string, string>> source = new(StringComparer.Ordinal) { ["a module"] = commands };
            HelpConsoleOptions options = new(new Hashtable { ["ModuleHelpMap"] = source });

            // Act
            commands["another function"] = "something new";
            source["another module"] = commands;

            // Assert
            _ = Assert.Single(options.ModuleHelpMap);
            _ = Assert.Single(options.ModuleHelpMap["a module"]);
        }

        /// <summary>
        /// Verifies that the view is rebuilt on each read rather than handed out.
        /// </summary>
        [Fact]
        public void ModuleHelpMap_IsRebuiltOnEachRead()
        {
            // Act
            HelpConsoleOptions options = new(SampleOptions.HelpConsole());

            // Assert
            Assert.NotSame(options.ModuleHelpMap, options.ModuleHelpMap);
        }

        /// <summary>
        /// Verifies that two consoles offering the same help are equal despite holding separate maps.
        /// </summary>
        /// <remarks>
        /// The reason the backing field is a nested <c>ValueDictionary</c>: the outer dictionary compares
        /// its values with the same comparer, so an ordinary inner dictionary would compare by reference
        /// and the outer comparison would fail even for identical contents.
        /// </remarks>
        [Fact]
        public void Equality_IsByTheContentsOfTheNestedMap()
        {
            // Arrange
            Hashtable different = SampleOptions.HelpConsole();
            different["ModuleHelpMap"] = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
            {
                ["a module"] = new Dictionary<string, string>(StringComparer.Ordinal) { ["a function"] = "something else" },
            };

            // Assert
            Assert.Equal(new HelpConsoleOptions(SampleOptions.HelpConsole()), new HelpConsoleOptions(SampleOptions.HelpConsole()));
            Assert.NotEqual(new HelpConsoleOptions(SampleOptions.HelpConsole()), new HelpConsoleOptions(different));
        }
    }
}
