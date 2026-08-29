using System;
using System.Collections;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Interfaces.Classic;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Classic
{
    /// <summary>
    /// Tests the Classic list selection dialog, which exists only so that the dialog manager's
    /// construction table has an entry for it.
    /// </summary>
    /// <remarks>
    /// The same arrangement as the Classic input dialog: the style never gained a list selection dialog,
    /// so the constructor refuses rather than showing the half-built form the designer file describes.
    /// See <see cref="InputDialogTests"/> for why that refusal is worth a test of its own.
    /// </remarks>
    public sealed class ListSelectionDialogTests
    {
        /// <summary>
        /// Verifies that constructing one at runtime is refused.
        /// </summary>
        [Fact]
        public void Constructor_RefusesToBuildAtRuntime()
        {
            // Arrange
            Hashtable table = SampleOptions.ListSelectionDialog();

            // Act & Assert
            NotSupportedException thrown = DialogHost.Run(() => Assert.Throws<NotSupportedException>(() => new ListSelectionDialog(new ListSelectionDialogOptions(table))));
            Assert.Contains("Fluent", thrown.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the designer's parameterless constructor is refused at runtime too.
        /// </summary>
        [Fact]
        public void DesignerConstructor_RefusesToBuildAtRuntime()
        {
            // Act & Assert
            _ = DialogHost.Run(static () => Assert.Throws<NotSupportedException>(static () => new ListSelectionDialog()));
        }
    }
}
