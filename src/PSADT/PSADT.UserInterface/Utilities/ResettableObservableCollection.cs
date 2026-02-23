using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PSADT.UserInterface.Utilities
{
    /// <summary>
    /// An ObservableCollection that allows for resettings its items while only firing a single OnCollectionChanged event.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ResettableObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Replaces the contents of the collection with the specified items, optionally forcing a reset even if both
        /// the source and current collection are empty.
        /// </summary>
        /// <remarks>If force is false and both the incoming items and the current collection are empty,
        /// the method returns without making any changes. Notifications are suppressed during the reset process to
        /// prevent unnecessary updates.</remarks>
        /// <param name="items">The collection of items to set as the new contents of the collection. If empty, the collection will be
        /// cleared.</param>
        /// <param name="force">true to force the reset operation even if both the incoming items and the current collection are empty;
        /// otherwise, false.</param>
        internal void ResetItems(IEnumerable<T> items, bool force = false)
        {
            T[] incoming = [.. items];
            if (!force && incoming.Length == 0 && Count == 0)
            {
                return;
            }
            _suppressNotification = true;
            ClearItems();
            foreach (T item in incoming)
            {
                Add(item);
            }
            _suppressNotification = false;
            OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Raises the collection changed event to notify subscribers of changes to the collection, unless notifications
        /// are currently suppressed.
        /// </summary>
        /// <remarks>Overrides the base implementation to provide custom notification behavior. If
        /// notifications are suppressed, the event is not raised and subscribers are not notified of the
        /// change.</remarks>
        /// <param name="e">The event data that describes the change to the collection.</param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
            {
                base.OnCollectionChanged(e);
            }
        }

        /// <summary>
        /// Private state flag to suppress CollectionChanged events until the collection has been reset.
        /// </summary>
        private bool _suppressNotification;
    }
}
