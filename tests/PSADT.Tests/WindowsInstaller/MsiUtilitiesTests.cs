using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using PSADT.Tests.TestHelpers;
using PSADT.WindowsInstaller;
using Xunit;

namespace PSADT.Tests.WindowsInstaller
{
    /// <summary>
    /// Tests the Windows Installer helpers.
    /// </summary>
    /// <remarks>
    /// The tests that need a real database take their fixture from the installer cache under the Windows
    /// directory, which holds a copy of every package installed through Windows Installer. Using one of
    /// those means reading a database a real product shipped rather than one authored here, at the cost of
    /// not knowing its contents in advance - so those assertions are about the shape of what comes back
    /// and about the tables and properties every package must carry, never about a particular product's
    /// values. Every database is opened read-only, and a machine with an empty cache skips rather than
    /// fails, which an installer-free build agent will be.
    /// <para>
    /// The packed GUID form is the interesting one. Windows Installer stores product, upgrade and
    /// component codes in a 32-character form that is not simply the GUID with its braces removed: the
    /// first three fields are byte-reversed and written nibble-swapped, and the last eight bytes are
    /// written as swapped character pairs. Getting any of those three transformations wrong still
    /// produces a plausible-looking 32-character string, so the fixed vectors below are computed by hand
    /// from the documented layout rather than from the implementation.
    /// </para>
    /// </remarks>
    public sealed class MsiUtilitiesTests
    {
        /// <summary>
        /// A GUID whose every byte is distinct, so a transposition anywhere in the packing is visible.
        /// </summary>
        /// <remarks>
        /// Built from its fields rather than parsed from text, because the field boundaries are exactly
        /// what the packed form rearranges: this is 01020304-0506-0708-090A-0B0C0D0E0F10.
        /// </remarks>
        private static readonly Guid SequentialGuid = new(0x01020304, 0x0506, 0x0708, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10);

        /// <summary>
        /// The packed form of <see cref="SequentialGuid"/>.
        /// </summary>
        /// <remarks>
        /// Derived by hand. The in-memory byte order of the GUID is 04 03 02 01, 06 05, 08 07, then
        /// 09 0A 0B 0C 0D 0E 0F 10, and each byte is written low nibble first: 04 becomes "40", 03
        /// becomes "30", and so on, with the final 10 becoming "01".
        /// </remarks>
        private const string SequentialGuidPacked = "403020106050807090A0B0C0D0E0F001";

        /// <summary>
        /// Verifies that a GUID packs to the documented 32-character form.
        /// </summary>
        [Fact]
        public void CompressGuid_ProducesTheDocumentedPackedForm()
        {
            Assert.Equal(SequentialGuidPacked, MsiUtilities.CompressGuid(SequentialGuid));
        }

        /// <summary>
        /// Verifies that the empty GUID packs to all zeroes, which is the degenerate case a caller is
        /// most likely to hit by accident.
        /// </summary>
        [Fact]
        public void CompressGuid_PacksTheEmptyGuidToZeroes()
        {
            Assert.Equal(new string('0', 32), MsiUtilities.CompressGuid(Guid.Empty));
        }

        /// <summary>
        /// Verifies that the packed form is always 32 upper-case hexadecimal characters, whatever the
        /// input, since Windows Installer will not accept anything else.
        /// </summary>
        /// <param name="guidString">The GUID to pack.</param>
        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        [InlineData("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")]
        [InlineData("01020304-0506-0708-090A-0B0C0D0E0F10")]
        [InlineData("DEADBEEF-1234-5678-9ABC-DEF012345678")]
        [InlineData("2E5E1E4F-0A9C-4E1F-8B7A-6C5D4E3F2A1B")]
        public void CompressGuid_AlwaysProducesUpperCaseHexadecimal(string guidString)
        {
            // Act
            string packed = MsiUtilities.CompressGuid(new Guid(guidString));

            // Assert
            Assert.Equal(32, packed.Length);
            Assert.All(packed, static c => Assert.True(c is (>= '0' and <= '9') or (>= 'A' and <= 'F'), $"'{c}' is not upper-case hexadecimal."));
        }

        /// <summary>
        /// Verifies that the packed form unpacks to the GUID it was built from, which is the property
        /// every caller reading a product code out of a database depends on.
        /// </summary>
        [Fact]
        public void DecompressPackedGuid_ReversesTheDocumentedPackedForm()
        {
            Assert.Equal(SequentialGuid, MsiUtilities.DecompressPackedGuid(SequentialGuidPacked));
        }

