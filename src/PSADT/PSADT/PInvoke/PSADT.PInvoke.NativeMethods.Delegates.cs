using System;

namespace PSADT.PInvoke
{
    /// <summary>
    /// Contains native method delegate declarations for Win32 API calls.
    /// </summary>
    internal static partial class NativeMethods
    {
        #region user32.dll

        /// <summary>
        /// Delegate for enumerating windows.
        /// </summary>
        /// <param name="hWnd">Handle to a window.</param>
        /// <param name="lItems">A reference to an application-defined value.</param>
        /// <returns>True to continue enumeration, false to stop.</returns>
        public delegate bool EnumWindowsProcD(IntPtr hWnd, ref IntPtr lItems);

        /// <summary>
        /// Delegate for enumerating child windows.
        /// </summary>
        /// <param name="hWnd">Handle to a window.</param>
        /// <param name="lParam">A pointer to application-defined data.</param>
        /// <returns>True to continue enumeration, false to stop.</returns>
        public delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);

        #endregion
    }
}
