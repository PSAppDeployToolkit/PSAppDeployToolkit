using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace PSADT.UserInterface.LibraryInterfaces
{
    /// <summary>
    /// GDI32 interface P/Invoke wrappers.
    /// </summary>
    internal static class Gdi32
    {
        /// <summary>
        /// Managed DeleteObject wrapper with error handling.
        /// </summary>
        /// <param name="ho"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static BOOL DeleteObject(HGDIOBJ ho)
        {
            var res = PInvoke.DeleteObject(ho);
            if (!res)
            {
                throw new InvalidOperationException("The specified handle is not valid or is currently selected into a DC.");
            }
            return res;
        }

        /// <summary>
        /// Managed DeleteObject wrapper with error handling.
        /// </summary>
        /// <param name="ho"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static BOOL DeleteObject(IntPtr ho)
        {
            return DeleteObject(new HGDIOBJ(ho));
        }
    }
}
