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
    public partial class GalleryLayoutPage : UserControl
    {
        private const string BorderStackPanelXamlSource = "<!-- Intentionally partial layout snippet for a page that already declares the Fluence xmlns. -->\n" +
                                                          "<ui:Border\n" +
                                                          "    Padding=\"14\"\n" +
                                                          "    Background=\"{DynamicResource CardBackgroundFillColorSecondaryBrush}\"\n" +
                                                          "    BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                          "    BorderThickness=\"1\"\n" +
                                                          "    CornerRadius=\"8\">\n" +
                                                          "    <ui:StackPanel Spacing=\"10\">\n" +
                                                          "        <TextBlock Style=\"{StaticResource BodyStrongTextBlockStyle}\"\n" +
                                                          "                   Text=\"Settings group\" />\n" +
                                                          "        <TextBlock Text=\"StackPanel spacing keeps rows readable while Border frames the group.\"\n" +
                                                          "                   TextWrapping=\"Wrap\" />\n" +
                                                          "        <ui:Separator />\n" +
                                                          "        <TextBlock Text=\"Separator divides related rows.\" />\n" +
                                                          "    </ui:StackPanel>\n" +
                                                          "</ui:Border>";

        private const string DockPanelXamlSource = "<!-- Intentionally partial layout snippet for a page that already declares the Fluence xmlns. -->\n" +
                                                   "<DockPanel LastChildFill=\"True\">\n" +
                                                   "    <ui:Button DockPanel.Dock=\"Right\"\n" +
                                                   "               Appearance=\"Accent\"\n" +
                                                   "               Content=\"Apply\" />\n" +
                                                   "    <TextBlock VerticalAlignment=\"Center\"\n" +
                                                   "               Text=\"DockPanel keeps the command aligned to the edge.\" />\n" +
                                                   "</DockPanel>";

        private const string ExpanderXamlSource = "<!-- Intentionally partial layout snippet for a page that already declares the Fluence xmlns. -->\n" +
                                                  "<ui:Expander\n" +
                                                  "    x:Name=\"AdvancedOptionsExpander\"\n" +
                                                  "    Header=\"Advanced options\">\n" +
                                                  "    <TextBlock Text=\"Expander shows secondary settings only when useful.\"\n" +
                                                  "               Margin=\"{DynamicResource DemoLargeTopGapMargin}\"\n" +
                                                  "               TextWrapping=\"Wrap\" />\n" +
                                                  "</ui:Expander>";

        private const string DockPanelExpanderXamlSource = "<!-- Intentionally partial layout snippet for a page that already declares the Fluence xmlns. -->\n" +
                                                           "<ui:Expander x:Name=\"DockPanelOptionsExpander\">\n" +
                                                           "    <ui:Expander.Header>\n" +
                                                           "        <DockPanel LastChildFill=\"True\">\n" +
                                                           "            <ui:Button DockPanel.Dock=\"Right\"\n" +
                                                           "                       Content=\"Edit\" />\n" +
                                                           "            <TextBlock VerticalAlignment=\"Center\"\n" +
                                                           "                       Text=\"Delivery options\" />\n" +
                                                           "        </DockPanel>\n" +
                                                           "    </ui:Expander.Header>\n" +
                                                           "    <DockPanel LastChildFill=\"True\">\n" +
                                                           "        <ui:ToggleSwitch DockPanel.Dock=\"Right\"\n" +
                                                           "                         OffContent=\"Off\"\n" +
                                                           "                         OnContent=\"On\" />\n" +
                                                           "        <TextBlock VerticalAlignment=\"Center\"\n" +
                                                           "                   Text=\"Notify me when the package ships.\"\n" +
                                                           "                   TextWrapping=\"Wrap\" />\n" +
                                                           "    </DockPanel>\n" +
                                                           "</ui:Expander>";

        public GalleryLayoutPage()
        {
            InitializeComponent();
            DemoSamplePageWiring.Apply(
                (System.Windows.DependencyObject)Content,
                new DemoSampleSource(1, BorderStackPanelXamlSource, string.Empty),
                new DemoSampleSource(2, DockPanelXamlSource, string.Empty),
                new DemoSampleSource(3, ExpanderXamlSource, string.Empty),
                new DemoSampleSource(4, DockPanelExpanderXamlSource, string.Empty));
        }
    }
}
