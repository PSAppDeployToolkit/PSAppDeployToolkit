using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class TreeViewPage
    {
        private ObservableCollection<ExplorerItem> DataSource;

        public TreeViewPage()
        {
            InitializeComponent();

            DataSource = GetData();
            DataContext = DataSource;

            UpdateExampleCode();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new EmptyAutomationPeer(this);
        }

        private ObservableCollection<ExplorerItem> GetData()
        {
            var list = new ObservableCollection<ExplorerItem>();
            ExplorerItem folder1 = new ExplorerItem()
            {
                Name = "Work Documents",
                Type = ExplorerItem.ExplorerItemType.Folder,
                Children =
                {
                    new ExplorerItem()
                    {
                        Name = "Functional Specifications",
                        Type = ExplorerItem.ExplorerItemType.Folder,
                        Children =
                        {
                            new ExplorerItem()
                            {
                                Name = "TreeView spec",
                                Type = ExplorerItem.ExplorerItemType.File,
                              }
                        }
                    },
                    new ExplorerItem()
                    {
                        Name = "Feature Schedule",
                        Type = ExplorerItem.ExplorerItemType.File,
                    },
                    new ExplorerItem()
                    {
                        Name = "Overall Project Plan",
                        Type = ExplorerItem.ExplorerItemType.File,
                    },
                    new ExplorerItem()
                    {
                        Name = "Feature Resources Allocation",
                        Type = ExplorerItem.ExplorerItemType.File,
                    }
                }
            };
            ExplorerItem folder2 = new ExplorerItem()
            {
                Name = "Personal Folder",
                Type = ExplorerItem.ExplorerItemType.Folder,
                Children =
                        {
                            new ExplorerItem()
                            {
                                Name = "Home Remodel Folder",
                                Type = ExplorerItem.ExplorerItemType.Folder,
                                Children =
                                {
                                    new ExplorerItem()
                                    {
                                        Name = "Contractor Contact Info",
                                        Type = ExplorerItem.ExplorerItemType.File,
                                    },
                                    new ExplorerItem()
                                    {
                                        Name = "Paint Color Scheme",
                                        Type = ExplorerItem.ExplorerItemType.File,
                                    },
                                    new ExplorerItem()
                                    {
                                        Name = "Flooring Woodgrain type",
                                        Type = ExplorerItem.ExplorerItemType.File,
                                    },
                                    new ExplorerItem()
                                    {
                                        Name = "Kitchen Cabinet Style",
                                        Type = ExplorerItem.ExplorerItemType.File,
                                    }
                                }
                            }
                        }
            };

            list.Add(folder1);
            list.Add(folder2);
            return list;
        }

        private class EmptyAutomationPeer : FrameworkElementAutomationPeer
        {
            public EmptyAutomationPeer(FrameworkElement owner) : base(owner)
            {
            }

            protected override List<AutomationPeer> GetChildrenCore()
            {
                return new List<AutomationPeer>();
            }
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsInitialized) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example2.CSharp = Example2CS;
            Example3.Xaml = Example3Xaml;
            Example3.CSharp = Example3CS;
        }

        private static readonly string _explorerCommonCS = $@"
public Page()
{{
    DataSource = GetData();
    DataContext = DataSource;
}}

protected override AutomationPeer OnCreateAutomationPeer()
{{
    return new EmptyAutomationPeer(this);
}}

