using System;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace PSADT.Registry
{
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
}
