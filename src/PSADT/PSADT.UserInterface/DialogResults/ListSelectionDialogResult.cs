using System;
using Newtonsoft.Json;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Represents the result of a list selection dialog.
    /// </summary>
    public sealed record ListSelectionDialogResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListSelectionDialogResult"/> class.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="selectedItem"></param>
        [JsonConstructor]
        internal ListSelectionDialogResult(string result, string selectedItem)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new ArgumentException("Result cannot be empty or whitespace.", nameof(result));
            }

            if (selectedItem is null)
            {
                throw new ArgumentNullException(nameof(selectedItem));
            }

            if (string.IsNullOrWhiteSpace(selectedItem))
            {
                throw new ArgumentException("SelectedItem cannot be empty or whitespace.", nameof(selectedItem));
            }

            Result = result;
            SelectedItem = selectedItem;
        }

        /// <summary>
        /// Gets the result of the dialog.
        /// </summary>
        public string Result { get; }

        /// <summary>
        /// Gets the item selected by the user from the list.
        /// </summary>
        public string SelectedItem { get; }
    }
}
