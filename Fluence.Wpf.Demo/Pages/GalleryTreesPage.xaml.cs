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
        private const string TreeViewHierarchyXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Trees.TreeViewHierarchy""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <ui:TreeView
        x:Name=""HierarchyTreeView""
        MaxHeight=""260""
        BorderBrush=""{DynamicResource CardStrokeColorDefaultBrush}""
        BorderThickness=""1"">
        <ui:TreeViewItem
            Header=""Workspace""
            IsExpanded=""True"">
            <ui:TreeViewItem Header=""Pages"" IsExpanded=""True"">
                <ui:TreeViewItem Header=""GalleryButtonsPage.xaml"" />
                <ui:TreeViewItem Header=""GalleryTreesPage.xaml"" />
                <ui:TreeViewItem Header=""GalleryDataPage.xaml"" />
            </ui:TreeViewItem>
            <ui:TreeViewItem Header=""Samples"">
                <ui:TreeViewItem Header=""Buttons"" />
                <ui:TreeViewItem Header=""Trees"" />
            </ui:TreeViewItem>
        </ui:TreeViewItem>
        <ui:TreeViewItem Header=""Resources"">
            <ui:TreeViewItem Header=""DemoSharedStyles.xaml"" />
        </ui:TreeViewItem>
    </ui:TreeView>
</UserControl>
";

        private const string TreeViewHierarchyCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Trees
{
    public partial class TreeViewHierarchy : UserControl
    {
        public TreeViewHierarchy()
        {
            InitializeComponent();
        }
    }
}
";
        private const string TreeViewSelectionXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Trees.TreeViewSelection""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <ui:TreeView
            x:Name=""SelectionTreeView""
            MaxHeight=""260""
            Margin=""0,0,0,12""
            BorderBrush=""{DynamicResource CardStrokeColorDefaultBrush}""
            BorderThickness=""1"">
            <ui:TreeViewItem
                Header=""Inbox""
                IsExpanded=""True"">
                <ui:TreeViewItem Header=""Priority"" IsExpanded=""True"">
                    <ui:TreeViewItem Header=""Contract review"" />
                    <ui:TreeViewItem Header=""Customer follow-up"" />
                </ui:TreeViewItem>
                <ui:TreeViewItem Header=""Later"">
                    <ui:TreeViewItem Header=""Design notes"" />
                    <ui:TreeViewItem Header=""Release checklist"" />
                </ui:TreeViewItem>
            </ui:TreeViewItem>
            <ui:TreeViewItem Header=""Archive"">
                <ui:TreeViewItem Header=""March"" />
                <ui:TreeViewItem Header=""April"" />
            </ui:TreeViewItem>
        </ui:TreeView>
    </StackPanel>
</UserControl>
";

        private const string TreeViewSelectionCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Trees
{
    public partial class TreeViewSelection : UserControl
    {
        public TreeViewSelection()
        {
            InitializeComponent();
        }
    }
}
";
        private const string TreeViewMultiSelectXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Trees.TreeViewMultiSelect""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf""
    xmlns:uicore=""clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf"">
    <ui:TreeView
        x:Name=""MultiSelectTreeView""
        MaxHeight=""260""
        BorderBrush=""{DynamicResource CardStrokeColorDefaultBrush}""
        BorderThickness=""1""
        SelectionMode=""{x:Static uicore:TreeViewSelectionMode.Multiple}"">
        <ui:TreeViewItem
            Header=""Documents""
            IsExpanded=""True"">
            <ui:TreeViewItem Header=""Contracts"" />
            <ui:TreeViewItem Header=""Invoices"" />
            <ui:TreeViewItem Header=""Receipts"" />
        </ui:TreeViewItem>
        <ui:TreeViewItem
            Header=""Pictures""
            IsExpanded=""True"">
            <ui:TreeViewItem Header=""Screenshots"" />
            <ui:TreeViewItem Header=""Archive"" />
        </ui:TreeViewItem>
    </ui:TreeView>
</UserControl>
";

        private const string TreeViewMultiSelectCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Trees
{
    public partial class TreeViewMultiSelect : UserControl
    {
        public TreeViewMultiSelect()
        {
            InitializeComponent();
        }
    }
}
";
        private const string TreeViewExpansionXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Trees.TreeViewExpansion""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <ui:TreeView
            x:Name=""ExpansionTreeView""
            MaxHeight=""260""
            Margin=""0,0,0,12""
            BorderBrush=""{DynamicResource CardStrokeColorDefaultBrush}""
            BorderThickness=""1"">
            <ui:TreeViewItem
                Header=""Project""
                IsExpanded=""True"">
                <ui:TreeViewItem Header=""Source"" IsExpanded=""True"">
                    <ui:TreeViewItem Header=""Controls"" />
                    <ui:TreeViewItem Header=""Themes"" />
                </ui:TreeViewItem>
                <ui:TreeViewItem Header=""Tests"">
                    <ui:TreeViewItem Header=""Control tests"" />
                    <ui:TreeViewItem Header=""Demo tests"" />
                </ui:TreeViewItem>
            </ui:TreeViewItem>
        </ui:TreeView>
        <StackPanel
            x:Name=""TreeExpansionActionsPanel""
            HorizontalAlignment=""Center""
            VerticalAlignment=""Center""
            Orientation=""Horizontal"">
            <ui:Button
                Margin=""0,0,8,0""
                Click=""ExpandAll_Click""
                Content=""Expand all""
                MinWidth=""140"" />
            <ui:Button
                Click=""CollapseAll_Click""
                Content=""Collapse all""
                MinWidth=""140"" />
        </StackPanel>
    </StackPanel>
</UserControl>
";

        private const string TreeViewExpansionCSharpSource = @"using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Trees
{
    public partial class TreeViewExpansion : UserControl
    {
        public TreeViewExpansion()
        {
            InitializeComponent();
        }

        private void ExpandAll_Click(object sender, RoutedEventArgs e)
        {
            SetExpanded(ExpansionTreeView.Items, true);
        }

        private void CollapseAll_Click(object sender, RoutedEventArgs e)
        {
            SetExpanded(ExpansionTreeView.Items, false);
        }

        private static void SetExpanded(ItemCollection items, bool expanded)
        {
            foreach (object obj in items)
            {
                if (obj is not Fluence.Wpf.Controls.TreeViewItem item)
                {
                    continue;
                }

                item.IsExpanded = expanded;
                SetExpanded(item.Items, expanded);
            }
        }
    }
}
";

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

            SetExpanded(ExpansionTreeView.Items, true);
        }

        private void CollapseAll_Click(object sender, RoutedEventArgs e)
        {
            if (ExpansionTreeView is null)
            {
                return;
            }

            SetExpanded(ExpansionTreeView.Items, false);
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
