using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Controls;
using SamplesCommon;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class CommandBarPage : Page, INotifyPropertyChanged
    {
        private bool multipleButtons = false;
        public bool MultipleButtons
        {
            get => multipleButtons;
            set
            {
                multipleButtons = value;
                OnPropertyChanged("MultipleButtons");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        public CommandBarPage()
        {
            InitializeComponent();
            AddKeyboardAccelerators();
            UpdateExampleCode();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            PrimaryCommandBar.IsOpen = true;
            UpdateExampleCode();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            PrimaryCommandBar.IsOpen = false;
            UpdateExampleCode();
        }

        private void OnElementClicked(object sender, RoutedEventArgs e)
        {
            var selectedFlyoutItem = sender as AppBarButton;
            SelectedOptionText.Text = "You clicked: " + (sender as AppBarButton).Label;
        }

        private void AddSecondaryCommands_Click(object sender, RoutedEventArgs e)
        {
            // Add compact button to the command bar. It provides functionality specific
            // to this page, and is removed when leaving the page.

            if (PrimaryCommandBar.SecondaryCommands.Count == 1)
            {
                var newButton = new AppBarButton
                {
                    Icon = new FontIcon(SegoeFluentIcons.Add),
                    Label = "Button 1"
                };
                newButton.AddKeyboardAccelerator(Key.N, ModifierKeys.Control);
                PrimaryCommandBar.SecondaryCommands.Add(newButton);

                newButton = new AppBarButton
                {
                    Icon = new FontIcon(SegoeFluentIcons.Delete),
                    Label = "Button 2"
                };
                PrimaryCommandBar.SecondaryCommands.Add(newButton);
                newButton.AddKeyboardAccelerator(Key.Delete);
                PrimaryCommandBar.SecondaryCommands.Add(new AppBarSeparator());

                newButton = new AppBarButton
                {
                    Icon = new FontIcon(SegoeFluentIcons.FontDecrease),
                    Label = "Button 3"
                };
                newButton.AddKeyboardAccelerator(Key.Subtract, ModifierKeys.Control);
                PrimaryCommandBar.SecondaryCommands.Add(newButton);

                newButton = new AppBarButton
                {
                    Icon = new FontIcon(SegoeFluentIcons.FontIncrease),
                    Label = "Button 4"
                };
                newButton.AddKeyboardAccelerator(Key.Add, ModifierKeys.Control);
                PrimaryCommandBar.SecondaryCommands.Add(newButton);

            }
            MultipleButtons = true;
        }

        private void RemoveSecondaryCommands_Click(object sender, RoutedEventArgs e)
        {
            RemoveSecondaryCommands();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            RemoveSecondaryCommands();
            base.OnNavigatingFrom(e);
        }

        private void RemoveSecondaryCommands()
        {
            while (PrimaryCommandBar.SecondaryCommands.Count > 1)
            {
                PrimaryCommandBar.SecondaryCommands.RemoveAt(PrimaryCommandBar.SecondaryCommands.Count - 1);
            }
            MultipleButtons = false;
        }

        private void AddKeyboardAccelerators()
        {
            EditButton.AddKeyboardAccelerator(Key.E, ModifierKeys.Control);

            ShareButton.AddKeyboardAccelerator(Key.F4);

            AddButton.AddKeyboardAccelerator(Key.A, ModifierKeys.Control);

            SettingsButton.AddKeyboardAccelerator(Key.I, ModifierKeys.Control);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ControlExampleSubstitution Substitution1 = new ControlExampleSubstitution
            {
                Key = "IsOpen",
                IsEnabled = true,
            };
            BindingOperations.SetBinding(Substitution1, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = PrimaryCommandBar,
                Path = new PropertyPath("IsOpen"),
            });
            ControlExampleSubstitution Substitution2 = new ControlExampleSubstitution
            {
                Key = "MultipleButtonsSecondaryCommands",
                Value = (string)Resources["MultipleButtonsSecondaryCommands"],
            };
            BindingOperations.SetBinding(Substitution2, ControlExampleSubstitution.IsEnabledProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath("MultipleButtons"),
            });
            ObservableCollection<ControlExampleSubstitution> Substitutions = new ObservableCollection<ControlExampleSubstitution>() { Substitution1, Substitution2 };
            Example1.Substitutions = Substitutions;
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsInitialized) return;

            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<ui:CommandBar x:Name=""PrimaryCommandBar""
    DefaultLabelPosition=""Right"" IsOpen=""False"">
    <ui:AppBarButton x:Name=""AddButton""
        Click=""OnElementClicked"" Label=""Add"">
        <ui:AppBarButton.Icon>
            <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Add}}""/>
        </ui:AppBarButton.Icon>
    </ui:AppBarButton>
    <ui:AppBarButton x:Name=""EditButton""
        Click=""OnElementClicked"" Label=""Edit"">
        <ui:AppBarButton.Icon>
            <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Edit}}""/>
        </ui:AppBarButton.Icon>

    </ui:AppBarButton>
    <ui:AppBarButton x:Name=""ShareButton""
        Click=""OnElementClicked"" Label=""Share"">
        <ui:AppBarButton.Icon>
            <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Share}}""/>
        </ui:AppBarButton.Icon>
    </ui:AppBarButton>
    <ui:CommandBar.SecondaryCommands>
        <ui:AppBarButton x:Name=""SettingsButton""
            Icon=""Setting"" Label=""Settings""
            Click=""OnElementClicked""/>
    </ui:CommandBar.SecondaryCommands>
</ui:CommandBar>
";

        #endregion

    }
}
