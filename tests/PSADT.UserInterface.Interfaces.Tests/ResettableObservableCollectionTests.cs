using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests
{
    /// <summary>
    /// Tests the collection that swaps out its contents behind a single notification.
    /// </summary>
    /// <remarks>
    /// The Fluent close-applications dialog rebinds its list every time the set of running processes
    /// changes, which for an ordinary <c>ObservableCollection</c> would mean one notification per item
    /// removed and one per item added - and a WPF <c>ListView</c> rebuilding a row for each. The whole
    /// point of this type is that a caller sees exactly one <c>Reset</c> however many items moved, so
    /// that is what these tests hold it to.
    /// </remarks>
    public sealed class ResettableObservableCollectionTests
    {
        /// <summary>
        /// Verifies that replacing the contents raises one notification regardless of how many items moved.
        /// </summary>
        [Fact]
        public void ResetItems_RaisesASingleResetHoweverManyItemsMoved()
        {
            // Arrange
            ResettableObservableCollection<string> collection = ["one", "two", "three"];
            List<NotifyCollectionChangedEventArgs> raised = [];
            collection.CollectionChanged += (_, e) => raised.Add(e);

            // Act
            collection.ResetItems(["four", "five", "six", "seven"]);

            // Assert
            NotifyCollectionChangedEventArgs single = Assert.Single(raised);
            Assert.Equal(NotifyCollectionChangedAction.Reset, single.Action);
        }

        /// <summary>
        /// Verifies that the contents afterwards are the incoming items, in the order they arrived.
        /// </summary>
        [Fact]
        public void ResetItems_ReplacesTheContentsInOrder()
        {
            // Arrange
            ResettableObservableCollection<string> collection = ["one", "two"];

            // Act
            collection.ResetItems(["three", "four", "five"]);

            // Assert
            Assert.Equal(["three", "four", "five"], collection);
        }

        /// <summary>
        /// Verifies that an empty source empties a collection that had items, and says so.
        /// </summary>
        [Fact]
        public void ResetItems_EmptiesACollectionThatHadItems()
        {
            // Arrange
            ResettableObservableCollection<string> collection = ["one", "two"];
            List<NotifyCollectionChangedEventArgs> raised = [];
            collection.CollectionChanged += (_, e) => raised.Add(e);

            // Act
            collection.ResetItems([]);

            // Assert
            Assert.Empty(collection);
            _ = Assert.Single(raised);
        }

        /// <summary>
        /// Verifies that nothing is announced when there was nothing to announce.
        /// </summary>
        /// <remarks>
        /// The short-circuit is what keeps the close-applications dialog quiet while it is polling and
        /// finding nothing running. Without it every poll would raise a Reset and the list would rebuild
        /// itself on a timer for no reason.
        /// </remarks>
        [Fact]
        public void ResetItems_SaysNothingWhenBothSidesAreEmpty()
        {
            // Arrange
            ResettableObservableCollection<string> collection = [];
            List<NotifyCollectionChangedEventArgs> raised = [];
            collection.CollectionChanged += (_, e) => raised.Add(e);

            // Act
            collection.ResetItems([]);

            // Assert
            Assert.Empty(raised);
        }

        /// <summary>
        /// Verifies that forcing overrides the short-circuit.
        /// </summary>
        /// <remarks>
        /// The dialog forces its first reset during construction so the list view binds even when there
        /// is nothing to show, which is what makes the "nothing is running" state render at all.
        /// </remarks>
        [Fact]
        public void ResetItems_AnnouncesAnEmptyResetWhenForced()
        {
            // Arrange
            ResettableObservableCollection<string> collection = [];
            List<NotifyCollectionChangedEventArgs> raised = [];
            collection.CollectionChanged += (_, e) => raised.Add(e);

            // Act
            collection.ResetItems([], force: true);

            // Assert
            NotifyCollectionChangedEventArgs single = Assert.Single(raised);
            Assert.Equal(NotifyCollectionChangedAction.Reset, single.Action);
        }

        /// <summary>
        /// Verifies that suppression is lifted afterwards, so ordinary changes still notify.
        /// </summary>
        /// <remarks>
        /// The suppression flag is a field rather than a scoped construct, so a reset that threw partway
        /// through would leave it set and silence the collection permanently. This is the test that would
        /// notice.
        /// </remarks>
        [Fact]
        public void ResetItems_LeavesOrdinaryChangesNotifying()
        {
            // Arrange
            ResettableObservableCollection<string> collection = [];
            collection.ResetItems(["one"]);
            List<NotifyCollectionChangedEventArgs> raised = [];
            collection.CollectionChanged += (_, e) => raised.Add(e);

            // Act
            collection.Add("two");

            // Assert
            NotifyCollectionChangedEventArgs single = Assert.Single(raised);
            Assert.Equal(NotifyCollectionChangedAction.Add, single.Action);
        }

        /// <summary>
        /// Verifies that the incoming items are read before the collection is emptied.
        /// </summary>
        /// <remarks>
        /// The source is an <see cref="IEnumerable{T}"/>, so it may well be a lazy projection over
        /// something - and in the close-applications dialog it is exactly that. Were it enumerated after
        /// the clear rather than copied before it, a source that reads from this collection would come
        /// back empty and the reset would silently wipe the list.
        /// </remarks>
        [Fact]
        public void ResetItems_ReadsTheSourceBeforeEmptyingItself()
        {
            // Arrange
            ResettableObservableCollection<string> collection = ["one", "two"];

            // Act
            collection.ResetItems(collection);

            // Assert
            Assert.Equal(["one", "two"], collection);
        }

        /// <summary>
        /// Records that property notifications are not suppressed along with the collection notification.
        /// </summary>
        /// <remarks>
        /// Only <c>OnCollectionChanged</c> is overridden, so the <c>Count</c> and <c>Item[]</c> property
        /// notifications the base class raises for the clear and for each add still go out. That is not a
        /// defect - the type promises a single collection notification and delivers one - but it is worth
        /// having written down, because a binding watching <c>Count</c> sees the reset as several changes
        /// rather than one.
        /// </remarks>
        [Fact]
        public void ResetItems_StillRaisesPropertyNotificationsPerItem()
        {
            // Arrange
            ResettableObservableCollection<string> collection = [];
            List<string?> properties = [];
            ((INotifyPropertyChanged)collection).PropertyChanged += (_, e) => properties.Add(e.PropertyName);

            // Act
            collection.ResetItems(["one", "two"]);

            // Assert
            Assert.True(properties.Count > 1, $"Expected several property notifications, saw {properties.Count}.");
            Assert.All(properties, static name => Assert.True(string.Equals(name, "Count", StringComparison.Ordinal) || string.Equals(name, "Item[]", StringComparison.Ordinal), $"Unexpected property notification '{name}'."));
        }
    }
}
