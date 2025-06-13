using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using MessageBoxEx = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
        }

        private void ComboBox_Selected(object sender, RoutedEventArgs e)
        {
            selec();
        }

        private void selec()
        {
            //ContentDialog dialog = new ContentDialog();
            //dialog.Content = "AAAAAAAAAA";
            //dialog.PrimaryButtonText = "OK";
            //dialog.SecondaryButtonText = "Cancel";
            //await dialog.ShowAsync();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selec();

        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            //if (ThemeManager.Current.ApplicationTheme != ApplicationTheme.Dark)
            //    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            //else
            //{
            //    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            //}
            WindowChrome.GetWindowChrome(this).GlassFrameThickness = new Thickness(0, 2, 0, 0);
            WindowChrome.GetWindowChrome(this).GlassFrameThickness = new Thickness(0, 1, 0, 0);

            //WindowHelper.ApplyDarkMode(this);
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            //naview.SelectedItem = sgsac;
        }

        private async void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            //AppBarToggleButton1.IsChecked = false;
            //AppBarToggleButton1.IsEnabled = false;
            //MicaHelper.RemoveTitleBar(this);
            //MicaHelper.Apply(this, BackdropType.Mica, false);
            await InputBox.ShowAsync("promptssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssok", "promptssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssok", "test");
        }

        private void Button_MessageBox_Click(object sender, RoutedEventArgs e)
        {
            string title = "Some title";
            string message = "This is a looooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong test text!";

            System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            System.Windows.MessageBox.Show("adawdawda", title, MessageBoxButton.YesNoCancel, MessageBoxImage.Error);


            MessageBoxEx.DefaultBackdropType = BackdropType.None;

            MessageBoxEx.Show("This is a test text!", "Some title", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            MessageBoxEx.Show("aaa");

            MessageBoxEx.DefaultBackdropType = BackdropType.Acrylic;

            MessageBoxEx.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            MessageBoxEx.Show("redadwada", null, MessageBoxButton.OK, FluentSystemIcons.HomeAdd_20_Regular);
            MessageBoxEx.Show("redadwada", null, MessageBoxButton.OK, SegoeFluentIcons.Airplane);

            MessageBoxEx.DefaultBackdropType = BackdropType.Mica;


            MessageBoxEx.Show("redadwada", null, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
            MessageBoxEx.ShowAsync(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question).GetAwaiter().GetResult();

            MessageBoxEx.DefaultBackdropType = BackdropType.Tabbed;


            MessageBoxEx.ShowAsync(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Hand, MessageBoxResult.Cancel).GetAwaiter().GetResult();
            MessageBoxEx.EnableLocalization = false;
            MessageBoxEx.ShowAsync("Press Alt and you should see underscores!", null, MessageBoxButton.YesNoCancel, MessageBoxImage.Hand).GetAwaiter().GetResult();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //WindowChrome.SetWindowChrome(this, new WindowChrome()
            //{
            //    GlassFrameThickness = new Thickness(0, 1, 0, 0),
            //    UseAeroCaptionButtons = false,
            //    CornerRadius = new CornerRadius(0),
            //    ResizeBorderThickness = new Thickness(4),
            //    NonClientFrameEdges = NonClientFrameEdges.None,
            //    CaptionHeight = 36d,
            //});
            // bool b = AcrylicHelper.Apply(this, true);
            //System.Windows.MessageBox.Show(b.ToString());

        }

        private void Button_ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            if (ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark)
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            }
            else
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            }
        }

        private void SettingsCard_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("clicked");
        }
    }
}
