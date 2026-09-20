using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using PSADT.PowerShellTestFixture;
using PSAppDeployToolkit.Foundation;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Foundation
{
    /// <summary>
    /// Tests the store every other type in the assembly reads its state from.
    /// </summary>
    /// <remarks>
    /// Three behaviours matter here. One is what each reader does when nothing has been seated, because the toolkit can
    /// be asked to log or read configuration before the module has initialised and the message a caller gets is the only
    /// guidance they have. The second is that a session is found by position rather than by identity.
    /// <para>
    /// The third is that there are two configurations rather than one - the values the module ships and the values a
    /// deployment is running under - and a reader takes whichever it is named for. They are the same shape, so a reader
    /// taking the wrong one would only be noticed by a deployment that had overridden the setting in question, which is
    /// why every case here seats two that differ.
    /// </para>
    /// </remarks>
    /// <param name="powerShell">The hosted engine, shared across the collection.</param>
    [Collection(PowerShellCollection.Name)]
    public sealed class ModuleDatabaseTests(PowerShellFixture powerShell)
    {
        /// <summary>
        /// Verifies that the readers needing a whole database say so in terms of loading the module.
        /// </summary>
        /// <remarks>
        /// These two are asked for by code that only runs inside PowerShell, so the useful advice is that the assembly
        /// was loaded some other way - not that a command was missed.
        /// </remarks>
        [Fact]
        public void GetSessionState_SaysTheAssemblyWasNotLoadedByTheModule()
        {
            Assert.Contains(
                "only supports loading via the PSAppDeployToolkit PowerShell module",
                Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetImportDuration()).Message,
                StringComparison.Ordinal);
            Assert.Contains(
                "only supports loading via the PSAppDeployToolkit PowerShell module",
                Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetSessionState()).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the readers needing initialised state name the command that provides it.
        /// </summary>
        /// <remarks>
        /// A different message from the one above, and deliberately so: the assembly loaded correctly but
        /// <c language="powershell">Initialize-ADTModule</c> has not run, which is something the caller can act on.
        /// </remarks>
        [Fact]
        public void GetEnvironment_NamesTheCommandThatInitialisesTheModule()
        {
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabaseWithoutState();
            foreach (Func<object> reader in new Func<object>[] { ModuleDatabase.GetEnvironment, ModuleDatabase.GetConfig, ModuleDatabase.GetStringTable })
            {
                Assert.Contains(
                    "[Initialize-ADTModule] is called",
                    Assert.Throws<InvalidOperationException>(reader).Message,
                    StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Verifies that a database with nothing in it is not mistaken for an initialised one.
        /// </summary>
        [Fact]
        public void IsInitialized_IsFalseUntilStateIsSeated()
        {
            // Three states rather than two: no database is the assembly loaded outside the module and is refused
            // rather than answered, a database without state is the module imported and nothing more.
            static void ReadIsInitialized()
            {
                _ = ModuleDatabase.IsInitialized();
            }
            _ = Assert.Throws<InvalidOperationException>(ReadIsInitialized);
            using (powerShell.SeatModuleDatabaseWithoutState())
            {
                Assert.False(ModuleDatabase.IsInitialized());
            }
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            Assert.True(ModuleDatabase.IsInitialized());
        }

        /// <summary>
        /// Verifies that a seated database is handed back with what was put in it.
        /// </summary>
        [Fact]
        public void GetConfig_HandsBackWhatWasSeated()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration { LogStyle = "Legacy" });

            // Act
            IDictionary toolkit = Assert.IsType<IDictionary>(ModuleDatabase.GetConfig()["Toolkit"], exactMatch: false);

            // Assert
            Assert.Equal("Legacy", toolkit["LogStyle"]);
            Assert.NotNull(ModuleDatabase.GetStringTable());
            Assert.Same(powerShell.ModuleSessionState, ModuleDatabase.GetSessionState());
        }

        /// <summary>
        /// Verifies that a value is reached by naming its section and its key, whatever type it was stored as.
        /// </summary>
        /// <remarks>
        /// The three types the configuration actually holds. Each is stored as itself rather than as a string, and
        /// this hands them back by cast rather than by conversion, so a setting written as text where a number was
        /// meant fails here rather than arriving as a zero.
        /// </remarks>
        [Fact]
        public void GetConfigValue_HandsBackTheValueAtTheSectionAndKey()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration { LogStyle = "Legacy", LogMaxSize = 25, LogAppend = true, DeferExitCode = 60012 });

            // Assert
            Assert.Equal("Legacy", ModuleDatabase.GetConfigValue<string>("Toolkit", "LogStyle"));
            Assert.Equal(25, ModuleDatabase.GetConfigValue<int>("Toolkit", "LogMaxSize"));
            Assert.True(ModuleDatabase.GetConfigValue<bool>("Toolkit", "LogAppend"));

            // Assert: the section is named rather than searched for, so the same key in the other one is a different value.
            Assert.Equal(60012, ModuleDatabase.GetConfigValue<int>("UI", "DeferExitCode"));
        }

        /// <summary>
        /// Verifies that neither the section nor the key minds how it is cased.
        /// </summary>
        /// <remarks>
        /// Inherited from the tables rather than done here, which is the point: the configuration is a PowerShell
        /// hashtable and every caller writes its section and key as literals, so the day one of them is typed in the
        /// wrong case should be uneventful.
        /// </remarks>
        [Fact]
        public void GetConfigValue_DoesNotMindHowTheSectionOrKeyIsCased()
        {
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration { LogStyle = "Legacy" });
            Assert.Equal("Legacy", ModuleDatabase.GetConfigValue<string>("toolkit", "logstyle"));
        }

        /// <summary>
        /// Verifies that a section that is not there is named rather than dereferenced.
        /// </summary>
        /// <remarks>
        /// The key is asserted absent from the message as well as the section being present in it. Without that the
        /// case passes against an implementation that never looked at the section at all and failed one step later on
        /// the key, since the message for that names the section too.
        /// </remarks>
        [Fact]
        public void GetConfigValue_NamesTheSectionWhenThereIsNoSuchSection()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());

            // Act
            string message = Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetConfigValue<string>("Assets", "Banner")).Message;

            // Assert
            Assert.Contains("'Assets' is not available", message, StringComparison.Ordinal);
            Assert.DoesNotContain("'Banner'", message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a section holding something other than a table is refused like one that is absent.
        /// </summary>
        /// <remarks>
        /// Reachable through a hand-edited <c language="text">config.psd1</c>, where a section written without its
        /// braces is a string rather than a table. Both are the same mistake as far as a caller is concerned.
        /// </remarks>
        [Fact]
        public void GetConfigValue_RefusesASectionThatIsNotATable()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            ModuleDatabase.GetConfig()["Toolkit"] = "not a table";

            // Act
            string message = Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetConfigValue<string>("Toolkit", "LogStyle")).Message;

            // Assert
            Assert.Contains("'Toolkit' is not available", message, StringComparison.Ordinal);
            Assert.DoesNotContain("'LogStyle'", message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a value of the wrong type names the key, the section and the type that was asked for.
        /// </summary>
        /// <remarks>
        /// The failure a caller is most likely to reach, since the types are only agreed by convention between a
        /// literal in a script and a literal at the call site. Naming all three is what makes it actionable.
        /// </remarks>
        [Fact]
        public void GetConfigValue_NamesTheKeyAndTypeWhenTheValueIsNotWhatWasAskedFor()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration { LogMaxSize = 25 });

            // Act
            string message = Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetConfigValue<string>("Toolkit", "LogMaxSize")).Message;

            // Assert
            Assert.Contains("'LogMaxSize'", message, StringComparison.Ordinal);
            Assert.Contains("'Toolkit'", message, StringComparison.Ordinal);
            Assert.Contains(nameof(String), message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a key that is not there is refused, whatever type was asked for.
        /// </summary>
        /// <remarks>
        /// A setting a deployment is meant to be running under is required, whatever type it is, so a configuration
        /// that lost one is a failure rather than a deployment carrying on with a guess - a missing exit code handed
        /// back as a zero would be a failed deployment reporting success, and a missing log path an empty string.
        /// <para>
        /// A key holding nothing is the same as a key that was never there, which is what the shipped configuration
        /// actually looks like: it names each optional setting and gives it <c language="powershell">$null</c>.
        /// </para>
        /// </remarks>
        [Fact]
        public void GetConfigValue_RefusesAnAbsentKeyWhateverTypeWasAskedFor()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());

            // Assert: absent outright. The reference type is the case that used to be answered with a null.
            _ = Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetConfigValue<int>("Toolkit", "NotASetting"));
            _ = Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetConfigValue<string>("Toolkit", "NotASetting"));

            // Assert: present, holding nothing.
            IDictionary toolkit = Assert.IsType<IDictionary>(ModuleDatabase.GetConfig()["Toolkit"], exactMatch: false);
            toolkit["LogMaxSize"] = null;
            toolkit["LogPath"] = null;
            _ = Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetConfigValue<int>("Toolkit", "LogMaxSize"));
            _ = Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetConfigValue<string>("Toolkit", "LogPath"));
        }

        /// <summary>
        /// Verifies that a setting the configuration is allowed to omit reports itself as omitted rather than failing.
        /// </summary>
        /// <remarks>
        /// How an optional setting is read, and a separate method rather than a nullable type argument because a
        /// nullable annotation on a type argument is gone by the time either of them runs. Asking the required reader
        /// for a <c language="csharp">string?</c> is refused by the compiler instead, which is the only place that
        /// distinction can be made at all.
        /// <para>
        /// Absent and present-but-empty are the same answer, which is what lets a configuration drop a setting it has
        /// no opinion about - and lets the shipped one name the setting and leave it null, as it does.
        /// </para>
        /// </remarks>
        [Fact]
        public void TryGetConfigValue_ReportsASettingTheConfigurationOmitted()
        {
            // Arrange: the fixture leaves the key out entirely when there is no override, as a configuration may.
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());

            // Assert: absent outright.
            Assert.False(ModuleDatabase.TryGetConfigValue("UI", "LanguageOverride", out string? absent));
            Assert.Null(absent);

            // Assert: present, holding nothing.
            IDictionary ui = Assert.IsType<IDictionary>(ModuleDatabase.GetConfig()["UI"], exactMatch: false);
            ui["LanguageOverride"] = null;
            Assert.False(ModuleDatabase.TryGetConfigValue("UI", "LanguageOverride", out string? empty));
            Assert.Null(empty);

            // Assert: present, so it is handed back like any other.
            ui["LanguageOverride"] = "en-AU";
            Assert.True(ModuleDatabase.TryGetConfigValue("UI", "LanguageOverride", out string? supplied));
            Assert.Equal("en-AU", supplied);
        }

        /// <summary>
        /// Verifies that a setting reported as omitted is told apart from one that was left out for a value type too.
        /// </summary>
        /// <remarks>
        /// A value type cannot say "nothing" in its own right, which is what made the earlier reader wrong: it handed
        /// back the type's default and a caller could not tell a configured zero from a missing setting. The answer is
        /// carried by the return value here rather than by the value, so both types report it the same way.
        /// </remarks>
        [Fact]
        public void TryGetConfigValue_ReportsAnOmissionForAValueTypeAsWell()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration { LogMaxSize = 0 });

            // Assert: a configured zero is a value, not an omission. The type argument is inferred here and named
            // explicitly for a string, since inferring one from an `out string?` would give a type the constraint
            // refuses.
            Assert.True(ModuleDatabase.TryGetConfigValue("Toolkit", "LogMaxSize", out int configured));
            Assert.Equal(0, configured);

            // Assert: and a setting that is not there reports itself as absent rather than as that same zero.
            Assert.False(ModuleDatabase.TryGetConfigValue("Toolkit", "NotASetting", out int missing));
            Assert.Equal(0, missing);
        }

        /// <summary>
        /// Verifies that a setting of another type is reported as no value rather than raised as a fault.
        /// </summary>
        /// <remarks>
        /// What the method's name promises: it asks whether the configuration holds a value of the type wanted, and a
        /// value of some other type is an answer of no rather than an error. The cost is that a setting a
        /// configuration left out and a setting it got wrong are indistinguishable to a caller reading the bool, so a
        /// caller that needs the difference named asks <c language="csharp">GetConfigValue</c> instead.
        /// </remarks>
        [Fact]
        public void TryGetConfigValue_ReportsASettingOfAnotherTypeAsNoValue()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            Assert.IsType<IDictionary>(ModuleDatabase.GetConfig()["UI"], exactMatch: false)["LanguageOverride"] = 3081;

            // Assert
            Assert.False(ModuleDatabase.TryGetConfigValue("UI", "LanguageOverride", out string? languageOverride));
            Assert.Null(languageOverride);

            // Assert: the same value read as the type it actually is, so what was shown above is the type asked for
            // deciding the answer rather than the setting being unreadable.
            Assert.True(ModuleDatabase.TryGetConfigValue("UI", "LanguageOverride", out int identifier));
            Assert.Equal(3081, identifier);
        }

        /// <summary>
        /// Verifies that a missing section is refused rather than reported as no value.
        /// </summary>
        /// <remarks>
        /// The one thing this does raise, and the line it draws: a section is part of the configuration's shape rather
        /// than its data, so one that is gone means the file is malformed rather than that every setting in it was
        /// declined. A caller cannot act on the latter reading, and would carry on with defaults for a whole section
        /// nobody wrote.
        /// </remarks>
        [Fact]
        public void TryGetConfigValue_RefusesAMissingSection()
        {
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            Assert.Contains(
                "'Assets' is not available",
                Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.TryGetConfigValue("Assets", "Banner", out string? _)).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that reading one value refuses an uninitialised module the same way reading the whole table does.
        /// </summary>
        /// <remarks>
        /// It reads through <c language="csharp">GetConfig</c> rather than around it, so the advice a caller gets
        /// before <c language="powershell">Initialize-ADTModule</c> has run is the same whichever of the two it asked.
        /// </remarks>
        [Fact]
        public void GetConfigValue_NamesTheCommandThatInitialisesTheModule()
        {
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabaseWithoutState();
            Assert.Contains(
                "[Initialize-ADTModule] is called",
                Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetConfigValue<string>("Toolkit", "LogStyle")).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the shipped configuration is readable from a database that has no state at all.
        /// </summary>
        /// <remarks>
        /// The whole reason the shipped defaults are reachable separately from the seated ones. A log entry can be
        /// written between the module being imported and <c language="powershell">Initialize-ADTModule</c> running,
        /// and before this existed the writer had no configuration to consult and guessed instead.
        /// </remarks>
        [Fact]
        public void GetDefaultConfig_AnswersFromADatabaseThatHasNoState()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabaseWithoutState(new ModuleConfiguration { LogStyle = "Legacy" });

            // Assert: the seated readers refuse, and the shipped ones answer.
            _ = Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetConfig());
            Assert.Equal("Legacy", Assert.IsType<IDictionary>(ModuleDatabase.GetDefaultConfig()["Toolkit"], exactMatch: false)["LogStyle"]);
            Assert.Equal(ModuleDatabaseScope.DefaultStringValues[string.Empty], ModuleDatabase.GetDefaultStringTable()[ModuleDatabaseScope.DefaultStringKey]);
        }

        /// <summary>
        /// Verifies that the shipped configuration is what is read even once a different one has been seated.
        /// </summary>
        /// <remarks>
        /// Two tables that differ, so the assertion cannot pass by reading either. It matters because the two are the
        /// same shape and a reader taking the wrong one would only be noticed by a deployment whose configuration
        /// overrode the setting in question.
        /// </remarks>
        [Fact]
        public void GetDefaultConfig_ReadsTheShippedValuesRatherThanTheSeatedOnes()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(
                new ModuleConfiguration { LogStyle = "Legacy" },
                environment: null,
                defaults: new ModuleConfiguration { LogStyle = "CMTrace" });

            // Assert
            Assert.Equal("Legacy", ModuleDatabase.GetConfigValue<string>("Toolkit", "LogStyle"));
            Assert.Equal("CMTrace", Assert.IsType<IDictionary>(ModuleDatabase.GetDefaultConfig()["Toolkit"], exactMatch: false)["LogStyle"]);
        }

        /// <summary>
        /// Verifies that the shipped strings are read for the locale asked for, and neutrally when none is.
        /// </summary>
        [Fact]
        public void GetDefaultStringTable_ReadsTheTableForTheLocaleAskedFor()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabaseWithoutState();

            // Assert
            Assert.Equal(
                ModuleDatabaseScope.DefaultStringValues[ModuleDatabaseScope.DefaultStringsLocale],
                ModuleDatabase.GetDefaultStringTable(CultureInfo.GetCultureInfo(ModuleDatabaseScope.DefaultStringsLocale))[ModuleDatabaseScope.DefaultStringKey]);
            Assert.Equal(
                ModuleDatabaseScope.DefaultStringValues[string.Empty],
                ModuleDatabase.GetDefaultStringTable(locale: null)[ModuleDatabaseScope.DefaultStringKey]);
        }

        /// <summary>
        /// Verifies that the shipped readers say the assembly was not loaded by the module when no database exists.
        /// </summary>
        /// <remarks>
        /// A database with no state is the module imported, and these answer from it. No database at all is the
        /// assembly loaded some other way, and there are no shipped defaults to answer from either.
        /// </remarks>
        [Fact]
        public void GetDefaultConfig_SaysTheAssemblyWasNotLoadedByTheModule()
        {
            Assert.Contains(
                "only supports loading via the PSAppDeployToolkit PowerShell module",
                Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetDefaultConfig()).Message,
                StringComparison.Ordinal);
            Assert.Contains(
                "only supports loading via the PSAppDeployToolkit PowerShell module",
                Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetDefaultStringTable()).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the readers answering from a seated state each hand back their own part of it.
        /// </summary>
        /// <remarks>
        /// Covered together because each is a one-line read of the same object, so what is worth pinning is that
        /// every one of them reaches the part it is named for rather than a neighbouring one.
        /// </remarks>
        [Fact]
        public void GetDirectories_AndItsSiblingsEachReadTheirOwnPartOfTheState()
        {
            // Arrange
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration { LogPath = temp.GetPath("Logs") });

            // Assert
            Assert.Equal(temp.GetPath("Logs"), Assert.Single(ModuleDatabase.GetDirectories().Script).FullName);
            Assert.NotNull(ModuleDatabase.GetLanguage());
            Assert.NotNull(ModuleDatabase.GetStringTable());
            Assert.InRange(ModuleDatabase.GetInitDuration(), TimeSpan.Zero, TimeSpan.FromMinutes(5));
            Assert.InRange(ModuleDatabase.GetImportDuration(), TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Verifies that the last exit code is written onto the seated state.
        /// </summary>
        /// <remarks>
        /// It starts absent rather than zero, because nothing has exited yet. A zero would read as a deployment that
        /// succeeded, which is how a failure path came to report success before this was made nullable.
        /// </remarks>
        [Fact]
        public void SetLastExitCode_WritesOntoTheSeatedState()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            Assert.Null(database.Database.State!.LastExitCode);

            // Act
            ModuleDatabase.SetLastExitCode(60008);

            // Assert
            Assert.Equal(60008, database.Database.State.LastExitCode);
        }

        /// <summary>
        /// Verifies that the database carries what it was built with, whether or not it has been initialized.
        /// </summary>
        /// <remarks>
        /// These are the parts a module knows at import rather than at initialization, so they answer from a database
        /// holding no state at all. <c language="csharp">Signed</c> is derived rather than stored, and the fixture's
        /// signature is deliberately not a valid one.
        /// </remarks>
        [Fact]
        public void Constructor_CarriesWhatTheModuleKnowsAtImport()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabaseWithoutState();

            // Assert
            Assert.NotEmpty(database.Database.Manifest);
            Assert.NotNull(database.Database.ModuleInfo);
            Assert.NotEmpty(database.Database.Assemblies);
            Assert.NotNull(database.Database.Signature);
            Assert.False(database.Database.Signed);
            Assert.False(database.Database.Compiled);
            Assert.NotNull(database.Database.Defaults.Config);
            Assert.NotNull(database.Database.Defaults.Strings);
            Assert.NotNull(database.Database.Callbacks);
        }

        /// <summary>
        /// Verifies that a database cannot be built without the parts every reader assumes are there.
        /// </summary>
        /// <remarks>
        /// The manifest and assemblies are checked for emptiness as well as absence, since an empty one is what a
        /// failed import leaves behind and would otherwise be seated and fail later somewhere unrelated.
        /// </remarks>
        [Fact]
        public void Constructor_RefusesADatabaseMissingWhatItMustCarry()
        {
            // Arrange
            using ModuleDatabaseScope seated = powerShell.SeatModuleDatabaseWithoutState();
            ModuleDatabase built = seated.Database;
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, ScriptBlock>> defaults = new Dictionary<string, IReadOnlyDictionary<string, ScriptBlock>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Config", new Dictionary<string, ScriptBlock>(StringComparer.OrdinalIgnoreCase) { { string.Empty, ScriptBlock.Create("@{}") } } },
                { "Strings", new Dictionary<string, ScriptBlock>(StringComparer.OrdinalIgnoreCase) { { string.Empty, ScriptBlock.Create("@{}") } } },
            };
            ReadOnlyCollection<FileInfo> assemblies = new([new(typeof(ModuleDatabase).Assembly.Location)]);

            // Assert
            _ = Assert.Throws<ArgumentNullException>(() => new ModuleDatabase(defaults, manifest: null!, built.ModuleInfo, assemblies, built.Signature, compiled: false));
            _ = Assert.Throws<ArgumentNullException>(() => new ModuleDatabase(defaults, new Hashtable(), built.ModuleInfo, assemblies, built.Signature, compiled: false));
            _ = Assert.Throws<ArgumentNullException>(() => new ModuleDatabase(defaults, built.Manifest, built.ModuleInfo, assemblies: null!, built.Signature, compiled: false));
            _ = Assert.Throws<ArgumentNullException>(() => new ModuleDatabase(defaults, built.Manifest, built.ModuleInfo, new ReadOnlyCollection<FileInfo>([]), built.Signature, compiled: false));
            _ = Assert.Throws<ArgumentNullException>(() => new ModuleDatabase(defaults, built.Manifest, moduleInfo: null!, assemblies, built.Signature, compiled: false));
            _ = Assert.Throws<ArgumentNullException>(() => new ModuleDatabase(defaults, built.Manifest, built.ModuleInfo, assemblies, signature: null!, compiled: false));
        }

        /// <summary>
        /// Verifies that the environment table is handed back when one was seated.
        /// </summary>
        [Fact]
        public void GetEnvironment_HandsBackTheTableThatWasSeated()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable environment = powerShell.NewEnvironmentTable();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration(), environment);

            // Assert
            Assert.Same(environment, ModuleDatabase.GetEnvironment());
        }

        /// <summary>
        /// Verifies that a seated database with no environment table still reports the initialisation message.
        /// </summary>
        /// <remarks>
        /// The state a caller reaches by loading the module and not initialising it, which is exactly what the message
        /// is for.
        /// </remarks>
        [Fact]
        public void GetEnvironment_StillRefusesWhenNoStateWasSeated()
        {
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabaseWithoutState();
            Assert.Contains(
                "[Initialize-ADTModule] is called",
                Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetEnvironment()).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that no session is reported as active until one is opened.
        /// </summary>
        /// <remarks>
        /// The check the client/server log reader makes on every frame it reads, so both the no-database and the
        /// empty-list cases are reached in normal running - before a session opens and after the last one closes.
        /// </remarks>
        [Fact]
        public void IsDeploymentSessionActive_IsFalseBeforeAnySessionOpens()
        {
            using (powerShell.SeatModuleDatabaseWithoutState())
            {
                Assert.False(ModuleDatabase.IsDeploymentSessionActive());
            }
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            Assert.False(ModuleDatabase.IsDeploymentSessionActive());
        }

        /// <summary>
        /// Verifies that asking for a session when none is open names the command that opens one.
        /// </summary>
        [Fact]
        public void GetDeploymentSession_NamesTheCommandThatOpensASession()
        {
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            Assert.Contains(
                "[Open-ADTSession] is called",
                Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetDeploymentSession()).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the most recently opened session is the one handed back.
        /// </summary>
        /// <remarks>
        /// Sessions nest: a deployment script may open one and call something that opens another, and the innermost is
        /// the one a log entry belongs to. Position in the list is what decides, so this is the behaviour every caller
        /// of <c language="powershell">Get-ADTSession</c> depends on.
        /// </remarks>
        [Fact]
        public void GetDeploymentSession_HandsBackTheMostRecentlyOpened()
        {
            // Arrange
            using TempDirectory temp = new();
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable environment = powerShell.NewEnvironmentTable();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), environment);

            DeploymentSession first = NewSession("First App");
            DeploymentSession second = NewSession("Second App");
            database.Sessions.Add(first);
            database.Sessions.Add(second);

            // Assert
            Assert.True(ModuleDatabase.IsDeploymentSessionActive());
            Assert.Same(second, ModuleDatabase.GetDeploymentSession());

            // Act: closing the innermost leaves the outer one current, as Close-ADTSession does.
            _ = database.Sessions.Remove(second);

            // Assert
            Assert.Same(first, ModuleDatabase.GetDeploymentSession());
        }

        /// <summary>
        /// Verifies that a script is invoked against the module's session state.
        /// </summary>
        /// <remarks>
        /// The session state matters rather than merely the runspace: the script blocks the toolkit builds refer to
        /// <c language="powershell">$Script:CommandTable</c>, which only the module's own scope can resolve.
        /// </remarks>
        [Fact]
        public void InvokeScript_RunsAgainstTheModulesSessionState()
        {
            using IDisposable scope = powerShell.Enter();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());

            // Act
            ModuleDatabase.InvokeScript(ScriptBlock.Create("$Script:InvokeScriptRan = $args[0]"), "yes");

            // Assert
            Assert.Equal("yes", powerShell.ModuleSessionState.PSVariable.GetValue("InvokeScriptRan"));
        }

        /// <summary>
        /// Verifies that the script's own command table is reachable from an invoked script.
        /// </summary>
        /// <remarks>
        /// Directly the thing that failed first when this fixture was built: without a module session state carrying
        /// that variable, every log entry written under a runspace fails.
        /// </remarks>
        [Fact]
        public void InvokeScript_ReachesTheModulesCommandTable()
        {
            using IDisposable scope = powerShell.Enter();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());

            // Act
            IEnumerable<string> names = ModuleDatabase.InvokeScript<string>(ScriptBlock.Create("$Script:CommandTable.Keys"));

            // Assert
            Assert.Contains("Get-PSCallStack", names, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that the generic form unwraps each result to the type asked for.
        /// </summary>
        [Fact]
        public void InvokeScript_UnwrapsEachResultToTheTypeAskedFor()
        {
            using IDisposable scope = powerShell.Enter();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());

            Assert.Equal([1, 2, 3], ModuleDatabase.InvokeScript<int>(ScriptBlock.Create("1; 2; 3")));
            Assert.Equal(["one"], ModuleDatabase.InvokeScript<string>(ScriptBlock.Create("'one'")), StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that the generic form fails rather than skipping a result of the wrong type.
        /// </summary>
        /// <remarks>
        /// It casts rather than filters, so a script returning something unexpected is a fault at the call site rather
        /// than a quietly short result. Worth pinning: the alternative would hide a changed script.
        /// </remarks>
        [Fact]
        public void InvokeScript_FailsOnAResultOfTheWrongType()
        {
            using IDisposable scope = powerShell.Enter();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());

            _ = Assert.Throws<InvalidCastException>(static () => ModuleDatabase.InvokeScript<int>(ScriptBlock.Create("'not a number'")).ToList());
        }

        /// <summary>
        /// Verifies that seating a database is refused from outside the module.
        /// </summary>
        /// <remarks>
        /// The guard reads the call stack for a frame belonging to <c language="text">PSAppDeployToolkit.psm1</c> inside a module of
        /// that name, so nothing a test can do will satisfy it - which is the point, and is why these tests seat the
        /// field directly instead.
        /// <para>
        /// It runs before the argument checks, so a caller outside the module is told where it is calling from rather
        /// than what it passed. Asserted with a valid argument as well as with nothing, so the ordering is what is
        /// being shown rather than a coincidence.
        /// </para>
        /// </remarks>
        [Fact]
        public void Init_IsRefusedFromOutsideTheModule()
        {
            using IDisposable scope = powerShell.Enter();

            // Assert: a valid session state still gets the guard's message rather than a complaint about the
            // null database beside it, which is the ordering being shown.
            Assert.Contains(
                "can only be initialized from within the PSAppDeployToolkit module",
                Assert.Throws<InvalidOperationException>(() => ModuleDatabase.Init(powerShell.ModuleSessionState, null!, DateTime.Now)).Message,
                StringComparison.Ordinal);
            _ = Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.Init(null!, null!, default));
        }

        /// <summary>
        /// Verifies that clearing the database is refused from outside the module.
        /// </summary>
        [Fact]
        public void Clear_IsRefusedFromOutsideTheModule()
        {
            using IDisposable scope = powerShell.Enter();

            Assert.Contains(
                "can only be cleared from within the PSAppDeployToolkit module",
                Assert.Throws<InvalidOperationException>(ModuleDatabase.Clear).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Builds a session against the seated database.
        /// </summary>
        /// <param name="appName">The application name, which is what tells two sessions apart in a log.</param>
        /// <returns>The session.</returns>
        private static DeploymentSession NewSession(string appName)
        {
            return new DeploymentSession(
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "AppName", appName },
                    { "AppVersion", "1.0.0" },
                    { "DeployMode", DeployMode.Silent },
                },
                noExitOnClose: true,
                compatibilityMode: false);
        }

        /// <summary>
        /// A configuration writing to a scratch directory and reading deferral history from a key that does not exist.
        /// </summary>
        /// <param name="temp">The scratch directory to log into.</param>
        /// <returns>The configuration.</returns>
        private static ModuleConfiguration Configuration(TempDirectory temp)
        {
            return new ModuleConfiguration
            {
                LogPath = temp.GetPath("Logs"),
                RegPath = @"HKCU:\SOFTWARE\PSAppDeployToolkit.Tests",
            };
        }
    }
}
