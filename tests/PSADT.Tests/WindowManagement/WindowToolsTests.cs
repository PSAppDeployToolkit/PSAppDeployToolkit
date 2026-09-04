using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PSADT.WindowManagement;
using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Tests.WindowManagement
{
    /// <summary>
    /// Tests the thin wrappers around the window enumeration the rest of this namespace is built on.
    /// </summary>
    /// <remarks>
    /// Only the reading members are covered. Bringing a window to the front takes the focus away from
    /// whatever the person at the machine was doing and attaches thread input to do it, which is a change
    /// to the machine's state rather than a query about it.
    /// <para>
    /// Nothing asserts that a particular window exists, since the set of windows depends entirely on what
    /// happens to be running. Nor does anything assume a window enumerated a moment ago is still there:
    /// these wrappers are loud rather than forgiving, and a window closing between being listed and being
    /// asked about produces an exception rather than an empty answer. The tests below allow for that
    /// wherever they walk over what was enumerated.
    /// </para>
    /// </remarks>
    public sealed class WindowToolsTests
    {
        /// <summary>
        /// Verifies that the enumeration yields real, distinct handles, since the callers above it look
        /// each one up and would do so twice for a duplicate.
        /// </summary>
        [Fact]
        public void EnumWindows_YieldsDistinctRealHandles()
        {
            // Act
            ReadOnlyCollection<HWND> windows = WindowTools.EnumWindows();

            // Assert
            Assert.DoesNotContain(HWND.Null, windows);
            Assert.Equal(windows.Count, windows.Distinct().Count());
        }

        /// <summary>
        /// Verifies that a handle naming no window is reported as such rather than refused, since
        /// answering that is the whole point of the test - it is what lets a caller pass over a window
        /// that has closed since it was enumerated.
        /// </summary>
        [Fact]
        public void IsWindow_ReportsAHandleThatNamesNoWindow()
        {
            Assert.False(WindowTools.IsWindow(InvalidWindowHandle));
            Assert.False(WindowTools.IsWindow(HWND.Null));
        }

        /// <summary>
        /// Verifies that the handles the enumeration yields are windows, since a caller checking one
        /// before using it would otherwise pass over every window on the machine.
        /// </summary>
        /// <remarks>
        /// Asserted of the set rather than of every handle: a window enumerated a moment ago may have
        /// closed by the time it is asked about, and that is exactly the case being catered for.
        /// </remarks>
        [Fact]
        public void IsWindow_ConfirmsEnumeratedWindows()
        {
            // Act
            ReadOnlyCollection<HWND> windows = WindowTools.EnumWindows();

            // Assert
            if (windows.Count > 0)
            {
                Assert.Contains(windows, static window => WindowTools.IsWindow(window));
            }
        }

        /// <summary>
        /// Verifies that a handle of nothing at all is refused rather than being asked about, since it
        /// cannot name a window and a caller passing one has lost track of what it holds.
        /// </summary>
        [Fact]
        public void GetWindowText_RefusesAHandleOfNothing()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => WindowTools.GetWindowText(HWND.Null));
        }

        /// <summary>
        /// Verifies that a handle of nothing at all is refused rather than being attributed to a process.
        /// </summary>
        [Fact]
        public void GetWindowThreadProcessId_RefusesAHandleOfNothing()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => WindowTools.GetWindowThreadProcessId(HWND.Null));
        }

        /// <summary>
        /// Verifies that a handle that names no window fails loudly rather than being reported as a window
        /// with no text, which a caller would then treat as an untitled window it could act on.
        /// </summary>
        [Fact]
        public void GetWindowText_FailsForAHandleThatNamesNoWindow()
        {
            Assert.NotNull(Record.Exception(static () => WindowTools.GetWindowText(InvalidWindowHandle)));
        }

        /// <summary>
        /// Verifies that a handle that names no window is not attributed to a process.
        /// </summary>
        [Fact]
        public void GetWindowThreadProcessId_FailsForAHandleThatNamesNoWindow()
        {
            Assert.NotNull(Record.Exception(static () => WindowTools.GetWindowThreadProcessId(InvalidWindowHandle)));
        }

        /// <summary>
        /// Verifies that a window's text, where there is any, has had its surrounding space removed and is
        /// never blank - the distinction the wrapper exists to make, since a window titled with spaces is
        /// no more use to a caller than one with no title at all.
        /// </summary>
        [Fact]
        public void GetWindowText_ReportsTrimmedTextOrNothing()
        {
            Assert.All(TextOfEveryWindowStillOpen(), static text => Assert.Equal(text.Trim(), text));
        }

        /// <summary>
        /// Verifies that each enumerated window is attributed to a process, since a window that belongs to
        /// nothing cannot be shown to a person as something to close.
        /// </summary>
        [Fact]
        public void GetWindowThreadProcessId_AttributesEnumeratedWindows()
        {
            // Act
            List<uint> attributed = [];
            foreach (HWND window in WindowTools.EnumWindows())
            {
                uint processId = 0;
                if (Record.Exception(() => processId = WindowTools.GetWindowThreadProcessId(window)) is null)
                {
                    attributed.Add(processId);
                }
            }

            // Assert
            Assert.All(attributed, static processId => Assert.True(processId > 0, "A window was attributed to a process with no identifier."));
        }

        /// <summary>
        /// A handle that is not nothing, but names no window either.
        /// </summary>
        private static readonly HWND InvalidWindowHandle = (HWND)(nint)int.MaxValue;

        /// <summary>
        /// The text of every enumerated window that was still open by the time it was asked, and that had
        /// any text to report.
        /// </summary>
        /// <returns>The window titles.</returns>
        private static IReadOnlyList<string> TextOfEveryWindowStillOpen()
        {
            List<string> titles = [];
            foreach (HWND window in WindowTools.EnumWindows())
            {
                string? title = null;
                if (Record.Exception(() => title = WindowTools.GetWindowText(window)) is null && title is not null)
                {
                    titles.Add(title);
                }
            }
            return titles;
        }
    }
}
