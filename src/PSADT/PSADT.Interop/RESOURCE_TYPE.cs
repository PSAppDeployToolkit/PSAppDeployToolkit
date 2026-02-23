using Windows.Win32.Foundation;

namespace PSADT.Interop
{
    /// <summary>
    /// Represents a type of resource used in Windows applications, such as bitmaps, icons, or menus.
    /// </summary>
    /// <remarks>This class provides a set of predefined resource types that correspond to standard Windows
    /// resource types.</remarks>
    internal sealed class RESOURCE_TYPE : TypedConstant<RESOURCE_TYPE>
    {
        /// <summary>
        /// Accelerator table.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_ACCELERATOR = new(Windows.Win32.PInvoke.RT_ACCELERATOR);

        /// <summary>
        /// Animated cursor.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_ANICURSOR = new(Windows.Win32.PInvoke.RT_ANICURSOR);

        /// <summary>
        /// Animated icon.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_ANIICON = new(Windows.Win32.PInvoke.RT_ANIICON);

        /// <summary>
        /// Bitmap resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_BITMAP = new(Windows.Win32.PInvoke.RT_BITMAP);

        /// <summary>
        /// Hardware-dependent cursor resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_CURSOR = new(Windows.Win32.PInvoke.RT_CURSOR);

        /// <summary>
        /// Dialog box.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_DIALOG = new(Windows.Win32.PInvoke.RT_DIALOG);

        /// <summary>
        /// Allows a resource editing tool to associate a string with an .rc file. Typically, the string is the name of the header file that provides symbolic names. The resource compiler parses the string but otherwise ignores the value.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_DLGINCLUDE = new(Windows.Win32.PInvoke.RT_DLGINCLUDE);

        /// <summary>
        /// Font resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_FONT = new(Windows.Win32.PInvoke.RT_FONT);

        /// <summary>
        /// Font directory resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_FONTDIR = new(Windows.Win32.PInvoke.RT_FONTDIR);

        /// <summary>
        /// Hardware-independent cursor resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_GROUP_CURSOR = new(Windows.Win32.PInvoke.RT_GROUP_CURSOR);

        /// <summary>
        /// Hardware-independent icon resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_GROUP_ICON = new(Windows.Win32.PInvoke.RT_GROUP_ICON);

        /// <summary>
        /// HTML resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_HTML = new(Windows.Win32.PInvoke.RT_HTML);

        /// <summary>
        /// Hardware-dependent icon resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_ICON = new(Windows.Win32.PInvoke.RT_ICON);

        /// <summary>
        /// Side-by-Side Assembly Manifest.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_MANIFEST = new(Windows.Win32.PInvoke.RT_MANIFEST);

        /// <summary>
        /// Menu resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_MENU = new(Windows.Win32.PInvoke.RT_MENU);

        /// <summary>
        /// Message-table entry.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_MESSAGETABLE = new(Windows.Win32.PInvoke.RT_MESSAGETABLE);

        /// <summary>
        /// Plug and Play resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_PLUGPLAY = new(Windows.Win32.PInvoke.RT_PLUGPLAY);

        /// <summary>
        /// Application-defined resource (raw data).
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_RCDATA = new(Windows.Win32.PInvoke.RT_RCDATA);

        /// <summary>
        /// String-table entry.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_STRING = new(Windows.Win32.PInvoke.RT_STRING);

        /// <summary>
        /// Version resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_VERSION = new(Windows.Win32.PInvoke.RT_VERSION);

        /// <summary>
        /// VXD.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_VXD = new(Windows.Win32.PInvoke.RT_VXD);

        /// <summary>
        /// Initializes a new instance of the <see cref="RESOURCE_TYPE"/> class with the specified handle.
        /// </summary>
        /// <param name="value">The handle to be associated with this instance.</param>
        /// <param name="name">The name of the constant, automatically captured from the calling member.</param>
        private RESOURCE_TYPE(PCWSTR value, [System.Runtime.CompilerServices.CallerMemberName] string name = null!) : base(value, name)
        {
        }
    }
}
