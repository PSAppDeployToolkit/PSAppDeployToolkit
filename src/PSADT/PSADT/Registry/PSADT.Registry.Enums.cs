namespace PSADT.Registry
{
    /// <summary>
    /// Specifies the encoding to use for binary registry values.
    /// </summary>
    public enum RegistryBinaryValueEncoding
    {
        /// <summary>
        /// No encoding is applied; binary data is treated as raw bytes.
        /// </summary>
        None,

        /// <summary>
        /// Binary data is encoded using UTF8.
        /// </summary>
        UTF8,

        /// <summary>
        /// Binary data is encoded using UTF16 (Unicode).
        /// </summary>
        UTF16,

        /// <summary>
        /// Binary data is encoded using UTF32.
        /// </summary>
        UTF32,

        /// <summary>
        /// Binary data is encoded using ASCII.
        /// </summary>
        ASCII
    }

    /// <summary>
    /// Specifies options for handling binary registry values as either a byte array or a string.
    /// </summary>
    public enum RegistryBinaryValueOptions
    {
        None = 0,
        DecodeAsString = 1,
        ConvertToHexString = 2
    }

    /// <summary>
    /// Specifies options for formatting a registry subkey path.
    /// </summary>
    public enum RegistrySubKeyDisplayOptions
    {
        RelativePath = 0,
        FullyQualifiedPath = 1,
        Leaf = 2
    }
}
