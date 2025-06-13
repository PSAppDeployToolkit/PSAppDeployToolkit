using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class ComboBoxPage : Page
    {
        public List<Tuple<string, FontFamily>> Fonts { get; } = new List<Tuple<string, FontFamily>>()
            {
                new Tuple<string, FontFamily>("Arial", new FontFamily("Arial")),
                new Tuple<string, FontFamily>("Comic Sans MS", new FontFamily("Comic Sans MS")),
                new Tuple<string, FontFamily>("Courier New", new FontFamily("Courier New")),
                new Tuple<string, FontFamily>("Segoe UI", new FontFamily("Segoe UI")),
                new Tuple<string, FontFamily>("Times New Roman", new FontFamily("Times New Roman"))
            };

        public List<double> FontSizes { get; } = new List<double>()
            {
                8,
                9,
                10,
                11,
                12,
                14,
                16,
                18,
                20,
                24,
                28,
                36,
                48,
                72
            };

        public ComboBoxPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string colorName = e.AddedItems[0].ToString();
            Color color = ((SolidColorBrush)Control1Output.Fill).Color;
            switch (colorName)
            {
                case "Yellow":
                    color = Colors.Yellow;
                    break;
                case "Green":
                    color = Colors.Green;
                    break;
                case "Blue":
                    color = Colors.Blue;
                    break;
                case "Red":
                    color = Colors.Red;
                    break;
            }
            Control1Output.Fill = new SolidColorBrush(color);
        }

        private void Combo2_Loaded(object sender, RoutedEventArgs e)
        {
            Combo2.SelectedIndex = 2;
        }

        private void Combo3_Loaded(object sender, RoutedEventArgs e)
        {
            Combo3.SelectedIndex = 2;
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example2.CSharp = Example2CS;
            Example3.Xaml = Example3Xaml;
        }

        public string Example1Xaml => $@"
<ComboBox x:Name=""Combo1""
    ui:ControlHelper.Header=""Colors""
    ui:ControlHelper.PlaceholderText=""Pick a color""
    SelectionChanged=""ColorComboBox_SelectionChanged"">
    <sys:String>Blue</sys:String>
    <sys:String>Green</sys:String>
    <sys:String>Red</sys:String>
    <sys:String>Yellow</sys:String>
</ComboBox>
";

        public string Example2Xaml => $@"
<ComboBox x:Name=""Combo2""
    ui:ControlHelper.Header=""Font""
    DataContext=""{{Binding RelativeSource={{RelativeSource Mode=FindAncestor, AncestorType={{x:Type ui:Page}}}}}}""
    DisplayMemberPath=""Item1""
    ItemsSource=""{{Binding Fonts}}""
    Loaded=""Combo2_Loaded""
    SelectedValuePath=""Item2"" />
";

        public string Example2CS => $@"
public List<Tuple<string, FontFamily>> Fonts {{ get; }} = new List<Tuple<string, FontFamily>>()
{{
    new Tuple<string, FontFamily>(""Arial"", new FontFamily(""Arial"")),
    new Tuple<string, FontFamily>(""Comic Sans MS"", new FontFamily(""Comic Sans MS"")),
    new Tuple<string, FontFamily>(""Courier New"", new FontFamily(""Courier New"")),
    new Tuple<string, FontFamily>(""Segoe UI"", new FontFamily(""Segoe UI"")),
    new Tuple<string, FontFamily>(""Times New Roman"", new FontFamily(""Times New Roman""))
}};
";

        public string Example3Xaml => $@"
<ComboBox x:Name=""Combo3""
    ui:ControlHelper.Header=""Font Size""
    DataContext=""{{Binding RelativeSource={{RelativeSource Mode=FindAncestor, AncestorType={{x:Type ui:Page}}}}}}""
    IsEditable=""True""
    ItemsSource=""{{Binding FontSizes}}""
    Loaded=""Combo3_Loaded"" />
";

        #endregion

    }
}
