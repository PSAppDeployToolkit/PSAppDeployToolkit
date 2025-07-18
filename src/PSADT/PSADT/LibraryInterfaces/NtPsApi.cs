using System;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies extended flags for process creation, allowing customization of process behavior.
    /// </summary>
    /// <remarks>This enumeration supports bitwise combination of its member values due to the <see cref="FlagsAttribute"/>.
    /// Sourced from https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2781-L2783</remarks>
    [Flags]
    internal enum EXTENDED_PROCESS_CREATION_FLAG : uint
    {
        EXTENDED_PROCESS_CREATION_FLAG_ELEVATION_HANDLED = 0x1,
        EXTENDED_PROCESS_CREATION_FLAG_FORCELUA = 0x2,
        EXTENDED_PROCESS_CREATION_FLAG_FORCE_BREAKAWAY = 0x4,
    }
}
