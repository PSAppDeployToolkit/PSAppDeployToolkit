using System;
using System.Collections.Generic;
using System.Management.Automation;
using PSAppDeployToolkit.Foundation;
using Xunit;

namespace PSAppDeployToolkit.Tests.Foundation
{
    /// <summary>
    /// Tests the holder for the config and string tables the module ships with.
    /// </summary>
    /// <remarks>
    /// Almost all of this type is its constructor, and the constructor is almost all validation. That matters more than
    /// it looks: the dictionaries are built by a several-thousand-line literal in <c language="text">ImportsLast.ps1</c>,
    /// so a key lost or misspelled there is a mistake nothing else would catch until a deployment asked for a default
    /// that had quietly stopped existing.
    /// </remarks>
    public sealed class ModuleDefaultsTests
    {
        /// <summary>
        /// Verifies that a well-formed pair of tables is accepted and handed back.
        /// </summary>
        [Fact]
        public void Constructor_HandsBackTheTablesItWasGiven()
        {
            // Arrange
            IReadOnlyDictionary<string, ScriptBlock> config = Entries(string.Empty);
            IReadOnlyDictionary<string, ScriptBlock> strings = Entries(string.Empty, "en-AU");

            // Act
            ModuleDefaults defaults = new(new Dictionary<string, IReadOnlyDictionary<string, ScriptBlock>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Config", config },
                { "Strings", strings },
            });

            // Assert
            Assert.Same(config, defaults.Config);
            Assert.Same(strings, defaults.Strings);
        }

        /// <summary>
        /// Verifies that nothing at all is refused rather than dereferenced.
        /// </summary>
        [Fact]
        public void Constructor_RefusesNothingAtAll()
        {
            _ = Assert.Throws<ArgumentException>(static () => new ModuleDefaults(defaults: null!));
        }

        /// <summary>
        /// Verifies that the outer table has to carry exactly the two it is for.
        /// </summary>
        /// <remarks>
        /// A third entry means something was added that nothing reads, which is worth refusing rather than ignoring.
        /// </remarks>
        [Fact]
        public void Constructor_RefusesAnOuterTableThatIsNotAPair()
        {
            _ = Assert.Throws<ArgumentException>(static () => new ModuleDefaults(new Dictionary<string, IReadOnlyDictionary<string, ScriptBlock>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Config", Entries(string.Empty) },
            }));
            _ = Assert.Throws<ArgumentException>(static () => new ModuleDefaults(new Dictionary<string, IReadOnlyDictionary<string, ScriptBlock>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Config", Entries(string.Empty) },
                { "Strings", Entries(string.Empty) },
                { "Something", Entries(string.Empty) },
            }));
        }

        /// <summary>
        /// Verifies that both halves have to be present under the names that are read back.
        /// </summary>
        /// <remarks>
        /// Two entries is not enough on its own, so a misspelling is caught rather than counted.
        /// </remarks>
        [Fact]
        public void Constructor_RefusesAnOuterTableMissingEitherHalf()
        {
            Assert.Contains(
                "'Config'",
                Assert.Throws<ArgumentException>(static () => new ModuleDefaults(new Dictionary<string, IReadOnlyDictionary<string, ScriptBlock>>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Configs", Entries(string.Empty) },
                    { "Strings", Entries(string.Empty) },
                })).Message,
                StringComparison.Ordinal);
            Assert.Contains(
                "'Strings'",
                Assert.Throws<ArgumentException>(static () => new ModuleDefaults(new Dictionary<string, IReadOnlyDictionary<string, ScriptBlock>>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Config", Entries(string.Empty) },
                    { "String", Entries(string.Empty) },
                })).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the config table carries exactly one entry.
        /// </summary>
        /// <remarks>
        /// There is one shipped config and it is not localised, unlike the strings, so more than one entry means the
        /// two tables have been confused for one another.
        /// </remarks>
        [Fact]
        public void Constructor_RefusesAConfigTableThatIsNotASingleEntry()
        {
            _ = Assert.Throws<ArgumentException>(static () => new ModuleDefaults(Pair(Entries(), Entries(string.Empty))));
            _ = Assert.Throws<ArgumentException>(static () => new ModuleDefaults(Pair(Entries(string.Empty, "en-AU"), Entries(string.Empty))));
        }

        /// <summary>
        /// Verifies that the strings table carries at least one entry.
        /// </summary>
        [Fact]
        public void Constructor_RefusesAnEmptyStringsTable()
        {
            _ = Assert.Throws<ArgumentException>(static () => new ModuleDefaults(Pair(Entries(string.Empty), Entries())));
        }

        /// <summary>
        /// Verifies that each table carries the entry every lookup falls back to.
        /// </summary>
        /// <remarks>
        /// The empty key is the neutral culture, which is what a deployment gets when its own language ships no table
        /// of its own. Without it a lookup for an unshipped language finds nothing rather than falling back.
        /// </remarks>
        [Fact]
        public void Constructor_RefusesATableWithNoNeutralEntry()
        {
            _ = Assert.Throws<ArgumentException>(static () => new ModuleDefaults(Pair(Entries("en-AU"), Entries(string.Empty))));
            _ = Assert.Throws<ArgumentException>(static () => new ModuleDefaults(Pair(Entries(string.Empty), Entries("en-AU"))));
        }

        /// <summary>
        /// Builds a table keyed on the given names, each holding a script block of no consequence.
        /// </summary>
        /// <param name="keys">The keys the table should carry.</param>
        /// <returns>The table.</returns>
        private static IReadOnlyDictionary<string, ScriptBlock> Entries(params string[] keys)
        {
            Dictionary<string, ScriptBlock> entries = new(StringComparer.OrdinalIgnoreCase);
            foreach (string key in keys)
            {
                entries.Add(key, ScriptBlock.Create("@{}"));
            }
            return entries;
        }

        /// <summary>
        /// Builds the outer table from the two halves, so a case can state only the half it turns on.
        /// </summary>
        /// <param name="config">The config half.</param>
        /// <param name="strings">The strings half.</param>
        /// <returns>The outer table.</returns>
        private static Dictionary<string, IReadOnlyDictionary<string, ScriptBlock>> Pair(IReadOnlyDictionary<string, ScriptBlock> config, IReadOnlyDictionary<string, ScriptBlock> strings)
        {
            return new Dictionary<string, IReadOnlyDictionary<string, ScriptBlock>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Config", config },
                { "Strings", strings },
            };
        }
    }
}
