using System.Runtime.Serialization;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Provides a base class for custom dialog result types that derive from CustomDialogResult.
    /// </summary>
    /// <remarks>This abstract class is intended to be extended by specific dialog result implementations.
    /// Derived classes should define behaviors and properties relevant to their dialog context. Use this class as a
    /// foundation for creating custom dialog results that require additional functionality beyond the standard dialog
    /// result.</remarks>
    [DataContract]
    [KnownType(typeof(InputDialogResult))]
    [KnownType(typeof(ListSelectionDialogResult))]
    public abstract class CustomDialogDerivative : CustomDialogResult
    {
        /// <summary>
        /// Initializes a new instance of the CustomDialogDerivativeResult class with the specified result string.
        /// </summary>
        /// <param name="result">The result string that represents the outcome of the dialog operation. This value cannot be null.</param>
        private protected CustomDialogDerivative(string result) : base(result)
        {
        }

        /// <summary>
        /// Gets the result value as a string.
        /// </summary>
        /// <remarks>Re-exposes the base type's non-public field so that PowerShell renders a derived result as
        /// a property table alongside its own values, rather than as the bare string a
        /// <see cref="CustomDialogResult"/> prints. A second field here would serialise the value twice.</remarks>
        public new string Result => base.Result;
    }
}
