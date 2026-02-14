using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies predefined icons that can be used in task dialog configurations.
    /// </summary>
    /// <remarks>The <see cref="TASKDIALOG_ICON"/> class provides constants for commonly used system icons in task dialogs, such as error, information, warning, and shield icons. These icons are typically used to visually convey the purpose or severity of a message displayed in a task dialog.</remarks>
    internal sealed class TASKDIALOG_ICON : TypedConstant<TASKDIALOG_ICON>
    {
        /// <summary>
        /// Represents the error icon used in task dialog configurations.
        /// </summary>
        /// <remarks>This constant is typically used to specify the icon displayed in a task dialog to indicate an error state.</remarks>
        internal static readonly TASKDIALOG_ICON TD_ERROR_ICON = new(Windows.Win32.PInvoke.TD_ERROR_ICON);

        /// <summary>
        /// Represents the information icon used in task dialog configurations.
        /// </summary>
        /// <remarks>This constant is typically used to specify an information icon in a task dialog. The value corresponds to a predefined system icon.</remarks>
        internal static readonly TASKDIALOG_ICON TD_INFORMATION_ICON = new(Windows.Win32.PInvoke.TD_INFORMATION_ICON);

        /// <summary>
        /// Represents the resource identifier for the shield icon used in task dialog configurations.
        /// </summary>
        /// <remarks>This value is typically used to specify a predefined icon in a task dialog, such as a security shield, to indicate a warning or security-related message.</remarks>
        internal static readonly TASKDIALOG_ICON TD_SHIELD_ICON = new(Windows.Win32.PInvoke.TD_SHIELD_ICON);

        /// <summary>
        /// Represents the warning icon used in task dialog configurations.
        /// </summary>
        /// <remarks>This constant is used to specify a warning icon in task dialog APIs. The value corresponds to the predefined warning icon resource.</remarks>
        internal static readonly TASKDIALOG_ICON TD_WARNING_ICON = new(Windows.Win32.PInvoke.TD_WARNING_ICON);

        /// <summary>
        /// Initializes a new instance of the <see cref="TASKDIALOG_ICON"/> class with the specified handle.
        /// </summary>
        /// <param name="value">The handle to be associated with this instance.</param>
        /// <param name="name">The name of the constant, automatically captured from the calling member.</param>
        private TASKDIALOG_ICON(PCWSTR value, [System.Runtime.CompilerServices.CallerMemberName] string name = null!) : base(value, name)
        {
        }
    }
}
