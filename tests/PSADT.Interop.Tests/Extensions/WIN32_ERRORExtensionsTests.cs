using System;
using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests.Extensions
{
    /// <summary>
    /// Tests the guard that turns a failing Win32 error into the matching managed exception while letting
    /// success pass through, so it can be chained onto a native call in an expression.
    /// </summary>
    public sealed class WIN32_ERRORExtensionsTests
    {
        /// <summary>
        /// Verifies that a successful result passes through unchanged.
        /// </summary>
        [Fact]
        public void ThrowOnFailure_PassesSuccessThrough()
        {
            // Assert
            Assert.Equal(WIN32_ERROR.ERROR_SUCCESS, WIN32_ERROR.ERROR_SUCCESS.ThrowOnFailure());
        }

        /// <summary>
        /// Verifies that a failing result is translated the same way as if it had been handed to
        /// ExceptionUtilities directly, so chaining the guard loses nothing.
        /// </summary>
        [Fact]
        public void ThrowOnFailure_TranslatesFailuresToTheManagedEquivalent()
        {
            // Assert
            _ = Assert.Throws<UnauthorizedAccessException>(static () => WIN32_ERROR.ERROR_ACCESS_DENIED.ThrowOnFailure());
        }
    }
}
