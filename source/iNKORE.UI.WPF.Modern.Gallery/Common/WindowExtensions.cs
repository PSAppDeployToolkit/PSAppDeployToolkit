using System.Windows;
using System.Windows.Interop;

namespace iNKORE.UI.WPF.Modern.Gallery.Helpers
{
    public static class WindowExtensions
    {
        public static void SetPlacement(this Window window, string placementXml)
        {
            WindowPlacement.SetPlacement(new WindowInteropHelper(window).Handle, placementXml);
        }

        public static string GetPlacement(this Window window)
        {
            return WindowPlacement.GetPlacement(new WindowInteropHelper(window).Handle);
        }
    }
}
