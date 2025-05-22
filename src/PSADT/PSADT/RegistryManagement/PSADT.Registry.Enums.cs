namespace PSADT.RegistryManagement
{
    /// <summary>
    /// Specifies the mode of operation for handling multiple string values in a collection.
    /// </summary>
    public enum MultiStringValueMode
    {
        /// <summary>
        /// Replaces the entire collection with the specified item.
        /// </summary>
        Replace,

        /// <summary>
        /// Adds the specified item to the collection.
        /// </summary>
        Add,

        /// <summary>
        /// Removes the specified item from the collection.
        /// </summary>
        Remove,
    }
}
