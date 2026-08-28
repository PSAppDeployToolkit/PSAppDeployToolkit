using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using PSADT.FileSystem;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.FileSystem
{
    /// <summary>
    /// Tests the portable executable header reader.
    /// </summary>
    /// <remarks>
    /// What this decides matters more than it looks: <c>ProcessLaunchInfo</c> asks it whether a file is a
    /// console or a windowed application, and uses the answer to decide whether to attach pipes to a
    /// launched process. A misread subsystem means output silently goes nowhere.
    /// <para>
    /// The malformed cases are assembled byte by byte rather than by corrupting a real binary, so each one
    /// isolates exactly one broken field and the reader has to reject it for the stated reason rather than
    /// for an incidental one.
    /// </para>
    /// </remarks>
    public sealed class ExecutableInfoTests
    {
        /// <summary>
        /// Verifies that the test host reads as a managed console application, which is what it is.
        /// </summary>
        [Fact]
        public void Get_ReadsTheTestHostAsAManagedConsoleApplication()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            string? testHost = current.MainModule?.FileName;
            Assert.NotNull(testHost);

            // Act
            ExecutableInfo info = ExecutableInfo.Get(testHost);

            // Assert
            Assert.Equal(testHost, info.FileInfo.FullName);
            Assert.Equal(Interop.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CUI, info.Subsystem);
            Assert.NotEqual(0u, info.EntryPoint);
            Assert.NotEqual(0ul, info.ImageBase);
        }

        /// <summary>
        /// Verifies that a native operating system binary reads with the subsystem it was linked for, and
        /// is not mistaken for a managed one.
        /// </summary>
        /// <param name="fileName">The system binary to read.</param>
        /// <param name="expectedSubsystem">The subsystem it was linked for.</param>
        [Theory]
        [InlineData("notepad.exe", Interop.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_GUI)]
        [InlineData("cmd.exe", Interop.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CUI)]
        [InlineData("kernel32.dll", Interop.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CUI)]
        public void Get_ReadsTheSubsystemOfANativeBinary(string fileName, Interop.IMAGE_SUBSYSTEM expectedSubsystem)
        {
            // Arrange
            string path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.System), fileName);
            Assert.True(File.Exists(path), $"Expected {path} to exist.");

            // Act
            ExecutableInfo info = ExecutableInfo.Get(path);

            // Assert
            Assert.Equal(expectedSubsystem, info.Subsystem);
            Assert.False(info.IsDotNetExecutable);
        }

        /// <summary>
        /// Verifies that the machine architecture read from the header matches the one the process is
        /// running as, which it must for a system binary on the same machine.
        /// </summary>
        [Fact]
        public void Get_ReadsTheMachineArchitecture()
        {
            // Arrange
            string path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.System), "kernel32.dll");

            // Act
            ExecutableInfo info = ExecutableInfo.Get(path);

            // Assert
            Dictionary<Architecture, Interop.IMAGE_FILE_MACHINE> expected = new()
            {
                [Architecture.X64] = Interop.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AMD64,
                [Architecture.X86] = Interop.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386,
                [Architecture.Arm64] = Interop.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM64,
            };
            Assert.True(expected.TryGetValue(RuntimeInformation.OSArchitecture, out Interop.IMAGE_FILE_MACHINE machine), $"No machine value is mapped for {RuntimeInformation.OSArchitecture}.");
            Assert.Equal(machine, info.Machine);
        }

        /// <summary>
        /// Verifies that a file whose first two bytes are not the DOS signature is rejected before
        /// anything else is read.
        /// </summary>
        [Fact]
        public void Get_RejectsAFileWithoutTheDosSignature()
        {
            // Arrange
            using TempDirectory temp = new();
            byte[] image = NewDosHeader();
            image[0] = (byte)'Z';
            image[1] = (byte)'M';
            string path = WriteImage(temp, "baddos.bin", image);

            // Act & Assert
            _ = Assert.Throws<BadImageFormatException>(() => ExecutableInfo.Get(path));
        }

        /// <summary>
        /// Verifies that a file with a valid DOS signature but no portable executable signature where the
        /// header points is rejected.
        /// </summary>
        [Fact]
        public void Get_RejectsAFileWithoutThePortableExecutableSignature()
        {
            // Arrange
            using TempDirectory temp = new();
            byte[] image = new byte[DosHeaderSize + 4];
            NewDosHeader().CopyTo(image, 0);
            image[DosHeaderSize + 0] = (byte)'N';
            image[DosHeaderSize + 1] = (byte)'E';
            string path = WriteImage(temp, "badsig.bin", image);

            // Act & Assert
            _ = Assert.Throws<BadImageFormatException>(() => ExecutableInfo.Get(path));
        }

        /// <summary>
        /// Verifies that a file whose optional header names neither of the two known formats is rejected,
        /// rather than being read as one of them.
        /// </summary>
        /// <param name="magic">The unrecognised optional header magic number.</param>
        [Theory]
        [InlineData((ushort)0x0000)]
        [InlineData((ushort)0x0107)]
        [InlineData((ushort)0x9999)]
        public void Get_RejectsAnUnknownOptionalHeaderMagic(ushort magic)
        {
            // Arrange
            using TempDirectory temp = new();
            string path = WriteImage(temp, "badmagic.bin", NewImageWithOptionalHeaderMagic(magic));

            // Act & Assert
            _ = Assert.Throws<BadImageFormatException>(() => ExecutableInfo.Get(path));
        }

        /// <summary>
        /// Verifies that a blank path is rejected as an absent argument rather than as a bad image.
        /// </summary>
        /// <param name="filePath">The blank path to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Get_RejectsABlankPath(string filePath)
        {
            _ = Assert.Throws<ArgumentException>(() => ExecutableInfo.Get(filePath));
        }

        /// <summary>
        /// Verifies that a null path is rejected as absent.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Get_RejectsANullPath()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => ExecutableInfo.Get(null!));
        }

        /// <summary>
        /// Verifies that a file that is not there is reported as missing.
        /// </summary>
        [Fact]
        public void Get_ReportsAMissingFile()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act & Assert
            _ = Assert.Throws<FileNotFoundException>(() => ExecutableInfo.Get(temp.GetPath("absent.exe")));
        }

        /// <summary>
        /// The size of the DOS header the reader expects to find at the start of an image.
        /// </summary>
        private const int DosHeaderSize = 64;

        /// <summary>
        /// The offset within the DOS header of the field pointing at the portable executable header.
        /// </summary>
        private const int LfanewOffset = 60;

        /// <summary>
        /// The size of the file header that follows the portable executable signature.
        /// </summary>
        private const int FileHeaderSize = 20;

        /// <summary>
        /// Builds a DOS header carrying the expected signature and pointing straight past itself.
        /// </summary>
        /// <returns>The header bytes.</returns>
        private static byte[] NewDosHeader()
        {
            byte[] header = new byte[DosHeaderSize];
            header[0] = (byte)'M';
            header[1] = (byte)'Z';
            BitConverter.GetBytes(DosHeaderSize).CopyTo(header, LfanewOffset);
            return header;
        }

        /// <summary>
        /// Builds an image that is well formed up to and including the portable executable signature and
        /// the file header, and then carries the given optional header magic number.
        /// </summary>
        /// <param name="magic">The optional header magic number to write.</param>
        /// <returns>The image bytes.</returns>
        private static byte[] NewImageWithOptionalHeaderMagic(ushort magic)
        {
            byte[] image = new byte[DosHeaderSize + 4 + FileHeaderSize + 2];
            NewDosHeader().CopyTo(image, 0);

            // "PE\0\0", then a file header of zeroes, then the magic under test.
            image[DosHeaderSize + 0] = (byte)'P';
            image[DosHeaderSize + 1] = (byte)'E';
            BitConverter.GetBytes(magic).CopyTo(image, DosHeaderSize + 4 + FileHeaderSize);
            return image;
        }

        /// <summary>
        /// Writes an image into the given directory and returns its path.
        /// </summary>
        /// <param name="temp">The directory to write into.</param>
        /// <param name="name">The name to give the file.</param>
        /// <param name="image">The bytes to write.</param>
        /// <returns>The full path of the written file.</returns>
        private static string WriteImage(TempDirectory temp, string name, byte[] image)
        {
            string path = temp.GetPath(name);
            File.WriteAllBytes(path, image);
            return path;
        }
    }
}
