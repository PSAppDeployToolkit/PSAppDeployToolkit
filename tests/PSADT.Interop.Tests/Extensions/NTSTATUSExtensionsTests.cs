using System;
using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests.Extensions
{
    /// <summary>
    /// Tests the guard that turns a failing status into the matching managed exception while letting a
    /// successful one pass through, so it can be chained onto a native call in an expression.
    /// </summary>
    public sealed class NTSTATUSExtensionsTests
    {
        /// <summary>
        /// Verifies that a successful status passes through unchanged.
        /// </summary>
        [Fact]
        public void ThrowOnFailure_PassesSuccessThrough()
        {
            // Assert
            Assert.Equal(NTSTATUS.STATUS_SUCCESS, NTSTATUS.STATUS_SUCCESS.ThrowOnFailure());
        }

        /// <summary>
        /// Verifies that a failing status is translated the same way as if it had been handed to
        /// ExceptionUtilities directly, so chaining the guard loses nothing.
        /// </summary>
        [Fact]
        public void ThrowOnFailure_TranslatesFailuresToTheManagedEquivalent()
        {
            // Assert
            _ = Assert.Throws<UnauthorizedAccessException>(static () => NTSTATUS.STATUS_ACCESS_DENIED.ThrowOnFailure());
        }
    }
}
