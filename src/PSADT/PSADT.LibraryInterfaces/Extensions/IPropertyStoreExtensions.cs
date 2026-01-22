using PSADT.LibraryInterfaces.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace PSADT.LibraryInterfaces.Extensions
{
    /// <summary>
    /// Provides extension methods for retrieving property values from an <see cref="IPropertyStore"/> as safe handles
    /// to PROPVARIANT structures.
    /// </summary>
    internal static class IPropertyStoreExtensions
    {
        /// <summary>
        /// Retrieves the value associated with the specified property key from the property store and returns it as a
        /// safe handle to a PROPVARIANT structure.
        /// </summary>
        /// <param name="store">The property store from which to retrieve the value.</param>
        /// <param name="key">The property key that identifies the value to retrieve.</param>
        /// <param name="pv">When this method returns, contains a safe handle to the PROPVARIANT value associated with the specified
        /// property key.</param>
        internal static unsafe void GetValue(this IPropertyStore store, in PROPERTYKEY key, out SafePropVariantHandle pv)
        {
            store.GetValue(in key, out PROPVARIANT pvLocal);
            pv = new(in pvLocal, true);

        }
    }
}
