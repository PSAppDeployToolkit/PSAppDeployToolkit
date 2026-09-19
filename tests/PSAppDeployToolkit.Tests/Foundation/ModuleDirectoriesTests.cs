using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PSAppDeployToolkit.Foundation;
using Xunit;

namespace PSAppDeployToolkit.Tests.Foundation
{
    /// <summary>
    /// Tests the three directory lists a seated module keeps.
    /// </summary>
    /// <remarks>
    /// The asymmetry is what this is really about. A deployment always has a script directory, but it need not carry a
    /// config or a strings folder of its own, in which case it runs on the module's shipped defaults. So the two
    /// optional lists have to arrive empty rather than null, since every caller enumerates them without checking.
    /// </remarks>
    public sealed class ModuleDirectoriesTests
    {
        /// <summary>
        /// Verifies that each list is handed back with what it was given, in order.
        /// </summary>
        [Fact]
        public void Constructor_KeepsEachListItWasGiven()
        {
            // Act
            ModuleDirectories directories = new(Directories(@"C:\Script\One", @"C:\Script\Two"), Directories(@"C:\Config"), Directories(@"C:\Strings"));

            // Assert
            Assert.Equal([@"C:\Script\One", @"C:\Script\Two"], [.. directories.Script.Select(static d => d.FullName)]);
            Assert.Equal([@"C:\Config"], [.. directories.Config.Select(static d => d.FullName)]);
            Assert.Equal([@"C:\Strings"], [.. directories.Strings.Select(static d => d.FullName)]);
        }

        /// <summary>
        /// Verifies that an absent config or strings list becomes an empty one.
        /// </summary>
        /// <remarks>
        /// This is the ordinary case for a deployment that ships neither, so it is the shape most callers actually see.
        /// </remarks>
        [Fact]
        public void Constructor_TurnsAnAbsentOptionalListIntoAnEmptyOne()
        {
            // Act
            ModuleDirectories directories = new(Directories(@"C:\Script"), config: null, strings: null);

            // Assert
            Assert.Empty(directories.Config);
            Assert.Empty(directories.Strings);
            _ = Assert.Single(directories.Script);
        }

        /// <summary>
        /// Verifies that an absent script list is refused rather than emptied.
        /// </summary>
        /// <remarks>
        /// Unlike the other two, there is no sensible empty case: a module with no script directory has nowhere to
        /// read a deployment's own config or strings from.
        /// </remarks>
        [Fact]
        public void Constructor_RefusesAnAbsentScriptList()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new ModuleDirectories(script: null!, config: null, strings: null));
        }

        /// <summary>
        /// Verifies that each list is copied rather than held onto.
        /// </summary>
        /// <remarks>
        /// The lists are handed in from PowerShell, which is free to keep mutating whatever it passed. A module's
        /// directories are fixed once it is seated, so holding the caller's collection would let them move underneath
        /// everything that has already read them.
        /// </remarks>
        [Fact]
        public void Constructor_CopiesEachListRatherThanHoldingIt()
        {
            // Arrange
            List<DirectoryInfo> script = [.. Directories(@"C:\Script")];

            // Act
            ModuleDirectories directories = new(script, config: null, strings: null);
            script.Add(new DirectoryInfo(@"C:\Added\Afterwards"));

            // Assert
            _ = Assert.Single(directories.Script);
        }

        /// <summary>
        /// Builds directories for the given paths without touching the file system.
        /// </summary>
        /// <param name="paths">The paths to build directories for.</param>
        /// <returns>The directories.</returns>
        private static IEnumerable<DirectoryInfo> Directories(params string[] paths)
        {
            return [.. paths.Select(static p => new DirectoryInfo(p))];
        }
    }
}
