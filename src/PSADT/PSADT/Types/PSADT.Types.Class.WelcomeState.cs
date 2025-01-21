namespace PSADT.Types
{
    /// <summary>
    /// Represents all data needed by a Show-ADTInstallationWelcome invocation.
    /// </summary>
    public class WelcomeState
    {
        /// <summary>
        /// Gets/sets the running applications.
        /// </summary>
        public RunningApplication[]? RunningApps { get; set; }

        /// <summary>
        /// Gets/sets the classic WelcomePrompt's starting position.
        /// </summary>
        public System.Drawing.Point FormStartLocation { get; set; }

        /// <summary>
        /// Gets/sets the InstallationWelcome's CloseProcesses countdown.
        /// </summary>
        public System.Diagnostics.Stopwatch? CloseProcessesCountdown { get; set; }

        /// <summary>
        /// Gets/sets the running process descriptions.
        /// </summary>
        public string[]? RunningAppDescriptions { get; set; }

        /// <summary>
        /// Gets/sets the WelcomePrompt's timer.
        /// </summary>
        public System.Windows.Forms.Timer? WelcomeTimer { get; set; }
    }
}
