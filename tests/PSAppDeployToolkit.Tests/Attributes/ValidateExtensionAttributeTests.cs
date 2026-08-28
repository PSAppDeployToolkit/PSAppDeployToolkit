using System;
using System.Management.Automation;
using System.Management.Automation.Language;
using PSAppDeployToolkit.Attributes;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Attributes
{
    /// <summary>
    /// Tests the attribute that restricts a path parameter to an approved set of extensions.
    /// </summary>
    /// <remarks>
    /// Two contracts here rather than one. The constructor validates the extensions the declaring parameter names,
    /// which fails at module import rather than at call time, so a mistake there breaks the whole module. Element
    /// validation is the part callers hit.
    /// </remarks>
    public sealed class ValidateExtensionAttributeTests
    {
        /// <summary>
        /// Verifies that the approved extensions are kept as given.
        /// </summary>
        [Fact]
        public void ExtensionNames_AreWhatTheAttributeWasGiven()
        {
            Assert.Equal([".msi", ".msp"], new ValidateExtensionAttribute(".msi", ".msp").ExtensionNames);
        }

        /// <summary>
        /// Verifies that the constructor refuses no extensions at all.
        /// </summary>
        [Fact]
        public void ValidateExtensionAttribute_RefusesAnEmptySet()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new ValidateExtensionAttribute(null!));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => new ValidateExtensionAttribute());
        }

        /// <summary>
        /// Verifies that the constructor refuses something that is not an extension.
        /// </summary>
        /// <remarks>
        /// An extension has to start with a period and carry at least one character after it, so a bare name and a
        /// lone period are both refused. The offending value is named in the exception, since a declaration listing
        /// several is otherwise hard to fault-find.
        /// </remarks>
        /// <param name="extension">The malformed extension.</param>
        [Theory]
        [InlineData("msi")]
        [InlineData(".")]
        [InlineData("")]
        public void ValidateExtensionAttribute_RefusesAMalformedExtension(string extension)
        {
            Assert.Equal(extension, Assert.Throws<ArgumentOutOfRangeException>(() => new ValidateExtensionAttribute(extension)).ActualValue);
        }

        /// <summary>
        /// Verifies that the constructor faults a malformed extension anywhere in the set, not just first.
        /// </summary>
        [Fact]
        public void ValidateExtensionAttribute_RefusesAMalformedExtensionAnywhereInTheSet()
        {
            Assert.Equal("msp", Assert.Throws<ArgumentOutOfRangeException>(static () => new ValidateExtensionAttribute(".msi", "msp", ".mst")).ActualValue);
        }

        /// <summary>
        /// Verifies that a path with an approved extension is accepted, whatever its case.
        /// </summary>
        /// <remarks>
        /// Case matters because Windows paths are case-insensitive and a caller may have taken the path from anywhere.
        /// </remarks>
        /// <param name="path">The path to accept.</param>
        [Theory]
        [InlineData(@"C:\Files\setup.msi")]
        [InlineData(@"C:\Files\setup.MSI")]
        [InlineData(@"C:\Files\patch.MsP")]
        [InlineData("setup.msi")]
        [InlineData(@"\\server\share\setup.msi")]
        public void ValidateElement_AcceptsAnApprovedExtension(string path)
        {
            ArgumentAttributes.ValidateElement(new ValidateExtensionAttribute(".msi", ".msp"), path);
        }

        /// <summary>
        /// Verifies that a path with an extension outside the set is refused, and told which are approved.
        /// </summary>
        [Fact]
        public void ValidateElement_RefusesAnUnapprovedExtension()
        {
            // Act
            ArgumentException exception = Assert.Throws<ArgumentException>(static () =>
                ArgumentAttributes.ValidateElement(new ValidateExtensionAttribute(".msi", ".msp"), @"C:\Files\setup.exe"));

            // Assert
            Assert.Contains(".exe", exception.Message, StringComparison.Ordinal);
            Assert.Contains(".msi, .msp", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a path with no extension at all is refused.
        /// </summary>
        [Fact]
        public void ValidateElement_RefusesAPathWithNoExtension()
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(static () =>
                ArgumentAttributes.ValidateElement(new ValidateExtensionAttribute(".msi"), @"C:\Files\setup"));
            Assert.Contains("does not have a valid extension", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that nothing at all is refused, in each of the shapes PowerShell uses for absence.
        /// </summary>
        /// <remarks>
        /// A CLR null is only one of them: an unset variable arrives as <c>AutomationNull</c>, a null string as
        /// <see cref="NullString"/>, and a database null as <see cref="DBNull"/>. Split by shape rather than looped so
        /// a failure names which one.
        /// </remarks>
        /// <param name="shape">What the absence is called, for the failure message.</param>
        /// <param name="nothing">The value standing for absence.</param>
        [Theory]
        [MemberData(nameof(Nothings))]
        public void ValidateElement_RefusesNothingAtAll(string shape, object? nothing)
        {
            Assert.NotNull(shape);
            _ = Assert.Throws<ArgumentNullException>(() => ArgumentAttributes.ValidateElement(new ValidateExtensionAttribute(".msi"), nothing));
        }

        /// <summary>
        /// The shapes PowerShell uses to mean nothing.
        /// </summary>
        public static TheoryData<string, object?> Nothings =>
            new()
            {
                { "null", null },
                { "AutomationNull", System.Management.Automation.Internal.AutomationNull.Value },
                { "NullString", NullString.Value },
                { "DBNull", DBNull.Value },
            };

        /// <summary>
        /// Verifies that a value which is not a string is refused for being the wrong type rather than the wrong
        /// extension.
        /// </summary>
        [Fact]
        public void ValidateElement_RefusesSomethingThatIsNotAString()
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(static () =>
                ArgumentAttributes.ValidateElement(new ValidateExtensionAttribute(".msi"), 42));
            Assert.Contains("is not a string", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a blank path is refused for being blank.
        /// </summary>
        /// <param name="path">The blank path.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateElement_RefusesABlankPath(string path)
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                ArgumentAttributes.ValidateElement(new ValidateExtensionAttribute(".msi"), path));
            Assert.Contains("null, empty, or white space", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a path wrapped by PowerShell is unwrapped before being checked.
        /// </summary>
        [Fact]
        public void ValidateElement_UnwrapsAPSObject()
        {
            ArgumentAttributes.ValidateElement(new ValidateExtensionAttribute(".msi"), PSObject.AsPSObject(@"C:\Files\setup.msi"));
        }

        /// <summary>
        /// Verifies that a collection is checked element by element.
        /// </summary>
        /// <remarks>
        /// The enumeration itself is PowerShell's, inherited from
        /// <see cref="ValidateEnumeratedArgumentsAttribute"/> - what is being confirmed is that the attribute is wired
        /// into it, so a bad element anywhere in a list is caught rather than only the first.
        /// </remarks>
        [Fact]
        public void Validate_ChecksEveryElementOfACollection()
        {
            // Arrange
            ValidateExtensionAttribute attribute = new(".msi", ".msp");

            // Assert
            ArgumentAttributes.Validate(attribute, new[] { @"C:\a.msi", @"C:\b.msp" });
            _ = Assert.Throws<ArgumentException>(() => ArgumentAttributes.Validate(attribute, new[] { @"C:\a.msi", @"C:\b.exe" }));
        }
    }
}
