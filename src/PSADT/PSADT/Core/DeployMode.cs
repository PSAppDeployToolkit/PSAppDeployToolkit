namespace PSADT.Core
{
    /// <summary>
    /// The deployment modes that a DeploymentSession can be.
    /// </summary>
    public enum DeployMode
    {
        /// <summary>
        /// The deployment mode is automatically determined based on the environment.
        /// </summary>
        Auto,

        /// <summary>
        /// The deployment is interactive, requiring user interaction.
        /// </summary>
        Interactive,

        /// <summary>
        /// The deployment is non-interactive, not requiring user interaction.
        /// </summary>
        NonInteractive,

        /// <summary>
        /// The deployment is silent, with no user interface displayed.
        /// </summary>
        Silent
    }
}
