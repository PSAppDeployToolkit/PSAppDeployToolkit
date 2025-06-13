using iNKORE.UI.WPF.Modern.Controls.Helpers;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WindowExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RadioButton_WindowStyle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton btn && btn.Content is string val)
            {
                try
                {
                    this.WindowStyle = (WindowStyle)Enum.Parse(typeof(WindowStyle), val);
                }
                catch
                {
                    try
                    {
                        this.WindowStyle = (WindowStyle)Enum.Parse(typeof(WindowStyle), val + "Window");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                }
            }
        }

        private void RadioButton_ResizeMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton btn && btn.Content is string val)
            {
                try
                {
                    this.ResizeMode = (ResizeMode)Enum.Parse(typeof(ResizeMode), val);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void CheckBox_UseModernWindowStyle_Click(object sender, RoutedEventArgs e)
        {
            WindowHelper.SetUseModernWindowStyle(this, CheckBox_UseModernWindowStyle.IsChecked ?? false);
        }

        private void RadioButton_SystemBackdropType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton btn && btn.Content is string val)
            {
                try
                {
                    var backdrop = (BackdropType)Enum.Parse(typeof(BackdropType), val);
                    WindowHelper.SetSystemBackdropType(this, backdrop);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

        }

        private void CheckBox_ApplyBackground_Click(object sender, RoutedEventArgs e)
        {
            WindowHelper.SetApplyBackground(this, CheckBox_ApplyBackground.IsChecked ?? false);
        }

        private void CheckBox_ApplyNoise_Click(object sender, RoutedEventArgs e)
        {
            WindowHelper.SetApplyNoise(this, CheckBox_ApplyBackground.IsChecked ?? false);
        }

        private void TitleBarButtonAvailabilitySelector_Loaded(object sender, RoutedEventArgs e)
        {
            if(sender is ComboBox comboBox)
            {
                comboBox.ItemsSource = new string[]
                {
                    "Auto",
                    "Collapsed",
                    "Disabled",
                    "Enabled"
                };

                comboBox.SelectedIndex = 0;
            }
        }

        private void TitleBarButtonAvailabilitySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(sender is ComboBox box && box.Tag is DependencyProperty prop && box.SelectedItem is string val)
            {
                try
                {
                    this.SetValue(prop, (TitleBarButtonAvailability)Enum.Parse(typeof(TitleBarButtonAvailability), val));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void RadioButton_CornerStyle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton btn && btn.Content is string val)
            {
                try
                {
                    WindowHelper.SetCornerStyle(this, (WindowCornerStyle)Enum.Parse(typeof(WindowCornerStyle), val));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

        }
    }
}