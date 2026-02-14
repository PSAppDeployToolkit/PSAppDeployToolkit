using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using PSADT.LibraryInterfaces;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Specifies the possible results of a message box operation.
    /// </summary>
    /// <remarks>This record represents the various outcomes of a message box interaction, such as the button selected by the user or other conditions like a timeout. Each value corresponds to a specific Windows API constant from MESSAGEBOX_RESULT. These results are typically used to determine the user's response to a prompt or dialog.</remarks>
    [DataContract]
    public sealed class DialogBoxResult : TypedConstant<DialogBoxResult>, IDialogResult
    {
        /// <summary>
        /// Represents the result of a message box operation where the user selects the "OK" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDOK. It is typically used to indicate that the user acknowledged the message or completed an action in response to a prompt.</remarks>
        public static readonly DialogBoxResult OK = new(MESSAGEBOX_RESULT.IDOK);

        /// <summary>
        /// Represents the result of a message box operation where the user selects the "Cancel" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDCANCEL. It is typically used to indicate that the user canceled the operation or dismissed the message box without making a selection.</remarks>
        public static readonly DialogBoxResult Cancel = new(MESSAGEBOX_RESULT.IDCANCEL);

        /// <summary>
        /// Represents the result of a message box operation where the user selected the "Abort" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDABORT. It is typically used to indicate that the user chose to abort an operation in response to a message box prompt.</remarks>
        public static readonly DialogBoxResult Abort = new(MESSAGEBOX_RESULT.IDABORT);

        /// <summary>
        /// Represents the result of a message box operation where the user selects the "Retry" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDRETRY. It is typically used to indicate that the user has chosen to retry an operation after encountering an error or prompt.</remarks>
        public static readonly DialogBoxResult Retry = new(MESSAGEBOX_RESULT.IDRETRY);

        /// <summary>
        /// Represents the result of a message box operation where the user selects "Ignore."
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDIGNORE. It is typically used to handle scenarios where the user chooses to ignore a warning or error.</remarks>
        public static readonly DialogBoxResult Ignore = new(MESSAGEBOX_RESULT.IDIGNORE);

        /// <summary>
        /// Represents a result indicating that the user selected "Yes" in a message box.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDYES. It is typically used to indicate that the user has confirmed an action or answered "Yes" to a prompt.</remarks>
        public static readonly DialogBoxResult Yes = new(MESSAGEBOX_RESULT.IDYES);

        /// <summary>
        /// Represents the result of a message box operation where the user selected "No".
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDNO. It is typically used to indicate that the user declined an action or answered "No" to a prompt.</remarks>
        public static readonly DialogBoxResult No = new(MESSAGEBOX_RESULT.IDNO);

        /// <summary>
        /// Represents the result of a message box when the Close button is selected.
        /// </summary>
        /// <remarks>This value corresponds to the Close button being clicked or the message box being dismissed without selecting any other option. It is typically used to handle scenarios where the user closes the message box without making a specific choice.</remarks>
        public static readonly DialogBoxResult Close = new(MESSAGEBOX_RESULT.IDCLOSE);

        /// <summary>
        /// Represents the result indicating that the user selected the "Try Again" option in a message box.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDTRYAGAIN. It is typically used to handle scenarios where the user opts to retry an operation after a failure.</remarks>
        public static readonly DialogBoxResult TryAgain = new(MESSAGEBOX_RESULT.IDTRYAGAIN);

        /// <summary>
        /// Represents the result of a message box operation where the user selects "Continue."
        /// </summary>
        /// <remarks>This value corresponds to the "Continue" button in a message box, typically used to indicate that the user has chosen to proceed with the operation.</remarks>
        public static readonly DialogBoxResult Continue = new(MESSAGEBOX_RESULT.IDCONTINUE);

        /// <summary>
        /// Represents the result of a message box operation when the operation times out.
        /// </summary>
        /// <remarks>This value is returned when a message box is displayed with a timeout and the timeout period elapses before the user interacts with the message box.</remarks>
        public static readonly DialogBoxResult Timeout = new(MESSAGEBOX_RESULT.IDTIMEOUT);

        /// <summary>
        /// Converts a numeric value to its corresponding <see cref="DialogBoxResult"/> instance.
        /// </summary>
        /// <param name="value">The numeric value to convert.</param>
        /// <returns>The corresponding <see cref="DialogBoxResult"/> static instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value does not correspond to a known result.</exception>
        internal static DialogBoxResult FromMessageBoxResult(MESSAGEBOX_RESULT value)
        {
            return !MessageBoxResultMap.TryGetValue(value, out DialogBoxResult? result)
                ? throw new ArgumentOutOfRangeException(nameof(value), value, $"Unknown DialogBoxResult value: {value}")
                : result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogBoxResult"/> class with the specified value.
        /// </summary>
        /// <param name="value">The MESSAGEBOX_RESULT value to be associated with this instance.</param>
        /// <param name="name">The name to be associated with this instance for string comparisons. Automatically captured from the caller member name.</param>
        private DialogBoxResult(MESSAGEBOX_RESULT value, [CallerMemberName] string name = null!) : base((nint)value, name)
        {
        }

        /// <summary>
        /// Provides a read-only mapping between message box result values and their corresponding dialog box results.
        /// </summary>
        /// <remarks>This dictionary enables consistent translation of user responses from message boxes
        /// to dialog box results within the deployment session. The mapping is intended for internal use and is not
        /// typically accessed directly by consumers of the API.</remarks>
        private static readonly ReadOnlyDictionary<MESSAGEBOX_RESULT, DialogBoxResult> MessageBoxResultMap = new(typeof(DialogBoxResult).GetFields(BindingFlags.Public | BindingFlags.Static).ToDictionary(static field => (MESSAGEBOX_RESULT)(nint)(DialogBoxResult)field.GetValue(null)!, static field => (DialogBoxResult)field.GetValue(null)!));
    }
}
