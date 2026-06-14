namespace PSADT.Interop
{
    /// <summary>
    /// Specifies the processor architectures supported by a package.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "This is how it's named in the Win32 API.")]
    public enum APPX_PACKAGE_ARCHITECTURE
    {
        /// <summary>
        /// The x86 processor architecture.
        /// </summary>
        APPX_PACKAGE_ARCHITECTURE_X86 = Windows.Win32.Storage.Packaging.Appx.APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_X86,

        /// <summary>
        /// The ARM processor architecture.
        /// </summary>
        APPX_PACKAGE_ARCHITECTURE_ARM = Windows.Win32.Storage.Packaging.Appx.APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_ARM,

        /// <summary>
        /// The x64 processor architecture.
        /// </summary>
        APPX_PACKAGE_ARCHITECTURE_X64 = Windows.Win32.Storage.Packaging.Appx.APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_X64,

        /// <summary>
        /// Any processor architecture.
        /// </summary>
        APPX_PACKAGE_ARCHITECTURE_NEUTRAL = Windows.Win32.Storage.Packaging.Appx.APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_NEUTRAL,

        /// <summary>
        /// The 64-bit ARM processor architecture.
        /// </summary>
        APPX_PACKAGE_ARCHITECTURE_ARM64 = Windows.Win32.Storage.Packaging.Appx.APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_ARM64,
    }
}
