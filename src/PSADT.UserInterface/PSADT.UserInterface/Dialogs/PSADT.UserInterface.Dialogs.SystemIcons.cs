using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using PSADT.UserInterface.LibraryInterfaces;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Static class to manage system icons.
    /// </summary>
    internal static class SystemIcons
    {
        /// <summary>
        /// Retrieves a system stock icon.
        /// </summary>
        /// <param name="siid"></param>
        /// <param name="iImageList"></param>
        /// <returns></returns>
        private static Icon GetSystemStockIcon(SHSTOCKICONID siid, SHIL_SIZE iImageList)
        {
            // Get a handle to specified stock icon.
            Shell32.SHGetImageList(iImageList, out var imageList);
            Shell32.SHGetStockIconInfo(siid, SHGSI_FLAGS.SHGSI_SYSICONINDEX, out var shii);
            imageList.GetIcon(shii.iSysImageIndex, (uint)IMAGELISTDRAWFLAGS.ILD_TRANSPARENT, out var iconHandle);
            using (iconHandle)
            {
                // Create a new icon to avoid GDI handle issues.
                using (var icon = Icon.FromHandle(iconHandle.DangerousGetHandle()))
                {
                    return (Icon)icon.Clone();
                }
            }
        }

        /// <summary>
        /// Builds a lookup table for system icons.
        /// </summary>
        /// <returns></returns>
        private static ReadOnlyDictionary<string, Icon> BuildSystemIconLookupTable()
        {
            // Define temporary list of system icons to look up.
            var lookupList = new[]
            {
                SHSTOCKICONID.SIID_APPLICATION,
                SHSTOCKICONID.SIID_ERROR,
                SHSTOCKICONID.SIID_HELP,
                SHSTOCKICONID.SIID_INFO,
                SHSTOCKICONID.SIID_SHIELD,
                SHSTOCKICONID.SIID_WARNING,
            };

            // Build an icon out for each stock icon.
            Dictionary<SHSTOCKICONID, Icon> icons = [];
            foreach(var iconId in lookupList)
            {
                icons.Add(iconId, GetSystemStockIcon(iconId, SHIL_SIZE.SHIL_JUMBO));
            }

            // Return a translated dictionary that matches System.Drawing.SystemIcons.
            return new ReadOnlyDictionary<string, Icon>(new Dictionary<string, Icon>
            {
                { "Application", icons[SHSTOCKICONID.SIID_APPLICATION] },
                { "Asterisk", icons[SHSTOCKICONID.SIID_INFO] },
                { "Error", icons[SHSTOCKICONID.SIID_ERROR] },
                { "Exclamation", icons[SHSTOCKICONID.SIID_WARNING] },
                { "Hand", icons[SHSTOCKICONID.SIID_ERROR] },
                { "Information", icons[SHSTOCKICONID.SIID_INFO] },
                { "Question", icons[SHSTOCKICONID.SIID_HELP] },
                { "Shield", icons[SHSTOCKICONID.SIID_SHIELD] },
                { "Warning", icons[SHSTOCKICONID.SIID_WARNING] },
                { "WinLogo", icons[SHSTOCKICONID.SIID_APPLICATION] },
            });
        }

        /// <summary>
        /// A lookup table for system icons.
        /// </summary>
        internal static readonly ReadOnlyDictionary<string, Icon> SystemIconLookupTable = BuildSystemIconLookupTable();
    }
}
