using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// BorderPage.xaml 的交互逻辑
    /// </summary>
    public partial class BorderPage : Page
    {
        public BorderPage()
        {
            InitializeComponent();
        }

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Control1 != null) Control1.BorderThickness = new Thickness(e.NewValue);

            UpdateExampleCode();
        }

        private void BGRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && Control1 != null)
            {
                string colorName = rb.Content.ToString();
                switch (colorName)
                {
                    case "Yellow":
                        Control1.Background = new SolidColorBrush(Colors.Yellow);
                        break;
                    case "Green":
                        Control1.Background = new SolidColorBrush(Colors.Green);
                        break;
                    case "Blue":
                        Control1.Background = new SolidColorBrush(Colors.Blue);
                        break;
                    case "White":
                        Control1.Background = new SolidColorBrush(Colors.White);
                        break;
                }
            }

            UpdateExampleCode();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && Control1 != null)
            {
                string colorName = rb.Content.ToString();
                switch (colorName)
                {
                    case "Yellow":
                        Control1.BorderBrush = new SolidColorBrush(Colors.Gold);
                        break;
                    case "Green":
                        Control1.BorderBrush = new SolidColorBrush(Colors.DarkGreen);
                        break;
                    case "Blue":
                        Control1.BorderBrush = new SolidColorBrush(Colors.DarkBlue);
                        break;
                    case "White":
                        Control1.BorderBrush = new SolidColorBrush(Colors.White);
                        break;
                }
            }

            UpdateExampleCode();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ControlExampleSubstitution Substitution1 = new ControlExampleSubstitution
            {
                Key = "BorderThickness",
            };
            BindingOperations.SetBinding(Substitution1, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = Control1,
                Path = new PropertyPath("BorderThickness.Top"),
            });

            ControlExampleSubstitution Substitution2 = new ControlExampleSubstitution
            {
                Key = "BorderBrush",
            };
            BindingOperations.SetBinding(Substitution2, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = Control1,
                Path = new PropertyPath("BorderBrush"),
            });

            ControlExampleSubstitution Substitution3 = new ControlExampleSubstitution
            {
                Key = "Background",
            };
            BindingOperations.SetBinding(Substitution3, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = Control1,
                Path = new PropertyPath("Background"),
            });

            ObservableCollection<ControlExampleSubstitution> Substitutions = new ObservableCollection<ControlExampleSubstitution>() { Substitution1, Substitution2, Substitution3 };
            Example1.Substitutions = Substitutions;

            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<Border x:Name=""Control1"" HorizontalAlignment=""Left""
    VerticalAlignment=""Top"" BorderThickness=""{Control1.BorderThickness.Left.ToString()}""
    Background=""{(Control1.Background as SolidColorBrush)?.Color.ToHEX()}"" BorderBrush=""{(Control1.BorderBrush as SolidColorBrush)?.Color.ToHEX()}"">
    <TextBlock Margin=""8,5"" FontSize=""18""
        Foreground=""Black""
        Text=""Text inside a border"" />
</Border>
";

        #endregion
    }
}
