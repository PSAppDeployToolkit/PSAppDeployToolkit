using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
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

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// Interaction logic for DatePickerPage.xaml
    /// </summary>
    public partial class DatePickerPage
    {
        public DatePickerPage()
        {
            InitializeComponent();

            UpdateExampleCode();
        }

        private void BlackoutDatesInPast(object sender, RoutedEventArgs e)
        {
            datePicker.BlackoutDates.AddDatesInPast();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<DatePicker x:Name=""datePicker"" IsEnabled=""{datePicker.IsEnabled}""
    ui:ControlHelper.Header=""{ControlHelper.GetHeader(datePicker)}""
    ui:ControlHelper.PlaceholderText=""{ControlHelper.GetPlaceholderText(datePicker)}""
    ui:ControlHelper.Description=""{ControlHelper.GetDescription(datePicker)}""
    IsTodayHighlighted=""{datePicker.IsTodayHighlighted}"" IsDropDownOpen=""{datePicker.IsDropDownOpen}"" />
";

        #endregion
    }
}
