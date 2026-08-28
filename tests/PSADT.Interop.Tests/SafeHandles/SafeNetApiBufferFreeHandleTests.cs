using System;
using System.Runtime.InteropServices;
using PSADT.Interop.SafeHandles;
using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests.SafeHandles
{
    /// <summary>
    /// Tests the wrapper for a string returned by the network management APIs. It derives its length the
    /// same way as the task memory wrapper but releases the block through a different function.
    /// </summary>
    /// <remarks>
    /// The handle here is constructed non-owning over ordinary task memory, because handing memory that
    /// did not come from the network management APIs to NetApiBufferFree is not something a test should
    /// do. The release path is therefore not exercised.
    /// </remarks>
    public sealed class SafeNetApiBufferFreeHandleTests
    {
        /// <summary>
        /// Verifies that a pointer holding nothing is refused, which is how a failed native call hands one
        /// back.
        /// </summary>
        [Fact]
        public void Constructor_RefusesAnEmptyPointer()
        {
            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => new SafeNetApiBufferFreeHandle(default, ownsHandle: false));
        }

        /// <summary>
        /// Verifies that the length is derived from the string by scanning to the terminator and scaling to
        /// bytes, and that the string reads back out.
        /// </summary>
        [Fact]
        public void Length_IsDerivedFromTheString()
        {
            // Arrange
            nint block = Marshal.StringToCoTaskMemUni("Hello");

            try
            {
                unsafe
                {
                    // Act
                    using SafeNetApiBufferFreeHandle handle = new((PWSTR)(char*)block, ownsHandle: false);

                    // Assert
                    Assert.Equal(10, handle.Length);
                    Assert.Equal("Hello", handle.ToStringUni());
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(block);
            }
        }
    }
}
