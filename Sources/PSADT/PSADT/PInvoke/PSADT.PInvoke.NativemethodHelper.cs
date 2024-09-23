using System;
using System.Text;
using System.Runtime.InteropServices;


namespace PSADT.PInvoke
{
    internal class NativeMethodHelper
    {
        /// <summary>
        /// Loads a string resource from the executable file associated with a specified module.
        /// </summary>
        /// <param name="hInstance">
        /// A handle to an instance of the module whose executable file contains the string resource. To get the handle to the application
        /// itself, call the GetModuleHandle function with NULL.
        /// </param>
        /// <param name="uID">The identifier of the string to be loaded.</param>
        /// <returns>The loaded string resource if successful.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the string resource cannot be loaded.</exception>
        public static string LoadString(SafeLibraryHandle hInstance, int uID)
        {
            const int bufferSize = 255;
            StringBuilder buffer = new StringBuilder(bufferSize);

            int result = NativeMethods.LoadString(hInstance, uID, buffer, buffer.Capacity);

            if (result == 0)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to load string for resource ID {uID}. Error: {errorCode}");
            }

            return buffer.ToString(0, result);
        }

    }
}
