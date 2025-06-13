using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using System.Windows;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class FlyoutPage : Page
    {
        public FlyoutPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        private void DeleteConfirmation_Click(object sender, RoutedEventArgs e)
        {
            Flyout f = FlyoutService.GetFlyout(Control1) as Flyout;
            if (f != null)
            {
                f.Hide();
            }
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<Button x:Name=""Control1""
    Content=""Empty cart"">
    <ui:FlyoutService.Flyout>
        <ui:Flyout x:Name=""Flyout1""
            Placement=""{Flyout1.Placement.ToString()}"">
            <StackPanel>
                <TextBlock
                    Margin=""0,0,0,12""
                    Style=""{{DynamicResource BaseTextBlockStyle}}""
                    Text=""All items will be removed. Do you want to continue?"" />
                <Button Click=""DeleteConfirmation_Click"" Content=""Yes, empty my cart"" />
            </StackPanel>
        </ui:Flyout>
    </ui:FlyoutService.Flyout>
</Button>
";

        #endregion

        private void RadioButtons_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateExampleCode();
        }
    }
}
