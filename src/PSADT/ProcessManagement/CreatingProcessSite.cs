using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.Shell;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// The site handed to ShellExecuteEx so that any process the shell creates itself is created with the given flags.
    /// </summary>
    /// <remarks>The shell asks the site for <see cref="ICreatingProcess"/> just before it calls CreateProcess, which
    /// is the only point at which the creation flags can be changed. It ignores what the callback returns, so a
    /// refusal is recorded here for the launch to act on once the call returns.</remarks>
    /// <param name="creationFlags">The creation flags to add, such as the priority class and CREATE_SUSPENDED.</param>
    internal sealed class CreatingProcessSite(PROCESS_CREATION_FLAGS creationFlags) : CreatingProcessSite.IServiceProvider, ICreatingProcess
    {
        /// <summary>
        /// Gets the creation flags the shell is asked to add.
        /// </summary>
        internal PROCESS_CREATION_FLAGS CreationFlags { get; } = creationFlags;

        /// <summary>
        /// Gets a value indicating whether the shell asked the site before creating a process.
        /// </summary>
        internal bool Invoked { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the shell added the flags to its process.
        /// </summary>
        internal bool Applied { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the shell created its process with CREATE_SUSPENDED.
        /// </summary>
        internal bool Suspended => Applied && CreationFlags.HasFlag(PROCESS_CREATION_FLAGS.CREATE_SUSPENDED);

        /// <summary>
        /// Gets the reason the flags were not added, if the shell asked and then refused.
        /// </summary>
        internal Exception? Failure { get; private set; }

        /// <inheritdoc/>
        HRESULT IServiceProvider.QueryService(in Guid guidService, in Guid riid, out nint ppvObject)
        {
            // The shell asks for a number of services; this object only provides the one it implements.
            if (guidService != CreatingProcessService)
            {
                ppvObject = 0;
                return HRESULT.E_NOINTERFACE;
            }
            nint unknown = Marshal.GetIUnknownForObject(this);
            try
            {
                Guid iid = riid;
                return (HRESULT)Marshal.QueryInterface(unknown, ref iid, out ppvObject);
            }
            finally
            {
                _ = Marshal.Release(unknown);
            }
        }

        /// <inheritdoc/>
        void ICreatingProcess.OnCreating(ICreateProcessInputs pcpi)
        {
            Invoked = true;
            try
            {
                pcpi.AddCreateFlags((uint)CreationFlags);
                Applied = true;
            }
            catch (Exception ex)
            {
                Failure = ex;
                return;
                throw;
            }
        }

        /// <summary>
        /// The service identifier the shell asks for <see cref="ICreatingProcess"/> under, which is its interface identifier.
        /// </summary>
        private static readonly Guid CreatingProcessService = typeof(ICreatingProcess).GUID;

        /// <summary>
        /// The interface the shell queries a site through for its services.
        /// </summary>
        /// <remarks>Hand-written because CsWin32 marshals the pointer it returns as IUnknown, where the shell expects
        /// the interface it asked for.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ComInterfaceGenerator", "SYSLIB1096:Convert to 'GeneratedComInterface'", Justification = "The generated interfaces this class also implements use built-in COM interop, which is all .NET Framework has.")]
        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IServiceProvider
        {
            /// <summary>
            /// Retrieves the specified interface of the specified service.
            /// </summary>
            /// <param name="guidService">The identifier of the service being requested.</param>
            /// <param name="riid">The identifier of the interface being requested on that service.</param>
            /// <param name="ppvObject">When this method returns, contains the interface pointer, or null if it was refused.</param>
            /// <returns>S_OK if the interface was returned; otherwise, E_NOINTERFACE.</returns>
            [PreserveSig]
            HRESULT QueryService(in Guid guidService, in Guid riid, out nint ppvObject);
        }
    }
}
