using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PSADT.UserInterface.Types
{
    /// <summary>
    /// An ObservableCollection that allows for resettings its items while only firing a single OnCollectionChanged event.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ResettableObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Resets the underlying ObservableCollection with the provided items.
        /// </summary>
        /// <param name="items"></param>
        public void ResetItems(IEnumerable<T> items)
        {
            _suppressNotification = true;
            ClearItems();
            foreach (var item in items)
            {
                Add(item);
            }
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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
        private bool _suppressNotification = false;
    }
}
