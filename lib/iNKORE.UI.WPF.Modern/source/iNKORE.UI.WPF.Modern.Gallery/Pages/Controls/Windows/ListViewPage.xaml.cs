using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class ListViewPage
    {
        public ListViewPage()
        {
            InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
            DataContext = await Contact.GetContactsAsync();
        }

        private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
        }

        public string Example1Xaml => $@"
<ListView x:Name=""listView1"" IsEnabled=""{listView1.IsEnabled}""
    ItemsSource=""{{Binding}}"" SelectionMode=""{listView1.SelectionMode}""
    VirtualizingPanel.VirtualizationMode=""Recycling"" />
";

        public string Example2Xaml => $@"
<ListView x:Name=""listView2"" IsEnabled=""{listView2.IsEnabled}""
    ItemTemplate=""{{StaticResource ContactListViewTemplate}}""
    ItemsSource=""{{Binding Source={{StaticResource ContactsCVS}}}}""
    VirtualizingPanel.IsVirtualizingWhenGrouping=""True""
    VirtualizingPanel.VirtualizationMode=""Recycling"">
    <ListView.GroupStyle>
        <GroupStyle>
            <GroupStyle.HeaderTemplate>
                <DataTemplate>
                    <TextBlock Style=""{{DynamicResource {{x:Static ui:ThemeKeys.TitleTextBlockStyleKey}}}}"" Text=""{{Binding Name, Mode=OneTime}}"" />
                </DataTemplate>
            </GroupStyle.HeaderTemplate>
        </GroupStyle>
    </ListView.GroupStyle>
</ListView>
";

        public string Example3Xaml => $@"
<ListView x:Name=""listView3"" 
    IsEnabled=""{listView3.IsEnabled}"" ItemsSource=""{{Binding}}""
    VirtualizingPanel.VirtualizationMode=""Recycling"">
    <ListView.View>
        <GridView x:Name=""gridView"" ColumnHeaderToolTip=""Employee Information"" AllowsColumnReorder=""{gridView.AllowsColumnReorder}"">
            <GridViewColumn
                Width=""120""
                DisplayMemberBinding=""{{Binding FirstName, Mode=OneTime}}""
                Header=""First Name"" />

            <GridViewColumn Width=""120"" DisplayMemberBinding=""{{Binding LastName, Mode=OneTime}}"">
                <GridViewColumnHeader>
                    Last Name
                    <GridViewColumnHeader.ContextMenu>
                        <ContextMenu>
                            <MenuItem Header=""Ascending"" />
                            <MenuItem Header=""Descending"" />
                        </ContextMenu>
                    </GridViewColumnHeader.ContextMenu>
                </GridViewColumnHeader>
            </GridViewColumn>

            <GridViewColumn
                Width=""240""
                DisplayMemberBinding=""{{Binding Company, Mode=OneTime}}""
                Header=""Company"" />
        </GridView>
    </ListView.View>
</ListView>
";

        #endregion
    }

    public class Contact : INotifyPropertyChanged
    {
        #region Properties
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Company { get; private set; }
        public string Name => FirstName + " " + LastName;
        #endregion

        public Contact(string firstName, string lastName, string company)
        {
            FirstName = firstName;
            LastName = lastName;
            Company = company;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region Public Methods
        public static Task<ObservableCollection<Contact>> GetContactsAsync()
        {
            IList<string> lines = new List<string>();
            var resourceStream = Application.GetResourceStream(new Uri("/Assets/Contacts.txt", UriKind.Relative));
            using (var reader = new StreamReader(resourceStream.Stream))
            {
                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine());
                }
            }

            var contacts = new ObservableCollection<Contact>();

            for (int i = 0; i < lines.Count; i += 3)
            {
                contacts.Add(new Contact(lines[i], lines[i + 1], lines[i + 2]));
            }

            return Task.FromResult(contacts);
        }

        public static async Task<ObservableCollection<GroupInfoList>> GetContactsGroupedAsync()
        {
            var query = from item in await GetContactsAsync()
                        group item by item.LastName.Substring(0, 1).ToUpper() into g
                        orderby g.Key
                        select new GroupInfoList(g) { Key = g.Key };

            return new ObservableCollection<GroupInfoList>(query);
        }

        public override string ToString()
        {
            return Name;
        }

        public void ChangeCompany(string company)
        {
            Company = company;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Company)));
        }
        #endregion
    }

    public class GroupInfoList : List<object>
    {
        public GroupInfoList(IEnumerable<object> items) : base(items)
        {
        }
        public object Key { get; set; }
    }

    public class ContactGroupKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string)value).Substring(0, 1).ToUpper();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
