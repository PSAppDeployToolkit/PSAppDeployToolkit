using System;
using System.Collections;
using System.Runtime.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Configures the dialog accent color and whether it is used as an exact visible fill.
    /// </summary>
    [DataContract]
    public sealed record class FluentAccentColorOptions
    {
        /// <summary>
        /// Initializes accent color options. Calculated accent shades are used by default.
        /// </summary>
        /// <param name="color">The packed ARGB accent color, or <see langword="null"/> to use the system color.</param>
        /// <param name="exact">Whether supplied colors are used as exact visible fills.</param>
        public FluentAccentColorOptions(int? color, bool exact = false)
        {
            Color = color;
            Exact = exact;
        }

        /// <summary>
        /// The packed ARGB accent color, if supplied.
        /// </summary>
        [DataMember]
        public readonly int? Color;

        /// <summary>
        /// Whether supplied colors are used as exact visible fills.
        /// </summary>
        [DataMember]
        public readonly bool Exact;

        internal static FluentAccentColorOptions? Parse(object? value)
        {
            if (value is null)
            {
                return null;
            }
            if (value is FluentAccentColorOptions options)
            {
                return options;
            }
            if (value is int color)
            {
                return new(color);
            }
            if (value is not IDictionary dictionary)
            {
                throw new ArgumentException("FluentAccentColor must be a packed integer or a Color/Exact dictionary.", nameof(value));
            }

            int? configuredColor = null;
            bool exact = false;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not string key)
                {
                    throw new ArgumentException("FluentAccentColor dictionary keys must be Color or Exact.", nameof(value));
                }
                if (string.Equals(key, nameof(Color), StringComparison.OrdinalIgnoreCase))
                {
                    if (entry.Value is not (null or int))
                    {
                        throw new ArgumentException("FluentAccentColor.Color must be a packed integer or null.", nameof(value));
                    }
                    configuredColor = (int?)entry.Value;
                }
                else if (string.Equals(key, nameof(Exact), StringComparison.OrdinalIgnoreCase))
                {
                    if (entry.Value is not bool configuredExact)
                    {
                        throw new ArgumentException("FluentAccentColor.Exact must be Boolean.", nameof(value));
                    }
                    exact = configuredExact;
                }
                else
                {
                    throw new ArgumentException($"Unknown FluentAccentColor member '{key}'.", nameof(value));
                }
            }
            return new(configuredColor, exact);
        }
    }
}
