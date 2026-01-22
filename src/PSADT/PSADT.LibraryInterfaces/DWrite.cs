using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.DirectWrite;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides helper methods for creating and interacting with DirectWrite factory objects.
    /// </summary>
    internal static class DWrite
    {
        /// <summary>
        /// Creates a DirectWrite factory object of the specified type and returns a COM interface pointer for the
        /// requested interface.
        /// </summary>
        /// <remarks>The caller is responsible for releasing the returned COM object when it is no longer
        /// needed. This method throws an exception if the underlying native call fails.</remarks>
        /// <typeparam name="T">The type of the COM interface to retrieve. Must be a supported DirectWrite factory interface.</typeparam>
        /// <param name="factoryType">The type of factory object to create. Specifies whether the factory object will be shared or isolated.</param>
        /// <param name="factory">When this method returns, contains the created factory object cast to the specified interface type.</param>
        /// <returns>An HRESULT value indicating whether the factory was created successfully. Returns S_OK if successful;
        /// otherwise, an error code.</returns>
        internal static unsafe HRESULT DWriteCreateFactory<T>(DWRITE_FACTORY_TYPE factoryType, out T factory)
        {
            Guid riid = typeof(T).GUID;
            HRESULT res = PInvoke.DWriteCreateFactory(factoryType, riid, out object factoryLocal).ThrowOnFailure();
            factory = (T)factoryLocal;
            return res;
        }

        /// <summary>
        /// Creates a 32-bit OpenType tag value from four ASCII character codes.
        /// </summary>
        /// <remarks>Each character should be a valid ASCII value. The resulting tag is commonly used to
        /// identify OpenType table types or features.</remarks>
        /// <param name="a">The first character of the tag, corresponding to the least significant byte.</param>
        /// <param name="b">The second character of the tag.</param>
        /// <param name="c">The third character of the tag.</param>
        /// <param name="d">The fourth character of the tag, corresponding to the most significant byte.</param>
        /// <returns>A 32-bit unsigned integer representing the OpenType tag composed of the specified characters.</returns>
        internal static uint DWRITE_MAKE_OPENTYPE_TAG(char a, char b, char c, char d)
        {
            return (byte)a | ((uint)(byte)b << 8) | ((uint)(byte)c << 16) | ((uint)(byte)d << 24);
        }
    }
}
