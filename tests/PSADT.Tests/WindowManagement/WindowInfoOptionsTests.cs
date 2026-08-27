using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using PSADT.WindowManagement;
using Xunit;

namespace PSADT.Tests.WindowManagement
{
    /// <summary>
    /// Tests the window enumeration filters and the window records they select.
    /// </summary>
    /// <remarks>
    /// Window handles are pointer sized, which makes them awkward to serialize: the same filter has to
    /// mean the same thing whether it was written by a 32-bit client or a 64-bit one. The type stores
    /// them widened to 64 bits and narrows them back on access, so the round trip through that
    /// conversion is what most of this file is about. The guards matter too, because an empty filter and
    /// an absent one would otherwise be indistinguishable while meaning opposite things.
    /// </remarks>
    public sealed class WindowInfoOptionsTests
    {
        /// <summary>
        /// Verifies that supplying nothing leaves every filter absent, which the enumerator reads as "do
        /// not filter on this".
        /// </summary>
        [Fact]
        public void Constructor_LeavesEveryFilterAbsentWhenNothingIsSupplied()
        {
            // Act
            WindowInfoOptions options = new(windowTitleRegex: null, windowHandleFilter: null, parentProcessFilter: null, parentProcessIdFilter: null, parentProcessMainWindowHandleFilter: null);

            // Assert
            Assert.Null(options.WindowTitleRegex);
            Assert.Null(options.WindowHandleFilter);
            Assert.Null(options.ParentProcessFilter);
            Assert.Null(options.ParentProcessIdFilter);
            Assert.Null(options.ParentProcessMainWindowHandleFilter);
        }

