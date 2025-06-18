namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies predefined icons that can be used in task dialog configurations.
    /// </summary>
    /// <remarks>The <see cref="TASKDIALOG_ICON"/> enumeration provides constants for commonly used system icons in task dialogs, such as error, information, warning, and shield icons. These icons are typically used to visually convey the purpose or severity of a message displayed in a task dialog.</remarks>
    public enum TASKDIALOG_ICON
    {
        /// <summary>
        /// Represents the error icon used in task dialog configurations.
        /// </summary>
        /// <remarks>This constant is typically used to specify the icon displayed in a task dialog to indicate an error state.</remarks>
        TD_ERROR_ICON = 65534,  // Windows.Win32.PInvoke.TD_ERROR_ICON

        /// <summary>
        /// Represents the information icon used in task dialog configurations.
        /// </summary>
        /// <remarks>This constant is typically used to specify an information icon in a task dialog. The value corresponds to a predefined system icon.</remarks>
        TD_INFORMATION_ICON = 65533,  // Windows.Win32.PInvoke.TD_INFORMATION_ICON

        /// <summary>
        /// Represents the resource identifier for the shield icon used in task dialog configurations.
        /// </summary>
        /// <remarks>This value is typically used to specify a predefined icon in a task dialog, such as a security shield, to indicate a warning or security-related message.</remarks>
        TD_SHIELD_ICON = 65532,  // Windows.Win32.PInvoke.TD_SHIELD_ICON

        /// <summary>
        /// Represents the warning icon used in task dialog configurations.
        /// </summary>
        /// <remarks>This constant is used to specify a warning icon in task dialog APIs. The value corresponds to the predefined warning icon resource.</remarks>
        TD_WARNING_ICON = 65535,  // Windows.Win32.PInvoke.TD_WARNING_ICON
    }
}
