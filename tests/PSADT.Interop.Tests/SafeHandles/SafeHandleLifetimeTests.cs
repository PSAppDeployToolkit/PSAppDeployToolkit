using System;
using System.Runtime.InteropServices;
using PSADT.Interop.SafeHandles;
using Windows.Win32.Foundation;
using Windows.Win32.System.RemoteDesktop;
using Xunit;

namespace PSADT.Interop.Tests.SafeHandles
{
    /// <summary>
    /// Tests the construction and release behaviour of the concrete handles. What matters here is that a
    /// handle refuses an unusable value up front, and that a handle which does not own what it wraps
    /// never tries to release it.
    /// </summary>
    /// <remarks>
    /// Handles are constructed non-owning wherever releasing would mean handing a fabricated value to a
    /// native free function. The one exception is SafeCoTaskMemHandle, which is given a genuine
    /// allocation so the release path can run for real. SafeFontTableHandle is not covered: it requires a
    /// live DirectWrite font face, which belongs to integration coverage rather than here.
    /// </remarks>
    public sealed class SafeHandleLifetimeTests
    {
        /// <summary>
        /// A fabricated handle value that is neither of the invalid sentinels. It is never released.
        /// </summary>
        private const nint UsableHandle = 1;

        /// <summary>
        /// Verifies that every handle taking a raw value rejects both invalid sentinels in its
        /// constructor, so an unusable handle cannot be wrapped and passed on as though it were fine.
        /// </summary>
        /// <param name="value">The sentinel expected to be rejected.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructors_RejectBothInvalidSentinels(int value)
        {
            // Arrange
            nint handle = value;

            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeNoReleaseHandle(handle));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeEnvironmentBlockHandle(handle, ownsHandle: false));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeThreadHandle(handle, ownsHandle: false));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeLsaFreeMemoryHandle(handle, 8, ownsHandle: false));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeWtsHandle(handle, 8, ownsHandle: false));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeWtsExHandle(handle, WTS_TYPE_CLASS.WTSTypeProcessInfoLevel0, 8, ownsHandle: false));
        }

        /// <summary>
        /// Verifies that the memory-backed handles also reject a length that describes no readable region,
        /// since every bounds check downstream is derived from it.
        /// </summary>
        /// <param name="length">The length expected to be rejected.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void MemoryConstructors_RejectANonPositiveLength(int length)
        {
            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeWtsHandle(UsableHandle, length, ownsHandle: false));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeLsaFreeMemoryHandle(UsableHandle, length, ownsHandle: false));
        }

        /// <summary>
        /// Verifies that a non-owning handle closes without invoking its release path. Each of these would
        /// hand a fabricated value to a native free function if ownership were honoured incorrectly, and
        /// that call would fail and surface as an exception from Dispose.
        /// </summary>
        [Fact]
        public void NonOwningHandles_CloseWithoutReleasing()
        {
            // Arrange
            using SafeEnvironmentBlockHandle environment = new(UsableHandle, ownsHandle: false);
            using SafeThreadHandle thread = new(UsableHandle, ownsHandle: false);
            using SafeNoReleaseHandle never = new(UsableHandle);

            // Act & Assert
            Assert.Null(Record.Exception(environment.Dispose));
            Assert.Null(Record.Exception(thread.Dispose));
            Assert.Null(Record.Exception(never.Dispose));
            Assert.True(environment.IsClosed);
            Assert.True(thread.IsClosed);
            Assert.True(never.IsClosed);
        }

        /// <summary>
        /// Verifies that a wrapped usable handle is not reported as invalid, which is the state every
        /// guard downstream keys off.
        /// </summary>
        [Fact]
        public void WrappedUsableHandle_IsNotReportedAsInvalid()
        {
            // Arrange
            using SafeNoReleaseHandle handle = new(UsableHandle);

            // Assert
            Assert.False(handle.IsInvalid);
            Assert.False(handle.IsClosed);
            Assert.Equal<long>(UsableHandle, handle.DangerousGetHandle().ToInt64());
        }

        /// <summary>
        /// Verifies that the two string-wrapping handles derive their length from the string rather than
        /// being told, by scanning to the terminator and scaling to bytes. Both are constructed non-owning
        /// over the same allocation, since only the network handle's release path would be wrong for it.
        /// </summary>
        [Fact]
        public void StringHandles_DeriveTheirLengthFromTheString()
        {
            // Arrange
            nint block = Marshal.StringToCoTaskMemUni("Hello");

            try
            {
                unsafe
                {
                    // Act
                    using SafeCoTaskMemHandle taskMemory = new((PWSTR)(char*)block, ownsHandle: false);
                    using SafeNetApiBufferFreeHandle networkBuffer = new((PWSTR)(char*)block, ownsHandle: false);

                    // Assert
                    Assert.Equal(10, taskMemory.Length);
                    Assert.Equal("Hello", taskMemory.ToStringUni());
                    Assert.Equal(10, networkBuffer.Length);
                    Assert.Equal("Hello", networkBuffer.ToStringUni());
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(block);
            }
        }

        /// <summary>
        /// Verifies that both string-wrapping handles refuse a pointer holding nothing, which is how a
        /// failed native call hands one back.
        /// </summary>
        [Fact]
        public void StringHandles_RefuseAnEmptyPointer()
        {
            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => new SafeCoTaskMemHandle(default, ownsHandle: false));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => new SafeNetApiBufferFreeHandle(default, ownsHandle: false));
        }

        /// <summary>
        /// Verifies that an owning task-memory handle releases the allocation it was given. This is the one
        /// release path here that runs against a genuine allocation, so a failure would surface rather
        /// than being masked by non-ownership.
        /// </summary>
        [Fact]
        public void SafeCoTaskMemHandle_ReleasesWhatItOwns()
        {
            unsafe
            {
                // Arrange
                using SafeCoTaskMemHandle handle = new((PWSTR)(char*)Marshal.StringToCoTaskMemUni("Owned"), ownsHandle: true);

                // Act & Assert
                Assert.Null(Record.Exception(handle.Dispose));
                Assert.True(handle.IsClosed);
                Assert.Null(Record.Exception(handle.Dispose));
            }
        }

        /// <summary>
        /// Verifies that an attribute list can be allocated for a given number of attributes. The
        /// allocation is a two-step call into Windows, sizing the buffer first and initialising it second,
        /// and the sizing step deliberately tolerates the insufficient-buffer error it is expected to
        /// provoke.
        /// </summary>
        /// <param name="count">The number of attributes to reserve room for.</param>
        [Theory]
        [InlineData(1u)]
        [InlineData(4u)]
        public void SafeProcThreadAttributeListHandle_AllocatesForTheRequestedCount(uint count)
        {
            // Act
            using SafeProcThreadAttributeListHandle handle = SafeProcThreadAttributeListHandle.Alloc(count);

            // Assert
            Assert.False(handle.IsInvalid);
            Assert.False(handle.IsClosed);
        }

        /// <summary>
        /// Verifies that an attribute list for no attributes is refused, since Windows has nothing to size
        /// and the resulting list could hold nothing.
        /// </summary>
        [Fact]
        public void SafeProcThreadAttributeListHandle_RefusesAnEmptyList()
        {
            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => SafeProcThreadAttributeListHandle.Alloc(0));
        }

        /// <summary>
        /// Verifies that the list releases cleanly and tolerates being released twice, which matters
        /// because it has both a native teardown call and a heap free to perform in order.
        /// </summary>
        [Fact]
        public void SafeProcThreadAttributeListHandle_ReleasesIdempotently()
        {
            // Arrange
            SafeProcThreadAttributeListHandle handle = SafeProcThreadAttributeListHandle.Alloc(1);

            // Act & Assert
            Assert.Null(Record.Exception(handle.Dispose));
            Assert.Null(Record.Exception(handle.Dispose));
            Assert.True(handle.IsClosed);
        }

        /// <summary>
        /// Verifies that an attribute can be written into an allocated list, which is the only reason the
        /// list exists. The value written is a child-process policy, which affects nothing until a process
        /// is actually created with the list.
        /// </summary>
        [Fact]
        public void SafeProcThreadAttributeListHandle_AcceptsAnAttributeUpdate()
        {
            // Arrange
            using SafeProcThreadAttributeListHandle handle = SafeProcThreadAttributeListHandle.Alloc(1);
            byte[] policy = BitConverter.GetBytes(1u);

            // Act
            BOOL result = handle.Update(PROC_THREAD_ATTRIBUTE.PROC_THREAD_ATTRIBUTE_CHILD_PROCESS_POLICY, policy);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that a rejected update is raised as an exception rather than reported as a false
        /// return, which is the only signal a caller gets that the attribute did not take. The value here
        /// is one byte where the policy attribute requires four.
        /// </summary>
        [Fact]
        public void SafeProcThreadAttributeListHandle_RaisesARejectedUpdate()
        {
            // Arrange
            using SafeProcThreadAttributeListHandle handle = SafeProcThreadAttributeListHandle.Alloc(1);
            byte[] undersized = [1];

            // Act
            Exception? exception = Record.Exception(() => handle.Update(PROC_THREAD_ATTRIBUTE.PROC_THREAD_ATTRIBUTE_CHILD_PROCESS_POLICY, undersized));

            // Assert
            Assert.NotNull(exception);
            Assert.IsNotType<InvalidOperationException>(exception);
        }

        /// <summary>
        /// Verifies that updating a released list is refused rather than writing through a dangling
        /// pointer.
        /// </summary>
        [Fact]
        public void SafeProcThreadAttributeListHandle_RefusesAnUpdateAfterRelease()
        {
            // Arrange
            SafeProcThreadAttributeListHandle handle = SafeProcThreadAttributeListHandle.Alloc(1);
            handle.Dispose();
            byte[] policy = BitConverter.GetBytes(1u);

            // Assert
            _ = Assert.Throws<InvalidOperationException>(() => handle.Update(PROC_THREAD_ATTRIBUTE.PROC_THREAD_ATTRIBUTE_CHILD_PROCESS_POLICY, policy));
        }
    }
}
