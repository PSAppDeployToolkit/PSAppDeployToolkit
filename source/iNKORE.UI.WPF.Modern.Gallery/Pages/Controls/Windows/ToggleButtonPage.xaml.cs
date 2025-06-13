using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Gallery.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// ToggleButtonPage.xaml 的交互逻辑
    /// </summary>
    public partial class ToggleButtonPage : Page
    {
        private TextBlock Control1Output;
        private ToggleButton Toggle1;

        public ToggleButtonPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        private void CheckBox_IsEnabled_Click(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }


        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            Control1Output.Text = "On";
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            Control1Output.Text = "Off";
        }

        private void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock b)
            {
                string name = b.Tag.ToString();

                switch (name)
                {
                    case "Control1Output":
                        Control1Output = b;
                        b.Text = (bool)Toggle1?.IsChecked ? "On" : "Off";
                        break;
                }
            }
        }

        private void Toggle_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton b)
            {
                string name = b.Tag.ToString();

                switch (name)
                {
                    case "Toggle1":
                        Toggle1 = b;
                        break;
                }
            }
        }

        private void CheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox b)
            {
                string name = b.Tag.ToString();

                switch (name)
                {
                    case "DisableToggle1":
                        Toggle1.SetBinding(IsEnabledProperty, new Binding
                        {
                            Source = b,
                            Path = new PropertyPath("IsChecked"),
                            Converter = new BoolNegationConverter()
                        });

                        ControlExampleSubstitution Substitution = new ControlExampleSubstitution
                        {
                            Key = "IsEnabled",
                            Value = @"IsEnabled=""False"" "
                        };
                        BindingOperations.SetBinding(Substitution, ControlExampleSubstitution.IsEnabledProperty, new Binding
                        {
                            Source = b,
                            Path = new PropertyPath("IsChecked"),
                        });
                        ObservableCollection<ControlExampleSubstitution> Substitutions = new ObservableCollection<ControlExampleSubstitution>() { Substitution };
                        Example1.Substitutions = Substitutions;

                        break;
                }
            }
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsInitialized) return;

            Example1.Xaml = Example1Xaml;
            Example1.CSharp = Example1CS;
        }

        public string Example1Xaml => $@"
<ToggleButton IsEnabled=""{CheckBox_IsEnabled.IsChecked}"" Content=""ToggleButton""
    Checked=""ToggleButton_Checked""
    Unchecked=""ToggleButton_Unchecked"" />
";

        public string Example1CS => $@"
private void ToggleButton_Checked(object sender, RoutedEventArgs e)
{{
    Control1Output.Text = ""On"";
}}

private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
{{
    Control1Output.Text = ""Off"";
}}
";

        #endregion
    }
}
