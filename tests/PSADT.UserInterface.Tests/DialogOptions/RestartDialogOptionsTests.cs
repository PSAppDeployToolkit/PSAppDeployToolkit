using System;
using System.Collections;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Tests.TestHelpers;
using PSAppDeployToolkit.Foundation;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogOptions
{
    /// <summary>
    /// Tests the options for the dialog that asks a user to restart.
    /// </summary>
    public sealed class RestartDialogOptionsTests
    {
        /// <summary>
        /// Verifies that the values handed in are the ones read back.
        /// </summary>
        [Fact]
        public void Constructor_KeepsWhatItIsGiven()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["CountdownDuration"] = TimeSpan.FromMinutes(20);
            table["CountdownNoMinimizeDuration"] = TimeSpan.FromMinutes(5);
            table["ShutdownReasonText"] = "a maintenance window";
            table["CustomMessageText"] = "a custom message";
            table["DialogAllowCancel"] = true;

            // Act
            RestartDialogOptions options = new(DeploymentType.Install, table);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(20), options.CountdownDuration);
            Assert.Equal(TimeSpan.FromMinutes(5), options.CountdownNoMinimizeDuration);
            Assert.Equal("a maintenance window", options.ShutdownReasonText);
            Assert.Equal("a custom message", options.CustomMessageText);
            Assert.True(options.DialogAllowCancel);
        }

        /// <summary>
        /// Verifies the defaults for the values a caller can leave out.
        /// </summary>
        /// <remarks>
        /// <c>DialogAllowCancel</c> is the one that is collapsed rather than kept nullable, and it
        /// defaults to false - so a restart prompt cannot be dismissed unless the caller says it can. The
        /// field used to be called <c>AllowCancel</c> while the dictionary key it is read from was
        /// <c>DialogAllowCancel</c>; they now agree, and agree with the <c>DialogAllowMove</c> and
        /// <c>DialogAllowMinimize</c> pair on the base type.
        /// </remarks>
        [Fact]
        public void Constructor_DefaultsTheOptionalValues()
        {
            // Act
            RestartDialogOptions options = new(DeploymentType.Install, SampleOptions.RestartDialog());

            // Assert
            Assert.Null(options.CountdownDuration);
            Assert.Null(options.CountdownNoMinimizeDuration);
            Assert.Null(options.ShutdownReasonText);
            Assert.Null(options.CustomMessageText);
            Assert.False(options.DialogAllowCancel);
        }

        /// <summary>
        /// Verifies that a custom message present but blank is refused.
        /// </summary>
        [Fact]
        public void Constructor_RefusesABlankCustomMessage()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["CustomMessageText"] = "   ";

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => new RestartDialogOptions(DeploymentType.Install, table));
        }

        /// <summary>
        /// Verifies that a shutdown reason present but blank is refused.
        /// </summary>
        /// <remarks>
        /// The two optional strings on this type used to be treated differently, with only
        /// <c>CustomMessageText</c> rejected when blank. Both now follow the rule the rest of these types
        /// keep: absent is a valid state, present-but-blank is not, since it says a value was meant and
        /// then supplies nothing.
        /// </remarks>
        /// <param name="value">The blank value to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_RefusesABlankShutdownReason(string value)
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["ShutdownReasonText"] = value;

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => new RestartDialogOptions(DeploymentType.Install, table));
        }

        /// <summary>
        /// Verifies that the deployment type selects the right wording for the one string that varies.
        /// </summary>
        /// <param name="deploymentType">The deployment type to build for.</param>
        [Theory]
        [InlineData(DeploymentType.Install)]
        [InlineData(DeploymentType.Uninstall)]
        [InlineData(DeploymentType.Repair)]
        public void Strings_SelectTheMessageByDeploymentType(DeploymentType deploymentType)
        {
            // Act
            RestartDialogOptions options = new(deploymentType, SampleOptions.RestartDialog());

            // Assert
            Assert.Equal($"a message ({deploymentType})", options.Strings.Message);
            Assert.Equal("a title", options.Strings.Title);
        }

        /// <summary>
        /// Verifies that a missing wording for the type being built names the key and the type.
        /// </summary>
        [Fact]
        public void Strings_NameTheDeploymentTypeWhenItsWordingIsMissing()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            SampleOptions.Nested(table, "Strings", "Message").Remove(nameof(DeploymentType.Uninstall));

            // Act & Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new RestartDialogOptions(DeploymentType.Uninstall, table));
            Assert.Contains("Message.Uninstall", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the strings table is required and that each of its keys is.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        [Theory]
        [InlineData("Title")]
        [InlineData("Message")]
        [InlineData("MessageTime")]
        [InlineData("MessageRestart")]
        [InlineData("TimeRemaining")]
        [InlineData("ButtonRestartNow")]
        [InlineData("ButtonRestartLater")]
        [InlineData("ButtonCancel")]
        public void Strings_RequireEveryKey(string key)
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            SampleOptions.Nested(table, "Strings").Remove(key);

            // Act & Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new RestartDialogOptions(DeploymentType.Install, table));
            Assert.Contains(key, exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the whole strings table being absent is refused.
        /// </summary>
        [Fact]
        public void Strings_AreRequired()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table.Remove("Strings");

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => new RestartDialogOptions(DeploymentType.Install, table));
        }

        /// <summary>
        /// Verifies that a blank string in the table is refused.
        /// </summary>
        /// <param name="key">The key to blank out.</param>
        [Theory]
        [InlineData("Title")]
        [InlineData("ButtonCancel")]
        public void Strings_RefuseABlankValue(string key)
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            SampleOptions.Nested(table, "Strings")[key] = "   ";

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => new RestartDialogOptions(DeploymentType.Install, table));
        }
    }
}
