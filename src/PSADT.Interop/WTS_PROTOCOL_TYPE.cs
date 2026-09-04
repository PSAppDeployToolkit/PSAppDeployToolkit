namespace PSADT.Interop
{
    /// <summary>
    /// WTS protocol types.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The type is correct for the underlying Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "These values are precisely as they're defined in the Win32 API.")]
    public enum WTS_PROTOCOL_TYPE : uint
    {
        /// <summary>
        /// Represents the console protocol type used in remote desktop services.
        /// </summary>
        /// <remarks>This value corresponds to the console session protocol type defined in the Windows
        /// Terminal Services API. It is typically used to identify a local console session.</remarks>
        Console = Windows.Win32.PInvoke.WTS_PROTOCOL_TYPE_CONSOLE,

        /// <summary>
        /// Represents the Independent Computing Architecture (ICA) protocol type.
        /// </summary>
        /// <remarks>The ICA protocol is commonly used for remote desktop and application virtualization
        /// scenarios. This value corresponds to the <see cref="Windows.Win32.PInvoke.WTS_PROTOCOL_TYPE_ICA"/> constant.</remarks>
        ICA = Windows.Win32.PInvoke.WTS_PROTOCOL_TYPE_ICA,

        /// <summary>
        /// Represents the Remote Desktop Protocol (RDP) protocol type.
        /// </summary>
        /// <remarks>This value corresponds to the RDP protocol type as defined in the Windows Terminal
        /// Services API. It is used to identify connections that utilize the Remote Desktop Protocol.</remarks>
        RDP = Windows.Win32.PInvoke.WTS_PROTOCOL_TYPE_RDP,
    }
}
