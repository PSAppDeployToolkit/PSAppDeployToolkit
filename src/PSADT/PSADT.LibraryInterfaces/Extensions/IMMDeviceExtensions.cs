using Windows.Win32;
using Windows.Win32.Media.Audio;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;

namespace PSADT.LibraryInterfaces.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="IMMDevice"/> interface to facilitate activation of COM interfaces.
    /// </summary>
    /// <remarks>This class contains methods that extend the functionality of the <see cref="IMMDevice"/>
    /// interface, enabling strongly-typed activation of COM interfaces on multimedia devices. These methods simplify
    /// interaction with multimedia devices by abstracting the activation process.</remarks>
    internal static class IMMDeviceExtensions
    {
        /// <summary>
        /// Activates a COM interface of the specified type on the given multimedia device.
        /// </summary>
        /// <remarks>This method is an extension for the <see cref="IMMDevice"/> interface, enabling
        /// activation of a specific COM interface type. The caller is responsible for ensuring that the specified type
        /// <typeparamref name="T"/> matches the interface being activated.</remarks>
        /// <typeparam name="T">The type of the COM interface to activate. Must be a class.</typeparam>
        /// <param name="device">The multimedia device on which the interface is activated.</param>
        /// <param name="dwClsCtx">The context in which the code that manages the newly activated interface will run. Typically a value from
        /// the <see cref="CLSCTX"/> enumeration.</param>
        /// <param name="pActivationParams">Optional activation parameters. Can be <see langword="null"/>.</param>
        /// <param name="ppInterface">When the method returns, contains the activated interface of type <typeparamref name="T"/>.</param>
        internal static void Activate<T>(this IMMDevice device, CLSCTX dwClsCtx, PROPVARIANT_unmanaged? pActivationParams, out T ppInterface) where T : class
        {
            Media_Audio_IMMDevice_Extensions.Activate(device, typeof(T).GUID, dwClsCtx, pActivationParams, out object ppInterfaceInner);
            ppInterface = (T)ppInterfaceInner;
        }
    }
}
