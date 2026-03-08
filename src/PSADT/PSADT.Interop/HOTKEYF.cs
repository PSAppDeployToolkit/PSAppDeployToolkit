using System;

namespace PSADT.Interop
{
    /// <summary>
    /// Defines modifier key flags used to specify keyboard shortcuts (hotkeys) in the Windows API.
    /// </summary>
    /// <remarks>This enumeration allows combining multiple modifier keys, such as SHIFT, CONTROL, and ALT,
    /// using bitwise operations to register complex hotkey combinations. The values correspond to those defined by the
    /// Win32 API, ensuring compatibility when interacting with native Windows functionality.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "This is as they're defined within the Win32 API.")]
    [Flags]
    internal enum HOTKEYF : uint
    {
        /// <summary>
        /// Represents the SHIFT key modifier for hotkeys.
        /// </summary>
        HOTKEYF_SHIFT = Windows.Win32.PInvoke.HOTKEYF_SHIFT,

        /// <summary>
        /// Represents the CONTROL key modifier for hotkeys.
        /// </summary>
        HOTKEYF_CONTROL = Windows.Win32.PInvoke.HOTKEYF_CONTROL,

        /// <summary>
        /// Represents the ALT key modifier for hotkeys.
        /// </summary>
        HOTKEYF_ALT = Windows.Win32.PInvoke.HOTKEYF_ALT,

        /// <summary>
        /// Represents the extended hotkey flag used in the Windows API.
        /// </summary>
        HOTKEYF_EXT = Windows.Win32.PInvoke.HOTKEYF_EXT,
    }
}
