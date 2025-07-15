using System;
using Newtonsoft.Json;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Represents the result of an input dialog.
    /// </summary>
    public sealed record InputDialogResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialogResult"/> class.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="text"></param>
        [JsonConstructor]
        internal InputDialogResult(string result, string? text = null)
        {
            Result = !string.IsNullOrWhiteSpace(result) ? result : throw new ArgumentNullException("Result cannot be null or empty.", (Exception?)null);
            Text = !string.IsNullOrWhiteSpace(text) ? text : null;
        }

        /// <summary>
        /// Gets the result of the dialog.
        /// </summary>
        [JsonProperty]
        public readonly string Result;

        /// <summary>
        /// Gets the text entered by the user.
        /// </summary>
        [JsonProperty]
        public readonly string? Text;
    }
}
