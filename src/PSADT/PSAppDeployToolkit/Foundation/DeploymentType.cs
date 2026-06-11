namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// The deployment types that a DeploymentSession can be.
    /// </summary>
    public enum DeploymentType
    {
        /// <summary>
        /// The deployment is for an installation.
        /// </summary>
        Install = 0,

        /// <summary>
        /// The deployment is for an uninstallation.
        /// </summary>
        Uninstall = 1,

        /// <summary>
        /// The deployment is for a repair.
        /// </summary>
        Repair = 2
    }
}
