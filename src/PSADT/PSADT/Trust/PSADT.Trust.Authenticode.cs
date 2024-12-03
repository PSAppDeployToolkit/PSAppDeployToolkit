using System;
using System.Runtime.InteropServices;
using PSADT.PInvoke;

namespace PSADT.Trust
{
    public static class Authenticode
    {
        /// <summary>
        /// Verifies the Authenticode signature of the specified file.
        /// </summary>
        /// <param name="filePath">The file path of the file to verify.</param>
        /// <returns>True if the Authenticode signature is valid; otherwise, false.</returns>
        public static bool Verify(string filePath)
        {
            using var filePathHandle = new SafeHGlobalHandle(Marshal.StringToCoTaskMemUni(filePath));
            using var fileInfoHandle = new SafeHGlobalHandle(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustFileInfo))));

            var fileInfo = new WinTrustFileInfo
            {
                cbStruct = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo)),
                pcwszFilePath = filePathHandle,
                hFile = IntPtr.Zero,
                pgKnownSubject = IntPtr.Zero
            };

            Marshal.StructureToPtr(fileInfo, fileInfoHandle.DangerousGetHandle(), false);

            var trustData = new WinTrustData
            {
                cbStruct = (uint)Marshal.SizeOf(typeof(WinTrustData)),
                dwUIChoice = 2,      // WTD_UI_NONE
                fdwRevocationChecks = 0,
                dwUnionChoice = 1,   // WTD_CHOICE_FILE
                pFile = fileInfoHandle
            };

            int result = NativeMethods.WinVerifyTrust(IntPtr.Zero, NativeMethods.WINTRUST_ACTION_GENERIC_VERIFY_V2, ref trustData);
            return result == 0; // 0 means the signature is valid
        }
    }
}
