using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlyoutExample
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

        private void Button_ShowFlyout_Click(object sender, RoutedEventArgs e)
        {
            Flyout_Test.ShowAt(Button_ShowFlyout);
        }

        private void DropDownButton_3_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Right-click to show the flyout. You have just left-clicked!");
        }
    }
}