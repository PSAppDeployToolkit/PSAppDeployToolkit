using System.Runtime.InteropServices;

namespace PSADT.Interop
{
    /// <summary>
    /// Represents an entry in an icon directory, containing metadata about the icon image such as dimensions, color
    /// depth, and offsets.
    /// </summary>
    /// <remarks>This structure is used to define the properties of an icon image in a resource file, allowing
    /// for efficient access to image data and its characteristics.</remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct ICONDIRENTRY
    {
        /// <summary>
        /// Gets the width of the object, measured in bytes.
        /// </summary>
        internal readonly byte bWidth;

        /// <summary>
        /// Gets the height value represented as a byte.
        /// </summary>
        internal readonly byte bHeight;

        /// <summary>
        /// Gets the total number of colors available.
        /// </summary>
        internal readonly byte bColorCount;

        /// <summary>
        /// Gets the reserved byte value, which is intended for future use or specific purposes.
        /// </summary>
        internal readonly byte bReserved;

        /// <summary>
        /// Gets the number of color planes for the image.
        /// </summary>
        internal readonly ushort wPlanes;

        /// <summary>
        /// Gets the number of bits used to represent a pixel in the image.
        /// </summary>
        /// <remarks>This property is typically used in image processing to determine the color depth of
        /// the image. A higher bit count allows for a greater range of colors.</remarks>
        internal readonly ushort wBitCount;

        /// <summary>
        /// Gets the number of bytes in the resource.
        /// </summary>
        internal readonly uint dwBytesInRes;

        /// <summary>
        /// Gets the offset, in bytes, of the image data within the file.
        /// </summary>
        internal readonly uint dwImageOffset;

        /// <summary>
        /// Gets a value indicating whether the current dimensions and resource size are valid.
        /// </summary>
        /// <remarks>This property returns <see langword="true"/> if both the width and height are greater
        /// than zero, and the resource size is also greater than zero. It is useful for validating the state of the
        /// object before performing operations that depend on valid dimensions and resource size.</remarks>
        internal readonly bool IsValid => bWidth > 0 && bHeight > 0 && dwBytesInRes > 0;
    }
}
