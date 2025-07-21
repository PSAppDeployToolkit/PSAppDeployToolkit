using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Foundation;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.System;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows.Controls;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;
using iNKORE.UI.WPF.Modern.Gallery.Helpers;
using System.Reflection;
using iNKORE.UI.WPF.Modern.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Gallery.DataModel;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : Page
    {
        public string Version => ThemeManager.AssemblyVersion;

        public SettingsPage()
        {
            this.InitializeComponent();
            Loaded += OnSettingsPageLoaded;
            navigationLocation.Loaded += NavigationLocation_Loaded;

            gitCloneTextBox.Text = $"git clone {ThemeManager.Link_GithubRepo}";


            //if (ElementSoundPlayer.State == ElementSoundPlayerState.On)
            //    soundToggle.IsOn = true;
            //if (ElementSoundPlayer.SpatialAudioMode == ElementSpatialAudioMode.On)
            //    spatialSoundBox.IsChecked = true;

            // ScreenshotSettingsGrid.Visibility = Visibility.Collapsed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            NavigationRootPage.Current.NavigationView.Header = "Settings";
        }

        private async void OnFeedbackButtonClick(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("feedback-hub:"));
        }

        private void OnSettingsPageLoaded(object sender, RoutedEventArgs e)
        {
            var currentTheme = ThemeHelper.RootTheme.ToString();
            foreach (ComboBoxItem item in themeMode.Items)
            {
                if (item.Tag?.ToString() == currentTheme)
                {
                    themeMode.SelectedItem = item;
                    return;
                }
            }
            themeMode.SelectedIndex = 2;
        }

        private void themeMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (themeMode.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string tag)
            {
                ThemeHelper.RootTheme = App.GetEnum<ElementTheme>(tag);
            }
        }

        // private void OnThemeRadioButtonKeyDown(object sender, KeyEventArgs e)
        // {
        //     if (e.Key == Key.Up)
        //     {
        //         NavigationRootPage.GetForElement(this).PageHeader.Focus();
        //     }
        // }

        // private void soundToggle_Toggled(object sender, RoutedEventArgs e)
        // {
            
        // }

        // private void spatialSoundBox_Toggled(object sender, RoutedEventArgs e)
        // {
            
        // }

        private void toCloneRepoCard_Click(object sender, RoutedEventArgs e)
        {
            // pulls in exactly "git clone https://…"
            Clipboard.SetText(gitCloneTextBox.Text);
        }


        private void bugRequestCard_Click(object sender, RoutedEventArgs e)
        {
            var url = "https://github.com/iNKORE-NET/UI.WPF.Modern/issues/new/choose";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }

        //private void soundToggle_Toggled(object sender, RoutedEventArgs e)
        //{
        //    if (soundToggle.IsOn == true)
        //    {
        //        spatialSoundBox.IsEnabled = true;
        //        //ElementSoundPlayer.State = ElementSoundPlayerState.On;
        //    }
        //    else
        //    {
        //        spatialSoundBox.IsEnabled = false;
        //        spatialSoundBox.IsChecked = false;

        //        //ElementSoundPlayer.State = ElementSoundPlayerState.Off;
        //        //ElementSoundPlayer.SpatialAudioMode = ElementSpatialAudioMode.Off;
        //    }
        //}

        private void screenshotModeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            //UIHelper.IsScreenshotMode = screenshotModeToggle.IsOn;
        }

        //private void spatialSoundBox_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (soundToggle.IsOn == true)
        //    {
        //        //ElementSoundPlayer.SpatialAudioMode = ElementSpatialAudioMode.Off;
        //    }
        //}

        private void NavigationLocation_Loaded(object sender, RoutedEventArgs e)
        {
            var navView = NavigationRootPage.Current.NavigationView;

            navigationLocation.SelectedIndex =
                (navView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top) ? 1 : 0;
        }

        private void navigationLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var root = NavigationRootPage.Current;
            var navView = root.NavigationView;
            bool isLeft = navigationLocation.SelectedIndex == 0;

            navView.PaneDisplayMode = isLeft 
                ? NavigationViewPaneDisplayMode.Left 
                : NavigationViewPaneDisplayMode.Top;

            if (!isLeft)
            {
                double offset = root.AppTitleBar.ActualHeight + 49;
                navView.Margin = new Thickness(0, offset, 0, 0);
            }
            else
            {
                navView.Margin = new Thickness(0);
            }
        }

        private async void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            folderPicker.FileTypeFilter.Add(".png"); // meaningless, but you have to have something
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                //UIHelper.ScreenshotStorageFolder = folder;
                //screenshotFolderLink.Content = UIHelper.ScreenshotStorageFolder.Path;
            }
        }

        private async void screenshotFolderLink_Click(object sender, RoutedEventArgs e)
        {
            //await Launcher.LaunchFolderAsync(UIHelper.ScreenshotStorageFolder);
        }

        private void OnResetTeachingTipsButtonClick(object sender, RoutedEventArgs e)
        {
            //ProtocolActivationClipboardHelper.ShowCopyLinkTeachingTip = true;
        }

        private async void soundPageHyperlink_Click(object sender, RoutedEventArgs args)
        {
            this.Frame.Navigate(ItemPage.Create(await ControlInfoDataSource.Instance.GetItemAsync(await ControlInfoDataSource.Instance.GetRealmAsync("Windows"), "Sound")));
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
