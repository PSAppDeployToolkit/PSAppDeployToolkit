using iNKORE.UI.WPF.Modern.Controls;
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

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// Interaction logic for ProgressRingPage.xaml
    /// </summary>
    public partial class ProgressRingPage
    {
        public ProgressRingPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void ProgressToggle_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void ProgressValue_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            UpdateExampleCode();
        }

        private void BackgroundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProgressRing targetRing = null;

            if (sender == BackgroundComboBox1)
                targetRing = ProgressRing1;
            else if (sender == BackgroundComboBox2)
                targetRing = ProgressRing2;

            if (targetRing == null)
                return;

            var combo = sender as ComboBox;
            var selectedItem = combo.SelectedItem as ComboBoxItem;
            if (selectedItem == null)
            {
                targetRing.ClearValue(Control.BackgroundProperty);
                UpdateExampleCode();
                return;
            }

            string colorName = selectedItem.Content as string;
            switch (colorName)
            {
                case "Transparent":
                    targetRing.Background = Brushes.Transparent;
                    break;
                case "LightGray":
                    targetRing.Background = Brushes.LightGray;
                    break;
                default:
                    targetRing.ClearValue(Control.BackgroundProperty);
                    break;
            }
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
        }

        public string Example1Xaml
        {
            get
            {
                string background = "Transparent";
                if (BackgroundComboBox1 != null)
                {
                    var selectedItem = BackgroundComboBox1.SelectedItem as ComboBoxItem;
                    if (selectedItem != null)
                    {
                        background = selectedItem.Content as string ?? "Transparent";
                    }
                }
                return $@"
<ui:ProgressRing IsActive=""{ProgressToggle.IsOn}"" Background=""{background}"" />
";
            }
        }

        public string Example2Xaml
        {
            get
            {
                string background = "Transparent";
                if (BackgroundComboBox1 != null)
                {
                    var selectedItem = BackgroundComboBox2.SelectedItem as ComboBoxItem;
                    if (selectedItem != null)
                    {
                        background = selectedItem.Content as string ?? "Transparent";
                    }
                }
                return $@"
<ui:ProgressRing Width=""60"" Height=""60"" Value=""{ProgressValue.Value}"" 
    IsIndeterminate=""False"" Background=""{background}"" />
        ";
            }
        }

        #endregion

    }
}
