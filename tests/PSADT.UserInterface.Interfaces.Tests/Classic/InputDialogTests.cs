using System;
using System.Collections;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Interfaces.Classic;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Classic
{
    /// <summary>
    /// Tests the Classic input dialog, which exists only so that the dialog manager's construction table
    /// has an entry for it.
    /// </summary>
    /// <remarks>
    /// The Classic style never gained an input dialog; asking for one is a caller error rather than a
    /// missing feature, and the constructor says so. That refusal is the whole of the type's behaviour,
    /// which makes it worth a test: if someone implements the dialog the test fails and tells them to
    /// come and delete it, and until then it stops the refusal being removed by accident and leaving a
    /// blank form to be shown to a user.
    /// </remarks>
    public sealed class InputDialogTests
    {
        /// <summary>
        /// Verifies that constructing one at runtime is refused.
        /// </summary>
        [Fact]
        public void Constructor_RefusesToBuildAtRuntime()
        {
            // Arrange
            Hashtable table = SampleOptions.InputDialog();

            // Act & Assert
            NotSupportedException thrown = DialogHost.Run(() => Assert.Throws<NotSupportedException>(() => new InputDialog(new InputDialogOptions(table))));
            Assert.Contains("Fluent", thrown.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the designer's parameterless constructor is refused at runtime too.
        /// </summary>
        /// <remarks>
        /// It exists for the Windows Forms designer, which constructs a form with no arguments to draw
        /// it. <c language="csharp">LicenseManager.UsageMode</c> is how a form tells the two apart, and a test run is
        /// runtime by definition, so this is the same refusal reached by the other door.
        /// </remarks>
        [Fact]
        public void DesignerConstructor_RefusesToBuildAtRuntime()
        {
            // Act & Assert
            _ = DialogHost.Run(static () => Assert.Throws<NotSupportedException>(static () => new InputDialog()));
        }
    }
}
