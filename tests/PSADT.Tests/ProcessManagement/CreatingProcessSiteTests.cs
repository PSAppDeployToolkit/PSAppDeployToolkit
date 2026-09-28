using System;
using System.Linq;
using System.Runtime.InteropServices;
using PSADT.ProcessManagement;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.Shell;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests the site handed to the shell, driven the way the shell drives it: through the COM vtables of
    /// the wrapper the runtime builds for it, rather than through the managed object.
    /// </summary>
    /// <remarks>
    /// The shell holds the site as a raw IUnknown, asks it for a service provider, asks that for the
    /// creating-process service, and calls the one method on it. Each of those crosses a vtable the runtime
    /// assembles from the interface definitions, so calling the managed methods directly would prove nothing
    /// about the part that can go wrong. Nothing here launches a process: the inputs the shell would supply
    /// are stood in for by a recording fake.
    /// </remarks>
    public sealed class CreatingProcessSiteTests
    {
        /// <summary>
        /// Verifies that asking the site for the creating-process service yields that interface itself, rather
        /// than the plain IUnknown the generated definition would have handed back.
        /// </summary>
        [Fact]
        public void QueryService_HandsBackTheCreatingProcessInterface()
        {
            // Arrange
            nint site = Marshal.GetIUnknownForObject(new CreatingProcessSite(PROCESS_CREATION_FLAGS.CREATE_SUSPENDED));
            nint expected = 0, returned = 0;
            try
            {
                expected = QueryInterface(site, CreatingProcessIid);

                // Act
                int result = QueryService(site, CreatingProcessIid, CreatingProcessIid, out returned);

                // Assert: the same pointer the wrapper itself hands out for that interface
                Assert.Equal(HRESULT.S_OK.Value, result);
                Assert.Equal(expected, returned);
            }
            finally
            {
                Release(returned, expected, site);
            }
        }

        /// <summary>
        /// Verifies that every other service the shell asks about is refused, since answering for one the site
        /// does not provide would hand the shell an object that cannot do what it was asked for.
        /// </summary>
        [Fact]
        public void QueryService_RefusesAnyOtherService()
        {
            // Arrange
            nint site = Marshal.GetIUnknownForObject(new CreatingProcessSite(PROCESS_CREATION_FLAGS.CREATE_SUSPENDED));
            nint returned = 0;
            try
            {
                // Act
                int result = QueryService(site, ServiceProviderIid, CreatingProcessIid, out returned);

                // Assert
                Assert.Equal(HRESULT.E_NOINTERFACE.Value, result);
                Assert.Equal(0, returned);
            }
            finally
            {
                Release(returned, site);
            }
        }

        /// <summary>
        /// Verifies that the callback adds its flags to whatever the shell had already chosen and records that it
        /// did, with the suspended flag among them being what tells the launch the process needs resuming.
        /// </summary>
        [Fact]
        public void OnCreating_AddsTheFlagsAndRecordsThem()
        {
            // Arrange
            CreatingProcessSite site = new(PROCESS_CREATION_FLAGS.CREATE_SUSPENDED | PROCESS_CREATION_FLAGS.BELOW_NORMAL_PRIORITY_CLASS);
            RecordingInputs inputs = new();
            nint unknown = Marshal.GetIUnknownForObject(site);
            try
            {
                // Act
                int result = OnCreating(unknown, inputs);

                // Assert
                Assert.Equal(HRESULT.S_OK.Value, result);
                Assert.Equal((uint)(PROCESS_CREATION_FLAGS.CREATE_SUSPENDED | PROCESS_CREATION_FLAGS.BELOW_NORMAL_PRIORITY_CLASS), inputs.AddedFlags);
                Assert.True(site.Invoked);
                Assert.True(site.Applied);
                Assert.True(site.Suspended);
                Assert.Null(site.Failure);
            }
            finally
            {
                Release(unknown);
            }
        }

        /// <summary>
        /// Verifies that flags without the suspended one among them, which is what a launch the shell would wait
        /// on for DDE asks for, are recorded as applied without the process being reported as held.
        /// </summary>
        [Fact]
        public void OnCreating_RecordsFlagsWithoutSuspensionAsAppliedButNotHeld()
        {
            // Arrange
            CreatingProcessSite site = new(PROCESS_CREATION_FLAGS.BELOW_NORMAL_PRIORITY_CLASS);
            RecordingInputs inputs = new();
            nint unknown = Marshal.GetIUnknownForObject(site);
            try
            {
                // Act
                int result = OnCreating(unknown, inputs);

                // Assert
                Assert.Equal(HRESULT.S_OK.Value, result);
                Assert.Equal((uint)PROCESS_CREATION_FLAGS.BELOW_NORMAL_PRIORITY_CLASS, inputs.AddedFlags);
                Assert.True(site.Applied);
                Assert.False(site.Suspended);
            }
            finally
            {
                Release(unknown);
            }
        }

        /// <summary>
        /// Verifies that a refusal to add the flag, which reaches the callback as an exception, is recorded for the
        /// launch rather than thrown on to the shell, which discards whatever the callback answers and creates the
        /// process anyway.
        /// </summary>
        [Fact]
        public void OnCreating_RecordsAFailureToAddTheFlag()
        {
            // Arrange
            CreatingProcessSite site = new(PROCESS_CREATION_FLAGS.CREATE_SUSPENDED);
            InvalidOperationException thrown = new("the flags were refused");
            RecordingInputs inputs = new(thrown);
            nint unknown = Marshal.GetIUnknownForObject(site);
            try
            {
                // Act
                int result = OnCreating(unknown, inputs);

                // Assert
                Assert.Equal(HRESULT.S_OK.Value, result);
                Assert.True(site.Invoked);
                Assert.False(site.Applied);
                Assert.False(site.Suspended);
                Assert.Same(thrown, site.Failure);
            }
            finally
            {
                Release(unknown);
            }
        }

        /// <summary>
        /// Queries the site for an interface, failing the test if the wrapper does not expose it.
        /// </summary>
        /// <param name="unknown">The site's IUnknown.</param>
        /// <param name="iid">The interface to ask for.</param>
        /// <returns>The interface pointer, which the caller releases.</returns>
        private static nint QueryInterface(nint unknown, Guid iid)
        {
            Assert.Equal(HRESULT.S_OK.Value, Marshal.QueryInterface(unknown, ref iid, out nint pointer));
            return pointer;
        }

        /// <summary>
        /// Asks the site's service provider for a service, as the shell does.
        /// </summary>
        /// <param name="unknown">The site's IUnknown.</param>
        /// <param name="guidService">The service to ask for.</param>
        /// <param name="riid">The interface to ask for on it.</param>
        /// <param name="ppvObject">When this method returns, contains the pointer handed back, which the caller releases.</param>
        /// <returns>The result the site answered with.</returns>
        private static int QueryService(nint unknown, Guid guidService, Guid riid, out nint ppvObject)
        {
            nint provider = QueryInterface(unknown, ServiceProviderIid);
            try
            {
                return Marshal.GetDelegateForFunctionPointer<QueryServiceFunction>(Slot(provider, 3))(provider, in guidService, in riid, out ppvObject);
            }
            finally
            {
                Release(provider);
            }
        }

        /// <summary>
        /// Calls the site's creating-process callback with the given inputs, as the shell does.
        /// </summary>
        /// <param name="unknown">The site's IUnknown.</param>
        /// <param name="inputs">The inputs to hand the callback.</param>
        /// <returns>The result the site answered with.</returns>
        private static int OnCreating(nint unknown, ICreateProcessInputs inputs)
        {
            nint creatingProcess = QueryInterface(unknown, CreatingProcessIid);
            nint inputsUnknown = Marshal.GetIUnknownForObject(inputs);
            try
            {
                return Marshal.GetDelegateForFunctionPointer<OnCreatingFunction>(Slot(creatingProcess, 3))(creatingProcess, inputsUnknown);
            }
            finally
            {
                Release(inputsUnknown, creatingProcess);
            }
        }

        /// <summary>
        /// Reads a vtable slot of an interface pointer.
        /// </summary>
        /// <param name="pointer">The interface pointer.</param>
        /// <param name="index">The slot's index, where the three IUnknown methods come first.</param>
        /// <returns>The address of the method in that slot.</returns>
        private static nint Slot(nint pointer, int index)
        {
            return Marshal.ReadIntPtr(Marshal.ReadIntPtr(pointer), index * IntPtr.Size);
        }

        /// <summary>
        /// Releases the interface pointers that were obtained.
        /// </summary>
        /// <param name="pointers">The pointers to release, of which zero stands for one never obtained.</param>
        private static void Release(params nint[] pointers)
        {
            foreach (nint pointer in pointers.Where(static pointer => pointer != 0))
            {
                _ = Marshal.Release(pointer);
            }
        }

        /// <summary>
        /// The identifier of the service provider interface, which the shell queries a site for first.
        /// </summary>
        private static readonly Guid ServiceProviderIid = new(0x6D5140C1, 0x7436, 0x11CE, 0x80, 0x34, 0x00, 0xAA, 0x00, 0x60, 0x09, 0xFA);

        /// <summary>
        /// The identifier of the creating-process interface, which doubles as its service identifier.
        /// </summary>
        private static readonly Guid CreatingProcessIid = typeof(ICreatingProcess).GUID;

        /// <summary>
        /// The shape of the service provider's one method, called through its vtable.
        /// </summary>
        /// <param name="self">The interface pointer the method is called on.</param>
        /// <param name="guidService">The service being asked for.</param>
        /// <param name="riid">The interface being asked for on it.</param>
        /// <param name="ppvObject">When the method returns, contains the pointer handed back.</param>
        /// <returns>The result the method answered with.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int QueryServiceFunction(nint self, in Guid guidService, in Guid riid, out nint ppvObject);

        /// <summary>
        /// The shape of the creating-process callback, called through its vtable.
        /// </summary>
        /// <param name="self">The interface pointer the method is called on.</param>
        /// <param name="pcpi">The inputs handed to the callback.</param>
        /// <returns>The result the method answered with.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int OnCreatingFunction(nint self, nint pcpi);

        /// <summary>
        /// Stands in for the inputs the shell supplies, recording the flags added and refusing them as told.
        /// </summary>
        /// <param name="failure">An exception to raise for an added flag, if any.</param>
        private sealed class RecordingInputs(Exception? failure = null) : ICreateProcessInputs
        {
            /// <summary>
            /// Gets the creation flags added so far.
            /// </summary>
            public uint AddedFlags { get; private set; }

            /// <inheritdoc/>
            public void AddCreateFlags(uint dwCreationFlags)
            {
                AddedFlags |= dwCreationFlags;
                if (failure is not null)
                {
                    throw failure;
                }
            }

            /// <inheritdoc/>
            public void GetCreateFlags(out uint pdwCreationFlags)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public void SetCreateFlags(uint dwCreationFlags)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public void SetHotKey(ushort wHotKey)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public void AddStartupFlags(uint dwStartupInfoFlags)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public void SetTitle(PCWSTR pszTitle)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public void SetEnvironmentVariable(PCWSTR pszName, PCWSTR pszValue)
            {
                throw new NotSupportedException();
            }
        }
    }
}
