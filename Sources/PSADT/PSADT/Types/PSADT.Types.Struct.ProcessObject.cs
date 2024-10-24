namespace PSADT.Types
{
    /// <summary>
    /// Represents basic information about a process.
    /// </summary>
    public readonly struct ProcessObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessObject"/> struct.
        /// </summary>
        /// <param name="name">The name of the process.</param>
        public ProcessObject(string name)
        {
            Name = name;
            Description = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessObject"/> struct.
        /// </summary>
        /// <param name="name">The name of the process.</param>
        /// <param name="description">The description of the process.</param>
        public ProcessObject(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the process.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Returns a string that represents the current <see cref="ProcessObject"/> object.
        /// </summary>
        /// <returns>A formatted string containing the name and description of the process.</returns>
        public override string ToString()
        {
            return $"Process Name: {Name}, Description: {Description}";
        }
    }
}
