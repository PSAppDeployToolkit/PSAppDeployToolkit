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
            Result = !string.IsNullOrWhiteSpace(result) ? result : throw new ArgumentNullException("Result cannot be null or empty.", (Exception?)null);
            SelectedItem = !string.IsNullOrWhiteSpace(selectedItem) ? selectedItem : throw new ArgumentNullException("SelectedItem cannot be null or empty.", (Exception?)null);
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
