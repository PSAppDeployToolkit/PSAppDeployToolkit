using System;

namespace PSADT.Types
{
    /// <summary>
    /// Represents all data needed by a Show-ADTInstallationWelcome invocation.
    /// </summary>
    public struct WelcomeState
    {
        /// <summary>
        /// Gets/sets the classic WelcomePrompt's starting position.
        /// </summary>
        public System.Drawing.Point FormStartLocation { get; set; }

        /// <summary>
        /// Gets/sets the InstallationWelcome's CloseApps countdown.
        /// </summary>
        public double CloseAppsCountdown { get; set; }

        /// <summary>
        /// Gets/sets the running process descriptions.
        /// </summary>
        public string[] RunningProcessDescriptions { get; set; }

        /// <summary>
        /// Gets/sets the WelcomePrompt's timer.
        /// </summary>
        public System.Windows.Forms.Timer WelcomeTimer { get; set; }
    }
}
