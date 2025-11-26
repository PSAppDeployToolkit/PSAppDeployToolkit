using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows; // for TestContentDialog
using SamplesCommon;
using System;
using System.Windows;

namespace iNKORE.UI.WPF.Modern.Gallery.Samples
{
    public partial class SampleContentDialogWindow : Window
    {
        public SampleContentDialogWindow()
        {
            InitializeComponent();
        }

        private void ShowDialogInThisWindow(object sender, RoutedEventArgs e)
        {
            _ = new TestContentDialog().ShowAsync();
        }

        private void ShowDialogInMainWindow(object sender, RoutedEventArgs e)
        {
            DispatcherHelper.RunOnMainThread(async () =>
            {
                try
                {
                    await new TestContentDialog() { Owner = Application.Current.MainWindow }.ShowAsync();
                }
                catch (Exception ex)
                {
                    this.RunOnUIThread(() =>
                    {
                        _ = new ContentDialog
                        {
                            Owner = this,
                            Content = ex.Message,
                            CloseButtonText = "Close"
                        }.ShowAsync();
                    });
                }
            });
        }
    }
}
