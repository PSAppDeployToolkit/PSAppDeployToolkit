using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PSADT.WindowManagement;
using Xunit;

namespace PSADT.Tests.WindowManagement
{
    /// <summary>
    /// Tests finding the visible windows belonging to processes, with the filters a caller narrows them
    /// by.
    /// </summary>
    /// <remarks>
    /// Nothing here asserts that a particular window is found: what is on screen depends entirely on what
    /// is running, and a run on a build agent may see nothing at all. So the tests fall into two groups -
    /// the filters that must be refused, which hold on any machine, and the shape of whatever is found,
    /// which holds whether that is one window or none.
    /// <para>
    /// The refusals are worth pinning for a reason beyond the message: this returns an iterator, and a
    /// method whose validation lives in the iterator body would defer every one of those checks until the
    /// caller began enumerating - by which point the caller has lost the context the mistake was made in.
    /// The tests below never enumerate, so a check that moved into the iterator would stop being seen.
    /// </para>
    /// </remarks>
    public sealed class WindowUtilitiesTests
    {
        /// <summary>
        /// Verifies that an empty filter list is refused as soon as it is passed, for each of the filters
        /// that takes one.
        /// </summary>
        /// <remarks>
        /// An empty list is a caller that meant to narrow the search and lost the contents of what it was
        /// narrowing by, which would otherwise read as "do not narrow at all" and return everything.
        /// </remarks>
        [Fact]
        public void GetProcessWindowInfo_RefusesAnEmptyFilterImmediately()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => WindowUtilities.GetProcessWindowInfo(parentProcesses: []));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => WindowUtilities.GetProcessWindowInfo(parentProcessFilter: []));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => WindowUtilities.GetProcessWindowInfo(parentProcessIdFilter: []));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => WindowUtilities.GetProcessWindowInfo(parentProcessMainWindowHandleFilter: []));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => WindowUtilities.GetProcessWindowInfo(windowHandleFilter: []));
        }

        /// <summary>
        /// Verifies that a title pattern with no content is refused as soon as it is passed, since one
        /// matches every window and cannot be what the caller meant.
        /// </summary>
        /// <param name="windowTitleRegex">The blank pattern to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetProcessWindowInfo_RefusesABlankTitlePatternImmediately(string windowTitleRegex)
        {
            _ = Assert.Throws<ArgumentException>(() => WindowUtilities.GetProcessWindowInfo(windowTitleRegex: windowTitleRegex));
        }

        /// <summary>
        /// Verifies that omitting every filter is allowed, since that is how a caller asks for all of the
        /// visible windows.
        /// </summary>
        [Fact]
        public void GetProcessWindowInfo_AllowsNoFiltersAtAll()
        {
            Assert.Null(Record.Exception(static () => _ = WindowUtilities.GetProcessWindowInfo().ToList()));
        }

        /// <summary>
        /// Verifies that every window reported is described completely, since each one may be shown to a
        /// person and acted on afterwards.
        /// </summary>
        [Fact]
        public void GetProcessWindowInfo_DescribesEveryWindowItReports()
        {
            Assert.All(WindowUtilities.GetProcessWindowInfo(), static window =>
            {
                Assert.False(string.IsNullOrWhiteSpace(window.WindowTitle));
                Assert.NotEqual(0, window.WindowHandle);
                Assert.False(string.IsNullOrWhiteSpace(window.ParentProcess));
                Assert.True(window.ParentProcessId > 0, "A window was attributed to a process with no identifier.");
            });
        }

        /// <summary>
        /// Verifies that a window handle nothing owns matches nothing, which is the filter doing its work
        /// rather than being ignored.
        /// </summary>
        [Fact]
        public void GetProcessWindowInfo_ReportsNothingForAHandleThatOwnsNoWindow()
        {
            Assert.Empty(WindowUtilities.GetProcessWindowInfo(windowHandleFilter: [int.MaxValue]));
        }

        /// <summary>
        /// Verifies that a title pattern nothing can match reports nothing.
        /// </summary>
        [Fact]
        public void GetProcessWindowInfo_ReportsNothingForATitleThatCannotMatch()
        {
            Assert.Empty(WindowUtilities.GetProcessWindowInfo(windowTitleRegex: "^PSADTNoSuchWindowTitleForTesting$"));
        }

        /// <summary>
        /// Verifies that a process name nothing is running under matches nothing.
        /// </summary>
        [Fact]
        public void GetProcessWindowInfo_ReportsNothingForAProcessThatIsNotRunning()
        {
            Assert.Empty(WindowUtilities.GetProcessWindowInfo(parentProcessFilter: ["PSADTNoSuchProcessNameForTesting"]));
        }

        /// <summary>
        /// Verifies that narrowing by process name reports only that process's windows, so a caller asking
        /// about one application is not shown another's.
        /// </summary>
        [Fact]
        public void GetProcessWindowInfo_NarrowsByProcessName()
        {
            // Arrange: whichever process owns the first window found, if any
            if (WindowUtilities.GetProcessWindowInfo().FirstOrDefault() is not WindowInfo first)
            {
                // Nothing is on screen, so there is nothing to narrow.
                return;
            }

            // Act
            IReadOnlyList<WindowInfo> narrowed = [.. WindowUtilities.GetProcessWindowInfo(parentProcessFilter: [first.ParentProcess])];

            // Assert
            Assert.All(narrowed, window => Assert.Equal(first.ParentProcess, window.ParentProcess, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Verifies that narrowing by process identifier reports only that process's windows.
        /// </summary>
        [Fact]
        public void GetProcessWindowInfo_NarrowsByProcessIdentifier()
        {
            // Arrange
            if (WindowUtilities.GetProcessWindowInfo().FirstOrDefault() is not WindowInfo first)
            {
                return;
            }

            // Act
            IReadOnlyList<WindowInfo> narrowed = [.. WindowUtilities.GetProcessWindowInfo(parentProcessIdFilter: [first.ParentProcessId])];

            // Assert
            Assert.All(narrowed, window => Assert.Equal(first.ParentProcessId, window.ParentProcessId));
        }

        /// <summary>
        /// Verifies that a supplied list of processes is used as given, rather than the running processes
        /// being enumerated again behind the caller's back.
        /// </summary>
        [Fact]
        public void GetProcessWindowInfo_UsesTheProcessesItIsGiven()
        {
            // Arrange: the test host, which is a console process and so owns no visible window
            using Process current = Process.GetCurrentProcess();

            // Act & Assert
            Assert.Empty(WindowUtilities.GetProcessWindowInfo(parentProcesses: [current]));
        }

        /// <summary>
        /// Verifies that the options object is passed through to the same filtering, since it is the form
        /// the module hands filters over in.
        /// </summary>
        [Fact]
        public void GetProcessWindowInfo_AppliesTheFiltersOnTheOptions()
        {
            // Arrange
            WindowInfoOptions options = new(
                windowTitleRegex: "^PSADTNoSuchWindowTitleForTesting$",
                windowHandleFilter: null,
                parentProcessFilter: null,
                parentProcessIdFilter: null,
                parentProcessMainWindowHandleFilter: null);

            // Act & Assert
            Assert.Empty(WindowUtilities.GetProcessWindowInfo(options));
        }
    }
}
