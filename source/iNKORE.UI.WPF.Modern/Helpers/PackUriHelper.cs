using System;

namespace iNKORE.UI.WPF.Modern.Helpers
{
    internal static class PackUriHelper
    {
        public static Uri GetAbsoluteUri(string path)
        {
            return new Uri($"pack://application:,,,/iNKORE.UI.WPF.Modern;component/{path}");
        }
    }
}
