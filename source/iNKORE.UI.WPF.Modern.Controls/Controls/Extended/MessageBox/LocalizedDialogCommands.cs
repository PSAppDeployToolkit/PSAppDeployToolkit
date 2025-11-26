using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace iNKORE.UI.WPF.Modern.Controls

{
    internal static class LocalizedDialogCommands
    {
        /// <summary>
        /// Fallback display strings for standard dialog box commands
        /// when localized resources are unavailable or the native API call fails.
        /// </summary>
        internal static readonly Dictionary<DialogBoxCommand, string> FallbackStrings = new()
        {
            { DialogBoxCommand.IDOK, "OK" },
            { DialogBoxCommand.IDCANCEL, "Cancel" },
            { DialogBoxCommand.IDABORT, "Abort" },
            { DialogBoxCommand.IDRETRY, "Retry" },
            { DialogBoxCommand.IDIGNORE, "Ignore" },
            { DialogBoxCommand.IDYES, "Yes" },
            { DialogBoxCommand.IDNO, "No" },
            { DialogBoxCommand.IDCLOSE, "Close" },
            { DialogBoxCommand.IDHELP, "Help" },
            { DialogBoxCommand.IDTRYAGAIN, "Try Again" },
            { DialogBoxCommand.IDCONTINUE, "Continue" }
        };

        public static string GetString(DialogBoxCommand command)
        {
            try
            {
                //return Marshal.PtrToStringAuto(MB_GetString((int)command))?.TrimStart('&')!;
                return Marshal.PtrToStringAuto(MB_GetString((int)command))?.Replace("&", "")!;
            }
            catch (EntryPointNotFoundException)
            {
                return FallbackStrings.TryGetValue(command, out var value)
                    ? value
                    : command.ToString();
            }
        }

        /// <summary>
        /// Returns strings for standard message box buttons.
        /// </summary>
        /// <param name="strId">The id of the string to return. These are identified by the ID* values assigned to the predefined buttons.</param>
        /// <returns>The string, or NULL if not found</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr MB_GetString(int strId);

        /// <summary>
        /// Represents possible dialogbox command id values by the MB_GetString function.
        /// </summary>
        public enum DialogBoxCommand : int
        {
            IDOK = 0,
            IDCANCEL = 1,
            IDABORT = 2,
            IDRETRY = 3,
            IDIGNORE = 4,
            IDYES = 5,
            IDNO = 6,
            IDCLOSE = 7,
            IDHELP = 8,
            IDTRYAGAIN = 9,
            IDCONTINUE = 10
        }
    }
}
