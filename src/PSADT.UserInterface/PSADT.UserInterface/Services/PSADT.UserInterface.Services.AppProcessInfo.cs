using System;
using System.Windows.Media;

namespace PSADT.UserInterface.Services
{
    /// <summary>
    /// Information about a process
    /// </summary>
    /// <param name="processName"></param>
    /// <param name="processDescription"></param>
    /// <param name="productName"></param>
    /// <param name="publisherName"></param>
    /// <param name="icon"></param>
    /// <param name="lastUpdated"></param>
    public sealed class AppProcessInfo(
        string processName,
        string? processDescription,
        string? productName,
        string? publisherName,
        ImageSource? icon,
        DateTime? lastUpdated = null) : IEquatable<AppProcessInfo>
    {
        /// <summary>
        /// Name of the process
        /// </summary>
        public readonly string ProcessName = processName;

        /// <summary>
        /// Description of the process
        /// </summary>
        public readonly string? ProcessDescription = processDescription;

        /// <summary>
        /// The product name of the process
        /// </summary>
        public readonly string? ProductName = productName;

        /// <summary>
        /// The publisher name of the process
        /// </summary>
        public readonly string? PublisherName = publisherName;

        /// <summary>
        /// The icon of the process
        /// </summary>
        public readonly ImageSource? Icon = icon;

        /// <summary>
        /// The last time the process information was updated
        /// </summary>
        public readonly DateTime LastUpdated = lastUpdated ?? DateTime.Now;

        /// <summary>
        /// Check if two AppProcessInfo objects are equal
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(AppProcessInfo? other)
        {
            // Input is null.
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            // Both have the same reference.
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Check if the properties are equal.
            return ProcessName == other.ProcessName &&
                   ProcessDescription == other.ProcessDescription &&
                   ProductName == other.ProductName &&
                   PublisherName == other.PublisherName &&
                   Icon == other.Icon &&
                   LastUpdated == other.LastUpdated;
        }

        /// <summary>
        /// Check if two AppProcessInfo objects are equal
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object? obj)
        {
            return Equals(obj as AppProcessInfo);
        }

        /// <summary>
        /// Check if two AppProcessInfo objects are not equal
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator==(AppProcessInfo? left, AppProcessInfo? right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }

        /// <summary>
        /// Check if two AppProcessInfo objects are not equal
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator!=(AppProcessInfo? left, AppProcessInfo? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Gets the immutable hash code for the AppProcessInfo object
        /// </summary>
        public override int GetHashCode()
        {
            return (this.ProcessName,
                    this.ProcessDescription,
                    this.ProductName,
                    this.PublisherName,
                    this.Icon,
                    this.LastUpdated).GetHashCode();
        }
    }
}
