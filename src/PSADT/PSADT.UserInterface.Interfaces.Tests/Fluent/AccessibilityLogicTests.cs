using System;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Interfaces.Fluent;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Fluent
{
    /// <summary>
    /// Contains smoke tests that verify the test-project wiring between <c>PSADT.Tests</c> and
    /// <c>PSADT.UserInterface.Interfaces</c>.
    /// </summary>
    /// <remarks>These tests confirm that the <c>ProjectReference</c> and <c>InternalsVisibleTo</c> entries
    /// are correctly configured so that internal Fluent dialog types are visible to the test assembly.
    /// Real accessibility-logic assertions are introduced in later tasks.</remarks>
    public class AccessibilityLogicTests
    {
        /// <summary>
        /// Verifies that the Fluent dialog types in <c>PSADT.UserInterface.Interfaces</c> are visible to
        /// this test assembly via the configured <c>InternalsVisibleTo</c> and <c>ProjectReference</c>.
        /// </summary>
        [Fact]
        public void FluentDialogTypesAreVisibleToTests()
        {
            // Proves the ProjectReference + InternalsVisibleTo wiring compiles and the internal Fluent
            // dialog types are visible to this test assembly. Real assertions follow in later tasks.
            Assert.Equal("PSADT.UserInterface.Interfaces", typeof(CloseAppsDialog).Assembly.GetName().Name);
        }

        /// <summary>
        /// Verifies that <see cref="CloseAppsDialog.DecideCloseAppsCountdownResult"/> produces the same
        /// Close/Continue/Defer outcome as the legacy accessible-name-based ternary it replaces.
        /// </summary>
        /// <param name="forced">Whether the countdown is forced.</param>
        /// <param name="hasRps">Whether a running-process service is present.</param>
        /// <param name="leftShowsClose">Whether the left button currently shows the close-apps text.</param>
        /// <param name="hideClose">Whether the close button is hidden.</param>
        /// <param name="deferrals">Whether deferrals are available.</param>
        /// <param name="expected">The expected <see cref="CloseAppsDialogResult"/> name.</param>
        [Theory]
        // forcedCountdown, hasRPS, leftShowsClose, hideClose, deferrals => expected
        [InlineData(false, true, true, false, false, "Close")]      // not forced, left shows Close => Close
        [InlineData(false, true, false, false, false, "Continue")]  // not forced, left shows NoProcesses => Continue
        [InlineData(true, false, true, false, false, "Continue")]   // forced + no running-process service => Continue
        [InlineData(true, true, false, false, false, "Continue")]   // forced + noProcesses text + not hidden => Continue
        [InlineData(true, true, false, true, true, "Defer")]        // forced + hideClose + deferrals => Defer
        [InlineData(true, true, true, false, true, "Defer")]        // forced + deferrals available => Defer (regardless of button text)
        public void DecideCloseAppsCountdownResult_MatchesLegacyNameBasedLogic(bool forced, bool hasRps, bool leftShowsClose, bool hideClose, bool deferrals, string expected)
        {
            CloseAppsDialogResult result = CloseAppsDialog.DecideCloseAppsCountdownResult(forced, hasRps, leftShowsClose, hideClose, deferrals);
            Assert.Equal(expected, result.ToString());
        }

        /// <summary>
        /// Verifies that <see cref="FluentDialog.StripAccessKeyMarker"/> strips the access-key marker
        /// (<c>_</c>) exactly as <c>SetButtonContentWithAccelerator</c> applies it to the button's
        /// accessible name.
        /// </summary>
        /// <param name="raw">The raw button text, possibly containing an underscore access-key marker.</param>
        /// <param name="expected">The expected accessible name after the marker is removed.</param>
        [Theory]
        [InlineData("Restart _Now", "Restart Now")]
        [InlineData("_Close Applications", "Close Applications")]
        [InlineData("Save __ Backup", "Save _ Backup")]  // escaped double-underscore collapses to one
        [InlineData("No Accelerator", "No Accelerator")]
        public void AccessKeyMarkerIsStrippedForAccessibleName(string raw, string expected)
        {
            // This is the exact transform SetButtonContentWithAccelerator applies to the accessible name.
            Assert.Equal(expected, FluentDialog.StripAccessKeyMarker(raw));
        }

        /// <summary>
        /// Verifies that <see cref="FluentDialog.DecideCountdownAnnouncement"/> announces at the warning
        /// window, the one-minute mark, every tick in the final ten seconds, and expiry, and stays silent
        /// between the one-minute mark and the final ten seconds.
        /// </summary>
        /// <param name="remainingSeconds">Remaining countdown time in whole seconds.</param>
        /// <param name="warningSeconds">Warning window size in seconds, or -1 for no warning configured.</param>
        /// <param name="warnAnnounced">Whether the "entering warning window" announcement has already been made.</param>
        /// <param name="finalMinAnnounced">Whether the "final minute" announcement has already been made.</param>
        /// <param name="expiredAnnounced">Whether the "expiry" announcement has already been made.</param>
        /// <param name="expectedAnnounce">Whether an announcement is expected at this tick.</param>
        [Theory]
        // remainingSeconds, warningSeconds(null=-1), warnAnnounced, finalMinAnnounced, expiredAnnounced => announce
        [InlineData(120, 90, false, false, false, false)]  // above all thresholds => silent
        [InlineData(90, 90, false, false, false, true)]    // entering warning window => announce once
        [InlineData(85, 90, true, false, false, false)]    // still in warning, already announced => silent
        [InlineData(60, 90, true, false, false, true)]     // crossing one minute => announce once
        [InlineData(45, 90, true, true, false, false)]     // 11-59 s, one-minute already announced => silent
        [InlineData(11, 90, true, true, false, false)]     // just above the final ten seconds => silent
        [InlineData(10, 90, true, true, false, true)]      // final ten seconds => announce every tick
        [InlineData(3, 90, true, true, false, true)]       // final ten seconds => announce every tick
        [InlineData(0, 90, true, true, false, true)]       // expiry => announce once
        [InlineData(0, 90, true, true, true, false)]       // expiry already announced => silent
        [InlineData(90, -1, false, false, false, false)]   // no warning configured, above one minute => silent
        [InlineData(50, -1, false, false, false, true)]    // no warning configured, <=60 s => announce once (parity with 60 s visual cue)
        public void DecideCountdownAnnouncement_AnnouncesAtThresholdsOnly(int remainingSeconds, int warningSeconds, bool warnAnnounced, bool finalMinAnnounced, bool expiredAnnounced, bool expectedAnnounce)
        {
            TimeSpan? warning = warningSeconds < 0 ? null : TimeSpan.FromSeconds(warningSeconds);
            FluentDialog.CountdownAnnounceDecision decision = FluentDialog.DecideCountdownAnnouncement(
                TimeSpan.FromSeconds(remainingSeconds), warning, warnAnnounced, finalMinAnnounced, expiredAnnounced);
            Assert.Equal(expectedAnnounce, decision.Announce);
        }

        /// <summary>
        /// Verifies that <see cref="FluentDialog.FormatCountdownForSpeech"/> speaks only the non-zero
        /// units, pluralizes them, and joins them with "and" before the final unit (e.g. "3 hours 2 minutes
        /// and 10 seconds"), reading "0 seconds" when fully elapsed.
        /// </summary>
        /// <param name="hours">Whole hours remaining.</param>
        /// <param name="minutes">Whole minutes remaining (0-59).</param>
        /// <param name="seconds">Whole seconds remaining (0-59).</param>
        /// <param name="expected">The expected spoken form.</param>
        [Theory]
        [InlineData(3, 2, 10, "3 hours 2 minutes and 10 seconds")]  // all three units
        [InlineData(0, 15, 24, "15 minutes and 24 seconds")]        // hours omitted
        [InlineData(0, 0, 10, "10 seconds")]                        // seconds only
        [InlineData(1, 1, 1, "1 hour 1 minute and 1 second")]       // singular units
        [InlineData(0, 2, 0, "2 minutes")]                          // minutes only
        [InlineData(1, 0, 0, "1 hour")]                             // hours only
        [InlineData(1, 0, 30, "1 hour and 30 seconds")]             // zero minute dropped between two units
        [InlineData(0, 0, 0, "0 seconds")]                          // fully elapsed
        public void FormatCountdownForSpeech_ReadsNonZeroUnitsWithAnd(int hours, int minutes, int seconds, string expected)
        {
            Assert.Equal(expected, FluentDialog.FormatCountdownForSpeech(new TimeSpan(hours, minutes, seconds)));
        }

        /// <summary>
        /// Verifies that <see cref="FluentDialog.NormalizeVersionForSpeech"/> rewrites dot-separated version
        /// tokens into words spoken segment-by-segment with "point" between segments (SR4), so a screen reader
        /// never voices a version like a date. Non-version text is returned unchanged, and the transform is
        /// scoped to dotted digit groups only.
        /// </summary>
        /// <param name="raw">The raw text (e.g. an app title) possibly containing a version token.</param>
        /// <param name="expected">The expected text after version normalization.</param>
        [Theory]
        [InlineData("MyApp 14.04.03", "MyApp fourteen point zero four point zero three")]  // brief's canonical example
        [InlineData("Acme 1.2", "Acme one point two")]                                     // single-digit segments
        [InlineData("Tool 10.0.100", "Tool ten point zero point one zero zero")]           // 3-digit segment read digit-by-digit
        [InlineData("Server 2.19041", "Server two point one nine zero four one")]          // build number read digit-by-digit
        [InlineData("No version here", "No version here")]                                 // nothing to change
        [InlineData("App 5", "App 5")]                                                     // lone integer is not a version token
        public void NormalizeVersionForSpeech_SpeaksDottedVersionsSegmentBySegment(string raw, string expected)
        {
            Assert.Equal(expected, FluentDialog.NormalizeVersionForSpeech(raw));
        }

        /// <summary>
        /// Verifies that <see cref="FluentDialog.ShortenTitleForSpeech"/> reduces an app title to its
        /// vendor/product portion by dropping everything from the first version token onward (version,
        /// language, architecture), falling back to the full speech-normalized title when there is no
        /// version token or nothing meaningful precedes it. Subsequent dialogs in a deployment announce
        /// this short form so the full title is only heard once.
        /// </summary>
        /// <param name="raw">The raw app title.</param>
        /// <param name="expected">The expected short spoken form.</param>
        [Theory]
        [InlineData("Adobe Creative Suite 2.1.45 EN", "Adobe Creative Suite")]  // version + language dropped
        [InlineData("7-Zip v21.07 x64", "7-Zip")]                               // v-prefixed version + architecture dropped
        [InlineData("No Version Product", "No Version Product")]                // no version token => unchanged
        [InlineData("2.1.45 EN", "two point one point forty five EN")]          // nothing before version => full normalized title
        public void ShortenTitleForSpeech_DropsVersionAndTrailingQualifiers(string raw, string expected)
        {
            Assert.Equal(expected, FluentDialog.ShortenTitleForSpeech(raw));
        }

        /// <summary>
        /// Verifies that <see cref="CloseAppsDialog.AppToClose"/> overrides ToString to return the friendly
        /// description. WPF's ItemAutomationPeer falls back to Item.ToString() for a data-bound list item's
        /// UI Automation Name, so without the override a screen reader reads the record-generated property
        /// dump ("AppToClose { Name = ..., Icon = ... }") for every row of the applications-to-close list.
        /// </summary>
        [Fact]
        public void AppToCloseToStringIsOverriddenForAccessibleName()
        {
            System.Reflection.MethodInfo toString = typeof(CloseAppsDialog.AppToClose).GetMethod(nameof(ToString), Type.EmptyTypes);
            Assert.Equal(typeof(CloseAppsDialog.AppToClose), toString.DeclaringType);
        }
    }
}
