using iNKORE.UI.WPF.Modern.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class ExpanderPage : Page
    {
        public ExpanderPage()
        {
            InitializeComponent();
        }

        private void ExpandDirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string expandDirection = e.AddedItems[0].ToString();
            var targetExpander = (sender as FrameworkElement)?.Tag as Expander;
            var headerRotate = ((targetExpander?.Header as Border)?.Child as TextBlock)?.LayoutTransform as RotateTransform;
            if (targetExpander == null) return;

            switch (expandDirection)
            {
                case "Down":
                default:
                    targetExpander.ExpandDirection = ExpandDirection.Down;
                    targetExpander.VerticalAlignment = VerticalAlignment.Top;
                    if (headerRotate != null) headerRotate.Angle = 0;
                    break;

                case "Up":
                    targetExpander.ExpandDirection = ExpandDirection.Up;
                    targetExpander.VerticalAlignment = VerticalAlignment.Bottom;
                    if (headerRotate != null) headerRotate.Angle = 0;
                    break;

                case "Left":
                    targetExpander.ExpandDirection = ExpandDirection.Left;
                    targetExpander.HorizontalAlignment = HorizontalAlignment.Right;
                    if (headerRotate != null) headerRotate.Angle = 90;
                    break;

                case "Right":
                    targetExpander.ExpandDirection = ExpandDirection.Right;
                    targetExpander.HorizontalAlignment = HorizontalAlignment.Left;
                    if (headerRotate != null) headerRotate.Angle = 90;
                    break;
            }

            if (this.IsLoaded)
            {
                UpdateExampleCode();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Setup bindings for Example 1
            ControlExampleSubstitution Substitution1 = new ControlExampleSubstitution
            {
                Key = "IsExpanded",
            };
            BindingOperations.SetBinding(Substitution1, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = Expander1,
                Path = new PropertyPath("IsExpanded"),
            });

            ControlExampleSubstitution Substitution2 = new ControlExampleSubstitution
            {
                Key = "ExpandDirection",
            };
            BindingOperations.SetBinding(Substitution2, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = Expander1DirectionComboBox,
                Path = new PropertyPath("SelectedValue"),
            });

            ControlExampleSubstitution Substitution3 = new ControlExampleSubstitution
            {
                Key = "VerticalAlignment",
            };
            BindingOperations.SetBinding(Substitution3, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = Expander1,
                Path = new PropertyPath("VerticalAlignment"),
            });

            ControlExampleSubstitution Substitution4 = new ControlExampleSubstitution
            {
                Key = "HorizontalAlignment",
            };
            BindingOperations.SetBinding(Substitution4, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = Expander1,
                Path = new PropertyPath("HorizontalAlignment"),
            });

            ObservableCollection<ControlExampleSubstitution> Substitutions = new ObservableCollection<ControlExampleSubstitution> { Substitution1, Substitution2, Substitution3, Substitution4 };
            Example1.Substitutions = Substitutions;

            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
            Example4.Xaml = Example4Xaml;
        }

        public string Example1Xaml => RotateTransform_Example1Text.Angle == 0 ? $@"
<Expander x:Name=""Expander1""
    Style=""{{StaticResource {{x:Static ui:ThemeKeys.ExpanderCardStyleKey}}}}""
    Content=""This is in the content""
    ExpandDirection=""{Expander1.ExpandDirection.ToString()}""
    Header=""This text is in the header"" />
" : $@"
<Expander x:Name=""Expander1"" VerticalAlignment=""Top""
    Content=""This is in the content""
    ExpandDirection=""Down"" IsExpanded=""False"">
    <Expander.Header>
        <TextBlock Text=""This text is in the header"" FontWeight=""Bold"">
            <TextBlock.LayoutTransform>
                <RotateTransform Angle=""{RotateTransform_Example1Text.Angle}""/>
            </TextBlock.LayoutTransform>
        </TextBlock>
    </Expander.Header>
</Expander>
";

        public string Example2Xaml => $@"
<Expander x:Name=""Expander2"" Style=""{{StaticResource {{x:Static ui:ThemeKeys.ExpanderCardStyleKey}}}}"">
    <Expander.Header>
        <ToggleButton Content=""This is a ToggleButton in the header"" />
    </Expander.Header>
    <Expander.Content>
        <Grid>
            <Button Margin=""15"" Content=""This is a Button in the content"" />
        </Grid>
    </Expander.Content>
</Expander>
";

            public string Example3Xaml => $@"
<Expander Style=""{{StaticResource {{x:Static ui:ThemeKeys.ExpanderCardStyleKey}}}}"">
    <Expander.Header>
        <ToggleButton HorizontalAlignment=""Center"" Content=""This ToggleButton is centered"" />
    </Expander.Header>
    <Expander.Content>
        <Button Margin=""4"" Content=""This Button is left aligned"" />
    </Expander.Content>
</Expander>
";

        public string Example4Xaml => $@"
<Expander x:Name=""Expander4""
    Content=""This is in the content""
    Style=""{{StaticResource {{x:Static ui:ThemeKeys.ExpanderCardStyleKey}}}}""
    ExpandDirection=""{Expander4.ExpandDirection.ToString()}""
    Header=""This text is in the header"" />
";

        #endregion

    }
}