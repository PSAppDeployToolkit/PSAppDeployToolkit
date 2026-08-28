using System;
using PSADT.WindowManagement;
using Xunit;

namespace PSADT.Tests.WindowManagement
{
    /// <summary>
    /// Tests the description of a single window.
    /// </summary>
    /// <remarks>
    /// This is what a caller receives for each window found and what it passes back to act on one, so it
    /// crosses the boundary between finding and doing. It is built from values already read off the
    /// window, so what is worth pinning is that it keeps them and that two descriptions of the same
    /// window compare equal - which is what lets a caller tell whether the set of windows has changed.
    /// </remarks>
    public sealed class WindowInfoTests
    {
        /// <summary>
        /// Verifies that every value handed in is the value read back.
        /// </summary>
        [Fact]
        public void WindowInfo_KeepsWhatItIsGiven()
        {
            // Act
            WindowInfo window = new("A window title", 0x1234, "notepad", 4321, 0x5678);

            // Assert
            Assert.Equal("A window title", window.WindowTitle);
            Assert.Equal(0x1234, window.WindowHandle);
            Assert.Equal("notepad", window.ParentProcess);
            Assert.Equal(4321, window.ParentProcessId);
            Assert.Equal(0x5678, window.ParentProcessMainWindowHandle);
        }

        /// <summary>
        /// Verifies that two descriptions of the same window are equal, since a caller watching for a
        /// change compares one set against another rather than window by window.
        /// </summary>
        [Fact]
        public void Equality_IsByEveryValue()
        {
            // Arrange
            WindowInfo window = new("A window title", 0x1234, "notepad", 4321, 0x5678);

            // Assert
            Assert.Equal(window, new WindowInfo("A window title", 0x1234, "notepad", 4321, 0x5678));
            Assert.NotEqual(window, new WindowInfo("Another title", 0x1234, "notepad", 4321, 0x5678));
            Assert.NotEqual(window, new WindowInfo("A window title", 0x9999, "notepad", 4321, 0x5678));
            Assert.NotEqual(window, new WindowInfo("A window title", 0x1234, "wordpad", 4321, 0x5678));
            Assert.NotEqual(window, new WindowInfo("A window title", 0x1234, "notepad", 9999, 0x5678));
            Assert.NotEqual(window, new WindowInfo("A window title", 0x1234, "notepad", 4321, 0x9999));
        }

        /// <summary>
        /// Verifies that equal descriptions hash alike, so a set of them behaves.
        /// </summary>
        [Fact]
        public void GetHashCode_AgreesWithEquality()
        {
            Assert.Equal(
                new WindowInfo("A window title", 0x1234, "notepad", 4321, 0x5678).GetHashCode(),
                new WindowInfo("A window title", 0x1234, "notepad", 4321, 0x5678).GetHashCode());
        }

        /// <summary>
        /// Verifies that a description with nothing in it is refused, since neither a window with no
        /// title nor one belonging to nothing is something a caller can act on.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void WindowInfo_RefusesABlankTitleOrProcess()
        {
            _ = Assert.Throws<ArgumentException>(static () => new WindowInfo("   ", 0x1234, "notepad", 4321, 0x5678));
            _ = Assert.Throws<ArgumentException>(static () => new WindowInfo("A window title", 0x1234, "   ", 4321, 0x5678));
            _ = Assert.Throws<ArgumentNullException>(static () => new WindowInfo(null!, 0x1234, "notepad", 4321, 0x5678));
            _ = Assert.Throws<ArgumentNullException>(static () => new WindowInfo("A window title", 0x1234, null!, 4321, 0x5678));
        }
    }
}
