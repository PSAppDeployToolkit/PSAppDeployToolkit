using System;
using System.Runtime.Serialization;
using PSADT.Utilities;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Represents the result of an input dialog.
    /// </summary>
    [DataContract]
    public sealed class InputDialogResult : CustomDialogDerivative
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialogResult"/> class.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="text"></param>
        internal InputDialogResult(string result, string? text = null) : base(result)
        {
            if (text is not null && string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be empty or whitespace.", nameof(text));
            }
            Text = text;
        }

        /// <summary>
        /// Gets the text entered by the user.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string? Text;

        /// <summary>
        /// Determines whether the specified object is equal to the current instance.
        /// </summary>
        /// <remarks>Compares both the Result and Text properties. String equality is not supported for derived types.</remarks>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if the specified object is an InputDialogResult with equal Result and Text values; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            return obj is InputDialogResult other && Result == other.Result && Text == other.Text;
        }

        /// <summary>
        /// Returns a hash code for the current instance based on all properties.
        /// </summary>
        /// <returns>A hash code combining Result and Text.</returns>
        public override int GetHashCode()
        {
            return CryptographicUtilities.GenerateHashCode(Result, Text);
        }
    }
}
