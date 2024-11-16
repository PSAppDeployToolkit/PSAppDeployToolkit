using System;
using System.IO;
using System.Globalization;
using System.Management.Automation;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace PSADT.Types
{
    public sealed class SessionObject
    {
        #region Constructors.


        /// <summary>
        /// Initializes a new instance of the <see cref="SessionObject"/> class.
        /// </summary>
        /// <param name="parameters">All parameters from Open-ADTSession.</param>
        public SessionObject(OrderedDictionary adtEnv, Hashtable adtConfig, Hashtable adtStrings, PSVariableIntrinsics callerVariables, Dictionary<string, object> parameters)
        {
            #region Init


            // Establish start date/time first so we can accurately mark the start of execution.
            CurrentDate = CurrentDateTime.ToString("dd-MM-yyyy");
            CurrentTime = CurrentDateTime.ToString("HH:mm:ss");

            // Establish initial variable values.
            ADTEnv = adtEnv;
            ADTConfig = adtConfig;
            ADTStrings = adtStrings;
            CallerVariables = callerVariables;

            // Establish the caller's variables.
            foreach (var p in this.GetType().GetProperties())
            {
                if (parameters.ContainsKey(p.Name))
                {
                    p.SetValue(this, parameters[p.Name]);
                }
            }

            // Ensure DeploymentType is title cased for aesthetics.
            DeploymentType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(DeploymentType.ToLower());

            // Establish script directories.
            if (null != ScriptDirectory)
            {
                if (string.IsNullOrWhiteSpace(DirFiles) && Directory.Exists(Path.Combine(ScriptDirectory, "Files")))
                {
                    DirFiles = Path.Combine(ScriptDirectory, "Files");
                }
                if (string.IsNullOrWhiteSpace(DirSupportFiles) && Directory.Exists(Path.Combine(ScriptDirectory, "SupportFiles")))
                {
                    DirSupportFiles = Path.Combine(ScriptDirectory, "SupportFiles");
                }
            }


            #endregion
        }


        #endregion
        #region Internal variables.


        /// <summary>
        /// Gets the environment table that was supplied during object instantiation.
        /// </summary>
        private OrderedDictionary ADTEnv { get; }

        /// <summary>
        /// Gets the config table that was supplied during object instantiation.
        /// </summary>
        private Hashtable ADTConfig { get; }

        /// <summary>
        /// Gets the string table that was supplied during object instantiation.
        /// </summary>
        private Hashtable ADTStrings { get; }

        /// <summary>
        /// Gets whether this session object is operating in compatibility mode.
        /// </summary>
        private bool CompatibilityMode { get; }

        /// <summary>
        /// Gets the caller's variables from the provided $ExecutionContext.
        /// </summary>
        private PSVariableIntrinsics CallerVariables { get; }

        /// <summary>
        /// Gets the mounted WIM files within this session.
        /// </summary>
        private readonly List<FileInfo> MountedWimFiles = [];

        /// <summary>
        /// Gets the log entries written with this session.
        /// </summary>
        private readonly List<LogEntry> LogBuffer = [];

        /// <summary>
        /// Gets the list of executables found within a Zero-Config MSI file.
        /// </summary>
        private ProcessObject[] DefaultMsiExecutablesList { get; }

        /// <summary>
        /// Gets whether this session object has finished processing the Zero-Config MSI file detection.
        /// </summary>
        private bool ZeroConfigInitiated { get; }

        /// <summary>
        /// Gets whether this session object was instantiated via a script or the command line.
        /// </summary>
        private bool RunspaceOrigin { get; }

        /// <summary>
        /// Gets whether this session object should force WIM file detection, even if AppName was defined.
        /// </summary>
        private bool ForceWimDetection { get; }

        /// <summary>
        /// Gets the drive letter used with subst during a Zero-Config WIM file mount operation.
        /// </summary>
        private string DirFilesSubstDrive { get; }

        /// <summary>
        /// Gets the registry path used for getting/setting deferral information.
        /// </summary>
        private string RegKeyDeferHistory { get; }

        /// <summary>
        /// Gets the deployment type name from the language string table for the given DeploymentType.
        /// </summary>
        private string DeploymentTypeName { get; }

        /// <summary>
        /// Gets whether this session object is in non-interactive mode.
        /// </summary>
        private bool DeployModeNonInteractive { get; }

        /// <summary>
        /// Gets whether this session object is in silent mode.
        /// </summary>
        private bool DeployModeSilent { get; }

        /// <summary>
        /// Gets the session object's filesystem log path.
        /// </summary>
        private string LogPath { get; }

        /// <summary>
        /// Gets the session object's closing exit code.
        /// </summary>
        private int ExitCode { get; set; }


        #endregion
        #region Frontend parameters.


        /// <summary>
        /// Gets the session object's deployment type.
        /// </summary>
        public string DeploymentType { get; } = "Install";

        /// <summary>
        /// Gets the session object's deployment mode.
        /// </summary>
        public string DeployMode { get; } = "Interactive";

        /// <summary>
        /// Gets whether this session object is allowed to exit with a reboot exit code.
        /// </summary>
        public bool AllowRebootPassThru { get; }

        /// <summary>
        /// Gets whether this session object should enable terminal services install mode.
        /// </summary>
        public bool TerminalServerMode { get; }

        /// <summary>
        /// Gets whether this session object should disable logging for the operation.
        /// </summary>
        public bool DisableLogging { get; }


        #endregion
        #region Frontend variables.


        /// <summary>
        /// Gets the session object's application vendor.
        /// </summary>
        public string AppVendor { get; }

        /// <summary>
        /// Gets the session object's application name.
        /// </summary>
        public string AppName { get; }

        /// <summary>
        /// Gets the session object's application version.
        /// </summary>
        public string AppVersion { get; }

        /// <summary>
        /// Gets the session object's application architecture.
        /// </summary>
        public string AppArch { get; }

        /// <summary>
        /// Gets the session object's application language.
        /// </summary>
        public string AppLang { get; }

        /// <summary>
        /// Gets the session object's application package revision.
        /// </summary>
        public string AppRevision { get; }

        /// <summary>
        /// Gets the session object's exit code(s) to indicate a successful deployment.
        /// </summary>
        public int[] AppSuccessExitCodes { get; } = [0];

        /// <summary>
        /// Gets the session object's exit code(s) to indicate a reboot is required.
        /// </summary>
        public int[] AppRebootExitCodes { get; } = [1641, 3010];

        /// <summary>
        /// Gets the session object's application package version.
        /// </summary>
        public Version AppScriptVersion { get; }

        /// <summary>
        /// Gets the session object's application package date.
        /// </summary>
        public string AppScriptDate { get; }

        /// <summary>
        /// Gets the session object's application package author.
        /// </summary>
        public string AppScriptAuthor { get; }

        /// <summary>
        /// Gets an override to the session object's installation name.
        /// </summary>
        public string InstallName { get; }

        /// <summary>
        /// Gets an override to the session object's installation title.
        /// </summary>
        public string InstallTitle { get; }

        /// <summary>
        /// Gets the session object's frontend script name.
        /// </summary>
        public string DeployAppScriptFriendlyName { get; }

        /// <summary>
        /// Gets the session object's frontend script version.
        /// </summary>
        public Version DeployAppScriptVersion { get; }

        /// <summary>
        /// Gets the session object's frontend script parameters.
        /// </summary>
        public IDictionary DeployAppScriptParameters { get; }

        /// <summary>
        /// Gets/sets the session object's installation phase'.
        /// </summary>
        public string InstallPhase { get; set; } = "Initialization";


        #endregion
        #region Other public variables.


        /// <summary>
        /// Gets the session object's starting date and time.
        /// </summary>
        public readonly DateTime CurrentDateTime = DateTime.Now;

        /// <summary>
        /// Gets the session object's starting date as a string.
        /// </summary>
        public string CurrentDate { get; }

        /// <summary>
        /// Gets the session object's starting time as a string.
        /// </summary>
        public string CurrentTime { get; }

        /// <summary>
        /// Gets the session object's UTC offset from GMT 0.
        /// </summary>
        public readonly TimeSpan CurrentTimeZoneBias = TimeZoneInfo.Local.BaseUtcOffset;

        /// <summary>
        /// Gets the script directory of the caller.
        /// </summary>
        public string ScriptDirectory { get; }

        /// <summary>
        /// Gets the specified or determined path to the Files folder.
        /// </summary>
        public string DirFiles { get; }

        /// <summary>
        /// Gets the specified or determined path to the SupportFiles folder.
        /// </summary>
        public string DirSupportFiles { get; }

        /// <summary>
        /// Gets the session object's Zero-Config MSI file path.
        /// </summary>
        public string DefaultMsiFile { get; }

        /// <summary>
        /// Gets the session object's Zero-Config MST file path.
        /// </summary>
        public string DefaultMstFile { get; }

        /// <summary>
        /// Gets the session object's Zero-Config MSP file paths.
        /// </summary>
        public string[] DefaultMspFiles { get; }

        /// <summary>
        /// Gets whether this session object found a valid Zero-Config MSI file.
        /// </summary>
        private bool UseDefaultMsi { get; }

        /// <summary>
        /// Gets the session object's Zero-Config MSP file paths.
        /// </summary>
        public string LogTempFolder { get; }

        /// <summary>
        /// Gets the session object's log filename.
        /// </summary>
        public string LogName { get; }


        #endregion
    }
}
