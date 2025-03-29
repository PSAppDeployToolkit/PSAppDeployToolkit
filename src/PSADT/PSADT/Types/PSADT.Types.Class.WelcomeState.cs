namespace PSADT.Types
{
    /// <summary>
    /// Represents all data needed by a Show-ADTInstallationWelcome invocation.
    /// </summary>
    public sealed class WelcomeState
    {
        /// <summary>
        /// Gets/sets the running applications.
        /// </summary>
        public RunningApplication[]? RunningApps;

        /// <summary>
        /// Gets/sets the classic WelcomePrompt's starting position.
        /// </summary>
        public System.Drawing.Point FormStartLocation;

        /// <summary>
        /// Gets/sets the InstallationWelcome's CloseProcesses countdown.
        /// </summary>
        public System.Diagnostics.Stopwatch? CloseProcessesCountdown;

        /// <summary>
        /// Gets/sets the running process descriptions.
        /// </summary>
        public string[]? RunningAppDescriptions;

        /// <summary>
        /// Gets/sets the WelcomePrompt's timer.
        /// </summary>
        public System.Windows.Forms.Timer? WelcomeTimer;
    }
}
