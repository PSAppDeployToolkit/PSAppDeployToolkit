using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PSADT.Interop;
using PSADT.UserInterface.Tests.TestHelpers;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests the mapping from the toolkit's icon names to the shell's stock icons.
    /// </summary>
    /// <remarks>
    /// The only part of this project that calls the operating system. Everything it calls is a read - the
    /// shell's image list and its stock icon table - so these tests observe the machine without changing
    /// it. Each handle is released as soon as it has been examined, since the icons come from a
    /// process-wide list.
    /// </remarks>
    public sealed class SystemIconsTests
    {
        /// <summary>
        /// Verifies that every icon the enum offers can actually be fetched.
        /// </summary>
        /// <remarks>
        /// The real regression this guards. The lookup is a dictionary indexed directly, so a
        /// <see cref="DialogSystemIcon"/> member added without a corresponding entry throws
        /// <see cref="KeyNotFoundException"/> - and only at the moment a dialog asks for that icon, which
        /// is a long way from the change that caused it. Driven from the enum rather than a written list,
        /// so a new member joins this test by existing.
        /// </remarks>
        /// <param name="icon">The icon to fetch.</param>
        [Theory]
        [MemberData(nameof(EveryIcon))]
        public void Get_ReturnsAValidHandleForEveryDeclaredIcon(DialogSystemIcon icon)
        {
            // Act
            using DestroyIconSafeHandle handle = SystemIcons.Get(icon, SHIL_SIZE.SHIL_LARGE);

            // Assert
            Assert.False(handle.IsInvalid);
            Assert.False(handle.IsClosed);
        }

        /// <summary>
        /// Verifies that each of the shell's image list sizes can be asked for.
        /// </summary>
        /// <remarks>
        /// Written as one test looping over the sizes rather than a theory, because
        /// <c language="csharp">SHIL_SIZE</c> is internal to <c language="csharp">PSADT.Interop</c> and a theory would have to expose it on a
        /// public member to supply it. <c language="csharp">SHIL_LAST</c> is an alias of <c language="csharp">SHIL_JUMBO</c> rather than a
        /// size of its own, so the sizes are deduplicated before being asked for.
        /// </remarks>
        [Fact]
        public void Get_ReturnsAValidHandleAtEveryImageListSize()
        {
            foreach (SHIL_SIZE size in EnumValues.Declared<SHIL_SIZE>().Distinct())
            {
                // Act
                using DestroyIconSafeHandle handle = SystemIcons.Get(DialogSystemIcon.Application, size);

                // Assert
                Assert.False(handle.IsInvalid);
            }
        }

        /// <summary>
        /// Verifies that the icons which share a shell icon really do.
        /// </summary>
        /// <remarks>
        /// Four pairs of members resolve to one stock icon each. That is deliberate rather than an
        /// oversight - the names exist so a caller can use whichever it already used - so it is stated
        /// here; giving one of a pair its own icon would then be a decision recorded in this list rather
        /// than a change nobody noticed.
        /// </remarks>
        /// <param name="left">One member of the pair.</param>
        /// <param name="right">The other member of the pair.</param>
        [Theory]
        [InlineData(DialogSystemIcon.Error, DialogSystemIcon.Hand)]
        [InlineData(DialogSystemIcon.Exclamation, DialogSystemIcon.Warning)]
        [InlineData(DialogSystemIcon.Application, DialogSystemIcon.WinLogo)]
        [InlineData(DialogSystemIcon.Asterisk, DialogSystemIcon.Information)]
        public void LookupTable_MapsTheseIconPairsToTheSameStockIcon(DialogSystemIcon left, DialogSystemIcon right)
        {
            // Act
            IReadOnlyDictionary<DialogSystemIcon, SHSTOCKICONID> table = LookupTable();

            // Assert
            Assert.Equal(table[left], table[right]);
        }

        /// <summary>
        /// Verifies that the lookup table holds an entry for every declared icon and nothing else.
        /// </summary>
        /// <remarks>
        /// <see cref="Get_ReturnsAValidHandleForEveryDeclaredIcon"/> already fails when an entry is
        /// missing, but it fails by calling the shell, so on a machine where that call is unavailable it
        /// would report the wrong thing. This reads the table itself and needs nothing from the operating
        /// system to say which member was forgotten.
        /// </remarks>
        [Fact]
        public void LookupTable_CoversEveryDeclaredIconAndNoOthers()
        {
            // Act - sorted with Array.Sort rather than a LINQ ordering, which the two target frameworks
            // resolve differently: net472 has no Enumerable.Order and binds to an async overload instead.
            DialogSystemIcon[] mapped = [.. LookupTable().Keys];
            DialogSystemIcon[] declared = EnumValues.Declared<DialogSystemIcon>();
            Array.Sort(mapped);
            Array.Sort(declared);

            // Assert
            Assert.Equal(declared, mapped);
        }

        /// <summary>
        /// Every icon the enum declares, as theory data.
        /// </summary>
        /// <returns>One row per member.</returns>
        public static TheoryData<DialogSystemIcon> EveryIcon()
        {
            TheoryData<DialogSystemIcon> data = [];
            foreach (DialogSystemIcon icon in EnumValues.Declared<DialogSystemIcon>())
            {
                data.Add(icon);
            }
            return data;
        }

        /// <summary>
        /// Reads the private lookup table out of <c language="csharp">SystemIcons</c>.
        /// </summary>
        /// <remarks>
        /// Reflection because the table is an implementation detail and should stay one. What is being
        /// asserted is that it is complete, which is not something the type offers a way to ask.
        /// </remarks>
        /// <returns>The table, keyed by icon.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the field has been renamed, removed or is null.</exception>
        private static IReadOnlyDictionary<DialogSystemIcon, SHSTOCKICONID> LookupTable()
        {
            FieldInfo field = typeof(SystemIcons).GetField("SystemIconLookupTable", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("The lookup table field was not found; it has been renamed or removed.");
            return (IReadOnlyDictionary<DialogSystemIcon, SHSTOCKICONID>)(field.GetValue(null) ?? throw new InvalidOperationException("The lookup table was null."));
        }
    }
}
