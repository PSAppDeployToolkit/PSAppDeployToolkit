using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides static methods for initializing the COM library and creating COM objects on the current thread.
    /// </summary>
    internal static class Ole32
    {
        /// <summary>
        /// Initializes the COM library for use by the calling thread with the specified concurrency model.
        /// </summary>
        /// <remarks>This method should be called before making any COM calls on the current thread. The
        /// concurrency model specified by the dwCoInit parameter cannot be changed once set for a thread. Calling this
        /// method multiple times on the same thread without uninitializing may result in a failure code.</remarks>
        /// <param name="dwCoInit">A value that specifies the concurrency model and initialization options for the COM library. This parameter
        /// determines how COM objects are managed on the calling thread.</param>
        /// <returns>An HRESULT value indicating the success or failure of the COM initialization. Returns S_OK if the COM
        /// library was initialized successfully, S_FALSE if it was already initialized on this thread, or an error code
        /// if initialization failed.</returns>
        internal static unsafe HRESULT CoInitializeEx(COINIT dwCoInit)
        {
            return PInvoke.CoInitializeEx(dwCoInit).ThrowOnFailure();
        }

        /// <summary>
        /// Creates a single uninitialized object of the class associated with a specified CLSID and retrieves a pointer
        /// to the requested interface.
        /// </summary>
        /// <remarks>This method wraps the native CoCreateInstance function and throws an exception if the
        /// operation fails. The caller is responsible for ensuring that the COM runtime is initialized on the current
        /// thread before calling this method.</remarks>
        /// <typeparam name="T">The interface type to retrieve from the created COM object. Must be a class interface.</typeparam>
        /// <param name="rclsid">The CLSID of the COM class object to be created.</param>
        /// <param name="pUnkOuter">If the object is being created as part of an aggregate, a pointer to the controlling IUnknown interface;
        /// otherwise, null.</param>
        /// <param name="dwClsContext">The execution context in which the code that manages the newly created object will run.</param>
        /// <param name="ppv">When this method returns, contains the interface pointer requested in type parameter T if the call is
        /// successful; otherwise, null.</param>
        /// <returns>An HRESULT value indicating the success or failure of the operation.</returns>
        internal static unsafe HRESULT CoCreateInstance<T>(in Guid rclsid, object? pUnkOuter, CLSCTX dwClsContext, out T ppv) where T : class
        {
            return PInvoke.CoCreateInstance(rclsid, pUnkOuter, dwClsContext, out ppv).ThrowOnFailure();
        }
    }
}
