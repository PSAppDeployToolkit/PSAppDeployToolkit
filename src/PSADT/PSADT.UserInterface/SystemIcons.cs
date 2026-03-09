using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using PSADT.Interop;
using PSADT.UserInterface.Utilities;
using Windows.Win32;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.Shell;

namespace PSADT.UserInterface
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "This exception is a guardrail that will never throw.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "The static constructor is very much needed.")]
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
            _ = NativeMethods.GetDpiForDefaultMonitor(MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY);
            int x = (int)((decimal)48 * dpiX / 96); int y = (int)((decimal)48 * dpiY / 96);
            if (x != y)
            {
                throw new InvalidOperationException($"Calculated DPI-based dimensions for stock icons are not square. Calculated dimensions: {x}x{y}.");
            }

            // Internal worker method to retrieve a stock icon as a bitmap.
            static Bitmap GetSystemStockIconBitmap(SHSTOCKICONID siid, SHIL_SIZE iImageList)
            {
                // Get a handle to specified stock icon.
                _ = NativeMethods.SHGetImageList(iImageList, out IImageList imageList);
                _ = NativeMethods.SHGetStockIconInfo(siid, SHGSI_FLAGS.SHGSI_SYSICONINDEX, out SHSTOCKICONINFO shii);
                using (shii)
                {
                    imageList.GetIcon(shii.iSysImageIndex, (uint)(IMAGE_LIST_DRAW_STYLE.ILD_TRANSPARENT | IMAGE_LIST_DRAW_STYLE.ILD_PRESERVEALPHA), out DestroyIconSafeHandle iconHandle);
                    InvalidOperationException.ThrowIfNullOrInvalid(iconHandle, $"Failed to get a valid handle for the stock icon '{siid}'.");
                    using (iconHandle)
                    {
                        bool iconHandleAddRef = false;
                        try
                        {
                            iconHandle.DangerousAddRef(ref iconHandleAddRef);
                            using Icon icon = Icon.FromHandle(iconHandle.DangerousGetHandle());
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
            }

            // Build an icon out for each stock icon.
            Dictionary<SHSTOCKICONID, Bitmap> icons = [];
            foreach (SHSTOCKICONID iconId in lookupList)
            {
                using Bitmap icon = GetSystemStockIconBitmap(iconId, SHIL_SIZE.SHIL_JUMBO);
                icons.Add(iconId, DrawingUtilities.ResizeBitmap(icon, x));
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
            return !SystemIconLookupTable.TryGetValue(icon, out Bitmap? bitmap)
                ? throw new KeyNotFoundException($"The requested system icon [{icon}] is not available.")
                : (Bitmap)bitmap.Clone();
        }

        /// <summary>
        /// A lookup table for system icons.
        /// </summary>
        private static readonly ReadOnlyDictionary<DialogSystemIcon, Bitmap> SystemIconLookupTable;
    }
}
