using System;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace PSADT.LibraryInterfaces.Extensions
{
    /// <summary>
    /// Provides extension methods for the IShellItem2 interface to facilitate property store retrieval and related
    /// operations.
    /// </summary>
    internal static class IShellItem2Extensions
    {
        /// <summary>
        /// Retrieves a property store interface of the specified type from the shell item using the given property
        /// store flags.
        /// </summary>
        /// <remarks>The caller is responsible for ensuring that the type parameter <typeparamref
        /// name="T"/> corresponds to a valid property store interface supported by the shell item. This method performs
        /// a COM interface query and casts the result to the specified type.</remarks>
        /// <typeparam name="T">The type of the property store interface to retrieve. Must be a COM interface supported by the shell item.</typeparam>
        /// <param name="item">The shell item from which to retrieve the property store interface.</param>
        /// <param name="flags">A combination of flags that specify the behavior of the property store retrieval operation.</param>
        /// <param name="ppv">When this method returns, contains the requested property store interface of type <typeparamref name="T"/>
        /// if successful.</param>
        internal static void GetPropertyStore<T>(this IShellItem2 item, GETPROPERTYSTOREFLAGS flags, out T ppv)
        {
            Guid riid = typeof(T).GUID;
            item.GetPropertyStore(flags, in riid, out object ppvLocal);
            ppv = (T)ppvLocal;
        }
    }
}
