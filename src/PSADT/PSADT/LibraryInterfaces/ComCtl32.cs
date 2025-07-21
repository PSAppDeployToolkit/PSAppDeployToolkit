using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides managed interop functionality for the Windows Common Controls library (ComCtl32.dll), including support for task dialogs and related UI components.
    /// </summary>
    /// <remarks>This class contains methods and utilities for interacting with the native ComCtl32 library, which provides user interface elements such as task dialogs. It is intended for internal use within the application and is not designed for direct use by external consumers. The methods in this class wrap native APIs, offering a managed interface for their functionality.</remarks>
    internal static class ComCtl32
    {
        /// <summary>
        /// Displays a task dialog, a modal dialog box that provides a flexible and customizable user interface for presenting information and receiving user input.
        /// </summary>
        /// <remarks>This method wraps the native TaskDialog API, providing a managed interface for displaying a task dialog. The dialog is modal and blocks the calling thread until the user closes it.</remarks>
        /// <param name="hwndOwner">A handle to the owner window for the task dialog. This can be <see langword="null"/> if the dialog has no owner.</param>
        /// <param name="hInstance">A handle to the module instance that contains the dialog box template. This can be <see langword="null"/> if not applicable.</param>
        /// <param name="pszWindowTitle">The title of the task dialog window. This can be <see langword="null"/> to use the default title.</param>
        /// <param name="pszMainInstruction">The main instruction text displayed prominently in the task dialog. This can be <see langword="null"/> if no main instruction is needed.</param>
        /// <param name="pszContent">The additional content text displayed in the task dialog. This can be <see langword="null"/> if no additional content is needed.</param>
        /// <param name="dwCommonButtons">A combination of <see cref="TASKDIALOG_COMMON_BUTTON_FLAGS"/> values that specify the common buttons to display in the task dialog.</param>
        /// <param name="pszIcon">The resource identifier or name of the icon to display in the task dialog. This can be <see langword="null"/> if no icon is needed.</param>
        /// <returns>A <see cref="MESSAGEBOX_RESULT"/> value indicating the result of the task dialog operation.</returns>
        internal unsafe static MESSAGEBOX_RESULT TaskDialog(HWND hwndOwner, HINSTANCE hInstance, string? pszWindowTitle, string? pszMainInstruction, string? pszContent, TASKDIALOG_COMMON_BUTTON_FLAGS dwCommonButtons, TASKDIALOG_ICON pszIcon)
        {
            fixed (char* pszWindowTitleLocal = pszWindowTitle, pszMainInstructionLocal = pszMainInstruction, pszContentLocal = pszContent)
            {
                int pnButtonLocal = 0;
                PInvoke.TaskDialog(hwndOwner, hInstance, pszWindowTitleLocal, pszMainInstructionLocal, pszContentLocal, dwCommonButtons, TaskDialogIconLookupTable[pszIcon], &pnButtonLocal).ThrowOnFailure();
                return (MESSAGEBOX_RESULT)pnButtonLocal;
            }
        }

        /// <summary>
        /// Provides a lookup table that maps <see cref="TASKDIALOG_ICON"/> values to their corresponding <see cref="PCWSTR"/> representations for use in task dialog configurations.
        /// </summary>
        /// <remarks>This read-only dictionary is pre-populated with mappings between standard task dialog icons (e.g., error, information, shield, and warning) and their associated native pointers (<see cref="PCWSTR"/>). It is intended to simplify the process of retrieving the appropriate icon resource for task dialogs.</remarks>
        private static readonly ReadOnlyDictionary<TASKDIALOG_ICON, PCWSTR> TaskDialogIconLookupTable = new(new Dictionary<TASKDIALOG_ICON, PCWSTR>
        {
            { TASKDIALOG_ICON.TD_ERROR_ICON, PInvoke.TD_ERROR_ICON },
            { TASKDIALOG_ICON.TD_INFORMATION_ICON, PInvoke.TD_INFORMATION_ICON },
            { TASKDIALOG_ICON.TD_SHIELD_ICON, PInvoke.TD_SHIELD_ICON },
            { TASKDIALOG_ICON.TD_WARNING_ICON, PInvoke.TD_WARNING_ICON },
        });
    }
}
