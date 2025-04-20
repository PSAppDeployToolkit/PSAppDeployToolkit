using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.UserInterface.LibraryInterfaces
{
    /// <summary>
    /// DwmApi interface P/Invoke wrappers.
    /// </summary>
    internal static class DwmApi
    {
        /// <summary>
        /// Retrieves the current colorization color and opacity blend state.
        /// </summary>
        /// <param name="pcrColorization"></param>
        /// <param name="pfOpaqueBlend"></param>
        /// <returns></returns>
        internal static HRESULT DwmGetColorizationColor(out uint pcrColorization, out BOOL pfOpaqueBlend)
        {
            return PInvoke.DwmGetColorizationColor(out pcrColorization, out pfOpaqueBlend).ThrowOnFailure();
        }
    }
}
