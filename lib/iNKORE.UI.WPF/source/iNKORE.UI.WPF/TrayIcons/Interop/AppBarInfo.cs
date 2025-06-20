// Some interop code taken from Mike Marshall's AnyForm

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace iNKORE.UI.WPF.TrayIcons.Interop
{
    /// <summary>
    /// This contains the logic to access the location of the app bar and communicate with it.
    /// </summary>
    public class AppBarInfo
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("shell32.dll")]
        private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA data);

        [DllImport("user32.dll")]
        private static extern int SystemParametersInfo(uint uiAction, uint uiParam,
            IntPtr pvParam, uint fWinIni);

        private const int ABM_GETTASKBARPOS = 0x00000005;


        private APPBARDATA m_data;

        /// <summary>
        /// Get on which edge the app bar is located
        /// </summary>
        public ScreenEdge Edge
        {
            get { return (ScreenEdge) m_data.uEdge; }
        }

        /// <summary>
        /// Get the working area
        /// </summary>
        public Rectangle WorkArea
        {
            get { return GetRectangle(m_data.rc); }
        }

        private Rectangle GetRectangle(RECT rc)
        {
            return new Rectangle(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
        }

        /// <summary>
        /// Update the location of the appbar with the specified classname and window name.
        /// </summary>
        /// <param name="strClassName">string</param>
        /// <param name="strWindowName">string</param>
        private void GetPosition(string strClassName, string strWindowName)
        {
            m_data = new APPBARDATA();
            m_data.cbSize = (uint) Marshal.SizeOf(m_data.GetType());

            IntPtr hWnd = FindWindow(strClassName, strWindowName);

            if (hWnd != IntPtr.Zero)
            {
                uint uResult = SHAppBarMessage(ABM_GETTASKBARPOS, ref m_data);

                if (uResult != 1)
                {
                    throw new Exception("Failed to communicate with the given AppBar");
                }
            }
            else
            {
                throw new Exception("Failed to find an AppBar that matched the given criteria");
            }
        }

        /// <summary>
        /// Updates the system taskbar position
        /// </summary>
        public void GetSystemTaskBarPosition()
        {
            GetPosition("Shell_TrayWnd", null);
        }

        /// <summary>
        /// A value that specifies an edge of the screen.
        /// </summary>
        public enum ScreenEdge
        {
            /// <summary>
            /// Undefined
            /// </summary>
            Undefined = -1,
            /// <summary>
            /// Left edge.
            /// </summary>
            Left = 0,
            /// <summary>
            /// Top edge.
            /// </summary>
            Top = 1,
            /// <summary>
            /// Right edge.
            /// </summary>
            Right = 2,
            /// <summary>
            /// Bottom edge.
            /// </summary>
            Bottom = 3
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}