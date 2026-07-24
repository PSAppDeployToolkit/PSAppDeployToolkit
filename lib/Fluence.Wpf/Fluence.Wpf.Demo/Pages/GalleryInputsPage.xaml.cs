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

using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryInputsPage : UserControl
    {
        private const string TextBoxInputXamlSource = "<UserControl\n" +
                                                      "    x:Class=\"Fluence.Wpf.Demo.Pages.Inputs.TextBoxInput\"\n" +
                                                      "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                      "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                      "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                      "    <ui:StackPanel Spacing=\"20\">\n" +
                                                      "        <ui:TextBox\n" +
                                                      "            Width=\"480\"\n" +
                                                      "            HorizontalAlignment=\"Left\"\n" +
                                                      "            PlaceholderText=\"Basic text box...\" />\n" +
                                                      "        <ui:TextBox\n" +
                                                      "            Width=\"480\"\n" +
                                                      "            HorizontalAlignment=\"Left\"\n" +
                                                      "            PlaceholderText=\"Search\">\n" +
                                                      "            <ui:TextBox.Icon>\n" +
                                                      "                <ui:FontIcon Glyph=\"&#xE721;\" IconFontSize=\"14\" />\n" +
                                                      "            </ui:TextBox.Icon>\n" +
                                                      "        </ui:TextBox>\n" +
                                                      "        <ui:TextBox\n" +
                                                      "            Width=\"480\"\n" +
                                                      "            HorizontalAlignment=\"Left\"\n" +
                                                      "            MaxLength=\"40\"\n" +
                                                      "            PlaceholderText=\"Limited to 40 characters...\" />\n" +
                                                      "    </ui:StackPanel>\n" +
                                                      "</UserControl>\n";

        private const string TextBoxInputCSharpSource = "using System.Windows.Controls;\n" +
                                                        "\n" +
                                                        "namespace Fluence.Wpf.Demo.Pages.Inputs\n" +
                                                        "{\n" +
                                                        "    public partial class TextBoxInput : UserControl\n" +
                                                        "    {\n" +
                                                        "        public TextBoxInput()\n" +
                                                        "        {\n" +
                                                        "            InitializeComponent();\n" +
                                                        "        }\n" +
                                                        "    }\n" +
                                                        "}\n";
        private const string TextBoxValidationXamlSource = "<UserControl\n" +
                                                           "    x:Class=\"Fluence.Wpf.Demo.Pages.Inputs.TextBoxValidation\"\n" +
                                                           "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                           "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                           "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\"\n" +
                                                           "    xmlns:uicore=\"clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf\">\n" +
                                                           "    <ui:StackPanel Spacing=\"20\">\n" +
                                                           "        <ui:TextBox\n" +
                                                           "            Width=\"480\"\n" +
                                                           "            HorizontalAlignment=\"Left\"\n" +
                                                           "            HelperText=\"Helper text can explain format before validation.\"\n" +
                                                           "            PlaceholderText=\"With helper text\" />\n" +
                                                           "        <ui:TextBox\n" +
                                                           "            Width=\"480\"\n" +
                                                           "            HorizontalAlignment=\"Left\"\n" +
                                                           "            Text=\"Valid input\"\n" +
                                                           "            ValidationMessage=\"Looks good.\"\n" +
                                                           "            ValidationState=\"{x:Static uicore:ValidationState.Success}\" />\n" +
                                                           "        <ui:TextBox\n" +
                                                           "            Width=\"480\"\n" +
                                                           "            HorizontalAlignment=\"Left\"\n" +
                                                           "            Text=\"Check this value\"\n" +
                                                           "            ValidationMessage=\"Review this before continuing.\"\n" +
                                                           "            ValidationState=\"{x:Static uicore:ValidationState.Warning}\" />\n" +
                                                           "        <ui:TextBox\n" +
                                                           "            Width=\"480\"\n" +
                                                           "            HorizontalAlignment=\"Left\"\n" +
                                                           "            Text=\"Bad value\"\n" +
                                                           "            ValidationMessage=\"Please fix this field.\"\n" +
                                                           "            ValidationState=\"{x:Static uicore:ValidationState.Error}\" />\n" +
                                                           "    </ui:StackPanel>\n" +
                                                           "</UserControl>\n";

        private const string TextBoxValidationCSharpSource = "using System.Windows.Controls;\n" +
                                                             "\n" +
                                                             "namespace Fluence.Wpf.Demo.Pages.Inputs\n" +
                                                             "{\n" +
                                                             "    public partial class TextBoxValidation : UserControl\n" +
                                                             "    {\n" +
                                                             "        public TextBoxValidation()\n" +
                                                             "        {\n" +
                                                             "            InitializeComponent();\n" +
                                                             "        }\n" +
                                                             "    }\n" +
                                                             "}\n";
        private const string PasswordBoxInputXamlSource = "<UserControl\n" +
                                                          "    x:Class=\"Fluence.Wpf.Demo.Pages.Inputs.PasswordBoxInput\"\n" +
                                                          "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                          "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                          "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                          "    <ui:StackPanel Spacing=\"20\">\n" +
                                                          "        <ui:PasswordBox\n" +
                                                          "            Width=\"480\"\n" +
                                                          "            HorizontalAlignment=\"Left\"\n" +
                                                          "            PlaceholderText=\"Enter password...\"\n" +
                                                          "            RevealButtonEnabled=\"True\"\n" +
                                                          "            ShowCapsLockIndicator=\"True\"\n" +
                                                          "            ShowPasswordStrength=\"True\" />\n" +
                                                          "        <ui:PasswordBox\n" +
                                                          "            Width=\"480\"\n" +
                                                          "            HorizontalAlignment=\"Left\"\n" +
                                                          "            Password=\"CorrectHorse7!\"\n" +
                                                          "            RevealButtonEnabled=\"True\"\n" +
                                                          "            ShowCapsLockIndicator=\"True\"\n" +
                                                          "            ShowPasswordStrength=\"True\" />\n" +
                                                          "        <ui:PasswordBox\n" +
                                                          "            Width=\"480\"\n" +
                                                          "            HorizontalAlignment=\"Left\"\n" +
                                                          "            IsEnabled=\"False\"\n" +
                                                          "            PlaceholderText=\"Disabled\" />\n" +
                                                          "    </ui:StackPanel>\n" +
                                                          "</UserControl>\n";

        private const string PasswordBoxInputCSharpSource = "using System.Windows.Controls;\n" +
                                                            "\n" +
                                                            "namespace Fluence.Wpf.Demo.Pages.Inputs\n" +
                                                            "{\n" +
                                                            "    public partial class PasswordBoxInput : UserControl\n" +
                                                            "    {\n" +
                                                            "        public PasswordBoxInput()\n" +
                                                            "        {\n" +
                                                            "            InitializeComponent();\n" +
                                                            "        }\n" +
                                                            "    }\n" +
                                                            "}\n";
        private const string NumberBoxInputXamlSource = "<UserControl\n" +
                                                        "    x:Class=\"Fluence.Wpf.Demo.Pages.Inputs.NumberBoxInput\"\n" +
                                                        "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                        "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                        "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                        "    <ui:StackPanel Spacing=\"20\">\n" +
                                                        "        <ui:NumberBox\n" +
                                                        "            Width=\"260\"\n" +
                                                        "            Header=\"Inline\"\n" +
                                                        "            Maximum=\"100\"\n" +
                                                        "            Minimum=\"0\"\n" +
                                                        "            SpinButtonPlacementMode=\"Inline\"\n" +
                                                        "            Value=\"5\" />\n" +
                                                        "        <ui:NumberBox\n" +
                                                        "            Width=\"260\"\n" +
                                                        "            Header=\"Compact\"\n" +
                                                        "            Maximum=\"100\"\n" +
                                                        "            Minimum=\"0\"\n" +
                                                        "            SpinButtonPlacementMode=\"Compact\"\n" +
                                                        "            Value=\"25\" />\n" +
                                                        "        <ui:NumberBox\n" +
                                                        "            Width=\"260\"\n" +
                                                        "            Header=\"Keyboard only\"\n" +
                                                        "            Maximum=\"100\"\n" +
                                                        "            Minimum=\"0\"\n" +
                                                        "            SpinButtonPlacementMode=\"Hidden\"\n" +
                                                        "            Value=\"50\" />\n" +
                                                        "        <ui:NumberBox\n" +
                                                        "            Width=\"260\"\n" +
                                                        "            Header=\"Disabled\"\n" +
                                                        "            IsEnabled=\"False\"\n" +
                                                        "            Value=\"42\" />\n" +
                                                        "    </ui:StackPanel>\n" +
                                                        "</UserControl>\n";

        private const string NumberBoxInputCSharpSource = "using System.Windows.Controls;\n" +
                                                          "\n" +
                                                          "namespace Fluence.Wpf.Demo.Pages.Inputs\n" +
                                                          "{\n" +
                                                          "    public partial class NumberBoxInput : UserControl\n" +
                                                          "    {\n" +
                                                          "        public NumberBoxInput()\n" +
                                                          "        {\n" +
                                                          "            InitializeComponent();\n" +
                                                          "        }\n" +
                                                          "    }\n" +
                                                          "}\n";
        private const string SliderInputXamlSource = "<UserControl\n" +
                                                     "    x:Class=\"Fluence.Wpf.Demo.Pages.Inputs.SliderInput\"\n" +
                                                     "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                     "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                     "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                     "    <ui:StackPanel Spacing=\"20\">\n" +
                                                     "        <ui:StackPanel Spacing=\"8\">\n" +
                                                     "            <TextBlock Text=\"Default\" />\n" +
                                                     "            <ui:Slider\n" +
                                                     "                Maximum=\"100\"\n" +
                                                     "                Minimum=\"0\"\n" +
                                                     "                Value=\"35\" />\n" +
                                                     "        </ui:StackPanel>\n" +
                                                     "        <ui:StackPanel Spacing=\"8\">\n" +
                                                     "            <TextBlock Text=\"Snapped to ticks\" />\n" +
                                                     "            <ui:Slider\n" +
                                                     "                IsSnapToTickEnabled=\"True\"\n" +
                                                     "                Maximum=\"10\"\n" +
                                                     "                Minimum=\"0\"\n" +
                                                     "                TickFrequency=\"1\"\n" +
                                                     "                TickPlacement=\"BottomRight\"\n" +
                                                     "                Value=\"4\" />\n" +
                                                     "        </ui:StackPanel>\n" +
                                                     "        <Grid\n" +
                                                     "            MaxWidth=\"292\"\n" +
                                                     "            HorizontalAlignment=\"Center\">\n" +
                                                     "            <Grid.ColumnDefinitions>\n" +
                                                     "                <ColumnDefinition Width=\"Auto\" />\n" +
                                                     "                <ColumnDefinition Width=\"32\" />\n" +
                                                     "                <ColumnDefinition Width=\"Auto\" />\n" +
                                                     "            </Grid.ColumnDefinitions>\n" +
                                                     "            <ui:StackPanel Grid.Column=\"0\" Spacing=\"8\" HorizontalAlignment=\"Center\">\n" +
                                                     "                <TextBlock HorizontalAlignment=\"Center\" Text=\"Vertical\" />\n" +
                                                     "                <ui:Slider\n" +
                                                     "                    Height=\"210\"\n" +
                                                     "                    Maximum=\"100\"\n" +
                                                     "                    Minimum=\"0\"\n" +
                                                     "                    Orientation=\"Vertical\"\n" +
                                                     "                    TickFrequency=\"10\"\n" +
                                                     "                    TickPlacement=\"BottomRight\"\n" +
                                                     "                    Value=\"40\" />\n" +
                                                     "            </ui:StackPanel>\n" +
                                                     "            <ui:StackPanel Grid.Column=\"2\" Spacing=\"8\" HorizontalAlignment=\"Center\">\n" +
                                                     "                <TextBlock HorizontalAlignment=\"Center\" Text=\"Disabled\" />\n" +
                                                     "                <ui:Slider\n" +
                                                     "                    Height=\"210\"\n" +
                                                     "                    IsEnabled=\"False\"\n" +
                                                     "                    Maximum=\"100\"\n" +
                                                     "                    Minimum=\"0\"\n" +
                                                     "                    Orientation=\"Vertical\"\n" +
                                                     "                    Value=\"25\" />\n" +
                                                     "            </ui:StackPanel>\n" +
                                                     "        </Grid>\n" +
                                                     "    </ui:StackPanel>\n" +
                                                     "</UserControl>\n";

        private const string SliderInputCSharpSource = "using System.Windows.Controls;\n" +
                                                       "\n" +
                                                       "namespace Fluence.Wpf.Demo.Pages.Inputs\n" +
                                                       "{\n" +
                                                       "    public partial class SliderInput : UserControl\n" +
                                                       "    {\n" +
                                                       "        public SliderInput()\n" +
                                                       "        {\n" +
                                                       "            InitializeComponent();\n" +
                                                       "        }\n" +
                                                       "    }\n" +
                                                       "}\n";

        private const string AutoSuggestBoxXamlSource = "<UserControl\n" +
                                                        "    x:Class=\"Fluence.Wpf.Demo.Pages.Inputs.AutoSuggestSample\"\n" +
                                                        "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                        "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                        "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                        "    <ui:AutoSuggestBox\n" +
                                                        "        x:Name=\"SearchBox\"\n" +
                                                        "        Width=\"280\"\n" +
                                                        "        PlaceholderText=\"Search fruit\"\n" +
                                                        "        QuerySubmitted=\"SearchBox_QuerySubmitted\"\n" +
                                                        "        TextChanged=\"SearchBox_TextChanged\" />\n" +
                                                        "</UserControl>\n";

        private const string AutoSuggestBoxCSharpSource = "using System;\n" +
                                                          "using System.Collections.Generic;\n" +
                                                          "using System.Windows.Controls;\n" +
                                                          "using Fluence.Wpf;\n" +
                                                          "\n" +
                                                          "namespace Fluence.Wpf.Demo.Pages.Inputs\n" +
                                                          "{\n" +
                                                          "    public partial class AutoSuggestSample : UserControl\n" +
                                                          "    {\n" +
                                                          "        private static readonly string[] Fruits =\n" +
                                                          "            { \"Apple\", \"Apricot\", \"Banana\", \"Cherry\", \"Mango\", \"Orange\", \"Peach\" };\n" +
                                                          "\n" +
                                                          "        public AutoSuggestSample()\n" +
                                                          "        {\n" +
                                                          "            InitializeComponent();\n" +
                                                          "        }\n" +
                                                          "\n" +
                                                          "        private void SearchBox_TextChanged(object sender, AutoSuggestBoxTextChangedEventArgs e)\n" +
                                                          "        {\n" +
                                                          "            if (e.Reason != AutoSuggestionBoxTextChangeReason.UserInput)\n" +
                                                          "            {\n" +
                                                          "                return;\n" +
                                                          "            }\n" +
                                                          "\n" +
                                                          "            List<string> matches = new();\n" +
                                                          "            foreach (string fruit in Fruits)\n" +
                                                          "            {\n" +
                                                          "                if (fruit.IndexOf(SearchBox.Text, StringComparison.OrdinalIgnoreCase) >= 0)\n" +
                                                          "                {\n" +
                                                          "                    matches.Add(fruit);\n" +
                                                          "                }\n" +
                                                          "            }\n" +
                                                          "\n" +
                                                          "            SearchBox.ItemsSource = matches;\n" +
                                                          "        }\n" +
                                                          "\n" +
                                                          "        private void SearchBox_QuerySubmitted(object sender, AutoSuggestBoxQuerySubmittedEventArgs e)\n" +
                                                          "        {\n" +
                                                          "            string submitted = e.ChosenSuggestion as string ?? e.QueryText;\n" +
                                                          "            // Act on the submitted query here.\n" +
                                                          "        }\n" +
                                                          "    }\n" +
                                                          "}\n";

        private static readonly string[] AutoSuggestFruits =
        [
            "Apple", "Apricot", "Banana", "Blackberry", "Blueberry", "Cherry",
            "Grape", "Lemon", "Lime", "Mango", "Orange", "Peach", "Pear",
            "Pineapple", "Plum", "Raspberry", "Strawberry", "Watermelon",
        ];

        public GalleryInputsPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (System.Windows.DependencyObject)Content,
                new DemoSampleSource(1, TextBoxInputXamlSource, TextBoxInputCSharpSource),
                new DemoSampleSource(2, TextBoxValidationXamlSource, TextBoxValidationCSharpSource),
                new DemoSampleSource(3, AutoSuggestBoxXamlSource, AutoSuggestBoxCSharpSource),
                new DemoSampleSource(4, PasswordBoxInputXamlSource, PasswordBoxInputCSharpSource),
                new DemoSampleSource(5, NumberBoxInputXamlSource, NumberBoxInputCSharpSource),
                new DemoSampleSource(6, SliderInputXamlSource, SliderInputCSharpSource));
        }

        private void DemoAutoSuggestBox_TextChanged(object sender, Fluence.Wpf.AutoSuggestBoxTextChangedEventArgs e)
        {
            if (e.Reason is not Fluence.Wpf.AutoSuggestionBoxTextChangeReason.UserInput)
            {
                return;
            }

            string query = DemoAutoSuggestBox.Text;
            System.Collections.Generic.List<string> matches = [];
            foreach (string fruit in AutoSuggestFruits)
            {
                if (string.IsNullOrWhiteSpace(query)
                    || fruit.StartsWith(query, System.StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(fruit);
                }
            }

            DemoAutoSuggestBox.ItemsSource = matches;
        }

        private void DemoAutoSuggestBox_QuerySubmitted(object sender, Fluence.Wpf.AutoSuggestBoxQuerySubmittedEventArgs e)
        {
            string submitted = e.ChosenSuggestion as string ?? e.QueryText;
            AutoSuggestResultLabel.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, "Submitted: {0}", submitted);
        }
    }
}
