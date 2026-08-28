using System;
using System.Runtime.InteropServices;
using PSADT.Interop.SafeHandles;
using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests.SafeHandles
{
    /// <summary>
    /// Tests the wrapper for a string allocated in task memory. It is given its length rather than told
    /// it, by scanning the string to its terminator.
    /// </summary>
    public sealed class SafeCoTaskMemHandleTests
    {
        /// <summary>
        /// Verifies that a pointer holding nothing is refused, which is how a failed native call hands one
        /// back.
        /// </summary>
        [Fact]
        public void Constructor_RefusesAnEmptyPointer()
        {
            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => new SafeCoTaskMemHandle(default, ownsHandle: false));
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
                    using SafeCoTaskMemHandle handle = new((PWSTR)(char*)block, ownsHandle: false);

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

        /// <summary>
        /// Verifies that an owning handle releases the allocation it was given. This runs against a genuine
        /// allocation rather than a fabricated pointer, so a failure in the release path surfaces here
        /// rather than being masked by non-ownership.
        /// </summary>
        [Fact]
        public void Dispose_ReleasesWhatItOwns()
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
    }
}
