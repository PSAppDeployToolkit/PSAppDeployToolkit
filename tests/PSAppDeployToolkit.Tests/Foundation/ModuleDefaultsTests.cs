using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using PSAppDeployToolkit.Foundation;
using Xunit;

namespace PSAppDeployToolkit.Tests.Foundation
{
    /// <summary>
    /// Tests the holder for the config and string tables the module ships with.
    /// </summary>
    /// <remarks>
    /// Most of this type is its constructor, and the constructor is almost all validation. That matters more than it
    /// looks: the dictionaries are built by a several-thousand-line literal in <c language="text">ImportsLast.ps1</c>,
    /// so a key lost or misspelled there is a mistake nothing else would catch until a deployment asked for a default
    /// that had quietly stopped existing.
    /// <para>
    /// The rest is the two readers that turn a shipped script block into a table. They walk its syntax tree rather
    /// than running it, which is the only reason the shipped values are readable before the module is initialised and
    /// there is a runspace to run anything in - so what is tested is both that the walk finds the table and that it
    /// never evaluates one.
    /// </para>
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
        /// Verifies that the shipped configuration is read out of its script block's syntax tree.
        /// </summary>
        /// <remarks>
        /// Every value keeps the type it was written as rather than arriving as a string, which matters because both
        /// readers of this table cast rather than convert - an <see cref="int"/> setting read back as a string fails
        /// at the cast, in a caller nowhere near the table.
        /// </remarks>
        [Fact]
        public void GetDefaultConfig_ReadsTheTableOutOfItsScriptBlock()
        {
            // Act
            IDictionary toolkit = Assert.IsType<IDictionary>(Defaults(ShippedConfig).GetDefaultConfig()["Toolkit"], exactMatch: false);

            // Assert
            Assert.Equal("Legacy", toolkit["LogStyle"]);
            Assert.Equal(20, toolkit["LogMaxSize"]);
            Assert.True(Assert.IsType<bool>(toolkit["LogAppend"]));

            // Assert: a setting shipped as $null is carried as a key holding nothing, rather than left out.
            Assert.True(toolkit.Contains("LogPathNoAdminRights"));
            Assert.Null(toolkit["LogPathNoAdminRights"]);
        }

        /// <summary>
        /// Verifies that the table handed back does not mind how its keys are cased.
        /// </summary>
        /// <remarks>
        /// The configuration a running module holds came from PowerShell, so it never minded. This one is built from
        /// a syntax tree instead, and everything reading it reads both - so if the two disagreed, a setting would be
        /// found before initialisation and lost after it, or the reverse.
        /// </remarks>
        [Fact]
        public void GetDefaultConfig_HandsBackATableThatDoesNotMindHowKeysAreCased()
        {
            IDictionary config = Defaults(ShippedConfig).GetDefaultConfig();
            Assert.Equal("Legacy", Assert.IsType<IDictionary>(config["toolkit"], exactMatch: false)["logstyle"]);
        }

        /// <summary>
        /// Verifies that the table is read rather than the script block run.
        /// </summary>
        /// <remarks>
        /// It is read through <c language="csharp">SafeGetValue</c>, which walks constants and refuses anything it
        /// would have to execute. That is the whole reason this is reachable before the module is initialised: there
        /// is no runspace to run a script block in yet, and a shipped default that needed one could not be a default.
        /// </remarks>
        [Fact]
        public void GetDefaultConfig_ReadsTheTableRatherThanRunningTheScriptBlock()
        {
            Assert.Contains(
                "dynamic",
                Assert.Throws<InvalidOperationException>(static () => Defaults("@{ Toolkit = @{ LogStyle = (Get-Date) } }").GetDefaultConfig()).Message,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that a script block that is not a single table fails rather than being read as an empty one.
        /// </summary>
        /// <remarks>
        /// The walk assumes the one shape the module ships - one statement, one pipeline element, one hashtable - and
        /// takes each step by position. Worth pinning because the failure lands nowhere near the cause: a script block
        /// with nothing in it indexes past the end of an empty statement list, which reads as a bug in whoever asked
        /// for the configuration rather than in whoever supplied it.
        /// </remarks>
        [Fact]
        public void GetDefaultConfig_FailsOnAScriptBlockThatIsNotASingleTable()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => Defaults(string.Empty).GetDefaultConfig());
            _ = Assert.Throws<InvalidCastException>(static () => Defaults("'not a table'").GetDefaultConfig());
            _ = Assert.Throws<InvalidCastException>(static () => Defaults("$table = @{ Toolkit = @{} }").GetDefaultConfig());
        }