        /// <summary>
        /// Verifies that packing and unpacking are inverse, across GUIDs chosen to exercise each field
        /// of the layout independently.
        /// </summary>
        /// <param name="guidString">The GUID to send through both directions.</param>
        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        [InlineData("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")]
        [InlineData("01020304-0506-0708-090A-0B0C0D0E0F10")]
        [InlineData("DEADBEEF-1234-5678-9ABC-DEF012345678")]
        [InlineData("2E5E1E4F-0A9C-4E1F-8B7A-6C5D4E3F2A1B")]
        // One field non-zero at a time, so a field written into the wrong slot cannot cancel out.
        [InlineData("FFFFFFFF-0000-0000-0000-000000000000")]
        [InlineData("00000000-FFFF-0000-0000-000000000000")]
        [InlineData("00000000-0000-FFFF-0000-000000000000")]
        [InlineData("00000000-0000-0000-FFFF-000000000000")]
        [InlineData("00000000-0000-0000-0000-FFFFFFFFFFFF")]
        public void CompressGuid_RoundTripsThroughDecompressPackedGuid(string guidString)
        {
            // Arrange
            Guid original = new(guidString);

            // Act & Assert
            Assert.Equal(original, MsiUtilities.DecompressPackedGuid(MsiUtilities.CompressGuid(original)));
        }

        /// <summary>
        /// Verifies that lower-case hexadecimal is accepted when unpacking, since a database or a
        /// registry key written by another tool need not match this implementation's casing.
        /// </summary>
        [Fact]
        public void DecompressPackedGuid_AcceptsLowerCaseHexadecimal()
        {
            Assert.Equal(
                MsiUtilities.DecompressPackedGuid(SequentialGuidPacked),
                MsiUtilities.DecompressPackedGuid(SequentialGuidPacked.ToLowerInvariant()));
        }

