using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Runtime.InteropServices;
using PSADT.Interop;
using Windows.Win32;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Shell;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Static class to manage system icons.
    /// </summary>
    internal static class SystemIcons
    {
        /// <summary>
        /// Retrieves the system icon associated with the specified <see cref="DialogSystemIcon"/>.
        /// </summary>
        /// <param name="icon">The <see cref="DialogSystemIcon"/> representing the desired system icon.</param>
        /// <param name="size">The desired size of the icon. If not specified, the default size of the cached icon is used.</param>
        /// <returns>An <see cref="Icon"/> object containing the requested system icon.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the specified <paramref name="icon"/> does not exist in the system icon lookup table.</exception>
        internal static DestroyIconSafeHandle Get(DialogSystemIcon icon, SHIL_SIZE size)
        {
            // Get a handle to specified stock icon.
            _ = NativeMethods.SHGetImageList(size, out IImageList imageList);
            try
            {
                _ = NativeMethods.SHGetStockIconInfo(SystemIconLookupTable[icon], SHGSI_FLAGS.SHGSI_SYSICONINDEX, out SHSTOCKICONINFO shii);
                using (shii)
                {
                    imageList.GetIcon(shii.iSysImageIndex, (uint)(IMAGE_LIST_DRAW_STYLE.ILD_TRANSPARENT | IMAGE_LIST_DRAW_STYLE.ILD_PRESERVEALPHA), out DestroyIconSafeHandle hIcon);
                    InvalidOperationException.ThrowIfNullOrInvalid(hIcon, $"Failed to get a valid handle for the stock icon '{icon}'.");
                    return hIcon;
                }
            }
            finally
            {
                _ = Marshal.FinalReleaseComObject(imageList);
            }
        }

        /// <summary>
        /// A lookup table for system icons.
        /// </summary>
        private static readonly ReadOnlyDictionary<DialogSystemIcon, SHSTOCKICONID> SystemIconLookupTable = new(new Dictionary<DialogSystemIcon, SHSTOCKICONID>
        {
            { DialogSystemIcon.Application, SHSTOCKICONID.SIID_APPLICATION },
            { DialogSystemIcon.Asterisk, SHSTOCKICONID.SIID_INFO },
            { DialogSystemIcon.Error, SHSTOCKICONID.SIID_ERROR },
            { DialogSystemIcon.Exclamation, SHSTOCKICONID.SIID_WARNING },
            { DialogSystemIcon.Hand, SHSTOCKICONID.SIID_ERROR },
            { DialogSystemIcon.Information, SHSTOCKICONID.SIID_INFO },
            { DialogSystemIcon.Question, SHSTOCKICONID.SIID_HELP },
            { DialogSystemIcon.Shield, SHSTOCKICONID.SIID_SHIELD },
            { DialogSystemIcon.Warning, SHSTOCKICONID.SIID_WARNING },
            { DialogSystemIcon.WinLogo, SHSTOCKICONID.SIID_APPLICATION },
        });
    }
}
