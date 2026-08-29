using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using PSADT.WindowsRuntime.Tests.TestHelpers;
using PSADT.WindowsRuntime.UI.Notifications;
using Windows.UI.Notifications;
using Xunit;

namespace PSADT.WindowsRuntime.Tests.UI.Notifications
{
    /// <summary>
    /// Tests the wrapper over the user's toast notification mode.
    /// </summary>
    /// <remarks>
    /// The wrapper guards a single property read with four separate metadata queries, and each of those
    /// queries can only report presence or absence - none of them reports that the query itself was
    /// malformed. A query naming a member that does not exist under the name given is therefore
    /// indistinguishable, from inside the method, from an operating system that lacks the feature, and
    /// turns the whole wrapper into one that always fails. That is the failure the tests here are shaped
    /// to find. That success implies a value is not asserted, for the reason given on the focus session
    /// wrapper: the compiler already proves it from the out parameter's annotation.
    /// <para>
    /// The failing path - the wrapper answering false on a system without the API - is written but
    /// cannot run here, for the same reason it cannot for the focus session wrapper: it cannot be
    /// arranged without lying to the runtime about which Windows it is on.
    /// <see cref="TryGetNotificationMode_DegradesRatherThanThrowingOnASystemWithoutTheApi"/> is gated the
    /// opposite way to the rest and skips on anything new enough to have the API.
    /// </para>
    /// </remarks>
    public sealed class NotificationsUtilitiesTests
    {
        /// <summary>
        /// The reason the tests that read the notification mode skip on an older system.
        /// </summary>
        private const string RequiresNotificationMode = "Requires Windows 10 version 1903 or later, which is where the notification mode API shipped.";

        /// <summary>
        /// Verifies that on a system without the notification mode API the wrapper returns false rather
        /// than throwing.
        /// </summary>
        /// <remarks>
        /// Written for a system it cannot run on here, for the reason given on the focus session
        /// wrapper's equivalent, and with one difference that makes it the more doubtful of the two: the
        /// enumeration this method reports is named in its signature, not just in its body, so a runtime
        /// that cannot resolve that enumeration cannot compile the method or anything that calls it, and
        /// no rearrangement of the guard inside the method would change that.
        /// </remarks>
        [Fact(Skip = "Requires a Windows build older than 18362, where the notification mode API is absent.", SkipWhen = nameof(TestEnvironment.HasFocusSessionsAndNotificationMode), SkipType = typeof(TestEnvironment))]
        public void TryGetNotificationMode_DegradesRatherThanThrowingOnASystemWithoutTheApi()
        {
            // Act
            bool succeeded = NotificationsUtilities.TryGetNotificationMode(out ToastNotificationMode? mode);

            // Assert
            Assert.False(succeeded);
            Assert.Null(mode);
        }

        /// <summary>
        /// Verifies that the wrapper succeeds on a system that has the notification mode API. All four of
        /// its guards answer false for an API that is absent and for an API that is present but asked
        /// about wrongly, so on a system known to have the API this is the test that separates the two.
        /// </summary>
        [Fact(Skip = RequiresNotificationMode, SkipUnless = nameof(TestEnvironment.HasFocusSessionsAndNotificationMode), SkipType = typeof(TestEnvironment))]
        public void TryGetNotificationMode_SucceedsOnASystemThatHasTheApi()
        {
            // Act
            bool succeeded = NotificationsUtilities.TryGetNotificationMode(out ToastNotificationMode? mode);

            // Assert
            Assert.True(succeeded, $"The notification mode API shipped in build 18362 and this system reports build {TestEnvironment.OperatingSystemBuild.ToString(CultureInfo.InvariantCulture)}, so the wrapper should have been able to read it.");
            _ = Assert.NotNull(mode);
        }

        /// <summary>
        /// Verifies that the wrapper reports the mode the notification manager reports, rather than a
        /// constant or some other property of the same object.
        /// </summary>
        [Fact(Skip = RequiresNotificationMode, SkipUnless = nameof(TestEnvironment.HasFocusSessionsAndNotificationMode), SkipType = typeof(TestEnvironment))]
        public void TryGetNotificationMode_ReportsWhatTheNotificationManagerReports()
        {
            // Arrange
            ToastNotificationMode? expected = ToastNotificationManager.GetDefault().NotificationMode;

            // Act
            bool succeeded = NotificationsUtilities.TryGetNotificationMode(out ToastNotificationMode? mode);

            // Assert
            Assert.True(succeeded);
            Assert.Equal(expected, mode);
        }

        /// <summary>
        /// Verifies that the mode handed back names a member the enumeration declares. The caller casts it
        /// straight to an integer and ships that number across a process boundary for something on the
        /// other side to interpret, so a value outside the enumeration would travel as a number nothing
        /// can decode rather than fail here.
        /// </summary>
        [Fact(Skip = RequiresNotificationMode, SkipUnless = nameof(TestEnvironment.HasFocusSessionsAndNotificationMode), SkipType = typeof(TestEnvironment))]
        public void TryGetNotificationMode_ReportsADeclaredMode()
        {
            // Act
            bool succeeded = NotificationsUtilities.TryGetNotificationMode(out ToastNotificationMode? mode);

            // Assert
            Assert.True(succeeded);
            Assert.Contains((long)Assert.NotNull(mode), DeclaredModes());
        }

        /// <summary>
        /// Verifies that no member of the enumeration is negative. The caller reserves -1 to mean that the
        /// mode could not be read, so a negative member would make a real mode indistinguishable from a
        /// failure to read one. Nothing in this repository would catch that: the enumeration belongs to
        /// Windows, and it would arrive with a Windows SDK update rather than with a change here.
        /// </summary>
        [Fact]
        public void ToastNotificationMode_DeclaresNoMemberThatCollidesWithTheCallersFailureSentinel()
        {
            // Arrange
            long[] declared = DeclaredModes();

            // Assert
            Assert.NotEmpty(declared);
            Assert.All(declared, static value => Assert.True(value >= 0, $"ToastNotificationMode declares [{value.ToString(CultureInfo.InvariantCulture)}], which collides with the -1 the caller uses to mean the mode could not be read."));
        }

        /// <summary>
        /// Verifies that the wrapper answers the same in either COM apartment, and answers the same on a
        /// second call as on the first, for the reason given on the focus session wrapper's equivalent.
        /// </summary>
        [Fact]
        public void TryGetNotificationMode_AnswersTheSameInEitherApartment()
        {
            // Act
            bool onTheRunnersThread = NotificationsUtilities.TryGetNotificationMode(out _);
            bool onSingleThreaded = Apartment.Run(ApartmentState.STA, static () => NotificationsUtilities.TryGetNotificationMode(out _));
            bool onMultiThreaded = Apartment.Run(ApartmentState.MTA, static () => NotificationsUtilities.TryGetNotificationMode(out _));

            // Assert
            Assert.Equal(onTheRunnersThread, onSingleThreaded);
            Assert.Equal(onTheRunnersThread, onMultiThreaded);
        }
        /// <summary>
        /// Reads the values the notification mode enumeration declares.
        /// </summary>
        /// <remarks>
        /// Read off the fields rather than through <c>Enum.GetValues</c> or <c>Enum.IsDefined</c>, whose
        /// generic overloads the analyzers ask for and .NET Framework does not have.
        /// </remarks>
        /// <returns>The declared values, in declaration order.</returns>
        private static long[] DeclaredModes()
        {
            return [.. typeof(ToastNotificationMode).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(static field => Convert.ToInt64(field.GetRawConstantValue(), CultureInfo.InvariantCulture))];
        }
    }
}
