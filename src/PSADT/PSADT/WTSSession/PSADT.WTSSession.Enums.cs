using Windows.Win32;

namespace PSADT.WTSSession
{
    /// <summary>
    /// WTS SessionInfoEx level identifiers.
    /// </summary>
    public enum WTS_INFO_LEVEL : uint
    {
        WTSINFOEX_LEVEL1 = 1,
    }

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
