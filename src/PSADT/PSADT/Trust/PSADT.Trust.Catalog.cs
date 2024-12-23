using System;
using System.Runtime.InteropServices;
using PSADT.PInvokes;
using Microsoft.Win32.SafeHandles;

namespace PSADT.Trust
{
    /// <summary>
    /// Class responsible for verifying catalog signatures of files.
    /// </summary>
    public class CatalogSignature : IDisposable
    {
        private SafeCatAdminHandle _hCatAdmin;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogSignature"/> class.
        /// </summary>
        /// <param name="verifyDriver">Indicates whether to use the DRIVER_ACTION_VERIFY flag for driver verification.</param>
        public CatalogSignature(bool verifyDriver = false)
        {
            _hCatAdmin = new SafeCatAdminHandle(); // Initialize with a new instance
            if (!Initialize(verifyDriver))
            {
                throw new Exception("Failed to initialize Catalog Verifier.");
            }
        }

        /// <summary>
        /// Initializes the catalog verification context.
        /// </summary>
        /// <param name="verifyDriver">Indicates whether to use the DRIVER_ACTION_VERIFY flag for driver verification.</param>
        /// <returns>True if the context is successfully acquired; otherwise, false.</returns>
        private bool Initialize(bool verifyDriver)
        {
            var subsystem = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
            uint flags = verifyDriver ? NativeMethods.DRIVER_ACTION_VERIFY : 0;
            return NativeMethods.CryptCATAdminAcquireContext(ref _hCatAdmin, ref subsystem, flags);
        }

        /// <summary>
        /// Creates a <see cref="SafeFileHandle"/> for the specified file.
        /// </summary>
        /// <param name="filePath">The file path of the file to open.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the opened file.</returns>
        public static SafeFileHandle CreateFile(string filePath)
        {
            return NativeMethods.CreateFile(filePath, NativeMethods.GENERIC_READ, 0, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
        }

        /// <summary>
        /// Verifies the catalog signature of the specified file.
        /// </summary>
        /// <param name="filePath">The file path of the file to verify.</param>
        /// <returns>True if the catalog signature is valid; otherwise, false.</returns>
        public bool Verify(string filePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CatalogSignature));

            using (var hFile = CreateFile(filePath))
            {
                if (hFile.IsInvalid)
                {
                    return false;
                }

                uint hashSize = 0;
                byte[] emptyHash = Array.Empty<byte>();
                NativeMethods.CryptCATAdminCalcHashFromFileHandle(hFile, ref hashSize, emptyHash, 0);
                byte[] hash = new byte[hashSize];
                if (!NativeMethods.CryptCATAdminCalcHashFromFileHandle(hFile, ref hashSize, hash, 0))
                {
                    return false;
                }

                IntPtr hCatInfo = NativeMethods.CryptCATAdminEnumCatalogFromHash(_hCatAdmin, hash, hashSize, 0, IntPtr.Zero);
                if (hCatInfo != IntPtr.Zero)
                {
                    // Verify the catalog signature using WinVerifyTrust
                    using var filePathHandle = new SafeHGlobalHandle(Marshal.StringToCoTaskMemUni(filePath));
                    using var fileInfoHandle = new SafeHGlobalHandle(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustFileInfo))));

                    var fileInfo = new WinTrustFileInfo()
                    {
                        cbStruct = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo)),
                        pcwszFilePath = filePathHandle,
                        hFile = IntPtr.Zero,
                        pgKnownSubject = IntPtr.Zero
                    };

                    Marshal.StructureToPtr(fileInfo, fileInfoHandle.DangerousGetHandle(), false);

                    var trustData = new WinTrustData()
                    {
                        cbStruct = (uint)Marshal.SizeOf(typeof(WinTrustData)),
                        dwUIChoice = 2,    // WTD_UI_NONE
                        fdwRevocationChecks = 0,
                        dwUnionChoice = 1, // WTD_CHOICE_FILE
                        pFile = fileInfoHandle
                    };

                    int result = NativeMethods.WinVerifyTrust(IntPtr.Zero, NativeMethods.WINTRUST_ACTION_GENERIC_VERIFY_V2, ref trustData);
                    return result == 0; // 0 means the catalog signature is valid
                }

                return false;
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="CatalogSignature"/> object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if called from Dispose, false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Dispose unmanaged resources
                if (_hCatAdmin != null && !_hCatAdmin.IsInvalid)
                {
                    _hCatAdmin.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer to ensure resources are cleaned up if Dispose is not called.
        /// </summary>
        ~CatalogSignature()
        {
            Dispose(false);
        }
    }
}
