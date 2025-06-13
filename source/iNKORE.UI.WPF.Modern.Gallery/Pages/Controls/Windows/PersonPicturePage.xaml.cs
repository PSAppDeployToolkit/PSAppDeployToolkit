using System.Windows;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Controls;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class PersonPicturePage
    {
        public PersonPicturePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void NumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
        }

        public string Example1Xaml => $@"
<ui:PersonPicture x:Name=""personPicture""
    DisplayName=""{personPicture.DisplayName}"" Initials=""{personPicture.Initials}""
    ProfilePicture=""{(ProfileImageRadio.IsChecked == true ? "https://docs.microsoft.com/windows/uwp/contacts-and-calendar/images/shoulder-tap-static-payload.png" : "{x:Null}")}""/>
";

        public string Example2Xaml => $@"
<ui:PersonPicture x:Name=""personPicture2""
    DisplayName=""{personPicture2.DisplayName}"" Initials=""{personPicture2.Initials}"" IsGroup=""{personPicture2.IsGroup}""
    BadgeGlyph=""{personPicture2.BadgeGlyph}"" BadgeNumber=""{personPicture.BadgeNumber}""
    ProfilePicture=""{(ProfileImageCheck.IsChecked == true ? "https://docs.microsoft.com/windows/uwp/contacts-and-calendar/images/shoulder-tap-static-payload.png" : "{x:Null}")}""/>
";

        #endregion
    }
}
