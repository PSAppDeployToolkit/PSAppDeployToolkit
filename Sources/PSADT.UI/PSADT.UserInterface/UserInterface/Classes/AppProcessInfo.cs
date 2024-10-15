using System.Windows.Media;

namespace PSADT.UserInterface.Utilities
{
    public class AppProcessInfo
    {
        public string? ProcessName { get; set; }
        public string? ProcessDescription { get; set; }
        public string? ProductName { get; set; }
        public string? PublisherName { get; set; }
        public ImageSource? Icon { get; set; }
    }
}