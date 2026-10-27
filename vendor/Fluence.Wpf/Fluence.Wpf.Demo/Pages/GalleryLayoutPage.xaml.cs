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
    public partial class GalleryLayoutPage : Page
    {
        private const string BorderStackPanelXamlSource = "<!-- Intentionally partial layout snippet for a page that already declares the Fluence xmlns. -->\n" +
                                                          "<fluence:Border\n" +
                                                          "    Padding=\"14\"\n" +
                                                          "    Background=\"{DynamicResource CardBackgroundFillColorSecondaryBrush}\"\n" +
                                                          "    BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                          "    BorderThickness=\"1\"\n" +
                                                          "    CornerRadius=\"8\">\n" +
                                                          "    <fluence:StackPanel Spacing=\"10\">\n" +
                                                          "        <TextBlock Style=\"{StaticResource BodyStrongTextBlockStyle}\"\n" +
                                                          "                   Text=\"Settings group\" />\n" +
                                                          "        <TextBlock Text=\"StackPanel spacing keeps rows readable while Border frames the group.\"\n" +
                                                          "                   TextWrapping=\"Wrap\" />\n" +
                                                          "        <fluence:Separator />\n" +
                                                          "        <TextBlock Text=\"Separator divides related rows.\" />\n" +
                                                          "    </fluence:StackPanel>\n" +
                                                          "</fluence:Border>";

        private const string DockPanelXamlSource = "<!-- Intentionally partial layout snippet for a page that already declares the Fluence xmlns. -->\n" +
                                                   "<fluence:DockPanel LastChildFill=\"True\" Spacing=\"12\">\n" +
                                                   "    <fluence:Button DockPanel.Dock=\"Right\"\n" +
                                                   "               Appearance=\"Accent\"\n" +
                                                   "               Content=\"Apply\" />\n" +
                                                   "    <TextBlock VerticalAlignment=\"Center\"\n" +
                                                   "               Text=\"DockPanel keeps the command aligned to the edge.\" />\n" +
                                                   "</fluence:DockPanel>";

        private const string ExpanderXamlSource = "<!-- Intentionally partial layout snippet for a page that already declares the Fluence xmlns. -->\n" +
                                                  "<fluence:Expander\n" +
                                                  "    x:Name=\"AdvancedOptionsExpander\"\n" +
                                                  "    Header=\"Advanced options\">\n" +
                                                  "    <TextBlock Text=\"Expander shows secondary settings only when useful.\"\n" +
                                                  "               Margin=\"{DynamicResource DemoLargeTopGapMargin}\"\n" +
                                                  "               TextWrapping=\"Wrap\" />\n" +
                                                  "</fluence:Expander>";

        private const string DockPanelExpanderXamlSource = "<!-- Intentionally partial layout snippet for a page that already declares the Fluence xmlns. -->\n" +
                                                           "<fluence:Expander x:Name=\"DockPanelOptionsExpander\">\n" +
                                                           "    <fluence:Expander.Header>\n" +
                                                           "        <fluence:DockPanel LastChildFill=\"True\" Spacing=\"8\">\n" +
                                                           "            <fluence:Button DockPanel.Dock=\"Right\"\n" +
                                                           "                       Content=\"Edit\" />\n" +
                                                           "            <TextBlock VerticalAlignment=\"Center\"\n" +
                                                           "                       Text=\"Delivery options\" />\n" +
                                                           "        </fluence:DockPanel>\n" +
                                                           "    </fluence:Expander.Header>\n" +
                                                           "    <fluence:DockPanel LastChildFill=\"True\" Spacing=\"8\">\n" +
                                                           "        <fluence:ToggleSwitch DockPanel.Dock=\"Right\"\n" +
                                                           "                         OffContent=\"Off\"\n" +
                                                           "                         OnContent=\"On\" />\n" +
                                                           "        <TextBlock VerticalAlignment=\"Center\"\n" +
                                                           "                   Text=\"Notify me when the package ships.\"\n" +
                                                           "                   TextWrapping=\"Wrap\" />\n" +
                                                           "    </fluence:DockPanel>\n" +
                                                           "</fluence:Expander>";

        private const string SmoothScrollViewerXamlSource = "<!-- Intentionally partial layout snippet for a page that already declares the Fluence xmlns. -->\n" +
                                                              "<fluence:SmoothScrollViewer Height=\"180\"\n" +
                                                              "                            HorizontalScrollBarVisibility=\"Disabled\"\n" +
                                                              "                            VerticalScrollBarVisibility=\"Visible\">\n" +
                                                              "    <fluence:StackPanel Spacing=\"8\">\n" +
                                                              "        <TextBlock Text=\"Scroll this list to see the remaining items.\" />\n" +
                                                              "        <TextBlock Text=\"Item 1 - Planning\" />\n" +
                                                              "        <TextBlock Text=\"Item 2 - Design\" />\n" +
                                                              "        <TextBlock Text=\"Item 3 - Build\" />\n" +
                                                              "        <TextBlock Text=\"Item 4 - Review\" />\n" +
                                                              "        <TextBlock Text=\"Item 5 - Release\" />\n" +
                                                              "        <TextBlock Text=\"Item 6 - Follow-up\" />\n" +
                                                              "        <TextBlock Text=\"Item 7 - Support\" />\n" +
                                                              "        <TextBlock Text=\"Item 8 - Archive\" />\n" +
                                                              "        <TextBlock Text=\"End of the list\" />\n" +
                                                              "    </fluence:StackPanel>\n" +
                                                              "</fluence:SmoothScrollViewer>";

        public GalleryLayoutPage()
        {
            InitializeComponent();
            DemoSamplePageWiring.Apply(
                (System.Windows.DependencyObject)Content,
                new DemoSampleSource(1, BorderStackPanelXamlSource, string.Empty),
                new DemoSampleSource(2, DockPanelXamlSource, string.Empty),
                new DemoSampleSource(3, ExpanderXamlSource, string.Empty),
                new DemoSampleSource(4, DockPanelExpanderXamlSource, string.Empty),
                new DemoSampleSource(5, SmoothScrollViewerXamlSource, string.Empty));
        }
    }
}
