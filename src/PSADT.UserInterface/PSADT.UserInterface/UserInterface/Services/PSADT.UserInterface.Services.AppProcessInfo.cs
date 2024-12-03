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
    public class AppProcessInfo(
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
        public string ProcessName { get; } = processName;

        /// <summary>
        /// Description of the process
        /// </summary>
        public string? ProcessDescription { get; } = processDescription;

        /// <summary>
        /// The product name of the process
        /// </summary>
        public string? ProductName { get; } = productName;

        /// <summary>
        /// The publisher name of the process
        /// </summary>
        public string? PublisherName { get; } = publisherName;

        /// <summary>
        /// The icon of the process
        /// </summary>
        public ImageSource? Icon { get; } = icon;

        /// <summary>
        /// The last time the process information was updated
        /// </summary>
        public DateTime LastUpdated { get; } = lastUpdated ?? DateTime.Now;

        /// <summary>
        /// Update the description of the process
        /// </summary>
        /// <param name="newDescription"></param>
        /// <returns></returns>
        public AppProcessInfo WithUpdatedDescription(string? newDescription)
        {
            return new AppProcessInfo(
                ProcessName,
                newDescription ?? ProcessDescription,
                ProductName,
                PublisherName,
                Icon,
                LastUpdated
            );
        }

        /// <summary>
        /// Update the LastUpdated timestamp of the process
        /// </summary>
        public AppProcessInfo WithUpdatedTimestamp()
        {
            return new AppProcessInfo(
                ProcessName,
                ProcessDescription,
                ProductName,
                PublisherName,
                Icon,
                DateTime.Now
            );
        }

        /// <summary>
        /// Check if two AppProcessInfo objects are equal
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(AppProcessInfo? other)
        {
            if (other is null)
                return false;

            return string.Equals(this.ProcessName, other.ProcessName, StringComparison.OrdinalIgnoreCase);
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
        /// Gets the immutable hash code for the AppProcessInfo object
        /// </summary>
        public override int GetHashCode()
        {
            return ProcessName.ToLowerInvariant().GetHashCode();
        }
    }
}
