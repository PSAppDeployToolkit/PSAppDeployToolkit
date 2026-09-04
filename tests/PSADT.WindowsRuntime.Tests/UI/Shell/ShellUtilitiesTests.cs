using System.Globalization;
using System.Threading;
using PSADT.WindowsRuntime.Tests.TestHelpers;
using PSADT.WindowsRuntime.UI.Shell;
using Windows.UI.Shell;
using Xunit;

namespace PSADT.WindowsRuntime.Tests.UI.Shell
{
    /// <summary>
    /// Tests the wrapper over the Windows focus session state.
    /// </summary>
    /// <remarks>
    /// The wrapper is four lines over a Windows Runtime API, so what is worth checking is not its
    /// arithmetic but that it reaches that API at all from an ordinary .NET Framework or .NET process,
    /// from either COM apartment, and that it reports what the API reports. Each test below takes one of
    /// those. That success implies a value is not among them: the annotation on the out parameter makes
    /// the compiler prove it, and a test restating it would only pass twice.
    /// <para>
    /// The failing path - the wrapper answering false because the running system has no focus session
    /// API - is written but cannot run here. It depends on the operating system lacking a type, which a
    /// test cannot arrange without lying to the runtime about which Windows it is on, so
    /// <see cref="TryGetFocusSessionActive_DegradesRatherThanThrowingOnASystemWithoutTheApi"/> is gated
    /// the opposite way to the rest and skips on anything new enough to have the API.
    /// </para>
    /// </remarks>
    public sealed class ShellUtilitiesTests
    {
        /// <summary>
        /// The reason the tests that read the focus state skip on an older system.
        /// </summary>
        private const string RequiresFocusSessions = "Requires Windows 10 version 1903 or later, which is where the focus session API shipped.";

        /// <summary>
        /// Verifies that on a system without the focus session API the wrapper returns false rather than
        /// throwing, which is the entire point of the guard in front of it.
        /// </summary>
        /// <remarks>
        /// This is written for a system it cannot run on here, and skips everywhere else. It is worth
        /// having because there is reason to doubt the guard holds on .NET Framework: the type it guards
        /// against is named in the same method body, and the runtime resolves the types a method mentions
        /// when it compiles that method rather than when execution reaches them - so the guard can be
        /// jumped over by a failure that happens before the method's first instruction runs. The
        /// supported platform list reaches back to Windows 10 1607, well below the 18362 this API needs,
        /// so a run on one of those is the measurement this test exists to take.
        /// </remarks>
        [Fact(Skip = "Requires a Windows build older than 18362, where the focus session API is absent.", SkipWhen = nameof(TestEnvironment.HasFocusSessionsAndNotificationMode), SkipType = typeof(TestEnvironment))]
        public void TryGetFocusSessionActive_DegradesRatherThanThrowingOnASystemWithoutTheApi()
        {
            // Act
            bool succeeded = ShellUtilities.TryGetFocusSessionActive(out bool? isActive);

            // Assert
            Assert.False(succeeded);
            Assert.Null(isActive);
        }

        /// <summary>
        /// Verifies that the wrapper succeeds on a system that has the focus session API. This is what
        /// catches a guard that is wrong rather than absent: every check in the method returns false for
        /// an API that is missing and for an API it fails to ask about correctly, and only the second of
        /// those is a defect. The build number, read independently of the runtime's own metadata queries,
        /// says which of the two a false answer here would be.
        /// </summary>
        [Fact(Skip = RequiresFocusSessions, SkipUnless = nameof(TestEnvironment.HasFocusSessionsAndNotificationMode), SkipType = typeof(TestEnvironment))]
        public void TryGetFocusSessionActive_SucceedsOnASystemThatHasTheApi()
        {
            // Act
            bool succeeded = ShellUtilities.TryGetFocusSessionActive(out bool? isActive);

            // Assert
            Assert.True(succeeded, $"The focus session API shipped in build 18362 and this system reports build {TestEnvironment.OperatingSystemBuild.ToString(CultureInfo.InvariantCulture)}, so the wrapper should have been able to read it.");
            _ = Assert.NotNull(isActive);
        }

        /// <summary>
        /// Verifies that the wrapper reports the state the focus session manager reports, rather than a
        /// constant, the opposite, or some other property of the same object.
        /// </summary>
        [Fact(Skip = RequiresFocusSessions, SkipUnless = nameof(TestEnvironment.HasFocusSessionsAndNotificationMode), SkipType = typeof(TestEnvironment))]
        public void TryGetFocusSessionActive_ReportsWhatTheFocusSessionManagerReports()
        {
            // Arrange
            bool? expected = FocusSessionManager.GetDefault().IsFocusActive;

            // Act
            bool succeeded = ShellUtilities.TryGetFocusSessionActive(out bool? isActive);

            // Assert
            Assert.True(succeeded);
            Assert.Equal(expected, isActive);
        }

        /// <summary>
        /// Verifies that the wrapper answers the same in either COM apartment, and answers the same on a
        /// second call as on the first. The client that consumes this assembly runs its user interface in
        /// a single-threaded apartment while the test runner's own threads are multi-threaded, so an
        /// apartment the wrapper cannot be called from would otherwise show up only in the client.
        /// </summary>
        [Fact]
        public void TryGetFocusSessionActive_AnswersTheSameInEitherApartment()
        {
            // Act
            bool onTheRunnersThread = ShellUtilities.TryGetFocusSessionActive(out _);
            bool onSingleThreaded = Apartment.Run(ApartmentState.STA, static () => ShellUtilities.TryGetFocusSessionActive(out _));
            bool onMultiThreaded = Apartment.Run(ApartmentState.MTA, static () => ShellUtilities.TryGetFocusSessionActive(out _));

            // Assert
            Assert.Equal(onTheRunnersThread, onSingleThreaded);
            Assert.Equal(onTheRunnersThread, onMultiThreaded);
        }
    }
}
