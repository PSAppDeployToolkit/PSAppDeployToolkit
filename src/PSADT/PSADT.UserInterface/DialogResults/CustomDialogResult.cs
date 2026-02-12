using System;
using System.Runtime.Serialization;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Represents the result of a custom dialog operation, encapsulating the outcome as a string value.
    /// </summary>
    /// <remarks>Use this class to capture and convey the outcome of a dialog interaction in a strongly typed
    /// manner. The result string is guaranteed to be non-null and non-empty, ensuring reliable interpretation of dialog
    /// outcomes.</remarks>
    [DataContract]
    [KnownType(typeof(CustomDialogDerivative))]
    [KnownType(typeof(InputDialogResult))]
    [KnownType(typeof(ListSelectionDialogResult))]
    public class CustomDialogResult : IDialogResult
    {
        /// <summary>
        /// Initializes a new instance of the CustomDialogResult class with the specified result string.
        /// </summary>
        /// <param name="result">The result string that represents the outcome of the dialog. This value cannot be null or empty.</param>
        /// <exception cref="ArgumentException">Thrown if the result parameter is null or empty.</exception>
        internal CustomDialogResult(string result)
        {
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new ArgumentNullException(nameof(result), "Result cannot be null or empty.");
            }
            Result = result;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current instance.
        /// </summary>
        /// <remarks>This override ensures that string comparisons work correctly with PowerShell's -eq operator,
        /// which calls Equals(object) rather than using custom operators. String equality only applies to the
        /// base CustomDialogResult type, not derived types. Two instances are only considered equal if they are
        /// of the exact same type.</remarks>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            // Only compare instances of the exact same type, not derived types.
            return obj switch
            {
                CustomDialogResult other when other.GetType() == GetType() => Result == other.Result,
                string str when GetType() == typeof(CustomDialogResult) => Result.Equals(str, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        /// <summary>
        /// Returns a string that represents the current result value.
        /// </summary>
        /// <returns>A string containing the value of the result represented by this instance.</returns>
        public override string ToString()
        {
            return Result;
        }

        /// <summary>
        /// Provides the hash code for the current result value, which is derived from the Result property.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Result.GetHashCode();
        }

        /// <summary>
        /// Gets the result of the last executed operation as a string.
        /// </summary>
        /// <remarks>This property is read-only and is set internally by the class. It reflects the
        /// outcome of the most recent operation performed by the dialog.</remarks>
        [DataMember]
        private string Result { get; set; }

        /// <summary>
        /// Converts a CustomDialogResult instance to its string representation.
        /// </summary>
        /// <remarks>This implicit conversion enables seamless use of CustomDialogResult objects in
        /// contexts where a string is expected, such as assignment or comparison operations. The conversion returns the
        /// value of the Result property.</remarks>
        /// <param name="dialogResult">The CustomDialogResult instance to convert to a string.</param>
        public static implicit operator string(CustomDialogResult dialogResult)
        {
            static void AssertNotNull(object value, string message)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(message, (Exception?)null);
                }
            }
            AssertNotNull(dialogResult, "CustomDialogResult instance cannot be null.");
            return dialogResult.Result;
        }
    }
}
