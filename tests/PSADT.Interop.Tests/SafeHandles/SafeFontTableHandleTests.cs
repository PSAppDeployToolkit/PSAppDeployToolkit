using Xunit;

namespace PSADT.Interop.Tests.SafeHandles
{
    /// <summary>
    /// Records the one handle type this suite does not cover, so the gap is visible in a test run rather
    /// than only in a document.
    /// </summary>
    public sealed class SafeFontTableHandleTests
    {
        /// <summary>
        /// The handle borrows a table from a live DirectWrite font face and hands it back on release, so it
        /// cannot be constructed without one. That belongs to integration coverage alongside the DirectWrite
        /// and shell interface extensions, which are excluded for the same reason.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped", Justification = "Deliberate: this records a known exclusion so it is visible in a test run rather than only in a document.")]
        [Fact(Skip = "Requires a live DirectWrite font face; belongs to integration coverage.")]
        public void Lifetime_RequiresALiveFontFace()
        {
            // Assert
            Assert.Fail("Not reachable: this test is always skipped.");
        }
    }
}
