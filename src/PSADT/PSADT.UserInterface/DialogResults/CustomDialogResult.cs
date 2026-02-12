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
        /// Determines whether the specified object is equal to the current instance.
        /// </summary>
        /// <remarks>This override ensures that string comparisons work correctly with PowerShell's -eq operator,
        /// which calls Equals(object) rather than using custom operators. String equality only applies to the
        /// base CustomDialogResult type, not derived types.</remarks>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            // String equality only applies to the base CustomDialogResult type, not derived types.
            return obj switch
            {
                CustomDialogResult other => Result == other.Result,
                string str when GetType() == typeof(CustomDialogResult) => Result == str,
                _ => false
            };
        }

        /// <summary>
        /// Gets the result of the last executed operation as a string.
        /// </summary>
        /// <remarks>This property is read-only and is set internally by the class. It reflects the
        /// outcome of the most recent operation performed by the dialog.</remarks>
        [DataMember]
        private string Result { get; set; }

        /// <summary>
        /// Determines whether the specified CustomDialogResult instance is equal to the given string.
        /// </summary>
        /// <remarks>This operator overload enables direct comparison between a CustomDialogResult and a
        /// string, allowing for simplified conditional checks. Only works for the base CustomDialogResult type,
        /// not derived types.</remarks>
        /// <param name="left">The CustomDialogResult instance to compare. This parameter cannot be null.</param>
        /// <param name="right">The string to compare with the CustomDialogResult instance.</param>
        /// <returns>true if the CustomDialogResult instance is equal to the specified string; otherwise, false.</returns>
        public static bool operator ==(CustomDialogResult left, string right)
        {
            // String equality only applies to the base CustomDialogResult type, not derived types.
            AssertNotNull(left, "Left operand cannot be null.");
            return left.GetType() == typeof(CustomDialogResult) && left.Result == right;
        }

        /// <summary>
        /// Determines whether a specified CustomDialogResult instance is not equal to a specified string value.
        /// </summary>
        /// <remarks>This operator overload enables direct comparison between a CustomDialogResult and a
        /// string, improving code readability in conditional expressions. Only works for the base CustomDialogResult type,
        /// not derived types.</remarks>
        /// <param name="left">The CustomDialogResult instance to compare. This parameter cannot be null.</param>
        /// <param name="right">The string value to compare with the CustomDialogResult instance.</param>
        /// <returns>true if the CustomDialogResult instance is not equal to the specified string; otherwise, false.</returns>
        public static bool operator !=(CustomDialogResult left, string right)
        {
            // String equality only applies to the base CustomDialogResult type, not derived types.
            AssertNotNull(left, "Left operand cannot be null.");
            return left.GetType() != typeof(CustomDialogResult) || left.Result != right;
        }

        /// <summary>
        /// Determines whether the specified string is equal to the value of the specified CustomDialogResult.
        /// </summary>
        /// <remarks>This operator overload enables direct comparison between a string and a
        /// CustomDialogResult, allowing for simplified conditional checks. Only works for the base CustomDialogResult type,
        /// not derived types.</remarks>
        /// <param name="left">The string to compare with the CustomDialogResult.</param>
        /// <param name="right">The CustomDialogResult to compare against the string. This parameter cannot be null.</param>
        /// <returns>true if the string is equal to the value of the CustomDialogResult; otherwise, false.</returns>
        public static bool operator ==(string left, CustomDialogResult right)
        {
            // String equality only applies to the base CustomDialogResult type, not derived types.
            AssertNotNull(right, "Right operand cannot be null.");
            return right.GetType() == typeof(CustomDialogResult) && right.Result == left;
        }

        /// <summary>
        /// Determines whether the specified string is not equal to the specified CustomDialogResult value.
        /// </summary>
        /// <remarks>This operator enables direct comparison between a string and a CustomDialogResult
        /// using the inequality operator (!=), which can simplify conditional logic when working with dialog
        /// results. Only works for the base CustomDialogResult type, not derived types.</remarks>
        /// <param name="left">The string to compare with the CustomDialogResult.</param>
        /// <param name="right">The CustomDialogResult to compare against the string. This parameter cannot be null.</param>
        /// <returns>true if the string is not equal to the CustomDialogResult; otherwise, false.</returns>
        public static bool operator !=(string left, CustomDialogResult right)
        {
            // String equality only applies to the base CustomDialogResult type, not derived types.
            AssertNotNull(right, "Right operand cannot be null.");
            return right.GetType() != typeof(CustomDialogResult) || right.Result != left;
        }

        /// <summary>
        /// Converts a CustomDialogResult instance to its string representation.
        /// </summary>
        /// <remarks>This implicit conversion enables seamless use of CustomDialogResult objects in
        /// contexts where a string is expected, such as assignment or comparison operations. The conversion returns the
        /// value of the Result property.</remarks>
        /// <param name="dialogResult">The CustomDialogResult instance to convert to a string.</param>
        public static implicit operator string(CustomDialogResult dialogResult)
        {
            AssertNotNull(dialogResult, "CustomDialogResult instance cannot be null.");
            return dialogResult.Result;
        }

        /// <summary>
        /// Throws an exception if the specified value is null, ensuring that required arguments are not missing.
        /// </summary>
        /// <param name="value">The object to validate for null. If this parameter is null, an exception is thrown.</param>
        /// <param name="message">The error message to include in the exception if the value is null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        private static void AssertNotNull(object value, string message)
        {
            if (value == null)
            {
                throw new ArgumentNullException(message, (Exception?)null);
            }
        }
    }
}
