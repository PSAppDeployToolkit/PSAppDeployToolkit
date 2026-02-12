using System;
using System.Runtime.Serialization;
using PSADT.Utilities;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Represents the result of a list selection dialog.
    /// </summary>
    [DataContract]
    public sealed class ListSelectionDialogResult : CustomDialogDerivative
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListSelectionDialogResult"/> class.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="selectedItem"></param>
        internal ListSelectionDialogResult(string result, string selectedItem) : base(result)
        {
            SelectedItem = !string.IsNullOrWhiteSpace(selectedItem) ? selectedItem : throw new ArgumentNullException("SelectedItem cannot be null or empty.", (Exception?)null);
        }

        /// <summary>
        /// Gets the item selected by the user from the list.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string SelectedItem;

        /// <summary>
        /// Determines whether the specified object is equal to the current instance.
        /// </summary>
        /// <remarks>Compares both the Result and SelectedItem properties. String equality is not supported for derived types.</remarks>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if the specified object is a ListSelectionDialogResult with equal Result and SelectedItem values; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            return obj is ListSelectionDialogResult other && Result == other.Result && SelectedItem == other.SelectedItem;
        }

        /// <summary>
        /// Returns a hash code for the current instance based on all properties.
        /// </summary>
        /// <returns>A hash code combining Result and SelectedItem.</returns>
        public override int GetHashCode()
        {
            return CryptographicUtilities.GenerateHashCode(Result, SelectedItem);
        }
    }
}
