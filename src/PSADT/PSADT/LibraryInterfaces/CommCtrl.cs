using System;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies predefined icons that can be used in task dialog configurations.
    /// </summary>
    /// <remarks>The <see cref="TASKDIALOG_ICON"/> structure provides constants for commonly used system icons in task dialogs, such as error, information, warning, and shield icons. These icons are typically used to visually convey the purpose or severity of a message displayed in a task dialog.</remarks>
    internal readonly record struct TASKDIALOG_ICON
    {
        /// <summary>
        /// Represents the error icon used in task dialog configurations.
        /// </summary>
        /// <remarks>This constant is typically used to specify the icon displayed in a task dialog to indicate an error state.</remarks>
        public static readonly TASKDIALOG_ICON TD_ERROR_ICON = Windows.Win32.PInvoke.TD_ERROR_ICON;

        /// <summary>
        /// Represents the information icon used in task dialog configurations.
        /// </summary>
        /// <remarks>This constant is typically used to specify an information icon in a task dialog. The value corresponds to a predefined system icon.</remarks>
        public static readonly TASKDIALOG_ICON TD_INFORMATION_ICON = Windows.Win32.PInvoke.TD_INFORMATION_ICON;

        /// <summary>
        /// Represents the resource identifier for the shield icon used in task dialog configurations.
        /// </summary>
        /// <remarks>This value is typically used to specify a predefined icon in a task dialog, such as a security shield, to indicate a warning or security-related message.</remarks>
        public static readonly TASKDIALOG_ICON TD_SHIELD_ICON = Windows.Win32.PInvoke.TD_SHIELD_ICON;

        /// <summary>
        /// Represents the warning icon used in task dialog configurations.
        /// </summary>
        /// <remarks>This constant is used to specify a warning icon in task dialog APIs. The value corresponds to the predefined warning icon resource.</remarks>
        public static readonly TASKDIALOG_ICON TD_WARNING_ICON = Windows.Win32.PInvoke.TD_WARNING_ICON;

        /// <summary>
        /// Initializes a new instance of the <see cref="TASKDIALOG_ICON"/> class with the specified handle.
        /// </summary>
        /// <param name="value">The handle to be associated with this instance.</param>
        private TASKDIALOG_ICON(IntPtr value)
        {
            Value = value;
        }

        /// <summary>
        /// Converts a <see cref="TASKDIALOG_ICON"/> instance to an <see cref="IntPtr"/>.
        /// </summary>
        /// <param name="h">The <see cref="TASKDIALOG_ICON"/> instance to convert.</param>
        public static explicit operator IntPtr(TASKDIALOG_ICON h)
        {
            return h.Value;
        }

        /// <summary>
        /// Converts a <see cref="TASKDIALOG_ICON"/> instance to an <see cref="PCWSTR"/>.
        /// </summary>
        /// <param name="h">The <see cref="TASKDIALOG_ICON"/> instance to convert.</param>
        public unsafe static explicit operator PCWSTR(TASKDIALOG_ICON h)
        {
            return (PCWSTR)h.Value.ToPointer();
        }

        /// <summary>
        /// Converts a <see cref="TASKDIALOG_ICON"/> instance to an <see cref="uint"/>.
        /// </summary>
        /// <param name="h">The <see cref="TASKDIALOG_ICON"/> instance to convert.</param>
        public static explicit operator uint(TASKDIALOG_ICON h)
        {
            return (uint)h.Value;
        }

        /// <summary>
        /// Converts an <see cref="IntPtr"/> to a <see cref="TASKDIALOG_ICON"/>.
        /// </summary>
        /// <param name="h">The handle represented as an <see cref="IntPtr"/> to be converted.</param>
        public static implicit operator TASKDIALOG_ICON(IntPtr h)
        {
            return new(h);
        }

        /// <summary>
        /// Converts an <see cref="PCWSTR"/> to a <see cref="TASKDIALOG_ICON"/>.
        /// </summary>
        /// <param name="h">The handle represented as an <see cref="PCWSTR"/> to be converted.</param>
        public unsafe static implicit operator TASKDIALOG_ICON(PCWSTR h)
        {
            return new((IntPtr)h.Value);
        }

        /// <summary>
        /// Converts an <see cref="uint"/> to a <see cref="TASKDIALOG_ICON"/>.
        /// </summary>
        /// <param name="h">The handle represented as an <see cref="uint"/> to be converted.</param>
        public static implicit operator TASKDIALOG_ICON(uint h)
        {
            return new((IntPtr)h);
        }

        /// <summary>
        /// Determines whether two specified objects, an <see cref="IntPtr"/> and a <see cref="TASKDIALOG_ICON"/>, are not
        /// equal.
        /// </summary>
        /// <param name="h1">The first pointer to compare.</param>
        /// <param name="h2">The <see cref="TASKDIALOG_ICON"/> object to compare, which contains a handle.</param>
        /// <returns><see langword="true"/> if the handle of <paramref name="h2"/> is not equal to <paramref name="h1"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(IntPtr h1, TASKDIALOG_ICON h2)
        {
            return h1 != h2.Value;
        }

        /// <summary>
        /// Determines whether two specified objects are equal.
        /// </summary>
        /// <param name="h1">The first pointer to compare.</param>
        /// <param name="h2">The second resource type to compare, which contains a handle.</param>
        /// <returns><see langword="true"/> if the handle of <paramref name="h2"/> is equal to <paramref name="h1"/>; otherwise,
        /// <see langword="false"/>.</returns>
        public static bool operator ==(IntPtr h1, TASKDIALOG_ICON h2)
        {
            return h1 == h2.Value;
        }

        /// <summary>
        /// Determines whether two specified objects, an <see cref="PCWSTR"/> and a <see cref="TASKDIALOG_ICON"/>, are not
        /// equal.
        /// </summary>
        /// <param name="h1">The first pointer to compare.</param>
        /// <param name="h2">The <see cref="TASKDIALOG_ICON"/> object to compare, which contains a handle.</param>
        /// <returns><see langword="true"/> if the handle of <paramref name="h2"/> is not equal to <paramref name="h1"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public unsafe static bool operator !=(PCWSTR h1, TASKDIALOG_ICON h2)
        {
            return (IntPtr)h1.Value != h2.Value;
        }

        /// <summary>
        /// Determines whether two specified objects are equal.
        /// </summary>
        /// <param name="h1">The first pointer to compare.</param>
        /// <param name="h2">The second resource type to compare, which contains a handle.</param>
        /// <returns><see langword="true"/> if the handle of <paramref name="h2"/> is equal to <paramref name="h1"/>; otherwise,
        /// <see langword="false"/>.</returns>
        public unsafe static bool operator ==(PCWSTR h1, TASKDIALOG_ICON h2)
        {
            return (IntPtr)h1.Value == h2.Value;
        }

        /// <summary>
        /// Determines whether two specified objects, a <see cref="uint"/> and a <see cref="TASKDIALOG_ICON"/>, are not
        /// equal.
        /// </summary>
        /// <param name="h1">The first operand, a 32-bit unsigned integer representing a resource handle.</param>
        /// <param name="h2">The second operand, a <see cref="TASKDIALOG_ICON"/> object containing a resource handle.</param>
        /// <returns><see langword="true"/> if the handle represented by <paramref name="h1"/> is not equal to the handle
        /// contained in <paramref name="h2"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(uint h1, TASKDIALOG_ICON h2)
        {
            return (IntPtr)h1 != h2;
        }

        /// <summary>
        /// Determines whether the specified <see cref="uint"/> and <see cref="TASKDIALOG_ICON"/> are equal.
        /// </summary>
        /// <param name="h1">The unsigned integer handle to compare.</param>
        /// <param name="h2">The <see cref="TASKDIALOG_ICON"/> instance to compare.</param>
        /// <returns><see langword="true"/> if the handle of <paramref name="h2"/> is equal to <paramref name="h1"/>; otherwise,
        /// <see langword="false"/>.</returns>
        public static bool operator ==(uint h1, TASKDIALOG_ICON h2)
        {
            return (IntPtr)h1 == h2;
        }

        /// <summary>
        /// Represents a handle to a system resource.
        /// </summary>
        /// <remarks>This field is used to store a pointer to a native resource. It is important to ensure
        /// that the handle is properly managed to prevent resource leaks. Typically, this involves releasing the handle
        /// when it is no longer needed.</remarks>
        private readonly IntPtr Value;
    }
}
