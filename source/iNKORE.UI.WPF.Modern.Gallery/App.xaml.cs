using iNKORE.UI.WPF.Modern.Gallery.DataModel;
using System.Reflection;
using System;
using System.Diagnostics;
using System.Windows;

namespace iNKORE.UI.WPF.Modern.Gallery
{
    public partial class App
    {
        public static bool IsMultiThreaded { get; } = false;

        public static TEnum GetEnum<TEnum>(string text) where TEnum : struct
        {
            if (!typeof(TEnum).GetTypeInfo().IsEnum)
            {
                throw new InvalidOperationException("Generic parameter 'TEnum' must be an enum.");
            }
            return (TEnum)Enum.Parse(typeof(TEnum), text);
        }

        public static Process BrowseWeb(string path)
        {
            try
            {
                return Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return null;
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString());
            e.Handled = true;
        }
    }
}
