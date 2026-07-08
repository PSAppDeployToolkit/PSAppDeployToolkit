/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryFormsPage : UserControl
    {
        private const string SignInFormXamlSource = "<UserControl\n" +
                                                    "    x:Class=\"Fluence.Wpf.Demo.Pages.Forms.SignInForm\"\n" +
                                                    "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                    "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                    "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                    "    <StackPanel MaxWidth=\"480\" HorizontalAlignment=\"Left\">\n" +
                                                    "        <TextBlock Margin=\"0,0,0,4\" Text=\"Email\" />\n" +
                                                    "        <ui:TextBox\n" +
                                                    "            Margin=\"0,0,0,12\"\n" +
                                                    "            PlaceholderText=\"name@example.com\" />\n" +
                                                    "        <TextBlock Margin=\"0,0,0,4\" Text=\"Password\" />\n" +
                                                    "        <ui:PasswordBox\n" +
                                                    "            Margin=\"0,0,0,12\"\n" +
                                                    "            PlaceholderText=\"Password\"\n" +
                                                    "            RevealButtonEnabled=\"True\"\n" +
                                                    "            ShowCapsLockIndicator=\"True\"\n" +
                                                    "            ShowPasswordStrength=\"True\" />\n" +
                                                    "        <ui:CheckBox\n" +
                                                    "            Margin=\"0,0,0,24\"\n" +
                                                    "            Content=\"Remember me\" />\n" +
                                                    "        <StackPanel Orientation=\"Horizontal\">\n" +
                                                    "            <ui:Button\n" +
                                                    "                Margin=\"0,0,8,0\"\n" +
                                                    "                Appearance=\"Accent\"\n" +
                                                    "                Content=\"Sign in\" />\n" +
                                                    "            <ui:Button Content=\"Create account\" />\n" +
                                                    "        </StackPanel>\n" +
                                                    "    </StackPanel>\n" +
                                                    "</UserControl>\n";

        private const string SignInFormCSharpSource = "using System.Windows.Controls;\n" +
                                                      "\n" +
                                                      "namespace Fluence.Wpf.Demo.Pages.Forms\n" +
                                                      "{\n" +
                                                      "    public partial class SignInForm : UserControl\n" +
                                                      "    {\n" +
                                                      "        public SignInForm()\n" +
                                                      "        {\n" +
                                                      "            InitializeComponent();\n" +
                                                      "        }\n" +
                                                      "    }\n" +
                                                      "}\n";
        private const string CheckoutFormXamlSource = "<UserControl\n" +
                                                      "    x:Class=\"Fluence.Wpf.Demo.Pages.Forms.CheckoutForm\"\n" +
                                                      "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                      "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                      "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                      "    <StackPanel MaxWidth=\"560\" HorizontalAlignment=\"Left\">\n" +
                                                      "        <TextBlock Margin=\"0,0,0,4\" Text=\"Contact email\" />\n" +
                                                      "        <ui:TextBox\n" +
                                                      "            Margin=\"0,0,0,12\"\n" +
                                                      "            PlaceholderText=\"name@example.com\" />\n" +
                                                      "        <TextBlock Margin=\"0,0,0,4\" Text=\"Shipping speed\" />\n" +
                                                      "        <ui:ComboBox\n" +
                                                      "            Margin=\"0,0,0,12\"\n" +
                                                      "            SelectedIndex=\"1\">\n" +
                                                      "            <ComboBoxItem Content=\"Standard\" />\n" +
                                                      "            <ComboBoxItem Content=\"Priority\" />\n" +
                                                      "            <ComboBoxItem Content=\"Overnight\" />\n" +
                                                      "        </ui:ComboBox>\n" +
                                                      "        <Grid x:Name=\"CheckoutFieldsGrid\" Margin=\"0,0,0,24\">\n" +
                                                      "            <Grid.ColumnDefinitions>\n" +
                                                      "                <ColumnDefinition Width=\"180\" />\n" +
                                                      "                <ColumnDefinition Width=\"16\" />\n" +
                                                      "                <ColumnDefinition Width=\"280\" />\n" +
                                                      "            </Grid.ColumnDefinitions>\n" +
                                                      "            <ui:NumberBox\n" +
                                                      "                x:Name=\"QuantityNumberBox\"\n" +
                                                      "                Grid.Column=\"0\"\n" +
                                                      "                Width=\"180\"\n" +
                                                      "                Header=\"Quantity\"\n" +
                                                      "                Maximum=\"10\"\n" +
                                                      "                Minimum=\"1\"\n" +
                                                      "                SpinButtonPlacementMode=\"Compact\"\n" +
                                                      "                Value=\"2\" />\n" +
                                                      "            <ui:TextBox\n" +
                                                      "                x:Name=\"OptionalTextBox\"\n" +
                                                      "                Grid.Column=\"2\"\n" +
                                                      "                Width=\"280\"\n" +
                                                      "                VerticalAlignment=\"Bottom\"\n" +
                                                      "                PlaceholderText=\"Optional\" />\n" +
                                                      "        </Grid>\n" +
                                                      "        <ui:CheckBox\n" +
                                                      "            x:Name=\"GiftCheckBox\"\n" +
                                                      "            Margin=\"0,0,0,24\"\n" +
                                                      "            Content=\"This is a gift\"\n" +
                                                      "            Description=\"Hide prices on the packing slip.\" />\n" +
                                                      "        <StackPanel x:Name=\"CheckoutButtonsPanel\" Orientation=\"Horizontal\">\n" +
                                                      "            <ui:Button\n" +
                                                      "                Margin=\"0,0,8,0\"\n" +
                                                      "                Appearance=\"Accent\"\n" +
                                                      "                Content=\"Place order\" />\n" +
                                                      "            <ui:Button Content=\"Save for later\" />\n" +
                                                      "        </StackPanel>\n" +
                                                      "    </StackPanel>\n" +
                                                      "</UserControl>\n";

        private const string CheckoutFormCSharpSource = "using System.Windows.Controls;\n" +
                                                        "\n" +
                                                        "namespace Fluence.Wpf.Demo.Pages.Forms\n" +
                                                        "{\n" +
                                                        "    public partial class CheckoutForm : UserControl\n" +
                                                        "    {\n" +
                                                        "        public CheckoutForm()\n" +
                                                        "        {\n" +
                                                        "            InitializeComponent();\n" +
                                                        "        }\n" +
                                                        "    }\n" +
                                                        "}\n";
        private const string SettingsFormXamlSource = "<UserControl\n" +
                                                      "    x:Class=\"Fluence.Wpf.Demo.Pages.Forms.SettingsForm\"\n" +
                                                      "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                      "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                      "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                      "    <StackPanel MaxWidth=\"560\" HorizontalAlignment=\"Left\">\n" +
                                                      "        <TextBlock Margin=\"0,0,0,4\" Text=\"Display name\" />\n" +
                                                      "        <ui:TextBox\n" +
                                                      "            Margin=\"0,0,0,12\"\n" +
                                                      "            Text=\"Avery Stone\" />\n" +
                                                      "        <TextBlock Margin=\"0,0,0,4\" Text=\"Theme\" />\n" +
                                                      "        <ui:ComboBox\n" +
                                                      "            Margin=\"0,0,0,12\"\n" +
                                                      "            SelectedIndex=\"0\">\n" +
                                                      "            <ComboBoxItem Content=\"Use system setting\" />\n" +
                                                      "            <ComboBoxItem Content=\"Light\" />\n" +
                                                      "            <ComboBoxItem Content=\"Dark\" />\n" +
                                                      "        </ui:ComboBox>\n" +
                                                      "        <ui:ToggleSwitch\n" +
                                                      "            Margin=\"0,0,0,24\"\n" +
                                                      "            Content=\"Email updates\"\n" +
                                                      "            IsChecked=\"True\"\n" +
                                                      "            OffContent=\"Off\"\n" +
                                                      "            OnContent=\"On\" />\n" +
                                                      "        <ui:Button\n" +
                                                      "            HorizontalAlignment=\"Left\"\n" +
                                                      "            Appearance=\"Accent\"\n" +
                                                      "            Content=\"Save settings\" />\n" +
                                                      "    </StackPanel>\n" +
                                                      "</UserControl>\n";

        private const string SettingsFormCSharpSource = "using System.Windows.Controls;\n" +
                                                        "\n" +
                                                        "namespace Fluence.Wpf.Demo.Pages.Forms\n" +
                                                        "{\n" +
                                                        "    public partial class SettingsForm : UserControl\n" +
                                                        "    {\n" +
                                                        "        public SettingsForm()\n" +
                                                        "        {\n" +
                                                        "            InitializeComponent();\n" +
                                                        "        }\n" +
                                                        "    }\n" +
                                                        "}\n";

        private const string DatePickerXamlSource = "<UserControl\n" +
                                                    "    x:Class=\"Fluence.Wpf.Demo.Pages.Forms.DueDateForm\"\n" +
                                                    "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                    "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                    "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                    "    <ui:DatePicker\n" +
                                                    "        x:Name=\"DueDatePicker\"\n" +
                                                    "        Header=\"Due date\"\n" +
                                                    "        PlaceholderText=\"Pick a date\"\n" +
                                                    "        SelectedDateChanged=\"DueDatePicker_SelectedDateChanged\" />\n" +
                                                    "</UserControl>\n";

        private const string DatePickerCSharpSource = "using System.Windows.Controls;\n" +
                                                      "using Fluence.Wpf;\n" +
                                                      "\n" +
                                                      "namespace Fluence.Wpf.Demo.Pages.Forms\n" +
                                                      "{\n" +
                                                      "    public partial class DueDateForm : UserControl\n" +
                                                      "    {\n" +
                                                      "        public DueDateForm()\n" +
                                                      "        {\n" +
                                                      "            InitializeComponent();\n" +
                                                      "        }\n" +
                                                      "\n" +
                                                      "        private void DueDatePicker_SelectedDateChanged(object sender, DatePickerSelectedValueChangedEventArgs e)\n" +
                                                      "        {\n" +
                                                      "            // e.NewDate carries the committed date (null when cleared).\n" +
                                                      "        }\n" +
                                                      "    }\n" +
                                                      "}\n";

        public GalleryFormsPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, TimePickerXamlSource, TimePickerCSharpSource),
                new DemoSampleSource(2, DatePickerXamlSource, DatePickerCSharpSource),
                new DemoSampleSource(3, ColorPickerXamlSource, ColorPickerCSharpSource),
                new DemoSampleSource(4, SignInFormXamlSource, SignInFormCSharpSource),
                new DemoSampleSource(5, CheckoutFormXamlSource, CheckoutFormCSharpSource),
                new DemoSampleSource(6, SettingsFormXamlSource, SettingsFormCSharpSource));

            Loaded += GalleryFormsPage_Loaded;
        }

        private const string TimePickerXamlSource = "<UserControl\n" +
                                                    "    x:Class=\"Fluence.Wpf.Demo.Pages.Forms.ReminderTimeForm\"\n" +
                                                    "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                    "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                    "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                    "    <ui:TimePicker\n" +
                                                    "        x:Name=\"ReminderTimePicker\"\n" +
                                                    "        Header=\"Reminder time\"\n" +
                                                    "        MinuteIncrement=\"5\"\n" +
                                                    "        PlaceholderText=\"Pick a time\"\n" +
                                                    "        SelectedTimeChanged=\"ReminderTimePicker_SelectedTimeChanged\" />\n" +
                                                    "</UserControl>\n";

        private const string TimePickerCSharpSource = "using System.Windows.Controls;\n" +
                                                      "using Fluence.Wpf;\n" +
                                                      "\n" +
                                                      "namespace Fluence.Wpf.Demo.Pages.Forms\n" +
                                                      "{\n" +
                                                      "    public partial class ReminderTimeForm : UserControl\n" +
                                                      "    {\n" +
                                                      "        public ReminderTimeForm()\n" +
                                                      "        {\n" +
                                                      "            InitializeComponent();\n" +
                                                      "        }\n" +
                                                      "\n" +
                                                      "        private void ReminderTimePicker_SelectedTimeChanged(object sender, TimePickerSelectedValueChangedEventArgs e)\n" +
                                                      "        {\n" +
                                                      "            // e.NewTime carries the committed time (null when cleared).\n" +
                                                      "        }\n" +
                                                      "    }\n" +
                                                      "}\n";

        private const string ColorPickerXamlSource = "<UserControl\n" +
                                                     "    x:Class=\"Fluence.Wpf.Demo.Pages.Forms.AccentColorForm\"\n" +
                                                     "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                     "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                     "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                     "    <!--\n" +
                                                     "        The text-entry area shows an RGB/HSV selector, per-channel inputs, an alpha\n" +
                                                     "        percentage input, and a hex input. Set IsMoreButtonVisible=\"True\" to collapse\n" +
                                                     "        it behind a More/Less toggle; the Is*Visible properties hide individual parts.\n" +
                                                     "    -->\n" +
                                                     "    <ui:ColorPicker\n" +
                                                     "        x:Name=\"AccentColorPicker\"\n" +
                                                     "        ColorChanged=\"AccentColorPicker_ColorChanged\"\n" +
                                                     "        IsAlphaEnabled=\"True\"\n" +
                                                     "        IsMoreButtonVisible=\"False\" />\n" +
                                                     "</UserControl>\n";

        private const string ColorPickerCSharpSource = "using System.Windows.Controls;\n" +
                                                       "using Fluence.Wpf;\n" +
                                                       "\n" +
                                                       "namespace Fluence.Wpf.Demo.Pages.Forms\n" +
                                                       "{\n" +
                                                       "    public partial class AccentColorForm : UserControl\n" +
                                                       "    {\n" +
                                                       "        public AccentColorForm()\n" +
                                                       "        {\n" +
                                                       "            InitializeComponent();\n" +
                                                       "        }\n" +
                                                       "\n" +
                                                       "        private void AccentColorPicker_ColorChanged(object sender, ColorPickerColorChangedEventArgs e)\n" +
                                                       "        {\n" +
                                                       "            // e.NewColor carries the picked color.\n" +
                                                       "        }\n" +
                                                       "    }\n" +
                                                       "}\n";

        private void DemoColorPicker_ColorChanged(object sender, Fluence.Wpf.ColorPickerColorChangedEventArgs e)
        {
            ColorPickerResultLabel.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, "Color: {0}", e.NewColor);
        }

        private void DemoTimePicker_SelectedTimeChanged(object sender, Fluence.Wpf.TimePickerSelectedValueChangedEventArgs e)
        {
            TimePickerResultLabel.Text = e.NewTime is System.TimeSpan newTime
                ? string.Format(System.Globalization.CultureInfo.CurrentCulture, "Selected: {0:t}", System.DateTime.Today.Add(newTime))
                : "No time selected";
        }

        private void DemoDatePicker_SelectedDateChanged(object sender, Fluence.Wpf.DatePickerSelectedValueChangedEventArgs e)
        {
            DatePickerResultLabel.Text = e.NewDate is System.DateTime newDate
                ? string.Format(System.Globalization.CultureInfo.CurrentCulture, "Selected: {0:d}", newDate)
                : "No date selected";
        }

        private void GalleryFormsPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GalleryFormsPage_Loaded;
            DependencyPropertyDescriptor
                .FromProperty(Controls.PasswordBox.PasswordProperty, typeof(Controls.PasswordBox))
                .AddValueChanged(SignInPasswordBox, SignInPassword_Changed);
        }

        private void SignInField_Changed(object? sender, RoutedEventArgs e)
        {
            if (SignInButton is null || SignInEmailBox is null || SignInPasswordBox is null)
            {
                return;
            }

            SignInButton.IsEnabled =
                !string.IsNullOrWhiteSpace(SignInEmailBox.Text) &&
                !string.IsNullOrWhiteSpace(SignInPasswordBox.Password);
        }

        private void SignInPassword_Changed(object? sender, System.EventArgs e)
        {
            SignInField_Changed(sender, new RoutedEventArgs());
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SignInStatusBar?.IsOpen = true;
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SettingsSavedBar?.IsOpen = true;
        }
    }
}
