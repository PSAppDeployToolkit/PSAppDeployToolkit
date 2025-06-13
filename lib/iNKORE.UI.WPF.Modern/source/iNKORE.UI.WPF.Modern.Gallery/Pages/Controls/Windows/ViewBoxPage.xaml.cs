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
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// ViewBoxPage.xaml 的交互逻辑
    /// </summary>
    public partial class ViewBoxPage : Page
    {
        public ViewBoxPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateExampleCode();
        }

        private void StretchDirectionButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && Control1 != null)
            {
                string direction = rb.Tag?.ToString();
                switch (direction)
                {
                    case "UpOnly":
                        Control1.StretchDirection = StretchDirection.UpOnly;
                        break;
                    case "DownOnly":
                        Control1.StretchDirection = StretchDirection.DownOnly;
                        break;
                    case "Both":
                        Control1.StretchDirection = StretchDirection.Both;
                        break;
                }
            }

            UpdateExampleCode();
        }

        private void StretchButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && Control1 != null)
            {
                string stretch = rb.Tag?.ToString();
                switch (stretch)
                {
                    case "None":
                        Control1.Stretch = Stretch.None;
                        break;
                    case "Fill":
                        Control1.Stretch = Stretch.Fill;
                        break;
                    case "Uniform":
                        Control1.Stretch = Stretch.Uniform;
                        break;
                    case "UniformToFill":
                        Control1.Stretch = Stretch.UniformToFill;
                        break;
                }
            }

            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsInitialized) return;

            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<Viewbox x:Name=""Control1"" StretchDirection=""{Control1.StretchDirection}""
    Width=""{SizeSlider.Value}"" Height=""{SizeSlider.Value}"" Stretch=""{Control1.Stretch}"">
    <Border BorderBrush=""Gray"" BorderThickness=""15"">
        <StackPanel Background=""DarkGray"">
            <StackPanel Orientation=""Horizontal"">
                <Rectangle Fill=""Blue""
                    Width=""40"" Height=""10""/>
                <Rectangle Fill=""Green""
                    Width=""40"" Height=""10""/>
                <Rectangle Fill=""Red""
                    Width=""40"" Height=""10""/>
                <Rectangle Fill=""Yellow""
                    Width=""40"" Height=""10""/>
            </StackPanel>
            <Image Source=""/Assets/Slices.png"" />
            <TextBlock HorizontalAlignment=""Center"" Text=""This is text."" />
        </StackPanel>
    </Border>
</Viewbox>
";

        #endregion

    }
}
