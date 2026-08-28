using System;
using System.IO;
using PSADT.AppManagement;
using Xunit;

namespace PSADT.Tests.AppManagement
{
    /// <summary>
    /// Tests the installed application record.
    /// </summary>
    /// <remarks>
    /// The work worth testing is the uninstall strings. Both arrive from the registry as a single command
    /// line and are split into an executable and its arguments, which is what a caller needs to launch an
    /// uninstall without going through a shell. Registry uninstall strings are notoriously inconsistent -
    /// quoted, unquoted, with and without switches - so the splitting is exercised across the shapes that
    /// actually appear.
    /// </remarks>
    public sealed class InstalledApplicationTests
    {
        /// <summary>
        /// Verifies that an uninstall string is split into the executable and the arguments following it.
        /// </summary>
        /// <param name="uninstallString">The registry value to split.</param>
        /// <param name="expectedPath">The executable the split should yield.</param>
        /// <param name="expectedArguments">The arguments the split should yield.</param>
        [Theory]
        [MemberData(nameof(UninstallStringCases))]
        public void Constructor_SplitsTheUninstallString(string uninstallString, string expectedPath, string[] expectedArguments)
        {
            // Act
            InstalledApplication application = Create(uninstallString: uninstallString);

            // Assert: compared through FileInfo so an unrooted expectation is resolved the same way the
            // record resolves it, rather than the test asserting a path the type never produces
            Assert.Equal(new FileInfo(expectedPath).FullName, application.UninstallStringFilePath?.FullName);
            Assert.Equal(expectedArguments, application.UninstallStringArgumentList);
        }

        /// <summary>
        /// Verifies that an uninstall string naming no directory is resolved against the current
        /// directory, which is what wrapping it in a <see cref="FileInfo"/> does.
        /// </summary>
        /// <remarks>
        /// Worth stating outright because plenty of registry uninstall strings are unrooted, and the
        /// resulting path then depends on wherever the calling process happens to be rather than on
        /// anything the registry said. A caller wanting the executable resolved against the search path
        /// has to use the name, not the full path.
        /// </remarks>
        [Fact]
        public void UninstallStringFilePath_ResolvesAnUnrootedNameAgainstTheCurrentDirectory()
        {
            // Act
            InstalledApplication application = Create(uninstallString: "MsiExec.exe /X{12345678-1234-1234-1234-123456789012}");

            // Assert
            Assert.Equal("MsiExec.exe", application.UninstallStringFilePath?.Name);
            Assert.Equal(Directory.GetCurrentDirectory(), application.UninstallStringFilePath?.DirectoryName);
        }

