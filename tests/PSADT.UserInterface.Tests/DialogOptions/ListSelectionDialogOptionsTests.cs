using System;
using System.Collections;
using System.Collections.Generic;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogOptions
{
    /// <summary>
    /// Tests the options for the dialog that offers the user a list to choose from.
    /// </summary>
    /// <remarks>
    /// The only options type holding a collection, which is what most of these are about: a list held by
    /// reference would defeat both the record's equality and its immutability, so the items are copied
    /// into a <c>ValueList</c> on the way in and rebuilt into a read-only view on the way out.
    /// </remarks>
    public sealed class ListSelectionDialogOptionsTests
    {
        /// <summary>
        /// Verifies that the values handed in are the ones read back.
        /// </summary>
        [Fact]
        public void Constructor_KeepsWhatItIsGiven()
        {
            // Arrange
            Hashtable table = SampleOptions.ListSelectionDialog("alpha", "bravo", "charlie");
            table["SelectedIndex"] = 2;

            // Act
            ListSelectionDialogOptions options = new(table);

            // Assert
            Assert.Equal(["alpha", "bravo", "charlie"], options.ListItems);
            Assert.Equal(2, options.SelectedIndex);
            Assert.Equal("choose one", options.Strings.ListSelectionMessage);
        }

        /// <summary>
        /// Verifies that a list with nothing in it is refused.
        /// </summary>
        /// <remarks>
        /// An empty list would render a dialog asking the user to choose from nothing, with a confirm
        /// button that can never be satisfied.
        /// </remarks>
        [Fact]
        public void Constructor_RefusesAnEmptyList()
        {
            // Arrange
            Hashtable table = SampleOptions.ListSelectionDialog();
            table["ListItems"] = Array.Empty<string>();

            // Act & Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new ListSelectionDialogOptions(table));
        }

        /// <summary>
        /// Verifies that a selected index outside the list is refused.
        /// </summary>
        /// <remarks>
        /// The upper bound is the interesting one: an index equal to the count is the off-by-one a caller
        /// converting from a one-based list would produce, and it would throw inside the dialog rather
        /// than here.
        /// </remarks>
        /// <param name="index">The out-of-range index to refuse.</param>
        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        [InlineData(int.MaxValue)]
        public void Constructor_RefusesASelectedIndexOutsideTheList(int index)
        {
            // Arrange - two items, so index 2 is one past the end.
            Hashtable table = SampleOptions.ListSelectionDialog("alpha", "bravo");
            table["SelectedIndex"] = index;

            // Act & Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ListSelectionDialogOptions(table));
            Assert.Equal("selectedIndex", exception.ParamName);
        }

        /// <summary>
        /// Verifies that the first and last positions are accepted.
        /// </summary>
        /// <param name="index">The boundary index to accept.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Constructor_AcceptsTheBoundaryPositions(int index)
        {
            // Arrange
            Hashtable table = SampleOptions.ListSelectionDialog("alpha", "bravo");
            table["SelectedIndex"] = index;

            // Act & Assert
            Assert.Equal(index, new ListSelectionDialogOptions(table).SelectedIndex);
        }

        /// <summary>
        /// Verifies that no selection is a valid state.
        /// </summary>
        [Fact]
        public void Constructor_LeavesTheSelectedIndexNullWhenItIsAbsent()
        {
            Assert.Null(new ListSelectionDialogOptions(SampleOptions.ListSelectionDialog()).SelectedIndex);
        }

        /// <summary>
        /// Verifies that the items are copied rather than referenced.
        /// </summary>
        /// <remarks>
        /// The caller hands in an <see cref="IReadOnlyList{T}"/>, which promises the caller will not
        /// change it but not that nobody else will - a <see cref="List{T}"/> cast to that interface is
        /// still mutable through its original reference. Copying is what makes the options genuinely
        /// immutable once built.
        /// </remarks>
        [Fact]
        public void ListItems_AreCopiedFromWhatTheCallerHandedIn()
        {
            // Arrange
            List<string> source = ["alpha", "bravo"];
            Hashtable table = SampleOptions.ListSelectionDialog();
            table["ListItems"] = source;
            ListSelectionDialogOptions options = new(table);

            // Act
            source.Add("charlie");
            source[0] = "changed";

            // Assert
            Assert.Equal(["alpha", "bravo"], options.ListItems);
        }

        /// <summary>
        /// Verifies that the list is rebuilt on each read rather than handed out.
        /// </summary>
        /// <remarks>
        /// The same guarantee the culture makes in <c>BaseDialogOptions</c>, for the same reason: the
        /// backing field is a <c>ValueList</c> that compares by contents, and the property builds a fresh
        /// read-only view so no caller can reach the storage behind it.
        /// </remarks>
        [Fact]
        public void ListItems_AreRebuiltOnEachRead()
        {
            // Act
            ListSelectionDialogOptions options = new(SampleOptions.ListSelectionDialog());

            // Assert
            Assert.Equal(options.ListItems, options.ListItems);
            Assert.NotSame(options.ListItems, options.ListItems);
        }

        /// <summary>
        /// Verifies that two dialogs offering the same items are equal despite holding separate lists.
        /// </summary>
        /// <remarks>
        /// This is the reason the backing field is a <c>ValueList</c> rather than an array or a
        /// <see cref="List{T}"/>, either of which would compare by reference and reduce the whole record
        /// to reference equality.
        /// </remarks>
        [Fact]
        public void Equality_IsByTheContentsOfTheList()
        {
            // Arrange
            Hashtable table = SampleOptions.ListSelectionDialog("alpha", "bravo");
            Hashtable same = SampleOptions.ListSelectionDialog("alpha", "bravo");
            Hashtable different = SampleOptions.ListSelectionDialog("alpha", "charlie");
            foreach (string key in new[] { "AppIconImage", "AppBannerImage" })
            {
                same[key] = table[key];
                different[key] = table[key];
            }

            // Assert
            Assert.Equal(new ListSelectionDialogOptions(table), new ListSelectionDialogOptions(same));
            Assert.NotEqual(new ListSelectionDialogOptions(table), new ListSelectionDialogOptions(different));
        }

        /// <summary>
        /// Verifies that the nested strings table is required and validated.
        /// </summary>
        [Fact]
        public void Strings_AreRequiredAndCannotBeBlank()
        {
            // Arrange
            Hashtable missingTable = SampleOptions.ListSelectionDialog();
            Hashtable missingKey = SampleOptions.ListSelectionDialog();
            Hashtable blank = SampleOptions.ListSelectionDialog();
            missingTable.Remove("Strings");
            missingKey["Strings"] = new Hashtable();
            blank["Strings"] = new Hashtable { ["ListSelectionMessage"] = "   " };

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => new ListSelectionDialogOptions(missingTable));
            _ = Assert.Throws<ArgumentNullException>(() => new ListSelectionDialogOptions(missingKey));
            _ = Assert.Throws<ArgumentException>(() => new ListSelectionDialogOptions(blank));
        }

        /// <summary>
        /// Records that this type alone defaults the top-most flag instead of requiring it.
        /// </summary>
        /// <remarks>
        /// Every other <c>BaseDialogOptions</c> derivative throws when <c>DialogTopMost</c> is absent;
        /// this one reads it as false. Stated rather than corrected, because either could be the intended
        /// behaviour and only the toolkit's authors know which: a list selection dialog that quietly
        /// opens behind another window is a real difference from one that refuses to open at all.
        /// </remarks>
        [Fact]
        public void Constructor_DefaultsTheTopMostFlagWhereItsSiblingsRequireIt()
        {
            // Arrange
            Hashtable table = SampleOptions.ListSelectionDialog();
            table.Remove("DialogTopMost");

            // Act & Assert
            Assert.False(new ListSelectionDialogOptions(table).DialogTopMost);
        }
    }
}
