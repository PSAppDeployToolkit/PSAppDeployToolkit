using System.Collections.Generic;
using PSADT.PInvoke;

namespace PSADT.ProcessEx
{
    /// <summary>
    /// Represents the options for launching a process.
    /// </summary>
    public class LaunchOptions
    {
        /// <summary>
        /// The file name or a fully-qualified path for the file to be executed.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// List of command-line arguments for the process being executed.
        /// </summary>
        public List<string> ArgumentList { get; set; } = new List<string>();

        /// <summary>
        /// The working directory for the process being executed.
        /// </summary>
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// Launch the process and hide it's window.
        /// </summary>
        public bool HideWindow { get; set; }

        /// <summary>
        /// The process creation flags the process should be started with.
        /// </summary>
        public List<CREATE_PROCESS> ProcessCreationFlags { get; set; } = new List<CREATE_PROCESS>();

        /// <summary>
        /// The process being executed should inherit environment variables from the parent process.
        /// </summary>
        public bool? InheritEnvironmentVariables { get; set; }

        /// Wait for the process to exit before continuing.
        /// </summary>
        public bool Wait { get; set; }

        /// <summary>
        /// The session id to execute the process in.
        /// </summary>
        public uint? SessionId { get; set; }

        /// <summary>
        /// Gets the username associated with the session ID where the process will be launched.
        /// The value is set internally and cannot be modified.
        /// </summary>
        public string Username { get; private set; } = string.Empty;

        /// <summary>
        /// Execute the process in all active user sessions.
        /// </summary>
        public bool AllActiveUserSessions { get; set; }

        /// <summary>
        /// Execute the process in primary active user session.
        /// </summary>
        public bool PrimaryActiveUserSession { get; set; }

        /// <summary>
        /// Use the user's linked admin token when executing the process.
        /// </summary>
        public bool UseLinkedAdminToken { get; set; }

        /// <summary>
        /// The PowerShell execution policy to use when launching PowerShell scripts.
        /// </summary>
        public string PsExecutionPolicy { get; set; } = "RemoteSigned";

        /// <summary>
        /// Bypass the PowerShell execution policy.
        /// </summary>
        public bool BypassPsExecutionPolicy { get; set; }

        /// <summary>
        /// List of exit codes that should be considered successful.
        /// </summary>
        public List<int> SuccessExitCodes { get; set; } = new List<int> { 0, 3010 };

        /// <summary>
        /// The timeout in seconds after which the console will automatically close.
        /// </summary>
        public int ConsoleTimeoutInSeconds { get; set; } = 30;

        /// <summary>
        /// Enable verbose output.
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Run this utilitty in debug mode to see log messages output to the console.
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Indicates whether the process is a GUI application.
        /// </summary>
        public bool IsGuiApplication { get; set; }

        /// <summary>
        /// Choose whether to redirect the process output.
        /// </summary>
        public bool RedirectOutput { get; set; } = true;

        /// <summary>
        /// Choose whether to merge stderr into stdout.
        /// </summary>
        public bool MergeStdErrAndStdOut { get; set; } = false;

        /// <summary>
        /// Choose where to save redirected output.
        /// </summary>
        public string? OutputDirectory { get; set; }

        /// <summary>
        /// Choose whether to terminate the process being executed on timeout.
        /// </summary>
        public bool TerminateOnTimeout { get; set; } = true;

        /// <summary>
        /// Specify environment variables to be added or overridden for the process being executed.
        /// </summary>
        public Dictionary<string, string> AdditionalEnvironmentVariables { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Display the help information for this utility.
        /// </summary>
        public bool Help { get; set; }

        /// <summary>
        /// Specify wait options for one or more processes being executed.
        /// </summary>
        public WaitType WaitOption { get; set; } = WaitType.WaitForAny;

        /// <summary>
        /// Updates the value of the Username property. This is the only way to modify the property's value.
        /// </summary>
        /// <param name="newValue">The new value to set for the property.</param>
        public void UpdateUsername(string username)
        {
            Username = username;
        }

        /// <summary>
        /// Adds an environment variable to the AdditionalEnvironmentVariables dictionary.
        /// </summary>
        /// <param name="key">The name of the environment variable.</param>
        /// <param name="value">The value of the environment variable.</param>
        public void AddEnvironmentVariable(string key, string value)
        {
            AdditionalEnvironmentVariables[key] = value;
        }

        /// <summary>
        /// Creates the process creation flags based on the current settings, including the <see cref="HideWindow"/> property and any additional flags.
        /// Ensures that no newFlag is added more than once, and that additional flags override any conflicting default flags.
        /// </summary>
        /// <returns>A <see cref="CREATE_PROCESS"/> value representing the combined process creation flags.</returns>
        public CREATE_PROCESS CreateProcessCreationFlags()
        {
            // Define the default flags based on the HideWindow property
            CREATE_PROCESS defaultFlags = CREATE_PROCESS.CREATE_UNICODE_ENVIRONMENT |
                                          CREATE_PROCESS.CREATE_BREAKAWAY_FROM_JOB |
                                          (HideWindow ? CREATE_PROCESS.CREATE_NO_WINDOW : CREATE_PROCESS.CREATE_NEW_CONSOLE);

            // StartProcess with no flags
            CREATE_PROCESS flags = 0;

            // Add additional flags specified via command-line or other configurations
            foreach (var newFlag in ProcessCreationFlags)
            {
                // Add the newFlag if it hasn't been added already
                if (!flags.HasFlag(newFlag))
                {
                    flags |= newFlag;
                }
            }

            // Handle conflicts between default and additional flags by giving preference to the additional flags
            /* Let's not override these default flags so that they are still controlled by the HideWindow property
            if (flags.HasFlag(CREATE_PROCESS.CREATE_NO_WINDOW))
            {
                defaultFlags &= ~CREATE_PROCESS.CREATE_NEW_CONSOLE;
            }
            else if (flags.HasFlag(CREATE_PROCESS.CREATE_NEW_CONSOLE))
            {
                defaultFlags &= ~CREATE_PROCESS.CREATE_NO_WINDOW;
            }*/

            if (flags.HasFlag(CREATE_PROCESS.DETACHED_PROCESS))
            {
                defaultFlags &= ~CREATE_PROCESS.CREATE_NEW_CONSOLE;
            }

            if (flags.HasFlag(CREATE_PROCESS.CREATE_NEW_PROCESS_GROUP))
            {
                defaultFlags &= ~CREATE_PROCESS.CREATE_NEW_CONSOLE;
            }

            // Combine the remaining default flags with the additional flags
            flags |= defaultFlags;

            return flags;
        }
    }
}