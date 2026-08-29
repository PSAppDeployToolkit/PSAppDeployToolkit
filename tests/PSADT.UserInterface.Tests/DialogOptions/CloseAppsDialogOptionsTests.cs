using System;
using System.Collections;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Tests.TestHelpers;
using PSAppDeployToolkit.Foundation;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogOptions
{
    /// <summary>
    /// Tests the options for the dialog that asks a user to close their applications.
    /// </summary>
    /// <remarks>
    /// The largest options type, and the only one besides the restart dialog whose constructor takes a
    /// <see cref="DeploymentType"/> alongside the dictionary. That second argument selects between three
    /// wordings of the same string at nine points in the nested tables, so most of what is checked here
    /// is that the selection happens and happens consistently.
    /// </remarks>
    public sealed class CloseAppsDialogOptionsTests
    {
        /// <summary>
        /// Verifies that the values handed in are the ones read back.
        /// </summary>
        [Fact]
        public void Constructor_KeepsWhatItIsGiven()
        {
            // Arrange
            DateTime deadline = new(2026, 12, 25, 9, 30, 0, DateTimeKind.Utc);
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["DeferralsRemaining"] = 3u;
            table["DeferralDeadline"] = deadline;
            table["UnlimitedDeferrals"] = true;
            table["ContinueOnProcessClosure"] = true;
            table["CountdownDuration"] = TimeSpan.FromMinutes(15);
            table["ForcedCountdown"] = true;
            table["HideCloseButton"] = true;
            table["CustomMessageText"] = "a custom message";

            // Act
            CloseAppsDialogOptions options = new(DeploymentType.Install, table);

            // Assert
            Assert.Equal(3u, options.DeferralsRemaining);
            Assert.Equal(deadline, options.DeferralDeadline);
            Assert.True(options.UnlimitedDeferrals);
            Assert.True(options.ContinueOnProcessClosure);
            Assert.Equal(TimeSpan.FromMinutes(15), options.CountdownDuration);
            Assert.True(options.ForcedCountdown);
            Assert.True(options.HideCloseButton);
            Assert.Equal("a custom message", options.CustomMessageText);
        }

        /// <summary>
        /// Verifies the defaults for the values a caller can leave out.
        /// </summary>
        /// <remarks>
        /// The four booleans read with a <c>?? false</c> rather than staying nullable, so an absent key
        /// means the safe option in each case: deferrals are limited, the deployment waits, the countdown
        /// is not forced and the close button is shown.
        /// </remarks>
        [Fact]
        public void Constructor_DefaultsTheOptionalValues()
        {
            // Act
            CloseAppsDialogOptions options = new(DeploymentType.Install, SampleOptions.CloseAppsDialog());

            // Assert
            Assert.Null(options.DeferralsRemaining);
            Assert.Null(options.DeferralDeadline);
            Assert.Null(options.CountdownDuration);
            Assert.Null(options.CustomMessageText);
            Assert.False(options.UnlimitedDeferrals);
            Assert.False(options.ContinueOnProcessClosure);
            Assert.False(options.ForcedCountdown);
            Assert.False(options.HideCloseButton);
        }

        /// <summary>
        /// Verifies that a custom message present but blank is refused.
        /// </summary>
        [Fact]
        public void Constructor_RefusesABlankCustomMessage()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["CustomMessageText"] = "   ";

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => new CloseAppsDialogOptions(DeploymentType.Install, table));
        }

        /// <summary>
        /// Verifies that the deployment type selects the right wording throughout both string tables.
        /// </summary>
        /// <remarks>
        /// The nine per-deployment-type lookups are spread across two nested types and each is written out
        /// separately in the source, so a copy-and-paste that read the wrong key would affect one string
        /// and leave the other eight correct. The fixture suffixes every value with the deployment type it
        /// was filed under, which is what makes a wrong branch visible rather than merely plausible.
        /// </remarks>
        /// <param name="deploymentType">The deployment type to build for.</param>
        [Theory]
        [InlineData(DeploymentType.Install)]
        [InlineData(DeploymentType.Uninstall)]
        [InlineData(DeploymentType.Repair)]
        public void Strings_AreSelectedByDeploymentType(DeploymentType deploymentType)
        {
            // Act
            CloseAppsDialogOptions options = new(deploymentType, SampleOptions.CloseAppsDialog());
            CloseAppsDialogOptions.CloseAppsDialogStrings.CloseAppsDialogClassicStrings classic = options.Strings.Classic;
            CloseAppsDialogOptions.CloseAppsDialogStrings.CloseAppsDialogFluentStrings fluent = options.Strings.Fluent;

            // Assert
            Assert.Equal($"welcome ({deploymentType})", classic.WelcomeMessage);
            Assert.Equal($"close these ({deploymentType})", classic.CloseAppsMessage);
            Assert.Equal($"this expires ({deploymentType})", classic.ExpiryMessage);
            Assert.Equal($"counting down to defer ({deploymentType})", classic.CountdownDefer);
            Assert.Equal($"counting down to close ({deploymentType})", classic.CountdownClose);
            Assert.Equal($"dialog message ({deploymentType})", fluent.DialogMessage);
            Assert.Equal($"nothing is running ({deploymentType})", fluent.DialogMessageNoProcesses);
            Assert.Equal($"left ({deploymentType})", fluent.ButtonLeftText);
            Assert.Equal($"left with nothing running ({deploymentType})", fluent.ButtonLeftTextNoProcesses);
        }

        /// <summary>
        /// Verifies that the strings which are the same for every deployment type are read as plain values.
        /// </summary>
        /// <remarks>
        /// The other half of the check above. These seven are not nested under a deployment type, so
        /// reading one of them as though it were would fail rather than pick the wrong wording - but only
        /// if something asks.
        /// </remarks>
        [Fact]
        public void Strings_ThatDoNotVaryAreReadDirectly()
        {
            // Act
            CloseAppsDialogOptions options = new(DeploymentType.Repair, SampleOptions.CloseAppsDialog());

            // Assert
            Assert.Equal("deferrals remaining", options.Strings.Classic.DeferralsRemaining);
            Assert.Equal("deferral deadline", options.Strings.Classic.DeferralDeadline);
            Assert.Equal("expiry warning", options.Strings.Classic.ExpiryWarning);
            Assert.Equal("Close", options.Strings.Classic.ButtonClose);
            Assert.Equal("Defer", options.Strings.Classic.ButtonDefer);
            Assert.Equal("Continue", options.Strings.Classic.ButtonContinue);
            Assert.Equal("continue tooltip", options.Strings.Classic.ButtonContinueTooltip);
            Assert.Equal("automatic start countdown", options.Strings.Fluent.AutomaticStartCountdown);
            Assert.Equal("right", options.Strings.Fluent.ButtonRightText);
        }

        /// <summary>
        /// Verifies that the strings table is required, along with each of its two halves.
        /// </summary>
        [Fact]
        public void Strings_AreRequiredAlongWithBothHalves()
        {
            // Arrange
            Hashtable noStrings = SampleOptions.CloseAppsDialog();
            Hashtable noClassic = SampleOptions.CloseAppsDialog();
            Hashtable noFluent = SampleOptions.CloseAppsDialog();
            noStrings.Remove("Strings");
            SampleOptions.Nested(noClassic, "Strings").Remove("Classic");
            SampleOptions.Nested(noFluent, "Strings").Remove("Fluent");

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => new CloseAppsDialogOptions(DeploymentType.Install, noStrings));
            Assert.Contains("Classic", Assert.Throws<ArgumentNullException>(() => new CloseAppsDialogOptions(DeploymentType.Install, noClassic)).Message, StringComparison.Ordinal);
            Assert.Contains("Fluent", Assert.Throws<ArgumentNullException>(() => new CloseAppsDialogOptions(DeploymentType.Install, noFluent)).Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a per-deployment-type string missing the wording for the type being built names
        /// the key and the type it was looking for.
        /// </summary>
        /// <remarks>
        /// The message is the only thing that tells whoever is editing the toolkit's string files which of
        /// three wordings they left out, so it is asserted rather than just the exception type.
        /// </remarks>
        [Fact]
        public void Strings_NameTheDeploymentTypeWhenItsWordingIsMissing()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            Hashtable classic = SampleOptions.Nested(table, "Strings", "Classic");
            SampleOptions.Nested(classic, "WelcomeMessage").Remove(nameof(DeploymentType.Repair));

            // Act & Assert - building for a type that is present still works.
            _ = new CloseAppsDialogOptions(DeploymentType.Install, SampleOptions.CloseAppsDialog());
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new CloseAppsDialogOptions(DeploymentType.Repair, table));
            Assert.Contains("WelcomeMessage.Repair", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a blank string anywhere in the nested tables is refused.
        /// </summary>
        /// <param name="half">Which of the two string tables to damage.</param>
        /// <param name="key">The key within it to blank out.</param>
        [Theory]
        [InlineData("Classic", "ButtonClose")]
        [InlineData("Classic", "ExpiryWarning")]
        [InlineData("Fluent", "ButtonRightText")]
        [InlineData("Fluent", "AutomaticStartCountdown")]
        public void Strings_RefuseABlankValue(string half, string key)
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            SampleOptions.Nested(table, "Strings", half)[key] = "   ";

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => new CloseAppsDialogOptions(DeploymentType.Install, table));
        }

        /// <summary>
        /// Verifies that two dialogs built for different deployment types are not equal.
        /// </summary>
        /// <remarks>
        /// The deployment type is not stored as a field - it is consumed during construction to pick the
        /// wording - so the only thing that distinguishes the two is the strings that came out of it. That
        /// they compare as different is what proves the nested string records take part in the comparison.
        /// </remarks>
        [Fact]
        public void Equality_ReflectsTheDeploymentTypeThroughTheStringsItSelected()
        {
            // Arrange
            Hashtable first = SampleOptions.CloseAppsDialog();
            Hashtable second = SampleOptions.CloseAppsDialog();
            second["AppIconImage"] = first["AppIconImage"];
            second["AppBannerImage"] = first["AppBannerImage"];

            // Assert
            Assert.Equal(new CloseAppsDialogOptions(DeploymentType.Install, first), new CloseAppsDialogOptions(DeploymentType.Install, second));
            Assert.NotEqual(new CloseAppsDialogOptions(DeploymentType.Install, first), new CloseAppsDialogOptions(DeploymentType.Uninstall, second));
        }
    }
}
