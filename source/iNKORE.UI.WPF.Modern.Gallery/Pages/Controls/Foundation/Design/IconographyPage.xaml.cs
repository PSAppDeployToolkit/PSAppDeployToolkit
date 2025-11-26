// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using iNKORE.UI.WPF.Modern.Gallery.Helpers;
using iNKORE.UI.WPF.Modern.Gallery.DataModel;
using iNKORE.UI.WPF.Modern;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Navigation;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Foundation
{
    public partial class IconographyPage : iNKORE.UI.WPF.Modern.Controls.Page
    {
        public List<double> FontSizes { get; } = new()
        {
            16,
            24,
            32,
            48
        };

        private string currentSearch = null;

        public IconData SelectedItem
        {
            get { return (IconData)GetValue(SelectedItemProperty); }
            set
            {
                SetValue(SelectedItemProperty, value);
                SetSampleCodePresenterCode(value);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                var uri = e.Uri?.AbsoluteUri ?? (sender as System.Windows.Documents.Hyperlink)?.NavigateUri?.AbsoluteUri;
                if (!string.IsNullOrEmpty(uri))
                {
                    var psi = new ProcessStartInfo(uri) { UseShellExecute = true };
                    Process.Start(psi);
                }
            }
            catch
            {
            }
            e.Handled = true;
        }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem", typeof(IconData), typeof(IconographyPage), new PropertyMetadata(null, OnSelectedItemChanged));

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is IconographyPage page)
            {
                try
                {
                    page.UpdateSelectionVisuals(e.NewValue as IconData);
                }
                catch
                {
                }
            }
        }

        public IconographyPage()
        {
            InitializeComponent();
            Loaded += IconographyPage_Loaded;
            DataContext = this;
        }

        private void TogglePageTheme()
        {
            // Toggle requested theme only on the example container Border so
            // only the interior region re-evaluates DynamicResource brushes.
            try
            {
                var exampleBorder = FindName("ExampleContainerBorder") as FrameworkElement;
                if (exampleBorder != null)
                {
                    exampleBorder.ToggleTheme();
                    return;
                }

                // Fallback to toggling the whole page if the named Border isn't found
                this.LayoutRoot.ToggleTheme();
            }
            catch { }
        }

        // Dependency properties for side panel sample text
        public string FontIconXaml
        {
            get => (string)GetValue(FontIconXamlProperty);
            set => SetValue(FontIconXamlProperty, value);
        }
        public static readonly DependencyProperty FontIconXamlProperty = DependencyProperty.Register(nameof(FontIconXaml), typeof(string), typeof(IconographyPage), new PropertyMetadata(string.Empty));

        public string FontIconCSharp
        {
            get => (string)GetValue(FontIconCSharpProperty);
            set => SetValue(FontIconCSharpProperty, value);
        }
        public static readonly DependencyProperty FontIconCSharpProperty = DependencyProperty.Register(nameof(FontIconCSharp), typeof(string), typeof(IconographyPage), new PropertyMetadata(string.Empty));

        public string SymbolIconXaml
        {
            get => (string)GetValue(SymbolIconXamlProperty);
            set => SetValue(SymbolIconXamlProperty, value);
        }
        public static readonly DependencyProperty SymbolIconXamlProperty = DependencyProperty.Register(nameof(SymbolIconXaml), typeof(string), typeof(IconographyPage), new PropertyMetadata(string.Empty));

        public string SymbolIconCSharp
        {
            get => (string)GetValue(SymbolIconCSharpProperty);
            set => SetValue(SymbolIconCSharpProperty, value);
        }
        public static readonly DependencyProperty SymbolIconCSharpProperty = DependencyProperty.Register(nameof(SymbolIconCSharp), typeof(string), typeof(IconographyPage), new PropertyMetadata(string.Empty));

        private void IconographyPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (NavigationRootPage.Current?.NavigationView != null)
            {
                NavigationRootPage.Current.NavigationView.Header = "Iconography";
            }
            
            // Register toggle theme action on page header so Theme toggle also affects this page
            try
            {
                if (NavigationRootPage.Current?.PageHeader != null)
                {
                    NavigationRootPage.Current.PageHeader.ToggleThemeAction = TogglePageTheme;
                }
            }
            catch { }

            // Load icons on a background thread and assign them immediately to the repeater.
            Task.Run(async delegate
            {
                var icons = await IconsDataSource.Instance.LoadIcons();

                // Assign the full list to the ItemsRepeater on the UI thread.
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    var iconsItemsView = FindName("IconsItemsView") as iNKORE.UI.WPF.Modern.Controls.ItemsRepeater;
                    if (iconsItemsView != null)
                    {
                        iconsItemsView.ItemsSource = icons;

                        // Populate icon set ComboBox if present
                        var setCombo = FindName("IconSetComboBox") as ComboBox;
                        try
                        {
                            if (setCombo != null)
                            {
                                setCombo.ItemsSource = IconsDataSource.Instance.AvailableSets;
                                setCombo.SelectionChanged += IconSetComboBox_SelectionChanged;
                                if (IconsDataSource.Instance.AvailableSets.Count > 0)
                                {
                                    // Prefer SegoeFluentIcons when present
                                    var preferred = IconsDataSource.Instance.AvailableSets.FirstOrDefault(s => string.Equals(s, "SegoeFluentIcons", StringComparison.OrdinalIgnoreCase));
                                    if (!string.IsNullOrEmpty(preferred))
                                    {
                                        setCombo.SelectedItem = preferred;
                                    }
                                    else
                                    {
                                        setCombo.SelectedIndex = 0;
                                    }
                                }
                            }
                        }
                        catch { }

                        // Select the first item by default and show side panel
                        if (icons != null && icons.Count > 0)
                        {
                            var first = icons[0];
                            SelectedItem = first;
                            var sidePanel = FindName("SidePanel") as Border;
                            if (sidePanel != null)
                            {
                                sidePanel.Visibility = Visibility.Visible;
                            }
                            UpdateSelectionVisuals(first);
                        }
                    }
                }));
            });
        }

        private void SetSampleCodePresenterCode(IconData value)
        {
            // Update presenters and side panel when selection changes
            var sidePanel = FindName("SidePanel") as Border;
            if (value == null)
            {
                if (sidePanel != null)
                {
                    sidePanel.Visibility = Visibility.Collapsed;
                }
                return;
            }

            // Ensure side panel visible
            if (sidePanel != null)
            {
                sidePanel.Visibility = Visibility.Visible;
            }

            // Populate code strings for TextBoxes
            if (!string.IsNullOrEmpty(value.Set))
            {
                // Use static icon class reference when available
                // Example XAML: <ui:FontIcon Icon="{x:Static ui:SegoeFluentIcons.Edit}" />
                FontIconXaml = $"<ui:FontIcon Icon=\"{{x:Static ui:{value.Set}.{value.Name}}}\" />";
                // Use fully-qualified C# reference to the static property in Common.IconKeys
                FontIconCSharp = "using iNKORE.UI.WPF.Modern.Common.IconKeys;" + Environment.NewLine + Environment.NewLine + $"FontIcon icon = new FontIcon();" + Environment.NewLine + $"icon.Icon = {value.Set}.{value.Name};";
            }
            else
            {
                FontIconXaml = $"<ui:FontIcon Glyph=\"{value.TextGlyph}\" />";
                FontIconCSharp = "FontIcon icon = new FontIcon();" + Environment.NewLine + $"icon.Glyph = \"{value.CodeGlyph}\";";
            }

            if (!string.IsNullOrEmpty(value.SymbolName))
            {
                SymbolIconXaml = $"<SymbolIcon Symbol=\"{value.SymbolName}\" />";
                SymbolIconCSharp = "SymbolIcon icon = new SymbolIcon();" + Environment.NewLine + $"icon.Symbol = Symbol.{value.SymbolName};";
                var symbolPanel = FindName("SymbolPanel") as StackPanel;
                if (symbolPanel != null)
                {
                    symbolPanel.Visibility = Visibility.Visible;
                }
            }
            else
            {
                SymbolIconXaml = string.Empty;
                SymbolIconCSharp = string.Empty;
                var symbolPanel = FindName("SymbolPanel") as StackPanel;
                if (symbolPanel != null)
                {
                    symbolPanel.Visibility = Visibility.Collapsed;
                }
            }

            // Tags visibility
            try
            {
                var tagsView = FindName("TagsItemsView") as iNKORE.UI.WPF.Modern.Controls.ItemsRepeater;
                var noTags = FindName("NoTagsTextBlock") as TextBlock;
                if (value.Tags == null || value.Tags.Length == 0 || value.Tags.All(t => string.IsNullOrWhiteSpace(t)))
                {
                    if (tagsView != null) tagsView.Visibility = Visibility.Collapsed;
                    if (noTags != null) noTags.Visibility = Visibility.Collapsed;
                    TagsLabel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (tagsView != null) tagsView.Visibility = Visibility.Visible;
                    if (noTags != null) noTags.Visibility = Visibility.Collapsed;
                    TagsLabel.Visibility = Visibility.Visible;
                }
            }
            catch { }
        }

        private void SearchTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var tb = FindName("IconsSearchBox");
            var text = tb?.GetType().GetProperty("Text")?.GetValue(tb) as string;
            Filter(text);
        }

        public void Filter(string search)
        {
            currentSearch = search;
            string[] filter = search?.Split(' ');

            Task.Run(() =>
            {
                var newItems = new List<IconData>();
                foreach (var item in IconsDataSource.Icons)
                {
                    var fitsFilter = filter == null || filter.All(entry =>
                        item.Code.Contains(entry, StringComparison.CurrentCultureIgnoreCase) ||
                        item.Name.Contains(entry, StringComparison.CurrentCultureIgnoreCase) ||
                        item.Tags.Any(tag => !string.IsNullOrEmpty(tag) && tag.Contains(entry, StringComparison.CurrentCultureIgnoreCase)));

                    if (fitsFilter)
                    {
                        newItems.Add(item);
                    }
                }

                // Assign filtered list to repeater immediately on UI thread
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var iconsItemsView = FindName("IconsItemsView") as iNKORE.UI.WPF.Modern.Controls.ItemsRepeater;
                    if (iconsItemsView != null)
                    {
                        iconsItemsView.ItemsSource = newItems;
                        if (newItems.Count > 0)
                        {
                            SelectedItem = newItems[0];
                            UpdateSelectionVisuals(newItems[0]);
                        }
                    }
                }));
            });
        }

        private void IconsItemsView_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                // Try to read a SelectedItem property from the sender (GridView exposes this)
                var view = FindName("IconsItemsView");
                if (view != null)
                {
                    var selProp = view.GetType().GetProperty("SelectedItem");
                    if (selProp != null)
                    {
                        var sel = selProp.GetValue(view) as IconData;
                        if (sel != null) SelectedItem = sel;
                    }
                }
            }
            catch { }

            try
            {
                var argsType = e.GetType();
                var invokedProp = argsType.GetProperty("InvokedItem");
                if (invokedProp != null)
                {
                    var tag = invokedProp.GetValue(e) as string;
                    if (!string.IsNullOrEmpty(tag))
                    {
                        // Set text on the named search box if present
                        var searchBox = FindName("IconsSearchBox");
                        if (searchBox != null)
                        {
                            var textProp = searchBox.GetType().GetProperty("Text");
                            textProp?.SetValue(searchBox, tag);
                        }
                    }
                }
            }
            catch { }
        }

        private void IconsItemsView_ItemClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.DataContext is IconData data)
                {
                    SelectedItem = data;
                    UpdateSelectionVisuals(data);
                }
            }
            catch { }
        }

        private void IconSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.SelectedItem is string setName)
                {
                    var items = IconsDataSource.Instance.SetActiveSet(setName);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var iconsItemsView = FindName("IconsItemsView") as iNKORE.UI.WPF.Modern.Controls.ItemsRepeater;
                        if (iconsItemsView != null)
                        {
                            iconsItemsView.ItemsSource = items;
                            if (items.Count > 0)
                            {
                                SelectedItem = items[0];
                                UpdateSelectionVisuals(items[0]);
                            }
                        }
                    }));
                }
            }
            catch { }
        }

        private void TagButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn)
                {
                    // The TextBlock is inside the Button's visual tree; find it
                    var tb = FindDescendants<TextBlock>(btn).FirstOrDefault();
                    var tag = tb?.Text;
                    if (!string.IsNullOrEmpty(tag))
                    {
                        // Update search box text via the named control if available
                        var searchBoxField = this.GetType().GetField("IconsSearchBox", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                        if (searchBoxField != null)
                        {
                            var searchBox = searchBoxField.GetValue(this);
                            if (searchBox != null)
                            {
                                var textProp = searchBox.GetType().GetProperty("Text");
                                textProp?.SetValue(searchBox, tag);
                                var focusMethod = searchBox.GetType().GetMethod("Focus", System.Type.EmptyTypes);
                                focusMethod?.Invoke(searchBox, null);

                                // Try to open suggestion list if property exists
                                var isOpenProp = searchBox.GetType().GetProperty("IsSuggestionListOpen");
                                if (isOpenProp != null) isOpenProp.SetValue(searchBox, true);
                            }
                        }

                        // Also call Filter to update items immediately
                        Filter(tag);
                    }
                }
            }
            catch { }
        }

        // handler for Border-based tag chips. Extracts the tag text and
        // performs the same actions as TagButton_Click (populate search box,
        // focus, open suggestions, and filter).
        private void TagChip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border outerBorder)
                {
                    // The inner TextBlock is a descendant; find it
                    var tb = FindDescendants<TextBlock>(outerBorder).FirstOrDefault();
                    var tag = tb?.Text;
                    if (!string.IsNullOrEmpty(tag))
                    {
                        // Reuse the same reflection-based logic to set the search box
                        var searchBoxField = this.GetType().GetField("IconsSearchBox", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                        if (searchBoxField != null)
                        {
                            var searchBox = searchBoxField.GetValue(this);
                            if (searchBox != null)
                            {
                                var textProp = searchBox.GetType().GetProperty("Text");
                                textProp?.SetValue(searchBox, tag);
                                var focusMethod = searchBox.GetType().GetMethod("Focus", System.Type.EmptyTypes);
                                focusMethod?.Invoke(searchBox, null);

                                // Try to open suggestion list if property exists
                                var isOpenProp = searchBox.GetType().GetProperty("IsSuggestionListOpen");
                                if (isOpenProp != null) isOpenProp.SetValue(searchBox, true);
                            }
                        }

                        // Also call Filter to update items immediately
                        Filter(tag);
                    }
                }
            }
            catch { }
        }

        private async void CopyValueButton_Click(object sender, RoutedEventArgs e)
        {
            string textToCopy = null;
            try
            {
                if (sender is Button btn)
                {
                    // Primary path: Tag is bound to the value to copy
                    textToCopy = btn.Tag as string;

                    // Fallbacks for cases where the Tag binding isn't available or was modified
                    if (string.IsNullOrEmpty(textToCopy))
                    {
                        switch (btn.Name)
                        {
                            case "CopyInlineExampleButton":
                                textToCopy = (FindName("InlineExampleBox") as TextBox)?.Text;
                                break;
                            case "CopyNameButton":
                                textToCopy = SelectedItem?.Name;
                                break;
                            case "CopyTextGlyphButton":
                                textToCopy = SelectedItem?.TextGlyph;
                                break;
                            case "CopyCodeGlyphButton":
                                textToCopy = SelectedItem?.CodeGlyph;
                                break;
                            case "CopyXamlButton":
                                textToCopy = FontIconXaml;
                                break;
                            case "CopyCSharpButton":
                                textToCopy = FontIconCSharp;
                                break;
                            case "CopySymbolXamlButton":
                                textToCopy = SymbolIconXaml;
                                break;
                            case "CopySymbolCSharpButton":
                                textToCopy = SymbolIconCSharp;
                                break;
                        }
                    }

                    if (!string.IsNullOrEmpty(textToCopy))
                    {
                        var copied = TrySetClipboardText(textToCopy, 3, TimeSpan.FromMilliseconds(150));
                        if (!copied)
                        {
                            throw new InvalidOperationException("Unable to open clipboard after multiple attempts.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(ex.ToString(), "Unable to Perform Copy", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // Trigger the Geometry-style copy confirmation animation.
            // The VisualStateGroups are declared on LayoutRoot; use GoToElementState
            // so the states are found in the same namescope.
            if (sender is Button)
            {
                try
                {
                        var layoutRoot = FindName("LayoutRoot") as FrameworkElement;
                        if (layoutRoot != null)
                        {
                            // If the Button has a name like "CopyXamlButton" derive a per-button
                            // state name like "CopyXamlVisible" and "CopyXamlHidden" and try
                            // to trigger it. Fall back to the shared Control/Overlay groups.
                            if (sender is Button btn && !string.IsNullOrEmpty(btn.Name))
                            {
                                // Strip trailing "Button" if present
                                var baseName = btn.Name.EndsWith("Button") ? btn.Name.Substring(0, btn.Name.Length - 6) : btn.Name;
                                var visibleState = baseName + "Visible";
                                var hiddenState = baseName + "Hidden";

                                var started = VisualStateManager.GoToElementState(layoutRoot, visibleState, true);
                                if (!started)
                                {
                                    // fall back to existing Control/Overlay groups
                                    started = VisualStateManager.GoToElementState(layoutRoot, "ControlCornerRadiusCopyButtonVisible", true);
                                    if (!started)
                                    {
                                        VisualStateManager.GoToElementState(layoutRoot, "OverlayCornerRadiusCopyButtonVisible", true);
                                    }
                                }

                                // Revert after a short delay so the success checkmark animates back.
                                await Task.Delay(900);

                                // Try to revert per-button state first, then the shared states.
                                VisualStateManager.GoToElementState(layoutRoot, hiddenState, true);
                                VisualStateManager.GoToElementState(layoutRoot, "ControlCornerRadiusCopyButtonHidden", true);
                                VisualStateManager.GoToElementState(layoutRoot, "OverlayCornerRadiusCopyButtonHidden", true);
                            }
                            else
                            {
                                var started = VisualStateManager.GoToElementState(layoutRoot, "ControlCornerRadiusCopyButtonVisible", true);
                                if (!started)
                                {
                                    VisualStateManager.GoToElementState(layoutRoot, "OverlayCornerRadiusCopyButtonVisible", true);
                                }
                                await Task.Delay(900);
                                VisualStateManager.GoToElementState(layoutRoot, "ControlCornerRadiusCopyButtonHidden", true);
                                VisualStateManager.GoToElementState(layoutRoot, "OverlayCornerRadiusCopyButtonHidden", true);
                            }
                        }
                }
                catch
                {
                    // Swallow any VSM exceptions; copy already occurred.
                }
            }
        }

        // Recursive descendant finder
        private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int count = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (child is T t) yield return t;
                    queue.Enqueue(child);
                }
            }
        }

        // Robust clipboard setter: runs SetText on a new STA thread and retries when clipboard is busy.
        private static bool TrySetClipboardText(string text, int maxAttempts, TimeSpan delayBetweenAttempts)
        {
            if (string.IsNullOrEmpty(text)) return false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var success = false;
                var thread = new Thread(() =>
                {
                    try
                    {
                        // Use System.Windows.Clipboard on STA thread
                        System.Windows.Clipboard.SetText(text);
                        success = true;
                    }
                    catch
                    {
                        success = false;
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();
                thread.Join();

                if (success) return true;

                Thread.Sleep(delayBetweenAttempts);
            }

            return false;
        }

        //Keep for reference in the future
        private void UpdateSelectionVisuals(IconData selected)
        {

        }
    }
}
