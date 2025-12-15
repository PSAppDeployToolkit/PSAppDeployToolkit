using System;
using System.ComponentModel;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.Invoke.LibraryInterfaces
{
    /// <summary>
    /// Provides static methods for interacting with the Windows console through native kernel32.dll functions.
    /// </summary>
    /// <remarks>This class offers managed wrappers for common console operations such as allocating, freeing,
    /// and retrieving the handle of the console window. It is intended for use in scenarios where direct control over
    /// the process console is required, such as enabling console support in GUI applications or performing advanced
    /// interop tasks. All methods throw a Win32Exception if the underlying system call fails.</remarks>
    internal static class Kernel32
    {
        /// <summary>
        /// Allocates a new console for the calling process.
        /// </summary>
        /// <remarks>This method is typically used by applications that do not have a console by default,
        /// such as Windows Forms or WPF applications, to enable console input and output. If a console is already
        /// allocated for the process, this method will fail.</remarks>
        /// <returns>true if the console was successfully allocated; otherwise, false.</returns>
        /// <exception cref="Win32Exception">Thrown if the underlying system call fails. The exception's error code corresponds to the Win32 error code
        /// returned by the system.</exception>
        internal static BOOL AllocConsole()
        {
            BOOL res = PInvoke.AllocConsole();
            return !res ? throw new Win32Exception() : res;
        }

        /// <summary>
        /// Retrieves the window handle for the current console window.
        /// </summary>
        /// <remarks>This method is typically used to obtain the native window handle for interop
        /// scenarios or advanced console manipulation. The returned handle is valid only if the process has an
        /// associated console window.</remarks>
        /// <returns>An <see cref="IntPtr"/> that represents the handle to the console window. If the process does not have a
        /// console window, an exception is thrown.</returns>
        /// <exception cref="Win32Exception">Thrown if the handle to the console window cannot be retrieved.</exception>
        internal static HWND GetConsoleWindow()
        {
            HWND res = PInvoke.GetConsoleWindow();
            return res.IsNull ? throw new Win32Exception("Failed to get a handle for the console window.") : res;
        }

        /// <summary>
        /// Detaches the calling process from its console, if one is attached.
        /// </summary>
        /// <remarks>After calling this method, the process will no longer have access to its console
        /// window. Subsequent attempts to write to or read from the console may fail until a new console is allocated
        /// or attached.</remarks>
        /// <returns>true if the process was successfully detached from its console; otherwise, false.</returns>
        /// <exception cref="Win32Exception">Thrown if the underlying system call fails. The exception's error code corresponds to the Win32 error code
        /// returned by the operating system.</exception>
        internal static BOOL FreeConsole()
        {
            BOOL res = PInvoke.FreeConsole();
            return !res ? throw new Win32Exception() : res;
        }
    }
}