private ObservableCollection<ExplorerItem> GetData()
{{
    var list = new ObservableCollection<ExplorerItem>();
    ExplorerItem folder1 = new ExplorerItem()
    {{
        Name = ""Work Documents"",
        Type = ExplorerItem.ExplorerItemType.Folder,
        Children =
        {{
            new ExplorerItem()
            {{
                Name = ""Functional Specifications"",
                Type = ExplorerItem.ExplorerItemType.Folder,
                Children =
                {{
                    new ExplorerItem()
                    {{
                        Name = ""TreeView spec"",
                        Type = ExplorerItem.ExplorerItemType.File,
                        }}
                }}
            }},
            new ExplorerItem()
            {{
                Name = ""Feature Schedule"",
                Type = ExplorerItem.ExplorerItemType.File,
            }},
            new ExplorerItem()
            {{
                Name = ""Overall Project Plan"",
                Type = ExplorerItem.ExplorerItemType.File,
            }},
            new ExplorerItem()
            {{
                Name = ""Feature Resources Allocation"",
                Type = ExplorerItem.ExplorerItemType.File,
            }}
        }}
    }};
    ExplorerItem folder2 = new ExplorerItem()
    {{
        Name = ""Personal Folder"",
        Type = ExplorerItem.ExplorerItemType.Folder,
        Children =
                {{
                    new ExplorerItem()
                    {{
                        Name = ""Home Remodel Folder"",
                        Type = ExplorerItem.ExplorerItemType.Folder,
                        Children =
                        {{
                            new ExplorerItem()
                            {{
                                Name = ""Contractor Contact Info"",
                                Type = ExplorerItem.ExplorerItemType.File,
                            }},
                            new ExplorerItem()
                            {{
                                Name = ""Paint Color Scheme"",
                                Type = ExplorerItem.ExplorerItemType.File,
                            }},
                            new ExplorerItem()
                            {{
                                Name = ""Flooring Woodgrain type"",
                                Type = ExplorerItem.ExplorerItemType.File,
                            }},
                            new ExplorerItem()
                            {{
                                Name = ""Kitchen Cabinet Style"",
                                Type = ExplorerItem.ExplorerItemType.File,
                            }}
                        }}
                    }}
                }}
    }};

    list.Add(folder1);
    list.Add(folder2);
    return list;
}}

private class EmptyAutomationPeer : FrameworkElementAutomationPeer
{{
    public EmptyAutomationPeer(FrameworkElement owner) : base(owner)
    {{
    }}

    protected override List<AutomationPeer> GetChildrenCore()
    {{
        return new List<AutomationPeer>();
    }}
}}

public class ExplorerItem : INotifyPropertyChanged
{{
    public event PropertyChangedEventHandler PropertyChanged;
    public enum ExplorerItemType {{ Folder, File }};
    public String Name {{ get; set; }}
    public ExplorerItemType Type {{ get; set; }}
    private ObservableCollection<ExplorerItem> m_children;
    public ObservableCollection<ExplorerItem> Children
    {{
        get
        {{
            if (m_children == null)
            {{
                m_children = new ObservableCollection<ExplorerItem>();
            }}
            return m_children;
        }}
        set
        {{
            m_children = value;
        }}
    }}

    private bool m_isExpanded;
    public bool IsExpanded
    {{
        get {{ return m_isExpanded; }}
        set
        {{
            if (m_isExpanded != value)
            {{
                m_isExpanded = value;
                NotifyPropertyChanged(""IsExpanded"");
            }}
        }}
    }}

    private bool m_isSelected;
    public bool IsSelected
    {{
        get {{ return m_isSelected; }}

        set
        {{
            if (m_isSelected != value)
            {{
                m_isSelected = value;
                NotifyPropertyChanged(""IsSelected"");
            }}
        }}

    }}

    private void NotifyPropertyChanged(String propertyName)
    {{
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }}
}}
";

        public string Example1Xaml => $@"
<TreeView x:Name=""sampleTreeView"">
    <TreeViewItem Header=""Work Documents"" IsExpanded=""True"">
        <TreeViewItem Header=""XYZ Functional Spec"" />
        <TreeViewItem Header=""Feature Schedule"" />
        <TreeViewItem Header=""Overall Project Plan"" />
        <TreeViewItem Header=""Feature Resources Allocation"" />
    </TreeViewItem>
    <TreeViewItem Header=""Personal Documents"" IsExpanded=""True"">
        <TreeViewItem Header=""Home Remodel"" IsExpanded=""True"">
            <TreeViewItem Header=""Contractor Contact Info"" />
            <TreeViewItem Header=""Paint Color Scheme"" />
            <TreeViewItem Header=""Flooring woodgrain type"" />
            <TreeViewItem Header=""Kitchen cabinet style"" />
        </TreeViewItem>
    </TreeViewItem>
