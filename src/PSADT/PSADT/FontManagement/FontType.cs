namespace PSADT.FontManagement
{
    /// <summary>
    /// Represents supported font types and their registry suffixes.
    /// </summary>
    internal enum FontType
    {
        /// <summary>
        /// TrueType Font (.ttf, .ttc).
        /// </summary>
        TrueType,

        /// <summary>
        /// OpenType Font (.otf).
        /// </summary>
        OpenType,

        /// <summary>
        /// Font File (.fon, .fnt).
        /// </summary>
        Raster
    }
}
