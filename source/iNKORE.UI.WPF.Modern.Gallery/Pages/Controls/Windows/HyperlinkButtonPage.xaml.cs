using iNKORE.UI.WPF.Modern.Gallery.DataModel;
using SamplesCommon;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Navigation;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class HyperlinkButtonPage
    {
        public HyperlinkButtonPage()
        {
            InitializeComponent();
        }

        private async void GoToHyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationRootPage.RootFrame.Navigate(ItemPage.Create(await ControlInfoDataSource.Instance.GetItemAsync(await ControlInfoDataSource.Instance.GetRealmAsync("Windows"), "ToggleButton")));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ControlExampleSubstitution Substitution = new ControlExampleSubstitution
            {
                Key = "IsEnabled",
                Value = @"IsEnabled=""False"" "
            };
            BindingOperations.SetBinding(Substitution, ControlExampleSubstitution.IsEnabledProperty, new Binding
            {
                Source = DisableControl1,
                Path = new PropertyPath("IsChecked"),
            });
            ObservableCollection<ControlExampleSubstitution> Substitutions = new ObservableCollection<ControlExampleSubstitution>() { Substitution };
            Example1.Substitutions = Substitutions;

            UpdateExampleCode();
        }

        private void DisableControl1_Click(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
        }

        public string Example1Xaml => $@"
<ui:HyperlinkButton x:Name=""Control1""
    Content=""iNKORE Studios home page""
    IsEnabled=""{Control1.IsEnabled}""
    NavigateUri=""http://www.inkore.net"" />
";

        public string Example2Xaml => $@"
<ui:HyperlinkButton x:Name=""Control2""
    Click=""GoToHyperlinkButton_Click""
    RaiseHyperlinkClicks=""False""
    Content=""Go to ToggleButton"" />
";

        #endregion
    }
}
