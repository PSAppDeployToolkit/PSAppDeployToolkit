using iNKORE.UI.WPF.Modern.Gallery.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class DataGridPage
    {
        private readonly Stopwatch _stopwatch;
        private DataGridDataSource _viewModel = new DataGridDataSource();
        private CollectionViewSource _cvs;

        public DataGridPage()
        {
            _stopwatch = Stopwatch.StartNew();
            Loaded += OnLoaded;

            InitializeComponent();

            _cvs = (CollectionViewSource)Resources["cvs"];

            //GroupingToggle.IsChecked = true;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            DataContext = await _viewModel.GetDataAsync();

            var comboBoxColumn = dataGrid.Columns.FirstOrDefault(x => x.Header.Equals("Mountain")) as DataGridComboBoxColumn;
            if (comboBoxColumn != null)
            {
                comboBoxColumn.ItemsSource = await _viewModel.GetMountains();
            }

            _ = Dispatcher.BeginInvoke(() =>
              {
                  _stopwatch.Stop();
                  LoadTimeTextBlock.Text = _stopwatch.ElapsedMilliseconds + " ms";
              }, DispatcherPriority.ApplicationIdle);

            UpdateExampleCode();
        }

        private void ToggleTheme(object sender, RoutedEventArgs e)
        {
            this.ToggleTheme();
        }

        private void GroupingToggle_Checked(object sender, RoutedEventArgs e)
        {
            _cvs.GroupDescriptions.Add(new PropertyGroupDescription(nameof(DataGridDataItem.Range)));
            UpdateExampleCode();
        }

        private void GroupingToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _cvs.GroupDescriptions.Clear();
            UpdateExampleCode();
        }

        private void LoadTimeTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            LoadTimeTextBlock.Visibility = Visibility.Collapsed;
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
        }

        // There're too many options, and im too lazy to do them all.
        // If you're interested in this, you can do it yourself, PRs welcomed.
        public string Example1Xaml => $@"
<DataGrid x:Name=""dataGrid""
    AutoGenerateColumns=""False""
    GridLinesVisibility=""Horizontal""
    HeadersVisibility=""Column""
    ItemsSource=""{{Binding Source={{StaticResource cvs}}}}""
    RowDetailsTemplate=""{{StaticResource RowDetailsTemplate}}""
    RowDetailsVisibilityMode=""Collapsed""
    VirtualizingPanel.IsVirtualizingWhenGrouping=""True""
    VirtualizingPanel.VirtualizationMode=""Recycling"">
{(Example1_LayoutDensitySelector.IsCompact ? @"
    <FrameworkElement.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source=""/iNKORE.UI.WPF.Modern;component/Themes/DensityStyles/Compact.xaml"" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </FrameworkElement.Resources>
" : "")}
    <DataGrid.Columns>
        <DataGridTextColumn
            Width=""105""
            Binding=""{{Binding Rank}}""
            Header=""Rank"" />
        <DataGridComboBoxColumn
            Width=""200""
            Header=""Mountain""
            SelectedItemBinding=""{{Binding Mountain}}"" />
        <DataGridTextColumn
            Width=""135""
            Binding=""{{Binding Height_m}}""
            Header=""Height (m)"" />
        <DataGridTextColumn
            Width=""260""
            Binding=""{{Binding Range}}""
            Header=""Range"" />
        <DataGridTextColumn
            Width=""180""
            Binding=""{{Binding Parent_mountain}}""
            Header=""Parent Mountain"" />
        <DataGridCheckBoxColumn
            Width=""145""
            Binding=""{{Binding CheckBoxColumnValue}}""
            Header=""CheckBox Column""
            Visibility=""{CheckBoxColumnVisibilityToggle.IsChecked.ToString()}"" />
        <DataGridHyperlinkColumn
            Width=""220""
            Binding=""{{Binding HyperlinkColumnValue}}""
            Header=""Hyperlink Column""
            Visibility=""{HyperlinkColumnVisibilityToggle.IsChecked.ToString()}"" />
    </DataGrid.Columns>
    <DataGrid.GroupStyle>
        <GroupStyle ContainerStyle=""{{StaticResource DataGridRowGroupContainerStyle}}"" HeaderTemplate=""{{StaticResource RowGroupHeaderTemplate}}"" />
    </DataGrid.GroupStyle>
</DataGrid>
";

        #endregion

    }
}
