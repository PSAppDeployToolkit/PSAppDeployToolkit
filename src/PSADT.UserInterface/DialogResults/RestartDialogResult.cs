using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using PSADT.Interop;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Represents the possible outcomes of a dialog prompting the user to restart the system.
    /// </summary>
    /// <remarks>This enumeration is used to indicate the user's response to a dialog that requests action regarding restarting the system, such as restarting or canceling the operation.</remarks>
    [DataContract]
    public sealed class RestartDialogResult : TypedConstant<RestartDialogResult>, IDialogResult
    {
        /// <summary>
        /// Returned when the user has not responded to the dialog.
        /// </summary>
        public static readonly RestartDialogResult Unknown = new(0);

        /// <summary>
        /// Returned when the user has chosen to restart the system.
        /// </summary>
        public static readonly RestartDialogResult Restart = new(1);

        /// <summary>
        /// Specifies that the user has chosen to cancel the operation.
        /// </summary>
        public static readonly RestartDialogResult Cancel = new(2);

        /// <summary>
        /// Initializes a new instance of the <see cref="RestartDialogResult"/> class with the specified value.
        /// </summary>
        /// <param name="value">The numeric value to be associated with this instance.</param>
        /// <param name="name">The name to be associated with this instance for string comparisons. Automatically captured from the caller member name.</param>
        private RestartDialogResult(nint value, [CallerMemberName] string? name = null) : base(value, name)
        {
        }
    }
}
