namespace PSADT.CommandLine
{
    /// <summary>
    /// Represents a mapping between a command-line option and its corresponding long and short names.
    /// </summary>
    public class OptionMapping
    {
        /// <summary>
        /// The long name of the option.
        /// </summary>
        public string LongName { get; }

        /// <summary>
        /// The short name of the option.
        /// </summary>
        public string ShortName { get; }

        /// <summary>
        /// The corresponding strongly-typed option that this command-line argument represents (optional).
        /// </summary>
        public object? CorrespondingTypedOption { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionMapping"/> class.
        /// </summary>
        /// <param name="longName">The long name of the option.</param>
        /// <param name="shortName">The short name of the option.</param>
        /// <param name="correspondingTypedOption">The corresponding strongly-typed option that this command-line argument represents (optional).</param>
        public OptionMapping(string longName, string shortName, object? correspondingTypedOption = null)
        {
            LongName = longName.ToLowerInvariant();
            ShortName = shortName.ToLowerInvariant();
            CorrespondingTypedOption = correspondingTypedOption;
        }
    }
}
