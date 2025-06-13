using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class GridSplitterPage
    {
        public GridSplitterPage()
        {
            InitializeComponent();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition />
        <ColumnDefinition Width=""Auto"" />
        <ColumnDefinition />
    </Grid.ColumnDefinitions>
    <TextBlock
        Grid.Column=""0""
        HorizontalAlignment=""Center""
        VerticalAlignment=""Center""
        Style=""{{StaticResource HeaderTextBlockStyle}}""
        Text=""Column 1"" />
    <GridSplitter
        x:Name=""ColumnGridSplitter""
        Grid.Column=""1""
        Width=""5"" 
        ShowsPreview=""{ColumnGridSplitter.ShowsPreview}""/>
    <TextBlock
        Grid.Column=""2""
        HorizontalAlignment=""Center""
        VerticalAlignment=""Center""
        Style=""{{StaticResource HeaderTextBlockStyle}}""
        Text=""Column 2"" />
</Grid>
";

        #endregion

        private void CheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateExampleCode();
        }
    }
}
