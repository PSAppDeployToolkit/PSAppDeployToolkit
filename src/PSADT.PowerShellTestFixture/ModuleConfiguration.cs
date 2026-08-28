using System;
using System.Collections;

namespace PSADT.PowerShellTestFixture
{
    /// <summary>
    /// The subset of the module's configuration that the types under test read, with the defaults
    /// <c>config.psd1</c> ships so a test only names what it is varying.
    /// </summary>
    /// <remarks>
    /// Mutable on purpose: a test sets the two or three settings its case turns on and leaves the rest. The shape has
    /// to match what the real configuration hands over, since the code casts these values rather than converting them
    /// - an <see cref="int"/> stored where a <see cref="string"/> is expected fails at the cast, not at the read.
    /// </remarks>
    public sealed class ModuleConfiguration
    {
        /// <summary>
        /// Where logs are written. A test should point this at a scratch directory.
        /// </summary>
        public string LogPath { get; set; } = string.Empty;

        /// <summary>
        /// The log format, named as the configuration names it rather than as the enumeration.
        /// </summary>
        public string LogStyle { get; set; } = "CMTrace";

        /// <summary>
        /// How many previous logs to keep.
        /// </summary>
        public int LogMaxHistory { get; set; } = 10;

        /// <summary>
        /// The size in megabytes at which a log is rotated.
        /// </summary>
        public int LogMaxSize { get; set; } = 10;

        /// <summary>
        /// Whether to append to an existing log rather than rotate it.
        /// </summary>
        public bool LogAppend { get; set; } = true;

        /// <summary>
        /// Whether debug messages are written at all.
        /// </summary>
        public bool LogDebugMessage { get; set; }

        /// <summary>
        /// Whether logs go into a vendor, name and version hierarchy.
        /// </summary>
        public bool LogToHierarchy { get; set; }

        /// <summary>
        /// How many hierarchy levels to keep.
        /// </summary>
        public int LogMaxHierarchy { get; set; } = 3;

        /// <summary>
        /// Whether logs go into a subfolder named for the install.
        /// </summary>
        public bool LogToSubfolder { get; set; }

        /// <summary>
        /// Whether logs are compressed on session closure.
        /// </summary>
        public bool CompressLogs { get; set; }

        /// <summary>
        /// Whether log entries are echoed to the host.
        /// </summary>
        public bool LogWriteToHost { get; set; }

        /// <summary>
        /// Whether host output bypasses PowerShell and goes straight to the standard streams.
        /// </summary>
        public bool LogHostOutputToStdStreams { get; set; }

        /// <summary>
        /// The registry path deferral history is kept under. A test should point this somewhere that does not exist.
        /// </summary>
        public string RegPath { get; set; } = @"HKCU:\SOFTWARE";

        /// <summary>
        /// The exit code used when a deployment fails without one of its own.
        /// </summary>
        public int DefaultExitCode { get; set; } = 60001;

        /// <summary>
        /// The exit code used when a user defers.
        /// </summary>
        public int DeferExitCode { get; set; } = 60012;

        /// <summary>
        /// The language the configuration forces, or <see langword="null"/> to follow the machine.
        /// </summary>
        public string? LanguageOverride { get; set; }

        /// <summary>
        /// Renders this as the nested hashtable the module database holds.
        /// </summary>
        /// <returns>The configuration, shaped as the module builds it.</returns>
        public Hashtable ToHashtable()
        {
            Hashtable toolkit = new(StringComparer.OrdinalIgnoreCase)
            {
                { nameof(LogPath), LogPath },
                { nameof(LogStyle), LogStyle },
                { nameof(LogMaxHistory), LogMaxHistory },
                { nameof(LogMaxSize), LogMaxSize },
                { nameof(LogAppend), LogAppend },
                { nameof(LogDebugMessage), LogDebugMessage },
                { nameof(LogToHierarchy), LogToHierarchy },
                { nameof(LogMaxHierarchy), LogMaxHierarchy },
                { nameof(LogToSubfolder), LogToSubfolder },
                { nameof(CompressLogs), CompressLogs },
                { nameof(LogWriteToHost), LogWriteToHost },
                { nameof(LogHostOutputToStdStreams), LogHostOutputToStdStreams },
                { nameof(RegPath), RegPath },
            };
            Hashtable ui = new(StringComparer.OrdinalIgnoreCase)
            {
                { nameof(DefaultExitCode), DefaultExitCode },
                { nameof(DeferExitCode), DeferExitCode },
            };
            if (LanguageOverride is not null)
            {
                ui.Add(nameof(LanguageOverride), LanguageOverride);
            }
            return new Hashtable(StringComparer.OrdinalIgnoreCase) { { "Toolkit", toolkit }, { "UI", ui } };
        }
    }
}
