using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Utility class containing methods to do with the OLE32 library.
    /// </summary>
    internal static class Ole32
    {
        /// <summary>
        /// Initializes the COM library on the current thread and identifies the concurrency model as single-thread apartment (STA).
        /// </summary>
        /// <param name="dwCoInit"></param>
        /// <returns></returns>
        internal static unsafe HRESULT CoInitializeEx(COINIT dwCoInit)
        {
            return PInvoke.CoInitializeEx(dwCoInit).ThrowOnFailure();
        }

        /// <summary>
        /// Closes the COM library on the current thread, unloads all DLLs loaded by the thread, frees any other resources that the thread maintains, and forces all RPC connections on the thread to close.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rclsid"></param>
        /// <param name="pUnkOuter"></param>
        /// <param name="dwClsContext"></param>
        /// <param name="ppv"></param>
        /// <returns></returns>
        internal static unsafe HRESULT CoCreateInstance<T>(in Guid rclsid, object pUnkOuter, CLSCTX dwClsContext, out T ppv) where T : class
        {
            return PInvoke.CoCreateInstance(rclsid, pUnkOuter, dwClsContext, out ppv).ThrowOnFailure();
        }
    }
}
