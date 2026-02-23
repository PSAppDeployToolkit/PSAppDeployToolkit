namespace PSADT.Interop
{
    /// <summary>
    /// Flags for SHGetImageList function.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "These values are precisely as they're defined in the Win32 API.")]
    internal enum SHIL_SIZE : uint
    {
        /// <summary>
        /// The image size is normally 32x32 pixels. However, if the Use large icons option is selected from the Effects section of the Appearance tab in Display Properties, the image is 48x48 pixels.
        /// </summary>
        SHIL_LARGE = Windows.Win32.PInvoke.SHIL_LARGE,

        /// <summary>
        /// These images are the Shell standard small icon size of 16x16, but the size can be customized by the user.
        /// </summary>
        SHIL_SMALL = Windows.Win32.PInvoke.SHIL_SMALL,

        /// <summary>
        /// These images are the Shell standard extra-large icon size. This is typically 48x48, but the size can be customized by the user.
        /// </summary>
        SHIL_EXTRALARGE = Windows.Win32.PInvoke.SHIL_EXTRALARGE,

        /// <summary>
        /// These images are the size specified by GetSystemMetrics called with SM_CXSMICON and GetSystemMetrics called with SM_CYSMICON.
        /// </summary>
        SHIL_SYSSMALL = Windows.Win32.PInvoke.SHIL_SYSSMALL,

        /// <summary>
        /// Windows Vista and later. The image is normally 256x256 pixels.
        /// </summary>
        SHIL_JUMBO = Windows.Win32.PInvoke.SHIL_JUMBO,

        /// <summary>
        /// The largest valid flag value, for validation purposes.
        /// </summary>
        SHIL_LAST = Windows.Win32.PInvoke.SHIL_LAST,
    }
}
