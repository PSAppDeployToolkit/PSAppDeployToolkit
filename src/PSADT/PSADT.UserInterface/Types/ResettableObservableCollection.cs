using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PSADT.UserInterface.Types
{
    /// <summary>
    /// An ObservableCollection that allows for resettings its items while only firing a single OnCollectionChanged event.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ResettableObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Resets the underlying ObservableCollection with the provided items.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="force"></param>
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
        /// Override for base event to only fire when we're not suppressing the change.
        /// </summary>
        /// <param name="e"></param>
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
