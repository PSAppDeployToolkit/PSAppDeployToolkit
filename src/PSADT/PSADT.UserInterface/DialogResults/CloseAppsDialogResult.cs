using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using PSADT.LibraryInterfaces;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Represents the possible outcomes of a dialog prompting the user to close applications.
    /// </summary>
    /// <remarks>This enumeration is used to indicate the user's response to a dialog that requests action regarding open applications, such as closing them, continuing without closing, or deferring the operation.</remarks>
    [DataContract]
    public sealed class CloseAppsDialogResult : TypedConstant<CloseAppsDialogResult>, IDialogResult
    {
        /// <summary>
        /// Returned when the user has not responded to the dialog in time.
        /// </summary>
        public static readonly CloseAppsDialogResult Timeout = new(0);

        /// <summary>
        /// Specifies that the user has chosen to close the application.
        /// </summary>
        public static readonly CloseAppsDialogResult Close = new(1);

        /// <summary>
        /// Specifies that the user has chosen to continue without closing the application.
        /// </summary>
        public static readonly CloseAppsDialogResult Continue = new(2);

        /// <summary>
        /// Specifies that the user has chosen to defer the deployment.
        /// </summary>
        public static readonly CloseAppsDialogResult Defer = new(3);

        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialogResult"/> class with the specified value.
        /// </summary>
        /// <param name="value">The numeric value to be associated with this instance.</param>
        /// <param name="name">The name to be associated with this instance for string comparisons. Automatically captured from the caller member name.</param>
        private CloseAppsDialogResult(nint value, [CallerMemberName] string name = null!) : base(value, name)
        {
        }
    }
}
