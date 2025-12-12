namespace PSADT.Core
{
    /// <summary>
    /// The deployment types that a DeploymentSession can be.
    /// </summary>
    public enum DeploymentType
    {
        /// <summary>
        /// The deployment is for an installation.
        /// </summary>
        Install,

        /// <summary>
        /// The deployment is for an uninstallation.
        /// </summary>
        Uninstall,

        /// <summary>
        /// The deployment is for a repair.
        /// </summary>
        Repair
    }
}