        /// <summary>
        /// Verifies that the neutral table is the one read when no locale is named.
        /// </summary>
        /// <remarks>
        /// The invariant culture is named by the empty string, so asking for it and asking for nothing reach the same
        /// table - which is what makes the empty key the neutral one rather than merely a conventional place to put a
        /// fallback.
        /// </remarks>
        [Fact]
        public void GetDefaultStringTable_TakesTheNeutralTableWhenNoLocaleIsNamed()
        {
            ModuleDefaults defaults = Localised();
            Assert.Equal("neutral", defaults.GetDefaultStringTable()["Greeting"]);
            Assert.Equal("neutral", defaults.GetDefaultStringTable(CultureInfo.InvariantCulture)["Greeting"]);
        }

        /// <summary>
        /// Verifies that a named locale reads the table belonging to it.
        /// </summary>
        [Fact]
        public void GetDefaultStringTable_TakesTheTableBelongingToTheLocaleNamed()
        {
            Assert.Equal("australian", Localised().GetDefaultStringTable(CultureInfo.GetCultureInfo("en-AU"))["Greeting"]);
        }

        /// <summary>
        /// Verifies that a locale shipping no table of its own fails rather than falling back to the neutral one.
        /// </summary>
        /// <remarks>
        /// Deliberate, but only safe because the fallback is somebody else's job: <c language="powershell">Import-ADTModuleDataFile</c>
        /// asks whether the table carries the culture before asking for it. A caller that forgot to would get this
        /// rather than the neutral strings, so the guard belongs to the caller and cannot be dropped.
        /// </remarks>
        [Fact]
        public void GetDefaultStringTable_FailsForALocaleThatShipsNoTableOfItsOwn()
        {
            _ = Assert.Throws<KeyNotFoundException>(static () => Localised().GetDefaultStringTable(CultureInfo.GetCultureInfo("fr-FR")));
        }

        /// <summary>
        /// Builds defaults carrying the given configuration, with strings of no consequence beside it.
        /// </summary>
        /// <param name="configSource">The source of the script block the configuration is shipped in.</param>
        /// <returns>The defaults.</returns>
        private static ModuleDefaults Defaults(string configSource)
        {
            return new ModuleDefaults(Pair(
                new Dictionary<string, ScriptBlock>(StringComparer.OrdinalIgnoreCase) { { string.Empty, ScriptBlock.Create(configSource) } },
                Entries(string.Empty)));
        }

        /// <summary>
        /// Builds defaults whose strings ship a neutral table and one locale's, each saying which it is.
        /// </summary>
        /// <returns>The defaults.</returns>
        private static ModuleDefaults Localised()
        {
            return new ModuleDefaults(Pair(
                Entries(string.Empty),
                new Dictionary<string, ScriptBlock>(StringComparer.OrdinalIgnoreCase)
                {
                    { string.Empty, ScriptBlock.Create("@{ Greeting = 'neutral' }") },
                    { "en-AU", ScriptBlock.Create("@{ Greeting = 'australian' }") },
                }));
        }

        /// <summary>
        /// A shipped configuration carrying one setting of each kind the module ships.
        /// </summary>
        private const string ShippedConfig = "@{ Toolkit = @{ LogStyle = 'Legacy'; LogMaxSize = 20; LogAppend = $true; LogPathNoAdminRights = $null } }";

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
