using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace PSADT.PInvoke
{
    #region kernel32.dll

    /// <summary>
    /// Contains information about a newly created process and its primary thread.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        /// <summary>
        /// A handle to the newly created process.
        /// </summary>
        public IntPtr hProcess;

        /// <summary>
        /// A handle to the primary thread of the newly created process.
        /// </summary>
        public IntPtr hThread;

        /// <summary>
        /// The process identifier.
        /// </summary>
        public uint dwProcessId;

        /// <summary>
        /// The thread identifier.
        /// </summary>
        public uint dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    /// <summary>
    /// Specifies the window station, desktop, standard handles, and appearance of the main window for a process at creation time.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    /// <summary>
    /// Contains information about the current computer system. This includes the architecture and type of the processor, the number of
    /// processors in the system, the page size, and other such information.
    /// </summary>
    // typedef struct _SYSTEM_INFO { union { DWORD dwOemId; struct { WORD wProcessorArchitecture; WORD wReserved; }; }; DWORD dwPageSize;
    // LPVOID lpMinimumApplicationAddress; LPVOID lpMaximumApplicationAddress; DWORD_PTR dwActiveProcessorMask; DWORD
    // dwNumberOfProcessors; DWORD dwProcessorType; DWORD dwAllocationGranularity; WORD wProcessorLevel; WORD wProcessorRevision;}
    // SYSTEM_INFO; https://msdn.microsoft.com/en-us/library/windows/desktop/ms724958(v=vs.85).aspx
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct SYSTEM_INFO
    {
        /// <summary>
        /// <para>The processor architecture of the installed operating system. This member can be one of the following values.</para>
        /// <para>
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>PROCESSOR_ARCHITECTURE_AMD649</term>
        /// <term>x64 (AMD or Intel)</term>
        /// </item>
        /// <item>
        /// <term>PROCESSOR_ARCHITECTURE_ARM5</term>
        /// <term>ARM</term>
        /// </item>
        /// <item>
        /// <term>PROCESSOR_ARCHITECTURE_ARM6412</term>
        /// <term>ARM64</term>
        /// </item>
        /// <item>
        /// <term>PROCESSOR_ARCHITECTURE_IA646</term>
        /// <term>Intel Itanium-based</term>
        /// </item>
        /// <item>
        /// <term>PROCESSOR_ARCHITECTURE_INTEL0</term>
        /// <term>x86</term>
        /// </item>
        /// <item>
        /// <term>PROCESSOR_ARCHITECTURE_UNKNOWN0xffff</term>
        /// <term>Unknown architecture.</term>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        public ProcessorArchitecture wProcessorArchitecture;

        /// <summary>This member is reserved for future use.</summary>
        public ushort wReserved;

        /// <summary>
        /// The page size and the granularity of page protection and commitment. This is the page size used by the <c>VirtualAlloc</c> function.
        /// </summary>
        public uint dwPageSize;

        /// <summary>A pointer to the lowest memory address accessible to applications and dynamic-link libraries (DLLs).</summary>
        public IntPtr lpMinimumApplicationAddress;

        /// <summary>A pointer to the highest memory address accessible to applications and DLLs.</summary>
        public IntPtr lpMaximumApplicationAddress;

        /// <summary>
        /// A mask representing the set of processors configured into the system. Bit 0 is processor 0; bit 31 is processor 31.
        /// </summary>
        public nuint dwActiveProcessorMask;

        /// <summary>
        /// The number of logical processors in the current group. To retrieve this value, use the <c>GetLogicalProcessorInformation</c> function.
        /// </summary>
        public uint dwNumberOfProcessors;

        /// <summary>
        /// An obsolete member that is retained for compatibility. Use the <c>wProcessorArchitecture</c>, <c>wProcessorLevel</c>, and
        /// <c>wProcessorRevision</c> members to determine the type of processor.
        /// </summary>
        public uint dwProcessorType;

        /// <summary>
        /// The granularity for the starting address at which virtual memory can be allocated. For more information, see <c>VirtualAlloc</c>.
        /// </summary>
        public uint dwAllocationGranularity;

        /// <summary>
        /// <para>
        /// The architecture-dependent processor level. It should be used only for display purposes. To determine the feature set of a
        /// processor, use the <c>IsProcessorFeaturePresent</c> function.
        /// </para>
        /// <para>If <c>wProcessorArchitecture</c> is PROCESSOR_ARCHITECTURE_INTEL, <c>wProcessorLevel</c> is defined by the CPU vendor.</para>
        /// <para>If <c>wProcessorArchitecture</c> is PROCESSOR_ARCHITECTURE_IA64, <c>wProcessorLevel</c> is set to 1.</para>
        /// </summary>
        public ushort wProcessorLevel;

        /// <summary>
        /// <para>
        /// The architecture-dependent processor revision. The following table shows how the revision value is assembled for each type of
        /// processor architecture.
        /// </para>
        /// <para>
        /// <list type="table">
        /// <listheader>
        /// <term>Processor</term>
        /// <term>Value</term>
        /// </listheader>
        /// <item>
        /// <term>Intel Pentium, Cyrix, or NextGen 586</term>
        /// <term>
        /// The high byte is the model and the low byte is the stepping. For example, if the value is xxyy, the model number and stepping
        /// can be displayed as
        /// follows: Model xx, Stepping yy
        /// </term>
        /// </item>
        /// <item>
        /// <term>Intel 80386 or 80486</term>
        /// <term>
        /// A value of the form xxyz. If xx is equal to 0xFF, y - 0xA is the model number, and z is the stepping identifier.If xx is not
        /// equal to 0xFF, xx + 'A' is the stepping letter and yz is the minor stepping.
        /// </term>
        /// </item>
        /// <item>
        /// <term>ARM</term>
        /// <term>Reserved.</term>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        public ushort wProcessorRevision;
    }

    #endregion

    #region wtsapi32.dll

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct WTS_SESSION_INFO
    {
        /// <summary>Session identifier of the session.</summary>
        public uint SessionId;

        /// <summary>
        /// Pointer to a null-terminated string that contains the WinStation name of this session. The WinStation name is a name that
        /// Windows associates with the session, for example, "services", "console", or "RDP-Tcp#0".
        /// </summary>
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pWinStationName;

        /// <summary>A value from the WTS_CONNECTSTATE_CLASS enumeration type that indicates the session's current connection state.</summary>
        public WTS_CONNECTSTATE_CLASS State;
    }

    /// <summary>Contains the client network address of a Remote Desktop Services session.</summary>
    /// <remarks>
    /// <para>
    /// The client network address is reported by the RDP client itself when it connects to the server. This could be different than the
    /// address that actually connected to the server. For example, suppose there is a NAT between the client and the server. The client
    /// can report its own IP address, but the IP address that actually connects to the server is the NAT address. For VPN connections,
    /// the IP address might not be discoverable by the client. If it cannot be discovered, the client can report the only IP address it
    /// has, which may be the ISP assigned address. Because the address may not be the actual network address, it should not be used as
    /// a form of client authentication.
    /// </para>
    /// <para>The client network address is also not available in the following cases:</para>
    /// <list type="bullet">
    /// <item>
    /// <term>The connection is established through a Remote Desktop Gateway.</term>
    /// </item>
    /// <item>
    /// <term>The connection is originated by the <c>Microsoft Remote Desktop</c> app that is available in the Store.</term>
    /// </item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct WTS_CLIENT_ADDRESS
    {
        /// <summary>Address family. This member can be <c>AF_INET</c>, <c>AF_INET6</c>, <c>AF_IPX</c>, <c>AF_NETBIOS</c>, or <c>AF_UNSPEC</c>.</summary>
        public ADDRESS_FAMILY AddressFamily;

        /// <summary>
        /// <para>
        /// Client network address. The format of the field of <c>Address</c> depends on the address type as specified by the
        /// <c>AddressFamily</c> member.
        /// </para>
        /// <para>
        /// For an address family <c>AF_INET</c>: <c>Address</c> contains the IPV4 address of the client as a null-terminated string.
        /// </para>
        /// <para>
        /// For an family <c>AF_INET6</c>: <c>Address</c> contains the IPV6 address of the client as raw byte values. (For example, the
        /// address "FFFF::1" would be represented as the following series of byte values: "0xFF 0xFF 0x00 0x00 0x00 0x00 0x00 0x00 0x00
        /// 0x00 0x00 0x00 0x00 0x00 0x00 0x01")
        /// </para>
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Address;
    }

    /// <summary>
    /// Contains the virtual IP address assigned to a session. This structure is returned by the WTSQuerySessionInformation function
    /// when you specify "WTSSessionAddressV4" for the WTSInfoClass parameter.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WTS_SESSION_ADDRESS
    {
        /// <summary>A null-terminated string that contains the address family. Always set this member to "AF_INET".</summary>
        public ADDRESS_FAMILY AddressFamily;

        /// <summary>
        /// The virtual IP address assigned to the session. The format of this address is identical to that used in the
        /// WTS_CLIENT_ADDRESS structure. If the session does not have a virtual IP address, the WTSQuerySessionInformation
        /// function returns ERROR_NOT_SUPPORTED
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Address;
    }

    /// <summary>Contains information about the display of a Remote Desktop Connection (RDC) client.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WTS_CLIENT_DISPLAY
    {
        /// <summary>Horizontal dimension, in pixels, of the client's display.</summary>
        public uint HorizontalResolution;

        /// <summary>Vertical dimension, in pixels, of the client's display.</summary>
        public uint VerticalResolution;

        /// <summary>
        /// <para>Color depth of the client's display. This member can be one of the following values.</para>
        /// <para>1</para>
        /// <para>4 bits per pixel.</para>
        /// <para>2</para>
        /// <para>8 bits per pixel.</para>
        /// <para>4</para>
        /// <para>16 bits per pixel.</para>
        /// <para>8</para>
        /// <para>A 3-byte RGB values for a maximum of 2^24 colors.</para>
        /// <para>16</para>
        /// <para>15 bits per pixel.</para>
        /// <para>24</para>
        /// <para>24 bits per pixel.</para>
        /// <para>32</para>
        /// <para>32 bits per pixel.</para>
        /// </summary>
        public uint ColorDepth;
    }

    /// <summary>Contains information about a Remote Desktop Services session.</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WTSINFO
    {
        /// <summary>A value of the WTS_CONNECTSTATE_CLASS enumeration type that indicates the session's current connection state.</summary>
        public WTS_CONNECTSTATE_CLASS State;

        /// <summary>The session identifier.</summary>
        public uint SessionId;

        /// <summary>Uncompressed Remote Desktop Protocol (RDP) data from the client to the server.</summary>
        public uint IncomingBytes;

        /// <summary>Uncompressed RDP data from the server to the client.</summary>
        public uint OutgoingBytes;

        /// <summary>The number of frames of RDP data sent from the client to the server since the client connected.</summary>
        public uint IncomingFrames;

        /// <summary>The number of frames of RDP data sent from the server to the client since the client connected.</summary>
        public uint OutgoingFrames;

        /// <summary>Compressed RDP data from the client to the server.</summary>
        public uint IncomingCompressedBytes;

        /// <summary/>
        public uint OutgoingCompressedBy;

        /// <summary>A null-terminated string that contains the name of the WinStation for the session.</summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeMethods.WINSTATIONNAME_LENGTH)]
        public string WinStationName;

        /// <summary>A null-terminated string that contains the name of the domain that the user belongs to.</summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeMethods.DOMAIN_LENGTH)]
        public string Domain;

        /// <summary>A null-terminated string that contains the name of the user who owns the session.</summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeMethods.USERNAME_LENGTH + 2)]
        public string UserName;

        /// <summary>The most recent client connection time.</summary>
		public long ConnectTimeUTC;

        /// <summary>The most recent client connection time.</summary>
        public DateTime ConnectTime => DateTime.FromFileTimeUtc(ConnectTimeUTC);

        /// <summary>The last client disconnection time.</summary>
        public long DisconnectTimeUTC;

        /// <summary>The last client disconnection time.</summary>
        public DateTime DisconnectTime => DateTime.FromFileTimeUtc(DisconnectTimeUTC);

        /// <summary>The time of the last user input in the session.</summary>
        public long LastInputTimeUTC;

        /// <summary>The time of the last user input in the session.</summary>
        public DateTime LastInputTime => DateTime.FromFileTimeUtc(LastInputTimeUTC);

        /// <summary>The time that the user logged on to the session.</summary>
        public long LogonTimeUTC;

        /// <summary>The time that the user logged on to the session.</summary>
        public DateTime LogonTime => DateTime.FromFileTimeUtc(LogonTimeUTC);

        /// <summary>The time that the <c>WTSINFO</c> data structure was called.</summary>
        public long CurrentTimeUTC;

        /// <summary>The time that the <c>WTSINFO</c> data structure was called.</summary>
        public DateTime CurrentTime => DateTime.FromFileTimeUtc(CurrentTimeUTC);
    }

    #endregion

    #region advapi32.dll

    /// <summary>
    /// The TOKEN_LINKED_TOKEN structure contains a handle to the linked token.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_LINKED_TOKEN
    {
        public IntPtr LinkedToken;
    }

    /// <summary>
    /// The TOKEN_ELEVATION structure indicates whether a token has elevated privileges.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_ELEVATION
    {
        /// <summary>A nonzero value if the token has elevated privileges; otherwise, a zero value.</summary>
        [MarshalAs(UnmanagedType.Bool)]
        public bool TokenIsElevated;
    }

    /// <summary>
    /// The TOKEN_PRIVILEGES structure contains information about a set of privileges for an access token.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public LUID_AND_ATTRIBUTES[] Privileges;
    }

    /// <summary>
    /// Represents a 64-bit signed integer. This type is declared in ntdef.h as follows:
    /// typedef struct _LUID {
    ///   DWORD LowPart;
    ///   LONG  HighPart;
    /// } LUID, *PLUID;
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    /// <summary>
    /// The LUID_AND_ATTRIBUTES structure represents a locally unique identifier (LUID) and its attributes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public uint Attributes;
    }

    /// <summary>
    /// Represents a handle to a registry key (HKEY).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct HKEY : IEquatable<HKEY>
    {
        private readonly IntPtr handle;

        /// <summary>Initializes a new instance of the <see cref="HKEY"/> struct.</summary>
        /// <param name="preexistingHandle">An <see cref="IntPtr"/> object that represents the pre-existing handle to use.</param>
        public HKEY(IntPtr preexistingHandle) => handle = preexistingHandle;

        /// <summary>Returns an invalid handle by instantiating a <see cref="HKEY"/> object with <see cref="IntPtr.Zero"/>.</summary>
        public static HKEY NULL => new(IntPtr.Zero);

        /// <summary>Gets a value indicating whether this instance is a null handle.</summary>
        public bool IsNull => handle == IntPtr.Zero;

        /// <summary>
        /// Registry entries subordinate to this key define types (or classes) of documents and the properties associated with those types.
        /// Shell and COM applications use the information stored under this key.
        /// </summary>
        public static readonly HKEY HKEY_CLASSES_ROOT = new(new IntPtr(unchecked((int)0x80000000)));

        /// <summary>
        /// Contains information about the current hardware profile of the local computer system. The information under HKEY_CURRENT_CONFIG
        /// describes only the differences between the current hardware configuration and the standard configuration. Information about the
        /// standard hardware configuration is stored under the Software and System keys of HKEY_LOCAL_MACHINE.
        /// </summary>
        public static readonly HKEY HKEY_CURRENT_CONFIG = new(new IntPtr(unchecked((int)0x80000005)));

        /// <summary>
        /// Registry entries subordinate to this key define the preferences of the current user. These preferences include the settings of
        /// environment variables, data about program groups, colors, printers, network connections, and application preferences. This key
        /// makes it easier to establish the current user's settings; the key maps to the current user's branch in HKEY_USERS. In
        /// HKEY_CURRENT_USER, software vendors store the current user-specific preferences to be used within their applications. Microsoft,
        /// for example, creates the HKEY_CURRENT_USER\Software\Microsoft key for its applications to use, with each application creating its
        /// own subkey under the Microsoft key.
        /// </summary>
        public static readonly HKEY HKEY_CURRENT_USER = new(new IntPtr(unchecked((int)0x80000001)));

        /// <summary>
        /// Registry entries subordinate to this key define the physical state of the computer, including data about the bus type, system
        /// memory, and installed hardware and software. It contains subkeys that hold current configuration data, including Plug and Play
        /// information (the Enum branch, which includes a complete list of all hardware that has ever been on the system), network logon
        /// preferences, network security information, software-related information (such as server names and the location of the server),
        /// and other system information.
        /// </summary>
        public static readonly HKEY HKEY_LOCAL_MACHINE = new(new IntPtr(unchecked((int)0x80000002)));

        /// <summary>
        /// Registry entries subordinate to this key allow you to access performance data. The data is not actually stored in the registry;
        /// the registry functions cause the system to collect the data from its source.
        /// </summary>
        public static readonly HKEY HKEY_PERFORMANCE_DATA = new(new IntPtr(unchecked((int)0x80000004)));

        /// <summary>
        /// Registry entries subordinate to this key define the default user configuration for new users on the local computer and the user
        /// configuration for the current user.
        /// </summary>
        public static readonly HKEY HKEY_USERS = new(new IntPtr(unchecked((int)0x80000003)));

        /// <summary>Performs an explicit conversion from <see cref="HKEY"/> to <see cref="IntPtr"/>.</summary>
        /// <param name="h">The handle.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator IntPtr(HKEY h) => h.handle;

        /// <summary>Performs an implicit conversion from <see cref="IntPtr"/> to <see cref="HKEY"/>.</summary>
        /// <param name="h">The pointer to a handle.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator HKEY(IntPtr h) => new(h);

        /// <summary>Performs an implicit conversion from <see cref="HKEY"/> to <see cref="SafeRegistryHandle"/>.</summary>
        /// <param name="h">The pointer to a handle.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator HKEY(SafeRegistryHandle h) => new(h.DangerousGetHandle());

        /// <summary>Implements the operator ! which returns <see langword="true"/> if the handle is invalid.</summary>
        /// <param name="hMem">The <see cref="HKEY"/> instance.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !(HKEY hMem) => hMem.IsNull;

        /// <summary>Implements the operator !=.</summary>
        /// <param name="h1">The first handle.</param>
        /// <param name="h2">The second handle.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(HKEY h1, HKEY h2) => !(h1 == h2);

        /// <summary>Implements the operator ==.</summary>
        /// <param name="h1">The first handle.</param>
        /// <param name="h2">The second handle.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(HKEY h1, HKEY h2) => h1.Equals(h2);

        /// <summary>
        /// Determines whether the specified <see cref="HKEY"/> is equal to the current <see cref="HKEY"/>.
        /// </summary>
        /// <param name="other">The <see cref="HKEY"/> to compare with the current <see cref="HKEY"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="HKEY"/> is equal to the current <see cref="HKEY"/>; otherwise, <c>false</c>.</returns>
        public bool Equals(HKEY other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object? obj) => obj is HKEY h && handle == h.handle;

        public override int GetHashCode() => handle.GetHashCode();

        public IntPtr DangerousGetHandle() => handle;
    }

    #endregion

    #region winsta.dll

    [StructLayout(LayoutKind.Sequential)]
    public struct WINSTATIONINFORMATIONW
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 70)]
        private readonly byte[] Reserved1;

        public int SessionId;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private readonly byte[] Reserved2;

        public FILETIME ConnectTimeFT;
        public long ConnectTimeUTC => FileTimeToLong(ConnectTimeFT);
        public DateTime ConnectTime => DateTime.FromFileTimeUtc(ConnectTimeUTC);

        public FILETIME DisconnectTimeFT;
        public long DisconnectTimeUTC => FileTimeToLong(DisconnectTimeFT);
        public DateTime DisconnectTime => DateTime.FromFileTimeUtc(DisconnectTimeUTC);

        public FILETIME LastInputTimeFT;
        public long LastInputTimeUTC => FileTimeToLong(LastInputTimeFT);
        public DateTime LastInputTime => DateTime.FromFileTimeUtc(LastInputTimeUTC);

        public FILETIME LogonTimeFT;
        public long LogonTimeUTC => FileTimeToLong(LogonTimeFT);
        public DateTime LogonTime => DateTime.FromFileTimeUtc(LogonTimeUTC);

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1096)]
        private readonly byte[] Reserved3;

        public FILETIME CurrentTimeFT;
        public long CurrentTimeUTC => FileTimeToLong(CurrentTimeFT);
        public DateTime CurrentTime => DateTime.FromFileTimeUtc(CurrentTimeUTC);

        private static long FileTimeToLong(FILETIME fileTime)
        {
            // Combine the high and low parts into a ulong to avoid overflow
            ulong fileTimeLong = ((ulong)fileTime.dwHighDateTime << 32) | (ulong)fileTime.dwLowDateTime;

            // If the FILETIME is zero or invalid, return zero
            if (fileTimeLong == 0 || fileTimeLong > (ulong)DateTime.MaxValue.ToFileTimeUtc())
                return 0;

            return (long)fileTimeLong;
        }
    }

    #endregion

    #region ntdll.dll

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct OSVERSIONINFOEX
    {
        // The OSVersionInfoSize field must be set to Marshal.SizeOf(typeof(OSVERSIONINFOEX))
        public int OSVersionInfoSize;
        public int MajorVersion;
        public int MinorVersion;
        public int BuildNumber;
        public int PlatformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string CSDVersion;
        public ushort ServicePackMajor;
        public ushort ServicePackMinor;
        public SuiteMask SuiteMask;
        public ProductType ProductType;
        public byte Reserved;
    }

    #endregion

    #region user32.dll

    /// <summary>Provides a handle to a DPI awareness context.</summary>
	[StructLayout(LayoutKind.Sequential)]
    public readonly struct DPI_AWARENESS_CONTEXT : IEquatable<DPI_AWARENESS_CONTEXT>
    {
        private readonly IntPtr handle;

        /// <summary>Initializes a new instance of the <see cref="DPI_AWARENESS_CONTEXT"/> struct.</summary>
        /// <param name="preexistingHandle">An <see cref="IntPtr"/> object that represents the pre-existing handle to use.</param>
        public DPI_AWARENESS_CONTEXT(IntPtr preexistingHandle) => handle = preexistingHandle;

        /// <summary>Returns an invalid handle by instantiating a <see cref="DPI_AWARENESS_CONTEXT"/> object with <see cref="IntPtr.Zero"/>.</summary>
        public static DPI_AWARENESS_CONTEXT NULL => new(IntPtr.Zero);

        /// <summary>Gets a value indicating whether this instance is a null handle.</summary>
        public bool IsNull => handle == IntPtr.Zero;

        /// <summary>Performs an explicit conversion from <see cref="DPI_AWARENESS_CONTEXT"/> to <see cref="IntPtr"/>.</summary>
        /// <param name="h">The handle.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator IntPtr(DPI_AWARENESS_CONTEXT h) => h.handle;

        /// <summary>Performs an implicit conversion from <see cref="IntPtr"/> to <see cref="DPI_AWARENESS_CONTEXT"/>.</summary>
        /// <param name="h">The pointer to a handle.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator DPI_AWARENESS_CONTEXT(IntPtr h) => new(h);

        /// <summary>Implements the operator !=.</summary>
        /// <param name="h1">The first handle.</param>
        /// <param name="h2">The second handle.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(DPI_AWARENESS_CONTEXT h1, DPI_AWARENESS_CONTEXT h2) => !(h1 == h2);

        /// <summary>Implements the operator ==.</summary>
        /// <param name="h1">The first handle.</param>
        /// <param name="h2">The second handle.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(DPI_AWARENESS_CONTEXT h1, DPI_AWARENESS_CONTEXT h2) => h1.Equals(h2);

        /// <summary>Implements the equality operator.</summary>
        /// <param name="obj"></param>
        public override bool Equals(object? obj) => obj is DPI_AWARENESS_CONTEXT h && handle == h.handle;

        /// <summary>Indicates whether the current <see cref="DPI_AWARENESS_CONTEXT"/> is equal to another <see cref="DPI_AWARENESS_CONTEXT"/>.</summary>
        /// <param name="other"></param>
        /// <returns>The result of the operator.</returns>
        public bool Equals(DPI_AWARENESS_CONTEXT other) => handle == other.handle;

        /// <summary>Gets a hash code for this <see cref="DPI_AWARENESS_CONTEXT"/> object.</summary>
        public override int GetHashCode() => handle.GetHashCode();

        /// <summary>Gets the <see cref="IntPtr"/> value of the handle held by this instance.</summary>
        public IntPtr DangerousGetHandle() => handle;

        /// <summary>
        /// DPI unaware. This window does not scale for DPI changes and is always assumed to have a scale factor of 100% (96 DPI). It
        /// will be automatically scaled by the system on any other DPI setting.
        /// </summary>
        public static readonly DPI_AWARENESS_CONTEXT DPI_AWARENESS_CONTEXT_UNAWARE = new(new(-1));

        /// <summary>
        /// System DPI aware. This window does not scale for DPI changes. It will query for the DPI once and use that value for the
        /// lifetime of the process. If the DPI changes, the process will not adjust to the new DPI value. It will be automatically
        /// scaled up or down by the system when the DPI changes from the system value.
        /// </summary>
        public static readonly DPI_AWARENESS_CONTEXT DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = new(new(-2));

        /// <summary>
        /// Per monitor DPI aware. This window checks for the DPI when it is created and adjusts the scale factor whenever the DPI
        /// changes. These processes are not automatically scaled by the system.
        /// </summary>
        public static readonly DPI_AWARENESS_CONTEXT DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE = new(new(-3));

        /// <summary>
        /// <para>
        /// Also known as Per Monitor v2. An advancement over the original per-monitor DPI awareness mode, which enables applications to
        /// access new DPI-related scaling behaviors on a per top-level window basis.
        /// </para>
        /// <para>
        /// Per Monitor v2 was made available in the Creators Update of Windows 10, and is not available on earlier versions of the
        /// operating system.
        /// </para>
        /// <para>The additional behaviors introduced are as follows:</para>
        /// <list type="bullet">
        /// <item>
        /// <term>Child window DPI change notifications</term>
        /// <description>In Per Monitor v2 contexts, the entire window tree is notified of any DPI changes that occur.</description>
        /// </item>
        /// <item>
        /// <term>Scaling of non-client area</term>
        /// <description>
        /// All windows will automatically have their non-client area drawn in a DPI sensitive fashion. Calls to
        /// EnableNonClientDpiScaling are unnecessary.
        /// </description>
        /// </item>
        /// <item>
        /// <term>Scaling of Win32 menus</term>
        /// <description>All NTUSER menus created in Per Monitor v2 contexts will be scaling in a per-monitor fashion.</description>
        /// </item>
        /// <item>
        /// <term>Dialog Scaling</term>
        /// <description>Win32 dialogs created in Per Monitor v2 contexts will automatically respond to DPI changes.</description>
        /// </item>
        /// <item>
        /// <term>Improved scaling of comctl32 controls</term>
        /// <description>Various comctl32 controls have improved DPI scaling behavior in Per Monitor v2 contexts.</description>
        /// </item>
        /// <item>
        /// <term>Improved theming behavior</term>
        /// <description>
        /// UxTheme handles opened in the context of a Per Monitor v2 window will operate in terms of the DPI associated with that window.
        /// </description>
        /// </item>
        /// </list>
        /// </summary>
        public static readonly DPI_AWARENESS_CONTEXT DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(new(-4));

        /// <summary>
        /// <para>
        /// DPI unaware with improved quality of GDI-based content. This mode behaves similarly to DPI_AWARENESS_CONTEXT_UNAWARE, but
        /// also enables the system to automatically improve the rendering quality of text and other GDI-based primitives when the window
        /// is displayed on a high-DPI monitor.
        /// </para>
        /// <para>For more details, see Improving the high-DPI experience in GDI-based Desktop apps.</para>
        /// <para>
        /// DPI_AWARENESS_CONTEXT_UNAWARE_GDISCALED was introduced in the October 2018 update of Windows 10 (also known as version 1809).
        /// </para>
        /// </summary>
        public static readonly DPI_AWARENESS_CONTEXT DPI_AWARENESS_CONTEXT_UNAWARE_GDISCALED = new(new(-5));
    }

    #endregion

    #region shell32.dll

    /// <summary>Contains information about a file object.</summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHFILEINFO
    {
        /// <summary>
        /// A handle to the icon that represents the file. You are responsible for destroying this handle with DestroyIcon when you no
        /// longer need it.
        /// </summary>
        public IntPtr hIcon;

        /// <summary>The index of the icon image within the system image list.</summary>
        public int iIcon;

        /// <summary>
        /// An array of values that indicates the attributes of the file object. For information about these values, see the
        /// IShellFolder::GetAttributesOf method.
        /// </summary>
        public int dwAttributes;

        /// <summary>
        /// A string that contains the name of the file as it appears in the Windows Shell, or the path and file name of the file that
        /// contains the icon representing the file.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;

        /// <summary>A string that describes the type of file.</summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;

        /// <summary>Gets the size of this structure.</summary>
        /// <value>The structure size in bytes.</value>
        public static int Size => Marshal.SizeOf(typeof(SHFILEINFO));
    }

    #endregion

    #region wintrust.dll

    [StructLayout(LayoutKind.Sequential)]
    public struct WinTrustFileInfo
    {
        public uint cbStruct;
        public SafeHGlobalHandle pcwszFilePath; // Updated to use SafeHandle
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WinTrustData
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public SafeHGlobalHandle pFile; // Updated to use SafeHandle
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        public IntPtr pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CRYPTCATMEMBER
    {
        public uint cbStruct;
        public IntPtr pwszReferenceTag;
        public IntPtr pwszFileName;
        public Guid gSubjectType;
        public uint fdwMemberFlags;
        public IntPtr pIndirectData;
        public uint dwCertVersion;
        public uint dwReserved1;
        public uint dwReserved2;
        public uint dwReserved3;
    }

    #endregion

    #region ole32.dll

    [StructLayout(LayoutKind.Explicit)]
    public struct PropVariant : IDisposable
    {
        [FieldOffset(0)]
        private ushort vt;  // Value type tag

        [FieldOffset(2)]
        private ushort wReserved1;

        [FieldOffset(4)]
        private ushort wReserved2;

        [FieldOffset(6)]
        private ushort wReserved3;

        [FieldOffset(8)]
        private IntPtr pointerValue;

        [FieldOffset(8)]
        private int int32Value;

        [FieldOffset(8)]
        private uint uint32Value;

        [FieldOffset(8)]
        private long int64Value;

        [FieldOffset(8)]
        private ulong uint64Value;

        [FieldOffset(8)]
        private double doubleValue;

        public VarEnum Type => (VarEnum)vt;

        public int AsInt32 => int32Value;

        public uint AsUInt32 => uint32Value;

        public long AsInt64 => int64Value;

        public ulong AsUInt64 => uint64Value;

        public double AsDouble => doubleValue;

        public string? AsString
        {
            get
            {
                if (Type == VarEnum.VT_LPWSTR && pointerValue != IntPtr.Zero)
                {
                    return Marshal.PtrToStringUni(pointerValue);
                }
                return null;
            }
        }

        public void Dispose()
        {
            ClearPropVariant(ref this);
        }

        private static void ClearPropVariant(ref PropVariant propVariant)
        {
            NativeMethods.PropVariantClear(propVariant);
        }
    }

    #endregion

    #region PInvoke: netapi32.dll

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct LOCALGROUP_USERS_INFO_0
    {
        public string lgrui0_name;
    }

    #endregion

    #region shared_pinvoke

    /// <summary>
    /// Represents a handle to a Windows object (HANDLE).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct HANDLE : IEquatable<HANDLE>
    {
        private readonly IntPtr handle;

        /// <summary>Initializes a new instance of the <see cref="HANDLE"/> struct.</summary>
        /// <param name="preexistingHandle">An <see cref="IntPtr"/> object that represents the pre-existing handle to use.</param>
        public HANDLE(IntPtr preexistingHandle) => handle = preexistingHandle;

        /// <summary>Returns an invalid handle by instantiating a <see cref="HANDLE"/> object with <see cref="IntPtr.Zero"/>.</summary>
        public static HANDLE NULL => new(IntPtr.Zero);

        /// <summary>Gets a value indicating whether this instance is a null handle.</summary>
        public bool IsNull => handle == IntPtr.Zero;

        /// <summary>Performs an explicit conversion from <see cref="HANDLE"/> to <see cref="IntPtr"/>.</summary>
        /// <param name="h">The handle.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator IntPtr(HANDLE h) => h.handle;

        /// <summary>Performs an implicit conversion from <see cref="IntPtr"/> to <see cref="HANDLE"/>.</summary>
        /// <param name="h">The pointer to a handle.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator HANDLE(IntPtr h) => new(h);

        /// <summary>Performs an implicit conversion from <see cref="HANDLE"/> to <see cref="SafeHandle"/>.</summary>
        /// <param name="h">The pointer to a handle.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator HANDLE(SafeHandle h) => new(h.DangerousGetHandle());

        /// <summary>Implements the operator ! which returns <see langword="true"/> if the handle is invalid.</summary>
        /// <param name="hMem">The <see cref="HANDLE"/> instance.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !(HANDLE hMem) => hMem.IsNull;

        /// <summary>Implements the operator !=.</summary>
        /// <param name="h1">The first handle.</param>
        /// <param name="h2">The second handle.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(HANDLE h1, HANDLE h2) => !(h1 == h2);

        /// <summary>Implements the operator ==.</summary>
        /// <param name="h1">The first handle.</param>
        /// <param name="h2">The second handle.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(HANDLE h1, HANDLE h2) => h1.Equals(h2);

        /// <summary>
        /// Determines whether the specified <see cref="HANDLE"/> is equal to the current <see cref="HANDLE"/>.
        /// </summary>
        /// <param name="other">The <see cref="HANDLE"/> to compare with the current <see cref="HANDLE"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="HANDLE"/> is equal to the current <see cref="HANDLE"/>; otherwise, <c>false</c>.</returns>
        public bool Equals(HANDLE other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object? obj) => obj is HANDLE h && handle == h.handle;

        public override int GetHashCode() => handle.GetHashCode();

        public IntPtr DangerousGetHandle() => handle;
    }

    /// <summary>
    /// The SECURITY_ATTRIBUTES structure contains the security descriptor for an object and specifies whether the handle retrieved by specifying this structure is inheritable. This structure provides security settings for objects created by various functions, such as CreateFile, CreatePipe, CreateProcess, RegCreateKeyEx, or RegSaveKeyEx.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        /// <summary>
        /// The size, in bytes, of this structure.
        /// This value is set by the constructor to the size of the <see cref="SECURITY_ATTRIBUTES"/> structure.
        /// </summary>
        public int nLength;

        /// <summary>
        /// A pointer to a <see cref="SECURITY_DESCRIPTOR"/> structure that controls access to the object. If the value of this member is NULL, the object is assigned the default security descriptor associated with the access token of the calling process. This is not the same as granting access to everyone by assigning a NULL discretionary access control list (DACL). By default, the default DACL in the access token of a process allows access only to the user represented by the access token.
        /// For information about creating a security descriptor, see Creating a Security Descriptor.
        /// </summary>
        public IntPtr lpSecurityDescriptor;

        /// <summary>
        /// A Boolean value that specifies whether the returned handle is inherited when a new process is created. If this member is TRUE, the new process inherits the handle.
        /// </summary>
        public int bInheritHandle;

        /// <summary>
        /// Gets a value indicating whether the returned handle is inherited when a new process is created. If this member is TRUE, the new process inherits the handle.
        /// </summary>
        public readonly bool InheritHandle => bInheritHandle != 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="SECURITY_ATTRIBUTES"/> struct.
        /// </summary>
        /// <returns>A new instance of <see cref="SECURITY_ATTRIBUTES"/>.</returns>
        public static SECURITY_ATTRIBUTES Create()
        {
            return new SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
            };
        }
    }

    /// <summary>
    /// Specifies a date and time, using individual members for the month, day, year, weekday, hour, minute, second, and millisecond. The
    /// time is either in coordinated universal time (UTC) or local time, depending on the function that is being called.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct SYSTEMTIME : IEquatable<SYSTEMTIME>, IComparable<SYSTEMTIME>
    {
        /// <summary>The year. The valid values for this member are 1601 through 30827.</summary>
        public ushort wYear;

        /// <summary>
        /// The month. This member can be one of the following values.
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>1</term>
        /// <term>January</term>
        /// </item>
        /// <item>
        /// <term>2</term>
        /// <term>February</term>
        /// </item>
        /// <item>
        /// <term>3</term>
        /// <term>March</term>
        /// </item>
        /// <item>
        /// <term>4</term>
        /// <term>April</term>
        /// </item>
        /// <item>
        /// <term>5</term>
        /// <term>May</term>
        /// </item>
        /// <item>
        /// <term>6</term>
        /// <term>June</term>
        /// </item>
        /// <item>
        /// <term>7</term>
        /// <term>July</term>
        /// </item>
        /// <item>
        /// <term>8</term>
        /// <term>August</term>
        /// </item>
        /// <item>
        /// <term>9</term>
        /// <term>September</term>
        /// </item>
        /// <item>
        /// <term>10</term>
        /// <term>October</term>
        /// </item>
        /// <item>
        /// <term>11</term>
        /// <term>November</term>
        /// </item>
        /// <item>
        /// <term>12</term>
        /// <term>December</term>
        /// </item>
        /// </list>
        /// </summary>
        public ushort wMonth;

        /// <summary>
        /// The day of the week. This member can be one of the following values.
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>0</term>
        /// <term>Sunday</term>
        /// </item>
        /// <item>
        /// <term>1</term>
        /// <term>Monday</term>
        /// </item>
        /// <item>
        /// <term>2</term>
        /// <term>Tuesday</term>
        /// </item>
        /// <item>
        /// <term>3</term>
        /// <term>Wednesday</term>
        /// </item>
        /// <item>
        /// <term>4</term>
        /// <term>Thursday</term>
        /// </item>
        /// <item>
        /// <term>5</term>
        /// <term>Friday</term>
        /// </item>
        /// <item>
        /// <term>6</term>
        /// <term>Saturday</term>
        /// </item>
        /// </list>
        /// </summary>
        public ushort wDayOfWeek;

        /// <summary>The day of the month. The valid values for this member are 1 through 31.</summary>
        public ushort wDay;

        /// <summary>The hour. The valid values for this member are 0 through 23.</summary>
        public ushort wHour;

        /// <summary>The minute. The valid values for this member are 0 through 59.</summary>
        public ushort wMinute;

        /// <summary>The second. The valid values for this member are 0 through 59.</summary>
        public ushort wSecond;

        /// <summary>The millisecond. The valid values for this member are 0 through 999.</summary>
        public ushort wMilliseconds;

        private static readonly int[] DaysToMonth365 = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
        private static readonly int[] DaysToMonth366 = { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 };

        /// <summary>Initializes a new instance of the <see cref="SYSTEMTIME"/> struct with a <see cref="DateTime"/>.</summary>
        /// <param name="dt">The <see cref="DateTime"/> value.</param>
        /// <param name="toKind">Indicates whether the <see cref="SYSTEMTIME"/> should represent the local, universal or unknown time.</param>
        /// <exception cref="ArgumentOutOfRangeException">dt - Year value must be 1601 through 30827</exception>
        public SYSTEMTIME(DateTime dt, DateTimeKind toKind = DateTimeKind.Unspecified)
        {
            dt = toKind == DateTimeKind.Local ? dt.ToLocalTime() : toKind == DateTimeKind.Utc ? dt.ToUniversalTime() : dt;
            wYear = Convert.ToUInt16(dt.Year);
            if (wYear < 1601) throw new ArgumentOutOfRangeException(nameof(dt), @"Year value must be 1601 through 30827");
            wMonth = Convert.ToUInt16(dt.Month);
            wDayOfWeek = Convert.ToUInt16(dt.DayOfWeek);
            wDay = Convert.ToUInt16(dt.Day);
            wHour = Convert.ToUInt16(dt.Hour);
            wMinute = Convert.ToUInt16(dt.Minute);
            wSecond = Convert.ToUInt16(dt.Second);
            wMilliseconds = Convert.ToUInt16(dt.Millisecond);
        }

        /// <summary>Initializes a new instance of the <see cref="SYSTEMTIME"/> struct.</summary>
        /// <param name="year">The year. The valid values for this member are 1601 through 30827.</param>
        /// <param name="month">
        /// The month. This member can be one of the following values.
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>1</term>
        /// <term>January</term>
        /// </item>
        /// <item>
        /// <term>2</term>
        /// <term>February</term>
        /// </item>
        /// <item>
        /// <term>3</term>
        /// <term>March</term>
        /// </item>
        /// <item>
        /// <term>4</term>
        /// <term>April</term>
        /// </item>
        /// <item>
        /// <term>5</term>
        /// <term>May</term>
        /// </item>
        /// <item>
        /// <term>6</term>
        /// <term>June</term>
        /// </item>
        /// <item>
        /// <term>7</term>
        /// <term>July</term>
        /// </item>
        /// <item>
        /// <term>8</term>
        /// <term>August</term>
        /// </item>
        /// <item>
        /// <term>9</term>
        /// <term>September</term>
        /// </item>
        /// <item>
        /// <term>10</term>
        /// <term>October</term>
        /// </item>
        /// <item>
        /// <term>11</term>
        /// <term>November</term>
        /// </item>
        /// <item>
        /// <term>12</term>
        /// <term>December</term>
        /// </item>
        /// </list>
        /// </param>
        /// <param name="day">The day of the month. The valid values for this member are 1 through 31.</param>
        /// <param name="hour">The hour. The valid values for this member are 0 through 23.</param>
        /// <param name="minute">The minute. The valid values for this member are 0 through 59.</param>
        /// <param name="second">The second. The valid values for this member are 0 through 59.</param>
        /// <param name="millisecond">The millisecond. The valid values for this member are 0 through 999.</param>
        public SYSTEMTIME(ushort year, ushort month, ushort day, ushort hour = 0, ushort minute = 0, ushort second = 0,
            ushort millisecond = 0)
        {
            if (year < 1601 && year != 0) throw new ArgumentOutOfRangeException(nameof(year), @"year value must be 1601 through 30827 or 0");
            wYear = year;
            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException(nameof(month), @"month value must be 1 through 12");
            wMonth = month;
            if (day < 1 || day > 31) throw new ArgumentOutOfRangeException(nameof(day), @"day value must be 1 through 31");
            wDay = day;
            if (hour > 23) throw new ArgumentOutOfRangeException(nameof(hour), @"hour value must be 0 through 23");
            wHour = hour;
            if (minute > 59) throw new ArgumentOutOfRangeException(nameof(minute), @"minute value must be 0 through 59");
            wMinute = minute;
            if (second > 59) throw new ArgumentOutOfRangeException(nameof(second), @"second value must be 0 through 59");
            wSecond = second;
            if (millisecond > 999)
                throw new ArgumentOutOfRangeException(nameof(millisecond), @"millisecond value must be 0 through 999");
            wMilliseconds = millisecond;
            wDayOfWeek = 0;
            //wDayOfWeek = (ushort)ComputedDayOfWeek;
        }

        /// <summary>Gets or sets the day of the week.</summary>
        /// <value>The day of the week.</value>
        [ExcludeFromCodeCoverage]
        public DayOfWeek DayOfWeek
        {
            readonly get => (DayOfWeek)wDayOfWeek;
            set => wDayOfWeek = (ushort)value;
        }

        /// <summary>Gets the number of ticks that represent the date and time of this instance.</summary>
        public readonly long Ticks
        {
            get
            {
                if (ToUInt64 == 0) return 0;
                var days = IsLeapYear(wYear) ? DaysToMonth366 : DaysToMonth365;
                var y = wYear - 1;
                var n = y * 365 + y / 4 - y / 100 + y / 400 + days[wMonth - 1] + wDay - 1;
                return new TimeSpan(n, wHour, wMinute, wSecond, wMilliseconds).Ticks;
            }
        }

        /// <summary>Indicates if two <see cref="SYSTEMTIME"/> values are equal.</summary>
        /// <param name="s1">The first <see cref="SYSTEMTIME"/> value.</param>
        /// <param name="s2">The second <see cref="SYSTEMTIME"/> value.</param>
        /// <returns><c>true</c> if both values are equal; otherwise <c>false</c>.</returns>
        public static bool operator ==(SYSTEMTIME s1, SYSTEMTIME s2) => s1.wYear == s2.wYear && s1.wMonth == s2.wMonth &&
                                                                        s1.wDay == s2.wDay &&
                                                                        s1.wHour == s2.wHour && s1.wMinute == s2.wMinute &&
                                                                        s1.wSecond == s2.wSecond && s1.wMilliseconds ==
                                                                        s2.wMilliseconds;

        /// <summary>Indicates if two <see cref="SYSTEMTIME"/> values are not equal.</summary>
        /// <param name="s1">The first <see cref="SYSTEMTIME"/> value.</param>
        /// <param name="s2">The second <see cref="SYSTEMTIME"/> value.</param>
        /// <returns><c>true</c> if both values are not equal; otherwise <c>false</c>.</returns>
        public static bool operator !=(SYSTEMTIME s1, SYSTEMTIME s2) => !(s1 == s2);

        /// <summary>Determines whether one specified <see cref="SYSTEMTIME"/> is greater than another specified <see cref="SYSTEMTIME"/>.</summary>
        /// <param name="s1">The first <see cref="SYSTEMTIME"/> value.</param>
        /// <param name="s2">The second <see cref="SYSTEMTIME"/> value.</param>
        /// <returns><c>true</c> if <paramref name="s1"/> is greater than <paramref name="s2"/>; otherwise, <c>false</c>.</returns>
        public static bool operator >(SYSTEMTIME s1, SYSTEMTIME s2) => s1.ToUInt64 > s2.ToUInt64;

        /// <summary>Determines whether one specified <see cref="SYSTEMTIME"/> is greater than or equal to another specified <see cref="SYSTEMTIME"/>.</summary>
        /// <param name="s1">The first <see cref="SYSTEMTIME"/> value.</param>
        /// <param name="s2">The second <see cref="SYSTEMTIME"/> value.</param>
        /// <returns><c>true</c> if <paramref name="s1"/> is greater than or equal to <paramref name="s2"/>; otherwise, <c>false</c>.</returns>
        public static bool operator >=(SYSTEMTIME s1, SYSTEMTIME s2) => s1.ToUInt64 >= s2.ToUInt64;

        /// <summary>Determines whether one specified <see cref="SYSTEMTIME"/> is less than another specified <see cref="SYSTEMTIME"/>.</summary>
        /// <param name="s1">The first <see cref="SYSTEMTIME"/> value.</param>
        /// <param name="s2">The second <see cref="SYSTEMTIME"/> value.</param>
        /// <returns><c>true</c> if <paramref name="s1"/> is less than <paramref name="s2"/>; otherwise, <c>false</c>.</returns>
        public static bool operator <(SYSTEMTIME s1, SYSTEMTIME s2) => s1.ToUInt64 < s2.ToUInt64;

        /// <summary>Determines whether one specified <see cref="SYSTEMTIME"/> is less than or equal to another specified <see cref="SYSTEMTIME"/>.</summary>
        /// <param name="s1">The first <see cref="SYSTEMTIME"/> value.</param>
        /// <param name="s2">The second <see cref="SYSTEMTIME"/> value.</param>
        /// <returns><c>true</c> if <paramref name="s1"/> is less than or equal to <paramref name="s2"/>; otherwise, <c>false</c>.</returns>
        public static bool operator <=(SYSTEMTIME s1, SYSTEMTIME s2) => s1.ToUInt64 <= s2.ToUInt64;

        /// <summary>The minimum value supported by <see cref="SYSTEMTIME"/>.</summary>
        public static readonly SYSTEMTIME MinValue = new SYSTEMTIME(1601, 1, 1);

        /// <summary>The maximum value supported by <see cref="SYSTEMTIME"/>.</summary>
        public static readonly SYSTEMTIME MaxValue = new SYSTEMTIME(30827, 12, 31, 23, 59, 59, 999);

        /// <summary>Compares two instances of <see cref="SYSTEMTIME"/> and returns an integer that indicates whether the first instance is earlier than, the same as, or later than the second instance.</summary>
        /// <param name="t1">The first object to compare. </param>
        /// <param name="t2">The second object to compare. </param>
        /// <returns>A signed number indicating the relative values of t1 and t2.
        /// <list type="table">
        /// <listheader><term>Value Type</term><term>Condition</term></listheader>
        /// <item><term>Less than zero</term><term>t1 is earlier than t2.</term></item>
        /// <item><term>Zero</term><term>t1 is the same as t2.</term></item>
        /// <item><term>Greater than zero</term><term>t1 is later than t2.</term></item>
        /// </list>
        ///</returns>
        public static int Compare(SYSTEMTIME t1, SYSTEMTIME t2)
        {
            var ticks1 = t1.ToUInt64;
            var ticks2 = t2.ToUInt64;
            if (ticks1 > ticks2) return 1;
            if (ticks1 < ticks2) return -1;
            return 0;
        }

        /// <summary>Compares the current object with another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value
        /// Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref
        /// name="other"/>. Greater than zero This object is greater than <paramref name="other"/>.
        /// </returns>
        public readonly int CompareTo(SYSTEMTIME other) => Compare(this, other);

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
        public readonly bool Equals(SYSTEMTIME other) => this == other;

        /// <summary>Determines whether the specified <see cref="object"/>, is equal to this instance.</summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override readonly bool Equals(object? obj) => base.Equals(obj);

        /// <summary>Returns a hash code for this instance.</summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override readonly int GetHashCode()
        {
            var u = ToUInt64;
            return unchecked((int)u) ^ (int)(u >> 32);
        }

        /// <summary>Converts this <see cref="SYSTEMTIME"/> instance to a <see cref="DateTime"/> instance.</summary>
        /// <param name="kind">Indicates whether this <see cref="SYSTEMTIME"/> instance is local, universal or neither.</param>
        /// <returns>An equivalent <see cref="DateTime"/> value.</returns>
        public readonly DateTime ToDateTime(DateTimeKind kind)
        {
            if (wYear == 0 || this == MinValue)
                return DateTime.MinValue;
            if (this == MaxValue)
                return DateTime.MaxValue;
            return new DateTime(wYear, wMonth, wDay, wHour, wMinute, wSecond, wMilliseconds, kind);
        }

        /// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
        /// <returns>A <see cref="string"/> that represents this instance.</returns>
        public override readonly string ToString() => ToString(DateTimeKind.Unspecified, null, null);

        /// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
        /// <returns>A <see cref="string"/> that represents this instance.</returns>
#pragma warning disable IDE0060 // Remove unused parameter
        public readonly string ToString(DateTimeKind kind, string? format, IFormatProvider? provider) => ToDateTime(kind).ToString(format, CultureInfo.CurrentCulture);
#pragma warning restore IDE0060 // Remove unused parameter

        [ExcludeFromCodeCoverage]
        private readonly DayOfWeek ComputedDayOfWeek => (DayOfWeek)((Ticks / 864000000000 + 1) % 7);

        private readonly ulong ToUInt64 => (ulong)wYear << 36 | ((ulong)wMonth & 0x000f) << 32 |
                                    ((ulong)wDay & 0x001f) << 27 | ((ulong)wHour & 0x000f) << 22 |
                                    ((ulong)wMinute & 0x003f) << 16 | ((ulong)wSecond & 0x003f) << 10 |
                                    (ulong)wMilliseconds & 0x3ff;

        private static bool IsLeapYear(ushort year) => year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
    }

    #endregion
}
