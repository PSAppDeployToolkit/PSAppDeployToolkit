using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PSADT.PInvoke
{
    /// <summary>
    /// Contains native method declarations for Win32 API calls.
    /// </summary>
    public static partial class NativeMethods
    {
        #region Fields: shared

        /// <summary>The data area passed to a system call is too small.</summary>
        public const int ERROR_INSUFFICIENT_BUFFER = 122;

        /// <summary>The program issued a command but the command length is incorrect.</summary>
        public const int ERROR_BAD_LENGTH = 24;

        #endregion

        #region Fields: advapi32.dll

        /// <summary>
        /// Error code indicating that the specified logon session does not exist.
        /// </summary>
        public const int ERROR_NO_SUCH_LOGON_SESSION = 1312;

        /// <summary>
        /// Error code indicating that the specified item was not found.
        /// </summary>
        public const int ERROR_NOT_FOUND = 1168;

        public const int ERROR_NOT_ALL_ASSIGNED = 1300;

        public const uint SE_PRIVILEGE_ENABLED = 0x00000002;

        public const uint TOKEN_DUPLICATE = 0x0002;

        public const uint TOKEN_QUERY = 0x0008;

        public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;

        public const uint SANDBOX_INERT = 0x2;

        #endregion

        #region Fields: shlwapi.dll

        public const int MAX_PATH = 260;

        #endregion

        #region Fields: kernel32.dll

        public const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;

        public const uint GENERIC_READ = 0x80000000;

        public const uint OPEN_EXISTING = 3;

        public const uint PIPE_ACCESS_DUPLEX = 0x00000003;

        public const uint PIPE_TYPE_MESSAGE = 0x00000004;

        public const uint PIPE_READMODE_MESSAGE = 0x00000002;

        public const uint PIPE_WAIT = 0x00000000;

        public const uint PIPE_UNLIMITED_INSTANCES = 255;

        #endregion

        #region Fields: wintrust.dll

        public static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

        public const uint DRIVER_ACTION_VERIFY = 0x00000001;

        #endregion

        #region Fields: user32.dll

        /// <summary>
        /// Represents the default primary monitor when retrieving monitor information.
        /// </summary>
        public const uint MONITOR_DEFAULTTOPRIMARY = 1;

        /// <summary>
        /// Constant for the message used to simulate a button click in a window.
        /// </summary>
        public const uint BM_CLICK = 0x00F5;

        /// <summary>
        /// A handle used to send a message to all top-level windows in the system.
        /// </summary>
        public static readonly IntPtr HWND_BROADCAST = new(0xffff);

        /// <summary>
        /// The system message sent to all top-level windows when a system-wide setting has changed.
        /// </summary>
        public const int WM_SETTINGCHANGE = 0x1a;

        /// <summary>
        /// The flag used in <see cref="SendMessageTimeout"/> to indicate that the function should return immediately if the receiving application is hung.
        /// </summary>
        public const int SMTO_ABORTIFHUNG = 0x0002;

        /// <summary>
        /// Event ID indicating that file type associations have changed.
        /// This is used in <see cref="SHChangeNotify"/> to notify the system of changes to file associations.
        /// </summary>
        public const int SHCNE_ASSOCCHANGED = 0x8000000;

        /// <summary>
        /// Flag used with <see cref="SHChangeNotify"/> to indicate that the function should return immediately without waiting for the change to be flushed to disk.
        /// </summary>
        public const uint SHCNF_FLUSHNOWAIT = 0x1000;

        #endregion

        #region Fields: gdi32.dll

        /// <summary>
        /// Specifies the logical pixels per inch (DPI) in the horizontal (X) direction for a display device.
        /// </summary>
        public const int LOGPIXELSX = 88;

        /// <summary>
        /// Specifies the logical pixels per inch (DPI) in the vertical (Y) direction for a display device.
        /// </summary>
        public const int LOGPIXELSY = 90;

        #endregion

        #region Fields: winsta.dll

        public const int WINSTATIONNAME_LENGTH = 32;
        public const int DOMAIN_LENGTH = 17;
        public const int USERNAME_LENGTH = 20;

        #endregion
    }
}
