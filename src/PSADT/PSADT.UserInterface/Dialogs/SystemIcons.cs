using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using PSADT.LibraryInterfaces;
using PSADT.UserInterface.Utilities;
using Windows.Win32;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.Shell;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Static class to manage system icons.
    /// </summary>
    internal static class SystemIcons
    {
        /// <summary>
        /// Initializes the <see cref="SystemIcons"/> class by populating a lookup table of system icons.
        /// </summary>
        /// <remarks>This static constructor retrieves a predefined set of system icons, resizes them
        /// based on the current system DPI, and maps them to corresponding <see cref="DialogSystemIcon"/> values. The
        /// resulting lookup table is stored in <see cref="SystemIconLookupTable"/> for use throughout the
        /// application.</remarks>
        static SystemIcons()
        {
            // Define temporary list of system icons to look up.
            ReadOnlyCollection<SHSTOCKICONID> lookupList = new(
            [
                SHSTOCKICONID.SIID_APPLICATION,
                SHSTOCKICONID.SIID_ERROR,
                SHSTOCKICONID.SIID_HELP,
                SHSTOCKICONID.SIID_INFO,
                SHSTOCKICONID.SIID_SHIELD,
                SHSTOCKICONID.SIID_WARNING,
            ]);

            // Get the DPI for the current system.
            SHCore.GetDpiForDefaultMonitor(MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY);
            int x = (int)(48.0 * (dpiX / 96.0)); int y = (int)(48.0 * (dpiY / 96.0));

            // Internal worker method to retrieve a stock icon as a Bitmap.
            static Bitmap GetSystemStockIconAsBitmap(SHSTOCKICONID siid, SHIL_SIZE iImageList)
            {
                // Get a handle to specified stock icon.
                Shell32.SHGetImageList(iImageList, out var imageList);
                Shell32.SHGetStockIconInfo(siid, SHGSI_FLAGS.SHGSI_SYSICONINDEX, out var shii);
                imageList.GetIcon(shii.iSysImageIndex, (uint)(IMAGE_LIST_DRAW_STYLE.ILD_TRANSPARENT | IMAGE_LIST_DRAW_STYLE.ILD_PRESERVEALPHA), out var iconHandle);
                using (iconHandle)
                {
                    bool iconHandleAddRef = false;
                    try
                    {
                        iconHandle.DangerousAddRef(ref iconHandleAddRef);
                        using var icon = Icon.FromHandle(iconHandle.DangerousGetHandle());
                        return icon.ToBitmap();
                    }
                    finally
                    {
                        if (iconHandleAddRef)
                        {
                            iconHandle.DangerousRelease();
                        }
                    }
                }
            }

            // Build an icon out for each stock icon.
            Dictionary<SHSTOCKICONID, Bitmap> icons = [];
            foreach (var iconId in lookupList)
            {
                using var icon = GetSystemStockIconAsBitmap(iconId, SHIL_SIZE.SHIL_JUMBO);
                icons.Add(iconId, DrawingUtilities.ResizeBitmap(icon, x, y));
            }

            // Return a translated dictionary that matches System.Drawing.SystemIcons.
            SystemIconLookupTable = new(new Dictionary<DialogSystemIcon, Bitmap>()
            {
                { DialogSystemIcon.Application, icons[SHSTOCKICONID.SIID_APPLICATION] },
                { DialogSystemIcon.Asterisk, icons[SHSTOCKICONID.SIID_INFO] },
                { DialogSystemIcon.Error, icons[SHSTOCKICONID.SIID_ERROR] },
                { DialogSystemIcon.Exclamation, icons[SHSTOCKICONID.SIID_WARNING] },
                { DialogSystemIcon.Hand, icons[SHSTOCKICONID.SIID_ERROR] },
                { DialogSystemIcon.Information, icons[SHSTOCKICONID.SIID_INFO] },
                { DialogSystemIcon.Question, icons[SHSTOCKICONID.SIID_HELP] },
                { DialogSystemIcon.Shield, icons[SHSTOCKICONID.SIID_SHIELD] },
                { DialogSystemIcon.Warning, icons[SHSTOCKICONID.SIID_WARNING] },
                { DialogSystemIcon.WinLogo, icons[SHSTOCKICONID.SIID_APPLICATION] },
            });
        }

        /// <summary>
        /// Retrieves the system icon associated with the specified <see cref="DialogSystemIcon"/>.
        /// </summary>
        /// <param name="icon">The <see cref="DialogSystemIcon"/> representing the desired system icon.</param>
        /// <returns>A <see cref="Bitmap"/> object containing the requested system icon.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the specified <paramref name="icon"/> does not exist in the system icon lookup table.</exception>
        internal static Bitmap Get(DialogSystemIcon icon)
        {
            // Return the requested system icon.
            if (!SystemIconLookupTable.TryGetValue(icon, out Bitmap? bitmap))
            {
                throw new KeyNotFoundException($"The requested system icon [{icon}] is not available.");
            }
            return (Bitmap)bitmap.Clone();
        }

        /// <summary>
        /// A lookup table for system icons.
        /// </summary>
        private static readonly ReadOnlyDictionary<DialogSystemIcon, Bitmap> SystemIconLookupTable;
    }
}
