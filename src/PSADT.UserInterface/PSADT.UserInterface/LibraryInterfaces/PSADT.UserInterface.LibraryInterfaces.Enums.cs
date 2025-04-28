using System;

namespace PSADT.UserInterface.LibraryInterfaces
{
    /// <summary>
    /// Flags for SHGetImageList function.
    /// </summary>
    internal enum SHIL_SIZE
    {
        /// <summary>
        /// The image size is normally 32x32 pixels. However, if the Use large icons option is selected from the Effects section of the Appearance tab in Display Properties, the image is 48x48 pixels.
        /// </summary>
        SHIL_LARGE = 0,

        /// <summary>
        /// These images are the Shell standard small icon size of 16x16, but the size can be customized by the user.
        /// </summary>
        SHIL_SMALL = 1,

        /// <summary>
        /// These images are the Shell standard extra-large icon size. This is typically 48x48, but the size can be customized by the user.
        /// </summary>
        SHIL_EXTRALARGE = 2,

        /// <summary>
        /// These images are the size specified by GetSystemMetrics called with SM_CXSMICON and GetSystemMetrics called with SM_CYSMICON.
        /// </summary>
        SHIL_SYSSMALL = 3,

        /// <summary>
        /// Windows Vista and later. The image is normally 256x256 pixels.
        /// </summary>
        SHIL_JUMBO = 4,

        /// <summary>
        /// The largest valid flag value, for validation purposes.
        /// </summary>
        SHIL_LAST,
    }

    /// <summary>
    /// Flags that specify how the image is drawn by IImageList.Draw.
    /// See: https://learn.microsoft.com/en-us/windows/win32/controls/imagelistdrawflags
    /// </summary>
    [Flags]
    internal enum IMAGELISTDRAWFLAGS : uint
    {
        /// <summary>
        /// 0x00000000: Draws the image using the background color for the image list.
        /// If the background color is CLR_NONE, the image is drawn transparently using the mask.
        /// </summary>
        ILD_NORMAL = 0x00000000,

        /// <summary>
        /// 0x00000001: Draws the image transparently using the mask, regardless of the background color.
        /// This value has no effect if the image list does not contain a mask.
        /// </summary>
        ILD_TRANSPARENT = 0x00000001,

        /// <summary>
        /// 0x00000002: Draws the image, blending 25 percent with the blend color specified by rgbFg.
        /// This value has no effect if the image list does not contain a mask.
        /// </summary>
        ILD_BLEND25 = 0x00000002,

        /// <summary>
        /// 0x00000002: Alias for ILD_BLEND25.
        /// </summary>
        ILD_FOCUS = 0x00000002,

        /// <summary>
        /// 0x00000004: Draws the image, blending 50 percent with the blend color specified by rgbFg.
        /// This value has no effect if the image list does not contain a mask.
        /// </summary>
        ILD_BLEND50 = 0x00000004,

        /// <summary>
        /// 0x00000004: Alias for ILD_BLEND50.
        /// </summary>
        ILD_SELECTED = 0x00000004,

        /// <summary>
        /// 0x00000004: Alias for ILD_BLEND50.
        /// </summary>
        ILD_BLEND = 0x00000004,

        /// <summary>
        /// 0x00000010: Draws the mask.
        /// </summary>
        ILD_MASK = 0x00000010,

        /// <summary>
        /// 0x00000020: If the overlay does not require a mask to be drawn, set this flag.
        /// </summary>
        ILD_IMAGE = 0x00000020,

        /// <summary>
        /// 0x00000040: Draws the image using the raster operation code specified by the dwRop member.
        /// </summary>
        ILD_ROP = 0x00000040,

        /// <summary>
        /// 0x00000F00: To extract the overlay image from the fStyle member, use the logical AND
        /// to combine fStyle with this value.
        /// </summary>
        ILD_OVERLAYMASK = 0x00000F00,

        /// <summary>
        /// 0x00001000: Preserves the alpha channel in the destination.
        /// </summary>
        ILD_PRESERVEALPHA = 0x00001000,

        /// <summary>
        /// 0x00002000: Causes the image to be scaled to cx, cy instead of being clipped.
        /// </summary>
        ILD_SCALE = 0x00002000,

        /// <summary>
        /// 0x00004000: Scales the image to the current DPI of the display.
        /// </summary>
        ILD_DPISCALE = 0x00004000,

        /// <summary>
        /// 0x00008000: Windows Vista and later. Draw the image if it is available in the cache.
        /// Do not extract it automatically; the called draw method returns E_PENDING to the
        /// calling component, which should then provide an alternative action.
        /// </summary>
        ILD_ASYNC = 0x00008000
    }

    /// <summary>
    /// Flags that specify the type of information to retrieve from the system (the flags we support).
    /// </summary>
    internal enum SYSTEM_INFORMATION_CLASS : int
    {
        SystemProcessIdInformation = 0x58
    }
}