</TreeView>
";

        public string Example2Xaml => $@"
<TreeView x:Name=""TreeView1"" ItemsSource=""{{Binding}}"">
    <TreeView.ItemContainerStyle>
        <Style BasedOn=""{{StaticResource {{x:Static ui:ThemeKeys.DefaultTreeViewItemStyleKey}}}}"" TargetType=""TreeViewItem"">
            <Setter Property=""IsExpanded"" Value=""True"" />
        </Style>
    </TreeView.ItemContainerStyle>
    <TreeView.ItemTemplate>
        <HierarchicalDataTemplate ItemsSource=""{{Binding Children}}"">
            <TextBlock Text=""{{Binding Name}}"" />
        </HierarchicalDataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
";

        public string Example2CS => $@"
{_explorerCommonCS}
";

        public string Example3Xaml => $@"
<FrameworkElement.Resources>
    <HierarchicalDataTemplate x:Key=""FolderTemplate"" ItemsSource=""{{Binding Children}}"">
        <StackPanel Orientation=""Horizontal"">
            <Image Width=""20"" Source=""/Assets/folder.png"" />
            <TextBlock Margin=""0,0,10,0"" />
            <TextBlock Text=""{{Binding Name}}"" />
        </StackPanel>
    </HierarchicalDataTemplate>

    <DataTemplate x:Key=""FileTemplate"">
        <StackPanel Orientation=""Horizontal"">
            <Image Width=""20"" Source=""/Assets/file.png"" />
            <TextBlock Margin=""0,0,10,0"" />
            <TextBlock Text=""{{Binding Name}}"" />
        </StackPanel>
    </DataTemplate>

    <pcw:ExplorerItemTemplateSelector
        x:Key=""ExplorerItemTemplateSelector""
        FileTemplate=""{{StaticResource FileTemplate}}""
        FolderTemplate=""{{StaticResource FolderTemplate}}"" />
</FrameworkElement.Resources>

<TreeView x:Name=""FileTree""
        ItemTemplateSelector=""{{StaticResource ExplorerItemTemplateSelector}}""
        ItemsSource=""{{Binding}}"">
    <TreeView.ItemContainerStyle>
        <Style BasedOn=""{{StaticResource DefaultTreeViewItemStyle}}"" TargetType=""TreeViewItem"">
            <Setter Property=""IsExpanded"" Value=""True"" />
        </Style>
    </TreeView.ItemContainerStyle>
</TreeView>
";

        public string Example3CS => $@"
{_explorerCommonCS}

class ExplorerItemTemplateSelector : DataTemplateSelector
{{
    public DataTemplate FolderTemplate {{ get; set; }}
    public DataTemplate FileTemplate {{ get; set; }}

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {{
        var explorerItem = (ExplorerItem)item;
        return explorerItem.Type == ExplorerItem.ExplorerItemType.Folder ? FolderTemplate : FileTemplate;
    }}
}}
";

        #endregion

    }

    public class ExplorerItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public enum ExplorerItemType { Folder, File };
        public String Name { get; set; }
        public ExplorerItemType Type { get; set; }
        private ObservableCollection<ExplorerItem> m_children;
        public ObservableCollection<ExplorerItem> Children
        {
            get
            {
                if (m_children == null)
                {
                    m_children = new ObservableCollection<ExplorerItem>();
                }
                return m_children;
            }
            set
            {
                m_children = value;
            }
        }

        private bool m_isExpanded;
        public bool IsExpanded
        {
            get { return m_isExpanded; }
            set
            {
                if (m_isExpanded != value)
                {
                    m_isExpanded = value;
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        private bool m_isSelected;
        public bool IsSelected
        {
            get { return m_isSelected; }

            set
            {
                if (m_isSelected != value)
                {
                    m_isSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }

        }

        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    class ExplorerItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FolderTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var explorerItem = (ExplorerItem)item;
            return explorerItem.Type == ExplorerItem.ExplorerItemType.Folder ? FolderTemplate : FileTemplate;
        }
    }
}