        /// <summary>
        /// Verifies that anything other than exactly 32 characters is rejected, rather than read past
        /// the end of or short of the buffer.
        /// </summary>
        /// <param name="packed">The malformed input to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("4030201060508070")]
        [InlineData("403020106050807090A0B0C0D0E0F00")]
        [InlineData("403020106050807090A0B0C0D0E0F0011")]
        public void DecompressPackedGuid_RejectsAnythingButThirtyTwoCharacters(string packed)
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => MsiUtilities.DecompressPackedGuid(packed));
        }

        /// <summary>
        /// Verifies that a null input is rejected as an out-of-range length rather than dereferenced,
        /// because the span overload treats a null string as empty.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void DecompressPackedGuid_RejectsNullAsAnEmptyInput()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => MsiUtilities.DecompressPackedGuid(null!));
        }

        /// <summary>
        /// Verifies that a non-hexadecimal character anywhere in the input is rejected, including at the
        /// first and last positions where an off-by-one in the validation loop would miss it.
        /// </summary>
        /// <param name="packed">The malformed input to reject.</param>
        [Theory]
        [InlineData("G03020106050807090A0B0C0D0E0F001")]
        [InlineData("403020106050807090A0B0C0D0E0F00G")]
        [InlineData("40302010605080709 A0B0C0D0E0F001")]
        [InlineData("40302010605080709-A0B0C0D0E0F001")]
        public void DecompressPackedGuid_RejectsNonHexadecimalCharacters(string packed)
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => MsiUtilities.DecompressPackedGuid(packed));
        }

        /// <summary>
        /// Verifies the version layout Windows Installer packs into a single word: major in the high
        /// byte, minor in the next, and build in the low half.
        /// </summary>
        /// <param name="packed">The packed version value.</param>
        /// <param name="major">The expected major version.</param>
        /// <param name="minor">The expected minor version.</param>
        /// <param name="build">The expected build number.</param>
        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(0x0102_0304, 1, 2, 772)]
        [InlineData(0x0A14_0BB8, 10, 20, 3000)]
        [InlineData(0x0100_0000, 1, 0, 0)]
        [InlineData(0x0001_0000, 0, 1, 0)]
        [InlineData(0x0000_FFFF, 0, 0, 65_535)]
        [InlineData(0x7F7F_FFFF, 127, 127, 65_535)]
        public void ParseVersionDWord_SplitsTheMajorMinorAndBuild(int packed, int major, int minor, int build)
        {
            // Act
            Version version = MsiUtilities.ParseVersionDWord(packed);

            // Assert
            Assert.Equal(major, version.Major);
            Assert.Equal(minor, version.Minor);
            Assert.Equal(build, version.Build);
        }

        /// <summary>
        /// Verifies that a value with the top bit set is read as an unsigned byte rather than sign
        /// extended, since the field is a version number and cannot be negative.
        /// </summary>
        [Fact]
        public void ParseVersionDWord_TreatsTheHighByteAsUnsigned()
        {
            // Act
            Version version = MsiUtilities.ParseVersionDWord(unchecked((int)0xFFFF_FFFF));

            // Assert
            Assert.Equal(new Version(255, 255, 65_535), version);
        }

        /// <summary>
        /// Verifies that an installer exit code becomes an exception carrying that code, which is what
        /// lets a caller rethrow it without losing the original result.
        /// </summary>
        /// <param name="exitCode">The exit code to translate.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1603)]
        [InlineData(1618)]
        [InlineData(1641)]
        [InlineData(3010)]
        public void GetExceptionForMsiExitCode_CarriesTheExitCode(int exitCode)
        {
            // Act
            Win32Exception exception = MsiUtilities.GetExceptionForMsiExitCode(exitCode);

            // Assert
            Assert.Equal(exitCode, exception.NativeErrorCode);
        }

        /// <summary>
        /// Verifies the shape of the message, which appends the symbolic name in brackets to the
        /// system's description and must not double up the sentence's full stop.
        /// </summary>
        /// <param name="exitCode">The exit code to translate.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1603)]
        [InlineData(1618)]
        [InlineData(3010)]
        public void GetExceptionForMsiExitCode_AppendsTheSymbolicNameOnce(int exitCode)
        {
            // Act
            string message = MsiUtilities.GetExceptionForMsiExitCode(exitCode).Message;

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.EndsWith(").", message, StringComparison.Ordinal);
            Assert.DoesNotContain("..", message, StringComparison.Ordinal);
            Assert.Equal(1, message.Split('(').Length - 1);
        }

        /// <summary>
        /// Verifies that the property table reads back as a dictionary carrying the product code, which is
        /// the property every installable package defines.
        /// </summary>
        [Fact(Skip = "No readable installer was found in the Windows Installer cache.", SkipUnless = nameof(TestEnvironment.HasCachedMsiPackage), SkipType = typeof(TestEnvironment))]
        public void GetMsiTableDictionary_ReadsThePropertyTable()
        {
            // Arrange
            FileInfo? package = TestEnvironment.CachedMsiPackage;
            Assert.NotNull(package);

            // Act
            IReadOnlyDictionary<string, object>? properties = MsiUtilities.GetMsiTableDictionary(package.FullName, "Property", 1, 2);

            // Assert
            Assert.NotNull(properties);
            Assert.NotEmpty(properties);
            Assert.True(properties.ContainsKey("ProductCode"), "Expected the package to define a ProductCode.");
        }

        /// <summary>
        /// Verifies that the table name is matched with regard to case, so a caller has to spell it as the
        /// database does.
        /// </summary>
        /// <remarks>
        /// This is the installer's own behaviour rather than a choice made here: the name is resolved by
        /// querying the catalogue with a SQL equality, and installer SQL compares strings case sensitively.
        /// Pinned because it is the opposite of what the surrounding INI and registry helpers do, and the
        /// difference is invisible from the signature.
        /// </remarks>
        [Fact(Skip = "No readable installer was found in the Windows Installer cache.", SkipUnless = nameof(TestEnvironment.HasCachedMsiPackage), SkipType = typeof(TestEnvironment))]
        public void GetMsiTableDictionary_MatchesTheTableNameWithRegardToCase()
        {
            // Arrange
            FileInfo? package = TestEnvironment.CachedMsiPackage;
            Assert.NotNull(package);

            // Act & Assert: the exact spelling resolves, and the others are reported as absent
            Assert.NotNull(MsiUtilities.GetMsiTableDictionary(package.FullName, "Property", 1, 2));
            _ = Assert.Throws<InvalidDataException>(() => MsiUtilities.GetMsiTableDictionary(package.FullName, "property", 1, 2));
            _ = Assert.Throws<InvalidDataException>(() => MsiUtilities.GetMsiTableDictionary(package.FullName, "PROPERTY", 1, 2));
        }

        /// <summary>
        /// Verifies that a table the database does not have is reported as bad data rather than as an
        /// empty result, which would read as a table that exists and holds nothing.
        /// </summary>
        [Fact(Skip = "No readable installer was found in the Windows Installer cache.", SkipUnless = nameof(TestEnvironment.HasCachedMsiPackage), SkipType = typeof(TestEnvironment))]
        public void GetMsiTableDictionary_ReportsATableThatIsNotThere()
        {
            // Arrange
            FileInfo? package = TestEnvironment.CachedMsiPackage;
            Assert.NotNull(package);

            // Act & Assert
            _ = Assert.Throws<InvalidDataException>(() => MsiUtilities.GetMsiTableDictionary(package.FullName, "NoSuchTable", 1, 2));
        }

        /// <summary>
        /// Verifies that a column number the table does not have is reported, for both the key and the
        /// value, since the two are resolved separately.
        /// </summary>
        [Fact(Skip = "No readable installer was found in the Windows Installer cache.", SkipUnless = nameof(TestEnvironment.HasCachedMsiPackage), SkipType = typeof(TestEnvironment))]
        public void GetMsiTableDictionary_ReportsAColumnThatIsNotThere()
        {
            // Arrange
            FileInfo? package = TestEnvironment.CachedMsiPackage;
            Assert.NotNull(package);

            // Act & Assert
            _ = Assert.Throws<InvalidDataException>(() => MsiUtilities.GetMsiTableDictionary(package.FullName, "Property", 99, 2));
            _ = Assert.Throws<InvalidDataException>(() => MsiUtilities.GetMsiTableDictionary(package.FullName, "Property", 1, 99));
        }

        /// <summary>
        /// Verifies that a single column reads back as a list of values, which is the other shape callers
        /// ask the database for.
        /// </summary>
        [Fact(Skip = "No readable installer was found in the Windows Installer cache.", SkipUnless = nameof(TestEnvironment.HasCachedMsiPackage), SkipType = typeof(TestEnvironment))]
        public void GetMsiTableColumnValues_ReadsASingleColumn()
        {
            // Arrange
            FileInfo? package = TestEnvironment.CachedMsiPackage;
            Assert.NotNull(package);

            // Act
            IReadOnlyList<object> names = MsiUtilities.GetMsiTableColumnValues(package.FullName, "Property", 1);

            // Assert
            Assert.NotEmpty(names);
            Assert.Contains("ProductCode", names);
        }

        /// <summary>
        /// Verifies that the state of a product that was never installed is reported as unknown rather
        /// than as installed, which is what decides whether an uninstall is attempted.
        /// </summary>
        [Fact]
        public void QueryProductState_ReportsAnUnknownProductAsUnknown()
        {
            Assert.Equal(Interop.INSTALLSTATE.INSTALLSTATE_UNKNOWN, MsiUtilities.QueryProductState(Guid.NewGuid()));
        }

        /// <summary>
        /// Verifies that a patch's supported product codes read back as parsed identifiers.
        /// </summary>
        [Fact(Skip = "No readable patch was found in the Windows Installer cache.", SkipUnless = nameof(TestEnvironment.HasCachedMspPackage), SkipType = typeof(TestEnvironment))]
        public void GetMspSupportedProductCodes_ReadsAPatch()
        {
            // Arrange
            FileInfo? patch = TestEnvironment.CachedMspPackage;
            Assert.NotNull(patch);

            // Act
            IReadOnlyList<Guid> productCodes = MsiUtilities.GetMspSupportedProductCodes(patch.FullName);

            // Assert
            Assert.NotEmpty(productCodes);
            Assert.All(productCodes, static code => Assert.NotEqual(Guid.Empty, code));
        }

        /// <summary>
        /// Verifies that a patch's metadata reads back as a document, through the same hardened loader the
        /// rest of the assembly uses for untrusted XML.
        /// </summary>
        [Fact(Skip = "No readable patch was found in the Windows Installer cache.", SkipUnless = nameof(TestEnvironment.HasCachedMspPackage), SkipType = typeof(TestEnvironment))]
        public void ExtractPatchXmlData_ReadsAPatchAsADocument()
        {
            // Arrange
            FileInfo? patch = TestEnvironment.CachedMspPackage;
            Assert.NotNull(patch);

            // Act
            XmlDocument document = MsiUtilities.ExtractPatchXmlData(patch.FullName);

            // Assert
            Assert.NotNull(document.DocumentElement);
            Assert.False(string.IsNullOrWhiteSpace(document.DocumentElement.Name));
        }

        /// <summary>
        /// Verifies that applying a transform to a patch is refused, since a patch is opened in a mode
        /// that cannot accept one and the failure would otherwise come from deep inside the installer.
        /// </summary>
        [Fact(Skip = "No readable patch was found in the Windows Installer cache.", SkipUnless = nameof(TestEnvironment.HasCachedMspPackage), SkipType = typeof(TestEnvironment))]
        public void GetMsiTableDictionary_RefusesTransformsAgainstAPatch()
        {
            // Arrange
            FileInfo? patch = TestEnvironment.CachedMspPackage;
            Assert.NotNull(patch);

            // Act & Assert
            _ = Assert.Throws<NotSupportedException>(() => MsiUtilities.GetMsiTableDictionary(patch.FullName, "Property", 1, 2, [patch.FullName]));
        }

        /// <summary>
        /// Verifies that an empty transform list is rejected, since a caller passing one has lost its
        /// contents rather than meaning "no transforms".
        /// </summary>
        [Fact(Skip = "No readable installer was found in the Windows Installer cache.", SkipUnless = nameof(TestEnvironment.HasCachedMsiPackage), SkipType = typeof(TestEnvironment))]
        public void GetMsiTableDictionary_RejectsAnEmptyTransformList()
        {
            // Arrange
            FileInfo? package = TestEnvironment.CachedMsiPackage;
            Assert.NotNull(package);

            // Act & Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => MsiUtilities.GetMsiTableDictionary(package.FullName, "Property", 1, 2, []));
        }

        /// <summary>
        /// Verifies that a transform is written for the properties it was given, and that it is a real
        /// one rather than an empty file.
        /// </summary>
        /// <remarks>
        /// Written into a temporary directory that is removed afterwards; the cached package it is
        /// derived from is only read. A transform is how a deployment overrides an installer's properties
        /// without editing it, so the file being produced at all is the part worth asserting - what it
        /// contains is the installer's own format rather than anything this library composes.
        /// <para>
        /// The property is one no real package carries, and that matters: the installer reports "no data"
        /// rather than writing a transform when the two databases turn out to be identical, so a property
        /// whose value the package already held would produce nothing and read as a failure.
        /// </para>
        /// </remarks>
        [Fact(Skip = "No readable installer was found in the Windows Installer cache.", SkipUnless = nameof(TestEnvironment.HasCachedMsiPackage), SkipType = typeof(TestEnvironment))]
        public void CreatePropertyTransformFile_WritesATransform()
        {
            // Arrange
            FileInfo? package = TestEnvironment.CachedMsiPackage;
            Assert.NotNull(package);
            using TempDirectory temp = new();
            string transformPath = temp.GetPath("properties.mst");

            // Act
            MsiUtilities.CreatePropertyTransformFile(package.FullName, transformPath, new Dictionary<string, string>(StringComparer.Ordinal) { ["PSADTTESTSPROPERTY"] = "1" });

            // Assert
            FileInfo transform = new(transformPath);
            Assert.True(transform.Exists, $"No transform was written to {transformPath}.");
            Assert.True(transform.Length > 0, "The transform written was empty.");
        }

        /// <summary>
        /// Verifies that a transform asked for with no properties is refused, since it would describe no
        /// change at all.
        /// </summary>
        [Fact]
        public void CreatePropertyTransformFile_RefusesAnEmptyPropertySet()
        {
            using TempDirectory temp = new();
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => MsiUtilities.CreatePropertyTransformFile(
                temp.WriteFile("package.msi", "not a database"),
                temp.GetPath("properties.mst"),
                new Dictionary<string, string>(StringComparer.Ordinal)));
        }

        /// <summary>
        /// Verifies that a transform asked for against a package that is not there is reported before
        /// anything is written.
        /// </summary>
        [Fact]
        public void CreatePropertyTransformFile_ReportsAPackageThatIsNotThere()
        {
            // Arrange
            using TempDirectory temp = new();
            string transformPath = temp.GetPath("properties.mst");

            // Act & Assert
            _ = Assert.Throws<FileNotFoundException>(() => MsiUtilities.CreatePropertyTransformFile(
                temp.GetPath("absent.msi"),
                transformPath,
                new Dictionary<string, string>(StringComparer.Ordinal) { ["PSADTTESTSPROPERTY"] = "1" }));
            Assert.False(File.Exists(transformPath), "A transform was written for a package that does not exist.");
        }

        /// <summary>
        /// Verifies that no properties at all is refused as a null argument.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void CreatePropertyTransformFile_RefusesNoPropertiesAtAll()
        {
            using TempDirectory temp = new();
            _ = Assert.Throws<ArgumentNullException>(() => MsiUtilities.CreatePropertyTransformFile(
                temp.WriteFile("package.msi", "not a database"),
                temp.GetPath("properties.mst"),
                null!));
        }
    }
}
