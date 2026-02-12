using System;
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
        internal CustomDialogDerivative(string result) : base(result)
        {
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new ArgumentNullException(nameof(result), "Result cannot be null or empty.");
            }
            Result = result;
        }

        /// <summary>
        /// Gets or sets the result value as a string.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string Result;
    }
}