        /// <summary>
        /// Verifies that the quiet uninstall string is split independently of the ordinary one, so a
        /// caller preferring the quiet form gets its own arguments rather than the other's.
        /// </summary>
        [Fact]
        public void Constructor_SplitsBothUninstallStringsIndependently()
        {
            // Act
            InstalledApplication application = Create(
                uninstallString: @"""C:\Program Files\App\uninstall.exe"" /interactive",
                quietUninstallString: @"""C:\Program Files\App\uninstall.exe"" /silent /norestart");

            // Assert
            Assert.Equal(@"C:\Program Files\App\uninstall.exe", application.UninstallStringFilePath?.FullName);
            Assert.Equal(["/interactive"], application.UninstallStringArgumentList);
            Assert.Equal(@"C:\Program Files\App\uninstall.exe", application.QuietUninstallStringFilePath?.FullName);
            Assert.Equal(["/silent", "/norestart"], application.QuietUninstallStringArgumentList);
        }

        /// <summary>
        /// Verifies that an absent uninstall string leaves the derived members unset rather than
        /// producing an empty path, so a caller can tell "no uninstall command" from "an empty one".
        /// </summary>
        [Fact]
        public void Constructor_LeavesTheDerivedMembersUnsetWithoutAnUninstallString()
        {
            // Act
            InstalledApplication application = Create();

            // Assert
            Assert.Null(application.UninstallString);
            Assert.Null(application.UninstallStringFilePath);
            Assert.Empty(application.UninstallStringArgumentList);
            Assert.Null(application.QuietUninstallString);
            Assert.Null(application.QuietUninstallStringFilePath);
            Assert.Empty(application.QuietUninstallStringArgumentList);
        }

        /// <summary>
        /// Verifies that the required registry identifiers and display name are rejected when blank,
        /// since a record without them cannot be matched back to what it describes.
        /// </summary>
        /// <param name="blank">The blank value to supply.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_RejectsBlankRequiredValues(string blank)
        {
            _ = Assert.Throws<ArgumentException>(() => Create(psPath: blank));
            _ = Assert.Throws<ArgumentException>(() => Create(psParentPath: blank));
            _ = Assert.Throws<ArgumentException>(() => Create(psChildName: blank));
            _ = Assert.Throws<ArgumentException>(() => Create(displayName: blank));
        }

        /// <summary>
        /// Verifies that the optional strings are rejected when present but blank, while being accepted
        /// when absent. A registry value that exists and holds only whitespace is bad data rather than a
        /// missing value.
        /// </summary>
        [Fact]
        public void Constructor_RejectsOptionalStringsThatArePresentButBlank()
        {
            _ = Assert.Throws<ArgumentException>(static () => Create(displayVersion: "   "));
            _ = Assert.Throws<ArgumentException>(static () => Create(uninstallString: "   "));
            _ = Assert.Throws<ArgumentException>(static () => Create(quietUninstallString: "   "));
            _ = Assert.Throws<ArgumentException>(static () => Create(publisher: "   "));
            Assert.Null(Record.Exception(static () => Create()));
        }

        /// <summary>
        /// Verifies that the product code is reported as valid only when one is present, which is what
        /// decides whether an uninstall can go through Windows Installer.
        /// </summary>
        [Fact]
        public void IsValidProductCode_ReflectsWhetherAProductCodeIsPresent()
        {
            Assert.False(Create().IsValidProductCode());
            Assert.True(Create(productCode: Guid.NewGuid()).IsValidProductCode());
            Assert.True(Create(productCode: Guid.Empty).IsValidProductCode());
        }

        /// <summary>
        /// Verifies the summary line, which is what appears in a log when an application is matched.
        /// </summary>
        [Fact]
        public void ToString_SummarisesTheApplication()
        {
            // Act
            string summary = Create(displayName: "Test App", displayVersion: "1.2.3", publisher: "Devicie").ToString();

            // Assert
            Assert.Equal("Installed Application: Test App (Version: 1.2.3, Publisher: Devicie)", summary);
        }

        /// <summary>
        /// Uninstall strings paired with the executable and arguments they should split into.
        /// </summary>
        public static TheoryData<string, string, string[]> UninstallStringCases
        {
            get
            {
                TheoryData<string, string, string[]> data = [];

                // Quoted path, which is the well-formed case.
                data.Add(@"""C:\Program Files\App\uninstall.exe""", @"C:\Program Files\App\uninstall.exe", []);
                data.Add(@"""C:\Program Files\App\uninstall.exe"" /S", @"C:\Program Files\App\uninstall.exe", ["/S"]);
                data.Add(@"""C:\Program Files\App\uninstall.exe"" /S /norestart", @"C:\Program Files\App\uninstall.exe", ["/S", "/norestart"]);

                // Unquoted path with no spaces, which plenty of installers write.
                data.Add(@"C:\Windows\uninstall.exe", @"C:\Windows\uninstall.exe", []);
                data.Add(@"C:\Windows\uninstall.exe /S", @"C:\Windows\uninstall.exe", ["/S"]);

                // Unquoted path containing a space, which the parser recognises as a path.
                data.Add(@"C:\Program Files\App\uninstall.exe", @"C:\Program Files\App\uninstall.exe", []);

                // The Windows Installer form, where the executable takes the product code.
                data.Add("MsiExec.exe /X{12345678-1234-1234-1234-123456789012}", "MsiExec.exe", ["/X{12345678-1234-1234-1234-123456789012}"]);
                data.Add(@"C:\Windows\System32\msiexec.exe /x {12345678-1234-1234-1234-123456789012} /qn", @"C:\Windows\System32\msiexec.exe", ["/x", "{12345678-1234-1234-1234-123456789012}", "/qn"]);

                // An argument that is itself a quoted path.
                data.Add(@"""C:\App\setup.exe"" /log ""C:\Program Files\log.txt""", @"C:\App\setup.exe", ["/log", @"C:\Program Files\log.txt"]);

                return data;
            }
        }

        /// <summary>
        /// Builds a record, naming every constructor argument once so the tests above can vary only what
        /// they care about.
        /// </summary>
        /// <param name="psPath">The provider path of the registry key.</param>
        /// <param name="psParentPath">The provider path of the key's parent.</param>
        /// <param name="psChildName">The key's own name.</param>
        /// <param name="productCode">The Windows Installer product code, if any.</param>
        /// <param name="upgradeCode">The Windows Installer upgrade code, if any.</param>
        /// <param name="displayName">The application's display name.</param>
        /// <param name="displayVersion">The application's display version, if any.</param>
        /// <param name="uninstallString">The uninstall command line, if any.</param>
        /// <param name="quietUninstallString">The silent uninstall command line, if any.</param>
        /// <param name="publisher">The publisher, if any.</param>
        /// <returns>The constructed record.</returns>
        private static InstalledApplication Create(
            string psPath = @"Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Test",
            string psParentPath = @"Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE",
            string psChildName = "Test",
            Guid? productCode = null,
            Guid? upgradeCode = null,
            string displayName = "Test App",
            string? displayVersion = null,
            string? uninstallString = null,
            string? quietUninstallString = null,
            string? publisher = null)
        {
            return new(
                psPath: psPath,
                psParentPath: psParentPath,
                psChildName: psChildName,
                productCode: productCode,
                upgradeCode: upgradeCode,
                displayName: displayName,
                displayVersion: displayVersion,
                uninstallString: uninstallString,
                quietUninstallString: quietUninstallString,
                installSource: null,
                installLocation: null,
                installDate: null,
                publisher: publisher,
                helpLink: null,
                estimatedSize: null,
                systemComponent: false,
                windowsInstaller: false,
                noRemove: false,
                is64BitApplication: null);
        }

        /// <summary>
        /// Verifies that every value handed in is the value read back, since they are passed positionally
        /// through a constructor with nineteen parameters.
        /// </summary>
        /// <remarks>
        /// Nothing here is computed - it is all carried straight through from the registry - which is
        /// exactly why it is worth asserting. Two arguments transposed at the call site would produce a
        /// record that looks entirely reasonable and describes the wrong thing, and the values are
        /// distinct here so that a transposition surfaces as the wrong one being reported.
        /// </remarks>
        [Fact]
        public void InstalledApplication_KeepsEveryValueItIsGiven()
        {
            // Arrange
            Guid productCode = new(0x11111111, 0x1111, 0x1111, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11);
            Guid upgradeCode = new(0x22222222, 0x2222, 0x2222, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22);
            DateTime installDate = new(2026, 8, 27, 0, 0, 0, DateTimeKind.Local);

            // Act
            InstalledApplication application = new(
                psPath: @"Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Contoso",
                psParentPath: @"Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE",
                psChildName: "Contoso",
                productCode: productCode,
                upgradeCode: upgradeCode,
                displayName: "Contoso Application",
                displayVersion: "1.2.3",
                uninstallString: @"""C:\Program Files\Contoso\uninstall.exe"" /quiet",
                quietUninstallString: @"""C:\Program Files\Contoso\uninstall.exe"" /silent",
                installSource: new DirectoryInfo(@"C:\Sources\Contoso"),
                installLocation: new DirectoryInfo(@"C:\Program Files\Contoso"),
                installDate: installDate,
                publisher: "Contoso Ltd",
                helpLink: new Uri("https://contoso.example/support"),
                estimatedSize: 4096,
                systemComponent: true,
                windowsInstaller: true,
                noRemove: true,
                is64BitApplication: true);

            // Assert: where it came from in the registry
            Assert.Equal(@"Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Contoso", application.PSPath, StringComparer.Ordinal);
            Assert.Equal(@"Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE", application.PSParentPath, StringComparer.Ordinal);
            Assert.Equal("Contoso", application.PSChildName, StringComparer.Ordinal);

            // Assert: what it is
            Assert.Equal(productCode, application.ProductCode);
            Assert.Equal(upgradeCode, application.UpgradeCode);
            Assert.Equal("Contoso Application", application.DisplayName, StringComparer.Ordinal);
            Assert.Equal("1.2.3", application.DisplayVersion, StringComparer.Ordinal);
            Assert.Equal("Contoso Ltd", application.Publisher, StringComparer.Ordinal);
            Assert.Equal(new Uri("https://contoso.example/support"), application.HelpLink);

            // Assert: where it lives
            Assert.NotNull(application.InstallSource);
            Assert.Equal(@"C:\Sources\Contoso", application.InstallSource.FullName, StringComparer.OrdinalIgnoreCase);
            Assert.NotNull(application.InstallLocation);
            Assert.Equal(@"C:\Program Files\Contoso", application.InstallLocation.FullName, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(installDate, application.InstallDate);
            Assert.Equal(4096u, application.EstimatedSize);

            // Assert: and the flags, which are the ones a transposition would hide in
            Assert.True(application.SystemComponent);
            Assert.True(application.WindowsInstaller);
            Assert.True(application.NoRemove);
            Assert.True(application.Is64BitApplication);
        }

        /// <summary>
        /// Verifies that everything optional is reported as absent when it was not supplied, so a caller
        /// can tell an application that declared nothing from one that declared nothing useful.
        /// </summary>
        [Fact]
        public void InstalledApplication_ReportsAbsentValuesAsAbsent()
        {
            // Act
            InstalledApplication application = Create();

            // Assert
            Assert.Null(application.ProductCode);
            Assert.Null(application.UpgradeCode);
            Assert.Null(application.DisplayVersion);
            Assert.Null(application.InstallSource);
            Assert.Null(application.InstallLocation);
            Assert.Null(application.InstallDate);
            Assert.Null(application.Publisher);
            Assert.Null(application.HelpLink);
            Assert.Null(application.EstimatedSize);
            Assert.Null(application.Is64BitApplication);
            Assert.False(application.SystemComponent);
            Assert.False(application.WindowsInstaller);
            Assert.False(application.NoRemove);
        }

        /// <summary>
        /// Verifies that two records describing the same installed application are equal, and that a
        /// difference in the uninstall command line - which the argument list is split out of - makes
        /// them unequal.
        /// </summary>
        /// <remarks>
        /// Applications are collected into lists that are compared and deduplicated as a whole, so this
        /// has to hold. It did not for a long while: the record carried the uninstaller's path as a
        /// <see cref="System.IO.FileInfo"/> and its arguments as a collection, and neither of those
        /// compares by value, so no two records ever matched however alike they were. Both are recorded
        /// in forms that compare by their contents instead.
        /// </remarks>
        [Fact]
        public void Equality_IsByValueIncludingTheUninstallerAndItsArguments()
        {
            // Arrange
            InstalledApplication left = Create(uninstallString: @"""C:\Program Files\App\uninstall.exe"" /quiet /norestart");
            InstalledApplication right = Create(uninstallString: @"""C:\Program Files\App\uninstall.exe"" /quiet /norestart");

            // Assert
            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());

            // Assert: and both the path and the arguments count towards the comparison
            Assert.NotEqual(left, Create(uninstallString: @"""C:\Program Files\Other\uninstall.exe"" /quiet /norestart"));
            Assert.NotEqual(left, Create(uninstallString: @"""C:\Program Files\App\uninstall.exe"" /quiet"));
        }
    }
}
