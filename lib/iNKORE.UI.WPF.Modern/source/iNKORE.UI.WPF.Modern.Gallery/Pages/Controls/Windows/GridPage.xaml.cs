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
    /// GridPage.xaml 的交互逻辑
    /// </summary>
    public partial class GridPage : Page
    {
        public GridPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ControlExampleSubstitution Substitution1 = new ControlExampleSubstitution
            {
                Key = "Column",
            };
            BindingOperations.SetBinding(Substitution1, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = ColumnSlider,
                Path = new PropertyPath("Value"),
            });
            ControlExampleSubstitution Substitution2 = new ControlExampleSubstitution
            {
                Key = "Row",
            };
            BindingOperations.SetBinding(Substitution2, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = RowSlider,
                Path = new PropertyPath("Value"),
            });
            Example1.Substitutions = new ObservableCollection<ControlExampleSubstitution> { Substitution1, Substitution2 };

            UpdateExampleCode();
        }

        private void ColumnSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsLoaded)
                UpdateExampleCode();
        }

        private void RowSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsLoaded)
                UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<Grid x:Name=""Control1""
    Width=""240"" Height=""160""
    Background=""Gray"">
    <Grid.Resources>
        <Style TargetType=""Rectangle"">
            <Setter Property=""Height"" Value=""40"" />
            <Setter Property=""Width"" Value=""40"" />
        </Style>
    </Grid.Resources>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width=""50"" />
        <ColumnDefinition Width=""Auto"" />
        <ColumnDefinition />
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
        <RowDefinition Height=""50"" />
        <RowDefinition Height=""Auto"" />
        <RowDefinition />
    </Grid.RowDefinitions>
    <Rectangle
        x:Name=""Rectangle1""
        Grid.Row=""{Grid.GetRow(Rectangle1)}""
        Grid.Column=""{Grid.GetColumn(Rectangle1)}""
        Width=""50""
        Height=""50""
        Fill=""Red"" />
    <Rectangle Grid.Row=""1"" Fill=""Blue"" />
    <Rectangle Grid.Column=""1"" Fill=""Green"" />
    <Rectangle
        Grid.Row=""1""
        Grid.Column=""1""
        Fill=""Yellow"" />
</Grid>
";

        #endregion
    }
}