        /// <summary>
        /// Verifies that an empty window handle filter is rejected, since it would match nothing and is
        /// almost certainly an absent filter that lost its contents.
        /// </summary>
        [Fact]
        public void Constructor_RejectsAnEmptyWindowHandleFilter()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => new WindowInfoOptions(windowTitleRegex: null, windowHandleFilter: new ReadOnlyCollection<nint>([]), parentProcessFilter: null, parentProcessIdFilter: null, parentProcessMainWindowHandleFilter: null));
        }

        /// <summary>
        /// Verifies that an empty parent process filter is rejected.
        /// </summary>
        [Fact]
        public void Constructor_RejectsAnEmptyParentProcessFilter()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => new WindowInfoOptions(windowTitleRegex: null, windowHandleFilter: null, parentProcessFilter: new ReadOnlyCollection<string>([]), parentProcessIdFilter: null, parentProcessMainWindowHandleFilter: null));
        }

        /// <summary>
        /// Verifies that an empty parent process identifier filter is rejected.
        /// </summary>
        [Fact]
        public void Constructor_RejectsAnEmptyParentProcessIdFilter()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => new WindowInfoOptions(windowTitleRegex: null, windowHandleFilter: null, parentProcessFilter: null, parentProcessIdFilter: new ReadOnlyCollection<int>([]), parentProcessMainWindowHandleFilter: null));
        }

        /// <summary>
        /// Verifies that an empty main window handle filter is rejected.
        /// </summary>
        [Fact]
        public void Constructor_RejectsAnEmptyMainWindowHandleFilter()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => new WindowInfoOptions(windowTitleRegex: null, windowHandleFilter: null, parentProcessFilter: null, parentProcessIdFilter: null, parentProcessMainWindowHandleFilter: new ReadOnlyCollection<nint>([])));
        }

        /// <summary>
        /// Verifies that a blank title pattern is rejected while an absent one is accepted, so a caller
        /// cannot ask to match every title by supplying whitespace.
        /// </summary>
        /// <param name="pattern">The blank pattern to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void Constructor_RejectsABlankTitlePattern(string pattern)
        {
            _ = Assert.Throws<ArgumentException>(() => new WindowInfoOptions(windowTitleRegex: pattern, windowHandleFilter: null, parentProcessFilter: null, parentProcessIdFilter: null, parentProcessMainWindowHandleFilter: null));
        }

        /// <summary>
        /// Verifies that the title pattern is kept verbatim, since it is compiled as a regular expression
        /// by the enumerator and any alteration would change what it matches.
        /// </summary>
        /// <param name="pattern">The pattern to keep.</param>
        [Theory]
        [InlineData("Notepad")]
        [InlineData("^Untitled - Notepad$")]
        [InlineData(@"Save\s+As")]
        [InlineData(".*")]
        public void Constructor_KeepsTheTitlePatternVerbatim(string pattern)
        {
            Assert.Equal(pattern, new WindowInfoOptions(windowTitleRegex: pattern, windowHandleFilter: null, parentProcessFilter: null, parentProcessIdFilter: null, parentProcessMainWindowHandleFilter: null).WindowTitleRegex);
        }

        /// <summary>
        /// Verifies that window handles survive being widened for storage and narrowed again, including
        /// the negative and boundary values a real handle can take.
        /// </summary>
        [Fact]
        public void WindowHandleFilter_RoundTripsThroughTheStoredForm()
        {
            // Arrange
            nint[] handles = [1, 0x1234, -1, int.MaxValue, int.MinValue];

            // Act
            WindowInfoOptions options = new(windowTitleRegex: null, windowHandleFilter: new ReadOnlyCollection<nint>(handles), parentProcessFilter: null, parentProcessIdFilter: null, parentProcessMainWindowHandleFilter: null);

            // Assert
            Assert.Equal(handles, options.WindowHandleFilter);
        }

        /// <summary>
        /// Verifies the same for the main window handle filter, which uses a separate stored collection.
        /// </summary>
        [Fact]
        public void ParentProcessMainWindowHandleFilter_RoundTripsThroughTheStoredForm()
        {
            // Arrange
            nint[] handles = [42, -42, int.MaxValue];

            // Act
            WindowInfoOptions options = new(windowTitleRegex: null, windowHandleFilter: null, parentProcessFilter: null, parentProcessIdFilter: null, parentProcessMainWindowHandleFilter: new ReadOnlyCollection<nint>(handles));

            // Assert
            Assert.Equal(handles, options.ParentProcessMainWindowHandleFilter);
        }

        /// <summary>
        /// Verifies that the two handle filters do not share storage, which a copy-paste in the
        /// conversion would cause and which no other test here would catch.
        /// </summary>
        [Fact]
        public void HandleFilters_DoNotShareStorage()
        {
            // Act
            WindowInfoOptions options = new(windowTitleRegex: null, windowHandleFilter: new ReadOnlyCollection<nint>([1]), parentProcessFilter: null, parentProcessIdFilter: null, parentProcessMainWindowHandleFilter: new ReadOnlyCollection<nint>([2]));

            // Assert
            Assert.Equal<nint[]>([1], [.. options.WindowHandleFilter!]);
            Assert.Equal<nint[]>([2], [.. options.ParentProcessMainWindowHandleFilter!]);
        }

        /// <summary>
        /// Verifies that the string and integer filters are carried through unchanged.
        /// </summary>
        [Fact]
        public void Constructor_CarriesTheProcessFiltersThrough()
        {
            // Act
            WindowInfoOptions options = new(windowTitleRegex: null, windowHandleFilter: null, parentProcessFilter: new ReadOnlyCollection<string>(["notepad", "wordpad"]), parentProcessIdFilter: new ReadOnlyCollection<int>([100, 200]), parentProcessMainWindowHandleFilter: null);

            // Assert
            Assert.Equal(["notepad", "wordpad"], options.ParentProcessFilter);
            Assert.Equal([100, 200], options.ParentProcessIdFilter);
        }

        /// <summary>
        /// Verifies that the options survive serialisation, which is how a filter reaches the client
        /// process that enumerates windows in the user's session.
        /// </summary>
        [Fact]
        public void DataContract_RoundTripsEveryFilter()
        {
            // Arrange
            WindowInfoOptions original = new(windowTitleRegex: "^Untitled", windowHandleFilter: new ReadOnlyCollection<nint>([1, -1, int.MaxValue]), parentProcessFilter: new ReadOnlyCollection<string>(["notepad"]), parentProcessIdFilter: new ReadOnlyCollection<int>([100]), parentProcessMainWindowHandleFilter: new ReadOnlyCollection<nint>([2, int.MinValue]));

            // The declared member types are interfaces and the stored ones are read-only collections, so
            // the concrete types have to be named. PSADT.ClientServer.Server names these same ones in the
            // known-type list it builds its serializer with.
            DataContractSerializer serializer = new(typeof(WindowInfoOptions), [
                typeof(ReadOnlyCollection<long>),
                typeof(ReadOnlyCollection<string>),
                typeof(ReadOnlyCollection<int>),
            ]);

            // Act
            using MemoryStream stream = new();
            serializer.WriteObject(stream, original);
            stream.Position = 0;
            object? deserialized = serializer.ReadObject(stream);
            Assert.NotNull(deserialized);
            WindowInfoOptions restored = (WindowInfoOptions)deserialized;

            // Assert
            Assert.Equal(original.WindowTitleRegex, restored.WindowTitleRegex);
            Assert.Equal(original.WindowHandleFilter, restored.WindowHandleFilter);
            Assert.Equal(original.ParentProcessFilter, restored.ParentProcessFilter);
            Assert.Equal(original.ParentProcessIdFilter, restored.ParentProcessIdFilter);
            Assert.Equal(original.ParentProcessMainWindowHandleFilter, restored.ParentProcessMainWindowHandleFilter);
        }

        /// <summary>
        /// Verifies that a window record rejects a blank title, since a window with no text is skipped by
        /// the enumerator and should never reach the record.
        /// </summary>
        /// <param name="windowTitle">The blank title to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void WindowInfo_RejectsABlankTitle(string windowTitle)
        {
            _ = Assert.Throws<ArgumentException>(() => new WindowInfo(windowTitle, 1, "notepad", 100, 2));
        }

        /// <summary>
        /// Verifies that a window record rejects a blank owning process name.
        /// </summary>
        /// <param name="parentProcess">The blank process name to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void WindowInfo_RejectsABlankParentProcess(string parentProcess)
        {
            _ = Assert.Throws<ArgumentException>(() => new WindowInfo("Untitled - Notepad", 1, parentProcess, 100, 2));
        }

        /// <summary>
        /// Verifies that a window record survives serialisation with its pointer-sized handles intact,
        /// which is how a window found in the user's session is reported back.
        /// </summary>
        [Fact]
        public void WindowInfo_DataContractRoundTripsTheHandles()
        {
            // Arrange
            WindowInfo original = new("Untitled - Notepad", int.MaxValue, "notepad", 1234, -1);
            DataContractSerializer serializer = new(typeof(WindowInfo));

            // Act
            using MemoryStream stream = new();
            serializer.WriteObject(stream, original);
            stream.Position = 0;
            object? deserialized = serializer.ReadObject(stream);
            Assert.NotNull(deserialized);
            WindowInfo restored = (WindowInfo)deserialized;

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal(original.WindowHandle, restored.WindowHandle);
            Assert.Equal(original.ParentProcessMainWindowHandle, restored.ParentProcessMainWindowHandle);
        }
    }
}
