namespace PSADT.AppManagement
{
    /// <summary>
    /// Defines the type of Windows Runtime package being deployed.
    /// </summary>
    public enum AppxPackageType
    {
        /// <summary>
        /// A single Appx/Msix package.
        /// </summary>
        Package = 0,

        /// <summary>
        /// A bundle of Appx/Msix packages. The main package and all dependencies are contained within a single file with a .msixbundle extension.
        /// </summary>
        Bundle = 1,
    }
}
