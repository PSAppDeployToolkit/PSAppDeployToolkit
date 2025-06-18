using Windows.Win32;

namespace PSADT.TerminalServices
{
    /// <summary>
    /// WTS protocol types.
    /// </summary>
    public enum WTS_PROTOCOL_TYPE : uint
    {
        Console = PInvoke.WTS_PROTOCOL_TYPE_CONSOLE,
        ICA = PInvoke.WTS_PROTOCOL_TYPE_ICA,
        RDP = PInvoke.WTS_PROTOCOL_TYPE_RDP,
    }
}
