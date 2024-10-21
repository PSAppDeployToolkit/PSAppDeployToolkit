using System.Windows.Media;

namespace PSADT.UserInterface.Services
{
    public class AppProcessInfo(
        string processName,
        string? processDescription,
        string? productName,
        string? publisherName,
        ImageSource? icon,
        DateTime? lastUpdated = null) : IEquatable<AppProcessInfo>
    {
        public string ProcessName { get; } = processName;
        public string? ProcessDescription { get; } = processDescription;
        public string? ProductName { get; } = productName;
        public string? PublisherName { get; } = publisherName;
        public ImageSource? Icon { get; } = icon;
        public DateTime LastUpdated { get; } = lastUpdated ?? DateTime.Now;

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

        public bool Equals(AppProcessInfo? other)
        {
            if (other is null)
                return false;

            return string.Equals(this.ProcessName, other.ProcessName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as AppProcessInfo);
        }

        public override int GetHashCode()
        {
            return ProcessName.ToLowerInvariant().GetHashCode();
        }
    }
}
