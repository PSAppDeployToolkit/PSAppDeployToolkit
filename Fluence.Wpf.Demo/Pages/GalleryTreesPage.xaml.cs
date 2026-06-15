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

using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryTreesPage : UserControl
    {
        private const string TreeViewHierarchyXamlSource = "<UserControl\n" +
                                                           "    x:Class=\"Fluence.Wpf.Demo.Pages.Trees.TreeViewHierarchy\"\n" +
                                                           "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                           "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                           "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                           "    <ui:TreeView\n" +
                                                           "        x:Name=\"HierarchyTreeView\"\n" +
                                                           "        MaxHeight=\"260\"\n" +
                                                           "        BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                           "        BorderThickness=\"1\">\n" +
                                                           "        <ui:TreeViewItem\n" +
                                                           "            Header=\"Workspace\"\n" +
                                                           "            IsExpanded=\"True\">\n" +
                                                           "            <ui:TreeViewItem Header=\"Pages\" IsExpanded=\"True\">\n" +
                                                           "                <ui:TreeViewItem Header=\"GalleryButtonsPage.xaml\" />\n" +
                                                           "                <ui:TreeViewItem Header=\"GalleryTreesPage.xaml\" />\n" +
                                                           "                <ui:TreeViewItem Header=\"GalleryDataPage.xaml\" />\n" +
                                                           "            </ui:TreeViewItem>\n" +
                                                           "            <ui:TreeViewItem Header=\"Samples\">\n" +
                                                           "                <ui:TreeViewItem Header=\"Buttons\" />\n" +
                                                           "                <ui:TreeViewItem Header=\"Trees\" />\n" +
                                                           "            </ui:TreeViewItem>\n" +
                                                           "        </ui:TreeViewItem>\n" +
                                                           "        <ui:TreeViewItem Header=\"Resources\">\n" +
                                                           "            <ui:TreeViewItem Header=\"DemoSharedStyles.xaml\" />\n" +
                                                           "        </ui:TreeViewItem>\n" +
                                                           "    </ui:TreeView>\n" +
                                                           "</UserControl>\n";

        private const string TreeViewHierarchyCSharpSource = "using System.Windows.Controls;\n" +
                                                             "\n" +
                                                             "namespace Fluence.Wpf.Demo.Pages.Trees\n" +
                                                             "{\n" +
                                                             "    public partial class TreeViewHierarchy : UserControl\n" +
                                                             "    {\n" +
                                                             "        public TreeViewHierarchy()\n" +
                                                             "        {\n" +
                                                             "            InitializeComponent();\n" +
                                                             "        }\n" +
                                                             "    }\n" +
                                                             "}\n";
        private const string TreeViewSelectionXamlSource = "<UserControl\n" +
                                                           "    x:Class=\"Fluence.Wpf.Demo.Pages.Trees.TreeViewSelection\"\n" +
                                                           "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                           "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                           "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                           "    <StackPanel>\n" +
                                                           "        <ui:TreeView\n" +
                                                           "            x:Name=\"SelectionTreeView\"\n" +
                                                           "            MaxHeight=\"260\"\n" +
                                                           "            Margin=\"0,0,0,12\"\n" +
                                                           "            BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                           "            BorderThickness=\"1\">\n" +
                                                           "            <ui:TreeViewItem\n" +
                                                           "                Header=\"Inbox\"\n" +
                                                           "                IsExpanded=\"True\">\n" +
                                                           "                <ui:TreeViewItem Header=\"Priority\" IsExpanded=\"True\">\n" +
                                                           "                    <ui:TreeViewItem Header=\"Contract review\" />\n" +
                                                           "                    <ui:TreeViewItem Header=\"Customer follow-up\" />\n" +
                                                           "                </ui:TreeViewItem>\n" +
                                                           "                <ui:TreeViewItem Header=\"Later\">\n" +
                                                           "                    <ui:TreeViewItem Header=\"Design notes\" />\n" +
                                                           "                    <ui:TreeViewItem Header=\"Release checklist\" />\n" +
                                                           "                </ui:TreeViewItem>\n" +
                                                           "            </ui:TreeViewItem>\n" +
                                                           "            <ui:TreeViewItem Header=\"Archive\">\n" +
                                                           "                <ui:TreeViewItem Header=\"March\" />\n" +
                                                           "                <ui:TreeViewItem Header=\"April\" />\n" +
                                                           "            </ui:TreeViewItem>\n" +
                                                           "        </ui:TreeView>\n" +
                                                           "    </StackPanel>\n" +
                                                           "</UserControl>\n";

        private const string TreeViewSelectionCSharpSource = "using System.Windows.Controls;\n" +
                                                             "\n" +
                                                             "namespace Fluence.Wpf.Demo.Pages.Trees\n" +
                                                             "{\n" +
                                                             "    public partial class TreeViewSelection : UserControl\n" +
                                                             "    {\n" +
                                                             "        public TreeViewSelection()\n" +
                                                             "        {\n" +
                                                             "            InitializeComponent();\n" +
                                                             "        }\n" +
                                                             "    }\n" +
                                                             "}\n";
        private const string TreeViewMultiSelectXamlSource = "<UserControl\n" +
                                                             "    x:Class=\"Fluence.Wpf.Demo.Pages.Trees.TreeViewMultiSelect\"\n" +
                                                             "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                             "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                             "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\"\n" +
                                                             "    xmlns:uicore=\"clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf\">\n" +
                                                             "    <ui:TreeView\n" +
                                                             "        x:Name=\"MultiSelectTreeView\"\n" +
                                                             "        MaxHeight=\"260\"\n" +
                                                             "        BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                             "        BorderThickness=\"1\"\n" +
                                                             "        SelectionMode=\"{x:Static uicore:TreeViewSelectionMode.Multiple}\">\n" +
                                                             "        <ui:TreeViewItem\n" +
                                                             "            Header=\"Documents\"\n" +
                                                             "            IsExpanded=\"True\">\n" +
                                                             "            <ui:TreeViewItem Header=\"Contracts\" />\n" +
                                                             "            <ui:TreeViewItem Header=\"Invoices\" />\n" +
                                                             "            <ui:TreeViewItem Header=\"Receipts\" />\n" +
                                                             "        </ui:TreeViewItem>\n" +
                                                             "        <ui:TreeViewItem\n" +
                                                             "            Header=\"Pictures\"\n" +
                                                             "            IsExpanded=\"True\">\n" +
                                                             "            <ui:TreeViewItem Header=\"Screenshots\" />\n" +
                                                             "            <ui:TreeViewItem Header=\"Archive\" />\n" +
                                                             "        </ui:TreeViewItem>\n" +
                                                             "    </ui:TreeView>\n" +
                                                             "</UserControl>\n";

        private const string TreeViewMultiSelectCSharpSource = "using System.Windows.Controls;\n" +
                                                               "\n" +
                                                               "namespace Fluence.Wpf.Demo.Pages.Trees\n" +
                                                               "{\n" +
                                                               "    public partial class TreeViewMultiSelect : UserControl\n" +
                                                               "    {\n" +
                                                               "        public TreeViewMultiSelect()\n" +
                                                               "        {\n" +
                                                               "            InitializeComponent();\n" +
                                                               "        }\n" +
                                                               "    }\n" +
                                                               "}\n";
        private const string TreeViewExpansionXamlSource = "<UserControl\n" +
                                                           "    x:Class=\"Fluence.Wpf.Demo.Pages.Trees.TreeViewExpansion\"\n" +
                                                           "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                           "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                           "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                           "    <StackPanel>\n" +
                                                           "        <ui:TreeView\n" +
                                                           "            x:Name=\"ExpansionTreeView\"\n" +
                                                           "            MaxHeight=\"260\"\n" +
                                                           "            Margin=\"0,0,0,12\"\n" +
                                                           "            BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                           "            BorderThickness=\"1\">\n" +
                                                           "            <ui:TreeViewItem\n" +
                                                           "                Header=\"Project\"\n" +
                                                           "                IsExpanded=\"True\">\n" +
                                                           "                <ui:TreeViewItem Header=\"Source\" IsExpanded=\"True\">\n" +
                                                           "                    <ui:TreeViewItem Header=\"Controls\" />\n" +
                                                           "                    <ui:TreeViewItem Header=\"Themes\" />\n" +
                                                           "                </ui:TreeViewItem>\n" +
                                                           "                <ui:TreeViewItem Header=\"Tests\">\n" +
                                                           "                    <ui:TreeViewItem Header=\"Control tests\" />\n" +
                                                           "                    <ui:TreeViewItem Header=\"Demo tests\" />\n" +
                                                           "                </ui:TreeViewItem>\n" +
                                                           "            </ui:TreeViewItem>\n" +
                                                           "        </ui:TreeView>\n" +
                                                           "        <StackPanel\n" +
                                                           "            x:Name=\"TreeExpansionActionsPanel\"\n" +
                                                           "            HorizontalAlignment=\"Center\"\n" +
                                                           "            VerticalAlignment=\"Center\"\n" +
                                                           "            Orientation=\"Horizontal\">\n" +
                                                           "            <ui:Button\n" +
                                                           "                Margin=\"0,0,8,0\"\n" +
                                                           "                Click=\"ExpandAll_Click\"\n" +
                                                           "                Content=\"Expand all\"\n" +
                                                           "                MinWidth=\"140\" />\n" +
                                                           "            <ui:Button\n" +
                                                           "                Click=\"CollapseAll_Click\"\n" +
                                                           "                Content=\"Collapse all\"\n" +
                                                           "                MinWidth=\"140\" />\n" +
                                                           "        </StackPanel>\n" +
                                                           "    </StackPanel>\n" +
                                                           "</UserControl>\n";

        private const string TreeViewExpansionCSharpSource = "using System.Windows;\n" +
                                                             "using System.Windows.Controls;\n" +
                                                             "\n" +
                                                             "namespace Fluence.Wpf.Demo.Pages.Trees\n" +
                                                             "{\n" +
                                                             "    public partial class TreeViewExpansion : UserControl\n" +
                                                             "    {\n" +
                                                             "        public TreeViewExpansion()\n" +
                                                             "        {\n" +
                                                             "            InitializeComponent();\n" +
                                                             "        }\n" +
                                                             "\n" +
                                                             "        private void ExpandAll_Click(object sender, RoutedEventArgs e)\n" +
                                                             "        {\n" +
                                                             "            SetExpanded(ExpansionTreeView.Items, true);\n" +
                                                             "        }\n" +
                                                             "\n" +
                                                             "        private void CollapseAll_Click(object sender, RoutedEventArgs e)\n" +
                                                             "        {\n" +
                                                             "            SetExpanded(ExpansionTreeView.Items, false);\n" +
                                                             "        }\n" +
                                                             "\n" +
                                                             "        private static void SetExpanded(ItemCollection items, bool expanded)\n" +
                                                             "        {\n" +
                                                             "            foreach (object obj in items)\n" +
                                                             "            {\n" +
                                                             "                if (obj is not Fluence.Wpf.Controls.TreeViewItem item)\n" +
                                                             "                {\n" +
                                                             "                    continue;\n" +
                                                             "                }\n" +
                                                             "\n" +
                                                             "                item.IsExpanded = expanded;\n" +
                                                             "                SetExpanded(item.Items, expanded);\n" +
                                                             "            }\n" +
                                                             "        }\n" +
                                                             "    }\n" +
                                                             "}\n";

        public GalleryTreesPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, TreeViewHierarchyXamlSource, TreeViewHierarchyCSharpSource),
                new DemoSampleSource(2, TreeViewSelectionXamlSource, TreeViewSelectionCSharpSource),
                new DemoSampleSource(3, TreeViewMultiSelectXamlSource, TreeViewMultiSelectCSharpSource),
                new DemoSampleSource(4, TreeViewExpansionXamlSource, TreeViewExpansionCSharpSource));
        }

        private void ExpandAll_Click(object sender, RoutedEventArgs e)
        {
            if (ExpansionTreeView is null)
            {
                return;
            }

            SetExpanded(ExpansionTreeView.Items, expanded: true);
        }

        private void CollapseAll_Click(object sender, RoutedEventArgs e)
        {
            if (ExpansionTreeView is null)
            {
                return;
            }

            SetExpanded(ExpansionTreeView.Items, expanded: false);
        }

        private static void SetExpanded(ItemCollection items, bool expanded)
        {
            foreach (object? obj in items)
            {
                if (obj is not Controls.TreeViewItem tvi)
                {
                    continue;
                }

                tvi.IsExpanded = expanded;
                if (tvi.Items.Count > 0)
                {
                    SetExpanded(tvi.Items, expanded);
                }
            }
        }
    }
}
