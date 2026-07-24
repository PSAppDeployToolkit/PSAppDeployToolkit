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

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryStatusPage : UserControl
    {
        private const string ProgressBarValueXamlSource = "<UserControl\n" +
                                                          "    x:Class=\"Fluence.Wpf.Demo.Pages.Status.ProgressBarValue\"\n" +
                                                          "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                          "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                          "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\"\n" +
                                                          "    xmlns:uicore=\"clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf\">\n" +
                                                          "    <StackPanel>\n" +
                                                          "        <ui:ProgressBar\n" +
                                                          "            x:Name=\"StandardProgressBar\"\n" +
                                                          "            Height=\"8\"\n" +
                                                          "            Margin=\"0,0,0,12\"\n" +
                                                          "            HorizontalAlignment=\"Stretch\"\n" +
                                                          "            Maximum=\"100\"\n" +
                                                          "            Minimum=\"0\"\n" +
                                                          "            Value=\"{Binding Value, Source={x:Reference ProgressValueNumberBox}}\" />\n" +
                                                          "        <ui:NumberBox\n" +
                                                          "            x:Name=\"ProgressValueNumberBox\"\n" +
                                                          "            Header=\"Value\"\n" +
                                                          "            HorizontalAlignment=\"Center\"\n" +
                                                          "            VerticalAlignment=\"Center\"\n" +
                                                          "            Maximum=\"100\"\n" +
                                                          "            Minimum=\"0\"\n" +
                                                          "            SmallChange=\"5\"\n" +
                                                          "            SpinButtonPlacementMode=\"{x:Static uicore:SpinButtonPlacementMode.Inline}\"\n" +
                                                          "            Value=\"50\" />\n" +
                                                          "        <Grid>\n" +
                                                          "            <Grid.ColumnDefinitions>\n" +
                                                          "                <ColumnDefinition Width=\"*\" />\n" +
                                                          "                <ColumnDefinition Width=\"16\" />\n" +
                                                          "                <ColumnDefinition Width=\"*\" />\n" +
                                                          "            </Grid.ColumnDefinitions>\n" +
                                                          "            <StackPanel Grid.Column=\"0\">\n" +
                                                          "                <TextBlock\n" +
                                                          "                    Margin=\"0,0,0,6\"\n" +
                                                          "                    Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                          "                    Text=\"Paused\" />\n" +
                                                          "                <ui:ProgressBar\n" +
                                                          "                    Height=\"8\"\n" +
                                                          "                    HorizontalAlignment=\"Stretch\"\n" +
                                                          "                    Maximum=\"100\"\n" +
                                                          "                    Minimum=\"0\"\n" +
                                                          "                    ProgressMode=\"Paused\"\n" +
                                                          "                    Value=\"62\" />\n" +
                                                          "            </StackPanel>\n" +
                                                          "            <StackPanel Grid.Column=\"2\">\n" +
                                                          "                <TextBlock\n" +
                                                          "                    Margin=\"0,0,0,6\"\n" +
                                                          "                    Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                          "                    Text=\"Failure\" />\n" +
                                                          "                <ui:ProgressBar\n" +
                                                          "                    Height=\"8\"\n" +
                                                          "                    HorizontalAlignment=\"Stretch\"\n" +
                                                          "                    Maximum=\"100\"\n" +
                                                          "                    Minimum=\"0\"\n" +
                                                          "                    ProgressMode=\"Error\"\n" +
                                                          "                    Value=\"78\" />\n" +
                                                          "            </StackPanel>\n" +
                                                          "        </Grid>\n" +
                                                          "    </StackPanel>\n" +
                                                          "</UserControl>\n";

        private const string ProgressBarValueCSharpSource = "using System.Windows.Controls;\n" +
                                                            "\n" +
                                                            "namespace Fluence.Wpf.Demo.Pages.Status\n" +
                                                            "{\n" +
                                                            "    public partial class ProgressBarValue : UserControl\n" +
                                                            "    {\n" +
                                                            "        public ProgressBarValue()\n" +
                                                            "        {\n" +
                                                            "            InitializeComponent();\n" +
                                                            "        }\n" +
                                                            "\n" +
                                                            "    }\n" +
                                                            "}\n";
        private const string ProgressBarIndeterminateXamlSource = "<UserControl\n" +
                                                                  "    x:Class=\"Fluence.Wpf.Demo.Pages.Status.ProgressBarIndeterminate\"\n" +
                                                                  "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                                  "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                                  "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                                  "    <StackPanel>\n" +
                                                                  "        <ui:ProgressBar\n" +
                                                                  "            x:Name=\"IndeterminateProgressBar\"\n" +
                                                                  "            Height=\"8\"\n" +
                                                                  "            Margin=\"0,0,0,12\"\n" +
                                                                  "            HorizontalAlignment=\"Stretch\"\n" +
                                                                  "            ProgressMode=\"Indeterminate\" />\n" +
                                                                  "        <StackPanel Orientation=\"Horizontal\">\n" +
                                                                  "            <ui:ToggleSwitch\n" +
                                                                  "                x:Name=\"IndeterminateToggle\"\n" +
                                                                  "                Margin=\"0,0,12,0\"\n" +
                                                                  "                Checked=\"IndeterminateToggle_Toggled\"\n" +
                                                                  "                HorizontalAlignment=\"Center\"\n" +
                                                                  "                VerticalAlignment=\"Center\"\n" +
                                                                  "                IsChecked=\"True\"\n" +
                                                                  "                OffContent=\"On / Off\"\n" +
                                                                  "                OnContent=\"On / Off\"\n" +
                                                                  "                Unchecked=\"IndeterminateToggle_Toggled\" />\n" +
                                                                  "        </StackPanel>\n" +
                                                                  "    </StackPanel>\n" +
                                                                  "</UserControl>\n";

        private const string ProgressBarIndeterminateCSharpSource = "using System.Windows;\n" +
                                                                    "using System.Windows.Controls;\n" +
                                                                    "using Fluence.Wpf;\n" +
                                                                    "\n" +
                                                                    "namespace Fluence.Wpf.Demo.Pages.Status\n" +
                                                                    "{\n" +
                                                                    "    public partial class ProgressBarIndeterminate : UserControl\n" +
                                                                    "    {\n" +
                                                                    "        public ProgressBarIndeterminate()\n" +
                                                                    "        {\n" +
                                                                    "            InitializeComponent();\n" +
                                                                    "        }\n" +
                                                                    "\n" +
                                                                    "        private void IndeterminateToggle_Toggled(object sender, RoutedEventArgs e)\n" +
                                                                    "        {\n" +
                                                                    "            if (IndeterminateProgressBar is null || IndeterminateToggle is null)\n" +
                                                                    "            {\n" +
                                                                    "                return;\n" +
                                                                    "            }\n" +
                                                                    "\n" +
                                                                    "            IndeterminateProgressBar.ProgressMode = IndeterminateToggle.IsChecked == true\n" +
                                                                    "                ? ProgressBarMode.Indeterminate\n" +
                                                                    "                : ProgressBarMode.Standard;\n" +
                                                                    "        }\n" +
                                                                    "    }\n" +
                                                                    "}\n";
        private const string ProgressBarStepsXamlSource = "<UserControl\n" +
                                                          "    x:Class=\"Fluence.Wpf.Demo.Pages.Status.ProgressBarSteps\"\n" +
                                                          "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                          "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                          "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                          "    <StackPanel>\n" +
                                                          "        <ui:ProgressBar\n" +
                                                          "            x:Name=\"StepProgressBar\"\n" +
                                                          "            Height=\"8\"\n" +
                                                          "            Margin=\"0,0,0,12\"\n" +
                                                          "            HorizontalAlignment=\"Stretch\"\n" +
                                                          "            CurrentStep=\"1\"\n" +
                                                          "            ProgressMode=\"StepProgress\"\n" +
                                                          "            Steps=\"10\" />\n" +
                                                          "        <StackPanel Orientation=\"Horizontal\">\n" +
                                                          "            <ui:Button\n" +
                                                          "                Margin=\"0,0,12,0\"\n" +
                                                          "                Click=\"ProgressStep_Click\"\n" +
                                                          "                Content=\"Back\"\n" +
                                                          "                Tag=\"Back\" />\n" +
                                                          "            <ui:Button\n" +
                                                          "                Margin=\"0,0,16,0\"\n" +
                                                          "                Appearance=\"Accent\"\n" +
                                                          "                Click=\"ProgressStep_Click\"\n" +
                                                          "                Content=\"Next\"\n" +
                                                          "                Tag=\"Next\" />\n" +
                                                          "            <TextBlock\n" +
                                                          "                x:Name=\"StepLabel\"\n" +
                                                          "                VerticalAlignment=\"Center\"\n" +
                                                          "                Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                          "                Text=\"Step 1 of 10\" />\n" +
                                                          "        </StackPanel>\n" +
                                                          "    </StackPanel>\n" +
                                                          "</UserControl>\n";

        private const string ProgressBarStepsCSharpSource = "using System;\n" +
                                                            "using System.Windows;\n" +
                                                            "using System.Windows.Controls;\n" +
                                                            "\n" +
                                                            "namespace Fluence.Wpf.Demo.Pages.Status\n" +
                                                            "{\n" +
                                                            "    public partial class ProgressBarSteps : UserControl\n" +
                                                            "    {\n" +
                                                            "        public ProgressBarSteps()\n" +
                                                            "        {\n" +
                                                            "            InitializeComponent();\n" +
                                                            "        }\n" +
                                                            "\n" +
                                                            "        private void ProgressStep_Click(object sender, RoutedEventArgs e)\n" +
                                                            "        {\n" +
                                                            "            string tag = sender is FrameworkElement button && button.Tag is not null ? button.Tag.ToString() : string.Empty;\n" +
                                                            "\n" +
                                                            "            if (string.Equals(tag, \"Next\", StringComparison.OrdinalIgnoreCase))\n" +
                                                            "            {\n" +
                                                            "                if (StepProgressBar.CurrentStep < StepProgressBar.Steps)\n" +
                                                            "                {\n" +
                                                            "                    StepProgressBar.CurrentStep++;\n" +
                                                            "                }\n" +
                                                            "            }\n" +
                                                            "            else if (StepProgressBar.CurrentStep > 0)\n" +
                                                            "            {\n" +
                                                            "                StepProgressBar.CurrentStep--;\n" +
                                                            "            }\n" +
                                                            "\n" +
                                                            "            StepLabel.Text = string.Format(\"Step {0} of {1}\", StepProgressBar.CurrentStep, StepProgressBar.Steps);\n" +
                                                            "        }\n" +
                                                            "    }\n" +
                                                            "}\n";
        private const string ProgressRingsXamlSource = "<UserControl\n" +
                                                       "    x:Class=\"Fluence.Wpf.Demo.Pages.Status.ProgressRings\"\n" +
                                                       "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                       "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                       "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\"\n" +
                                                       "    xmlns:uicore=\"clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf\">\n" +
                                                       "    <Grid HorizontalAlignment=\"Stretch\">\n" +
                                                       "        <Grid.ColumnDefinitions>\n" +
                                                       "            <ColumnDefinition Width=\"*\" />\n" +
                                                       "            <ColumnDefinition Width=\"*\" />\n" +
                                                       "            <ColumnDefinition Width=\"*\" />\n" +
                                                       "            <ColumnDefinition Width=\"*\" />\n" +
                                                       "        </Grid.ColumnDefinitions>\n" +
                                                       "        <Grid.RowDefinitions>\n" +
                                                       "            <RowDefinition Height=\"Auto\" />\n" +
                                                       "            <RowDefinition Height=\"Auto\" />\n" +
                                                       "            <RowDefinition Height=\"Auto\" />\n" +
                                                       "        </Grid.RowDefinitions>\n" +
                                                       "\n" +
                                                       "        <ui:ProgressRing\n" +
                                                       "            x:Name=\"IndeterminateProgressRing\"\n" +
                                                       "            Grid.Row=\"0\"\n" +
                                                       "            Grid.Column=\"0\"\n" +
                                                       "            Width=\"48\"\n" +
                                                       "            Height=\"48\"\n" +
                                                       "            HorizontalAlignment=\"Center\"\n" +
                                                       "            IsActive=\"True\"\n" +
                                                       "            IsIndeterminate=\"True\" />\n" +
                                                       "        <ui:ToggleSwitch\n" +
                                                       "            Grid.Row=\"1\"\n" +
                                                       "            Grid.Column=\"0\"\n" +
                                                       "            Margin=\"0,12,0,0\"\n" +
                                                       "            HorizontalAlignment=\"Center\"\n" +
                                                       "            VerticalAlignment=\"Center\"\n" +
                                                       "            IsChecked=\"{Binding IsActive, ElementName=IndeterminateProgressRing, Mode=TwoWay}\"\n" +
                                                       "            OffContent=\"On / Off\"\n" +
                                                       "            OnContent=\"On / Off\" />\n" +
                                                       "        <TextBlock\n" +
                                                       "            x:Name=\"IndeterminateProgressRingLabel\"\n" +
                                                       "            Grid.Row=\"2\"\n" +
                                                       "            Grid.Column=\"0\"\n" +
                                                       "            Margin=\"0,8,0,0\"\n" +
                                                       "            HorizontalAlignment=\"Center\"\n" +
                                                       "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                       "            Text=\"Indeterminate\" />\n" +
                                                       "\n" +
                                                       "        <ui:ProgressRing\n" +
                                                       "            x:Name=\"DeterminateProgressRing\"\n" +
                                                       "            Grid.Row=\"0\"\n" +
                                                       "            Grid.Column=\"1\"\n" +
                                                       "            Width=\"48\"\n" +
                                                       "            Height=\"48\"\n" +
                                                       "            HorizontalAlignment=\"Center\"\n" +
                                                       "            IsActive=\"True\"\n" +
                                                       "            IsIndeterminate=\"False\"\n" +
                                                       "            Maximum=\"100\"\n" +
                                                       "            Minimum=\"0\"\n" +
                                                       "            Value=\"50\" />\n" +
                                                       "        <ui:NumberBox\n" +
                                                       "            x:Name=\"ProgressRingValueBox\"\n" +
                                                       "            Grid.Row=\"1\"\n" +
                                                       "            Grid.Column=\"1\"\n" +
                                                       "            Width=\"132\"\n" +
                                                       "            Margin=\"0,24,0,0\"\n" +
                                                       "            HorizontalAlignment=\"Center\"\n" +
                                                       "            VerticalAlignment=\"Center\"\n" +
                                                       "            Maximum=\"100\"\n" +
                                                       "            Minimum=\"1\"\n" +
                                                       "            SmallChange=\"1\"\n" +
                                                       "            SpinButtonPlacementMode=\"{x:Static uicore:SpinButtonPlacementMode.Inline}\"\n" +
                                                       "            Value=\"{Binding Value, ElementName=DeterminateProgressRing, Mode=TwoWay}\" />\n" +
                                                       "        <TextBlock\n" +
                                                       "            x:Name=\"DeterminateProgressRingLabel\"\n" +
                                                       "            Grid.Row=\"2\"\n" +
                                                       "            Grid.Column=\"1\"\n" +
                                                       "            Margin=\"0,8,0,0\"\n" +
                                                       "            HorizontalAlignment=\"Center\"\n" +
                                                       "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                       "            Text=\"Determinate\" />\n" +
                                                       "\n" +
                                                       "        <ui:ProgressRing\n" +
                                                       "            x:Name=\"PausedProgressRing\"\n" +
                                                       "            Grid.Row=\"0\"\n" +
                                                       "            Grid.Column=\"2\"\n" +
                                                       "            Width=\"48\"\n" +
                                                       "            Height=\"48\"\n" +
                                                       "            HorizontalAlignment=\"Center\"\n" +
                                                       "            IsActive=\"True\"\n" +
                                                       "            IsIndeterminate=\"False\"\n" +
                                                       "            Maximum=\"100\"\n" +
                                                       "            Minimum=\"0\"\n" +
                                                       "            ProgressState=\"{x:Static uicore:ProgressRingState.Paused}\"\n" +
                                                       "            Value=\"80\" />\n" +
                                                       "        <TextBlock\n" +
                                                       "            Grid.Row=\"2\"\n" +
                                                       "            Grid.Column=\"2\"\n" +
                                                       "            Margin=\"0,8,0,0\"\n" +
                                                       "            HorizontalAlignment=\"Center\"\n" +
                                                       "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                       "            Text=\"Paused\" />\n" +
                                                       "\n" +
                                                       "        <ui:ProgressRing\n" +
                                                       "            x:Name=\"ErrorProgressRing\"\n" +
                                                       "            Grid.Row=\"0\"\n" +
                                                       "            Grid.Column=\"3\"\n" +
                                                       "            Width=\"48\"\n" +
                                                       "            Height=\"48\"\n" +
                                                       "            HorizontalAlignment=\"Center\"\n" +
                                                       "            IsActive=\"True\"\n" +
                                                       "            IsIndeterminate=\"False\"\n" +
                                                       "            Maximum=\"100\"\n" +
                                                       "            Minimum=\"0\"\n" +
                                                       "            ProgressState=\"{x:Static uicore:ProgressRingState.Error}\"\n" +
                                                       "            Value=\"80\" />\n" +
                                                       "        <TextBlock\n" +
                                                       "            Grid.Row=\"2\"\n" +
                                                       "            Grid.Column=\"3\"\n" +
                                                       "            Margin=\"0,8,0,0\"\n" +
                                                       "            HorizontalAlignment=\"Center\"\n" +
                                                       "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                       "            Text=\"Error\" />\n" +
                                                       "    </Grid>\n" +
                                                       "</UserControl>\n";

        private const string ProgressRingsCSharpSource = "using System.Windows.Controls;\n" +
                                                         "\n" +
                                                         "namespace Fluence.Wpf.Demo.Pages.Status\n" +
                                                         "{\n" +
                                                         "    public partial class ProgressRings : UserControl\n" +
                                                         "    {\n" +
                                                         "        public ProgressRings()\n" +
                                                         "        {\n" +
                                                         "            InitializeComponent();\n" +
                                                         "        }\n" +
                                                         "\n" +
                                                         "    }\n" +
                                                         "}\n";
        private const string InfoBarsXamlSource = "<UserControl\n" +
                                                  "    x:Class=\"Fluence.Wpf.Demo.Pages.Status.InfoBars\"\n" +
                                                  "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                  "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                  "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                  "    <StackPanel>\n" +
                                                  "        <ui:InfoBar\n" +
                                                  "            x:Name=\"InfoBarInformational\"\n" +
                                                  "            Title=\"Informational\"\n" +
                                                  "            Margin=\"0,0,0,8\"\n" +
                                                  "            HorizontalAlignment=\"Stretch\"\n" +
                                                  "            IsOpen=\"True\"\n" +
                                                  "            Message=\"This is a general information message.\"\n" +
                                                  "            Severity=\"Informational\" />\n" +
                                                  "        <ui:InfoBar\n" +
                                                  "            x:Name=\"InfoBarSuccess\"\n" +
                                                  "            Title=\"Success\"\n" +
                                                  "            Margin=\"0,0,0,8\"\n" +
                                                  "            HorizontalAlignment=\"Stretch\"\n" +
                                                  "            IsOpen=\"True\"\n" +
                                                  "            Message=\"The operation completed successfully.\"\n" +
                                                  "            Severity=\"Success\" />\n" +
                                                  "        <ui:InfoBar\n" +
                                                  "            x:Name=\"InfoBarWarning\"\n" +
                                                  "            Title=\"Warning\"\n" +
                                                  "            Margin=\"0,0,0,8\"\n" +
                                                  "            HorizontalAlignment=\"Stretch\"\n" +
                                                  "            IsOpen=\"True\"\n" +
                                                  "            Message=\"Proceed with caution.\"\n" +
                                                  "            Severity=\"Warning\" />\n" +
                                                  "        <ui:InfoBar\n" +
                                                  "            x:Name=\"InfoBarError\"\n" +
                                                  "            Title=\"Error\"\n" +
                                                  "            Margin=\"0,0,0,8\"\n" +
                                                  "            HorizontalAlignment=\"Stretch\"\n" +
                                                  "            IsOpen=\"True\"\n" +
                                                  "            Message=\"Something went wrong.\"\n" +
                                                  "            Severity=\"Error\">\n" +
                                                  "            <ui:InfoBar.ActionButton>\n" +
                                                  "                <ui:Button Appearance=\"Accent\" Content=\"Retry\" />\n" +
                                                  "            </ui:InfoBar.ActionButton>\n" +
                                                  "        </ui:InfoBar>\n" +
                                                  "        <ui:Button\n" +
                                                  "            Margin=\"0,4,0,0\"\n" +
                                                  "            HorizontalAlignment=\"Left\"\n" +
                                                  "            Click=\"ResetInfoBars_Click\"\n" +
                                                  "            Content=\"Reset All InfoBars\" />\n" +
                                                  "    </StackPanel>\n" +
                                                  "</UserControl>\n";

        private const string InfoBarsCSharpSource = "using System.Windows;\n" +
                                                    "using System.Windows.Controls;\n" +
                                                    "\n" +
                                                    "namespace Fluence.Wpf.Demo.Pages.Status\n" +
                                                    "{\n" +
                                                    "    public partial class InfoBars : UserControl\n" +
                                                    "    {\n" +
                                                    "        public InfoBars()\n" +
                                                    "        {\n" +
                                                    "            InitializeComponent();\n" +
                                                    "        }\n" +
                                                    "\n" +
                                                    "        private void ResetInfoBars_Click(object sender, RoutedEventArgs e)\n" +
                                                    "        {\n" +
                                                    "            InfoBarInformational.IsOpen = true;\n" +
                                                    "            InfoBarSuccess.IsOpen = true;\n" +
                                                    "            InfoBarWarning.IsOpen = true;\n" +
                                                    "            InfoBarError.IsOpen = true;\n" +
                                                    "        }\n" +
                                                    "    }\n" +
                                                    "}\n";

        public GalleryStatusPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, ProgressBarValueXamlSource, ProgressBarValueCSharpSource),
                new DemoSampleSource(2, ProgressBarIndeterminateXamlSource, ProgressBarIndeterminateCSharpSource),
                new DemoSampleSource(3, ProgressBarStepsXamlSource, ProgressBarStepsCSharpSource),
                new DemoSampleSource(4, ProgressRingsXamlSource, ProgressRingsCSharpSource),
                new DemoSampleSource(5, InfoBarsXamlSource, InfoBarsCSharpSource));

            Loaded += GalleryStatusPage_Loaded;
        }

        private void GalleryStatusPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GalleryStatusPage_Loaded;
            IndeterminateToggle_Toggled(sender: null, e: null);
        }

        private void IndeterminateToggle_Toggled(object? sender, RoutedEventArgs? e)
        {
            if (!IsLoaded || IndeterminateProgressBar is null || IndeterminateToggle is null)
            {
                return;
            }

            IndeterminateProgressBar.ProgressMode = IndeterminateToggle.IsChecked is true
                ? ProgressBarMode.Indeterminate
                : ProgressBarMode.Standard;
        }

        private void ProgressStep_Click(object sender, RoutedEventArgs e)
        {
            if (StepProgressBar is null)
            {
                return;
            }

            FrameworkElement? button = sender as FrameworkElement;

            string tag = button?.Tag?.ToString() ?? string.Empty;
            if (string.Equals(tag, "Next", StringComparison.OrdinalIgnoreCase))
            {
                if (StepProgressBar.CurrentStep < StepProgressBar.Steps)
                {
                    StepProgressBar.CurrentStep++;
                }
            }
            else if (StepProgressBar.CurrentStep > 0)
            {
                StepProgressBar.CurrentStep--;
            }

            StepLabel.Text = string.Format(CultureInfo.CurrentCulture, "Step {0} of {1}", StepProgressBar.CurrentStep, StepProgressBar.Steps);
        }

        private void ResetInfoBars_Click(object sender, RoutedEventArgs e)
        {
            InfoBarInformational.IsOpen = true;
            InfoBarSuccess.IsOpen = true;
            InfoBarWarning.IsOpen = true;
            InfoBarError.IsOpen = true;
        }
    }
}
