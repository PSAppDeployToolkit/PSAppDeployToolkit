using System;
using System.Collections.Generic;
using System.Linq;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests the enumeration naming each kind of dialog.
    /// </summary>
    /// <remarks>
    /// This one is not only serialized, it is parsed. The client executable reads a <c language="csharp">DialogType</c> off
    /// its own command line with <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> and refuses
    /// to start without a valid one, so the member names are a command line contract as much as the
    /// values are a wire one.
    /// </remarks>
    public sealed class DialogTypeTests
    {
        /// <summary>
        /// Verifies the members and their values.
        /// </summary>
        [Fact]
        public void Members_AreTheSerializedContract()
        {
            // Arrange
            (string Name, ulong Value)[] expected =
            [
                ("CloseAppsDialog", 0),
                ("CustomDialog", 1),
                ("DialogBox", 2),
                ("HelpConsole", 3),
                ("InputDialog", 4),
                ("ListSelectionDialog", 5),
                ("ProgressDialog", 6),
                ("RestartDialog", 7),
                ("SecureInputDialog", 8),
            ];

            // Assert
            Assert.Equal(expected, EnumValues.DeclaredPairs<DialogType>());
        }

        /// <summary>
        /// Verifies that every member is named for an options type that exists.
        /// </summary>
        /// <remarks>
        /// The pairing is by name and nothing enforces it, so a dialog kind added here without its
        /// options type - or an options type renamed without its member - would only be found when the
        /// client was asked to show that dialog. <c language="csharp">HelpConsole</c> is the exception the naming does not
        /// cover, since its options type is <c language="csharp">HelpConsoleOptions</c> rather than
        /// <c language="csharp">HelpConsoleDialogOptions</c>.
        /// <para>
        /// A member may also share another kind's options on purpose, which the pairing cannot express. Those
        /// are listed below with their reason, so that a deliberate sharing stays distinguishable from the
        /// omission this test is here to catch.
        /// </para>
        /// </remarks>
        [Fact]
        public void Members_EachNameAnOptionsTypeThatExists()
        {
            // Arrange - the members that deliberately share another kind's options, and why.
            Dictionary<string, string> shared = new(StringComparer.Ordinal)
            {
                ["SecureInputDialog"] = "shares InputDialogOptions with InputDialog; the dialog and its options are the same, and only the result type differs so that a masked answer comes back as a SecureString",
            };

            // Act
            string[] missing =
            [
                .. EnumValues.DeclaredNames<DialogType>()
                    .Where(name => !shared.ContainsKey(name) && typeof(BaseDialogOptions).Assembly
                        .GetType($"PSADT.UserInterface.DialogOptions.{name}Options", throwOnError: false) is null),
            ];

            // Assert
            Assert.True(missing.Length is 0, $"No options type was found for: {string.Join(", ", missing)}");
        }
    }
}
