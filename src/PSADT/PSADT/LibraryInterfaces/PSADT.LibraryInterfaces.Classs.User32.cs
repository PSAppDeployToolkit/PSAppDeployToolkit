using System;
using PSADT.PInvokes;

namespace PSADT.LibraryInterfaces
{
    public static class User32
    {
        public static IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert)
        {
            return NativeMethods.GetSystemMenu(hWnd, bRevert);
        }

        public static bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable)
        {
            return NativeMethods.EnableMenuItem(hMenu, uIDEnableItem, uEnable);
        }

        public static bool DestroyMenu(IntPtr hMenu)
        {
            return NativeMethods.DestroyMenu(hMenu);
        }

        public static bool IsWindowVisible(IntPtr hWnd)
        {
            return NativeMethods.IsWindowVisible(hWnd);
        }

        public static bool IsWindowEnabled(IntPtr hWnd)
        {
            return NativeMethods.IsWindowEnabled(hWnd);
        }

        public static IntPtr GetForegroundWindow()
        {
            return NativeMethods.GetForegroundWindow();
        }
    }
}
