using System.Windows;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class PivotPage
    {
        public PivotPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }


        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<TabControl ui:PivotHelper.Title=""EMAIL""
    Style=""{{StaticResource TabControlPivotStyle}}"">
    <TabItem Header=""All"">
        <TextBlock Text=""all emails go here."" />
    </TabItem>
    <TabItem Header=""Unread"">
        <TextBlock Text=""unread emails go here."" />
    </TabItem>
    <TabItem Header=""Flagged"">
        <TextBlock Text=""flagged emails go here."" />
    </TabItem>
    <TabItem Header=""Urgent"">
        <TextBlock Text=""urgent emails go here."" />
    </TabItem>
</TabControl>
";

        #endregion
    }
}
