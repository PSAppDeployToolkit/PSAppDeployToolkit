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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;
using WIN = Windows;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// CalendarViewPage.xaml 的交互逻辑
    /// </summary>
    public partial class CalendarViewPage : Page
    {
        public CalendarViewPage()
        {
            InitializeComponent();
            var langs = new LanguageList();
            CalendarLanguages.ItemsSource = langs.Languages;
        }

        private void SelectionMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Enum.TryParse((sender as ComboBox).SelectedItem.ToString(), out CalendarSelectionMode selectionMode))
            {
                Control1.SelectionMode = selectionMode;
            }

            UpdateExampleCode();
        }

        private void CalendarLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedLang = CalendarLanguages.SelectedValue.ToString();
            if (WIN.Globalization.Language.IsWellFormed(selectedLang))
            {
                Control1.Language = XmlLanguage.GetLanguage(selectedLang);
            }

            UpdateExampleCode();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ControlExampleSubstitution Substitution1 = new ControlExampleSubstitution
            {
                Key = "SelectionMode",
            };
            BindingOperations.SetBinding(Substitution1, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = Control1,
                Path = new PropertyPath("SelectionMode"),
            });

            ControlExampleSubstitution Substitution2 = new ControlExampleSubstitution
            {
                Key = "Language",
            };
            BindingOperations.SetBinding(Substitution2, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = Control1,
                Path = new PropertyPath("Language"),
            });

            ObservableCollection<ControlExampleSubstitution> Substitutions = new ObservableCollection<ControlExampleSubstitution>() { Substitution1, Substitution2 };
            ExampleAccessories.Substitutions = Substitutions;

            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            ExampleAccessories.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<Calendar x:Name=""Control1"" Language=""{Control1.Language.ToString()}""
    SelectionMode=""{Control1.SelectionMode.ToString()}"" />
";

        #endregion

    }
}
