using System;
using PSADT.Interop.SafeHandles;
using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests.SafeHandles
{
    /// <summary>
    /// Tests the process and thread attribute list. Allocating one is a two-step call into Windows, sizing
    /// the buffer first and initialising it second, and the sizing step deliberately tolerates the
    /// insufficient-buffer error it is expected to provoke.
    /// </summary>
    public sealed class SafeProcThreadAttributeListHandleTests
    {
        /// <summary>
        /// Verifies that a list can be allocated for a given number of attributes.
        /// </summary>
        /// <param name="count">The number of attributes to reserve room for.</param>
        [Theory]
        [InlineData(1u)]
        [InlineData(4u)]
        public void Alloc_AllocatesForTheRequestedCount(uint count)
        {
            // Act
            using SafeProcThreadAttributeListHandle handle = SafeProcThreadAttributeListHandle.Alloc(count);

            // Assert
            Assert.False(handle.IsInvalid);
            Assert.False(handle.IsClosed);
        }

        /// <summary>
        /// Verifies that a list for no attributes is refused, since Windows has nothing to size and the
        /// resulting list could hold nothing.
        /// </summary>
        [Fact]
        public void Alloc_RefusesAnEmptyList()
        {
            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => SafeProcThreadAttributeListHandle.Alloc(0));
        }

        /// <summary>
        /// Verifies that the list releases cleanly and tolerates being released twice, which matters
        /// because it has both a native teardown call and a heap free to perform in order.
        /// </summary>
        [Fact]
        public void Dispose_ReleasesIdempotently()
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
        public void Update_AcceptsAnAttribute()
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
        public void Update_RaisesARejectedAttribute()
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
        public void Update_RefusesAReleasedList()
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
