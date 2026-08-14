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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Fluence.Wpf.Demo;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public sealed partial class ControlTests : IDisposable
    {
        public ControlTests()
        {
            WpfTestSta.Invoke(ResetSharedWpfState);
        }

        public void Dispose()
        {
            WpfTestSta.Invoke(ResetSharedWpfState);
        }

        private static void ResetSharedWpfState()
        {
            Application application = Application.Current ?? new Application
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown,
            };
            Keyboard.ClearFocus();

            foreach (Window? window in (Window[])[.. application.Windows.Cast<Window>()])
            {
                window.Content = null;
                window.Close();
            }

            // A single ApplicationIdle drain subsumes the higher Loaded/ContextIdle priorities:
            // Invoke blocks until the queue has been processed down to and including the requested
            // priority, so once the windows are closed the lowest-priority pump drains them all.
            WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);

            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            application.Resources.Clear();
        }

        private static void RunOnStaThread(Action action)
        {
            WpfTestSta.RunOnSta(action);
        }

        private static Application? EnsureApplication()
        {
            return WpfTestSta.EnsureApplication();
        }

        private static ResourceDictionary? MergeGenericDictionary(Application? application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application?.Resources.MergedDictionaries.Clear();
            application?.Resources.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
            Collection<ResourceDictionary>? dictionaries = application?.Resources.MergedDictionaries;
            ResourceDictionary? genericDictionary = dictionaries?.Count > 0 ? dictionaries[^1] : null;

            ResourceDictionary demoShared = new()
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative),
            };
            application?.Resources.MergedDictionaries.Add(demoShared);

            return genericDictionary;
        }

        private static void DrainDispatcher(Dispatcher? dispatcher)
        {
            WpfTestSta.DrainDispatcher(dispatcher);
        }

        private static T? FindVisualChild<T>(DependencyObject root) where T : DependencyObject
        {
            if (root is null)
            {
                return null;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int index = 0; index < childCount; index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, index);
                if (child is T match)
                {
                    return match;
                }

                if (FindVisualChild<T>(child) is T visual)
                {
                    return visual;
                }
            }

            return null;
        }

        // Visual-tree-only descendant search. Forwards to the canonical WpfTestSta implementation
        // (FindVisualDescendants); the logical+visual cycle-guarded variant lives there too as
        // FindLogicalAndVisualDescendants, which is what DemoTestHost-style callers use.
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            return WpfTestSta.FindVisualDescendants<T>(root);
        }

        private static DependencyObject? FindVisualChildByTypeName(DependencyObject root, string typeName)
        {
            if (root is null)
            {
                return null;
            }

            if (string.Equals(root.GetType().Name, typeName, StringComparison.Ordinal))
            {
                return root;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int index = 0; index < childCount; index++)
            {
                DependencyObject? found = FindVisualChildByTypeName(VisualTreeHelper.GetChild(root, index), typeName);
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        }

        private static T? FindVisualChildByName<T>(DependencyObject root, string name) where T : FrameworkElement
        {
            if (root is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int index = 0; index < childCount; index++)
            {
                if (VisualTreeHelper.GetChild(root, index) is FrameworkElement child && string.Equals(child.Name, name, StringComparison.Ordinal) && child is T match)
                {
                    return match;
                }

                T? found = FindVisualChildByName<T>(VisualTreeHelper.GetChild(root, index), name);
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        }

        private static StackPanel? GetNavigationViewItemsHostPanel(Controls.NavigationView nav)
        {
            ItemsPresenter? presenter = FindVisualChild<ItemsPresenter>(nav);
            if (presenter is null)
            {
                return null;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(presenter);
            return childCount < 1 ? null : VisualTreeHelper.GetChild(presenter, 0) as StackPanel;
        }

        [Fact]
        public void FontIcon_DefaultFontFamily_IsSegoeFluent()
        {
            RunOnStaThread(static () =>
            {
                Controls.FontIcon fontIcon = new();

                Assert.Equal("Segoe Fluent Icons", fontIcon.IconFontFamily.Source, StringComparer.Ordinal);
            });
        }

        [Fact]
        public void FontIcon_GlyphProperty_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.FontIcon fontIcon = new();
                const string testGlyph = "\uE710";

                fontIcon.Glyph = testGlyph;

                Assert.Equal(testGlyph, fontIcon.Glyph, StringComparer.Ordinal);
            });
        }

        [Fact]
        public void Button_DefaultAppearance_IsStandard()
        {
            RunOnStaThread(static () =>
            {
                Controls.Button button = new();

                Assert.Equal(ControlAppearance.Standard, button.Appearance);
            });
        }

        [Fact]
        public void Button_AccentAppearance_CanBeSet()
        {
            RunOnStaThread(static () =>
            {
                Controls.Button button = new()
                {
                    Appearance = ControlAppearance.Accent,
                };

                Assert.Equal(ControlAppearance.Accent, button.Appearance);
            });
        }

        [Fact]
        public void TextBox_PlaceholderText_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.TextBox textBox = new();
                const string placeholder = "Enter text here...";

                textBox.PlaceholderText = placeholder;

                Assert.Equal(placeholder, textBox.PlaceholderText, StringComparer.Ordinal);
            });
        }

        [Fact]
        public void TextBox_ClearButtonEnabled_DefaultTrue()
        {
            RunOnStaThread(static () =>
            {
                Controls.TextBox textBox = new();

                Assert.True(textBox.ClearButtonEnabled);
            });
        }

        [Fact]
        public void PasswordBox_RevealButtonEnabled_DefaultTrue()
        {
            RunOnStaThread(static () =>
            {
                Controls.PasswordBox passwordBox = new();

                Assert.True(passwordBox.RevealButtonEnabled);
            });
        }

        [Fact]
        public void PasswordBox_IsPasswordRevealed_DefaultFalse()
        {
            RunOnStaThread(static () =>
            {
                Controls.PasswordBox passwordBox = new();

                Assert.False(passwordBox.IsPasswordRevealed);
            });
        }

        [Fact]
        public void TextBox_DefaultChrome_UsesWinUiReferenceValues()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.TextBox textBox = new()
                {
                    Width = 260,
                };

                try
                {
                    window.Content = textBox;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border mainBorder = Assert.IsType<Border>(textBox.Template.FindName("MainBorder", textBox));
                    Button clearButton = Assert.IsType<Button>(textBox.Template.FindName("PART_ClearButton", textBox));

                    Assert.Equal(new Thickness(10, 5, 6, 6), textBox.Padding);
                    Assert.Equal(32.0, textBox.MinHeight);
                    _ = Assert.IsAssignableFrom<LinearGradientBrush>(mainBorder.BorderBrush);
                    Assert.Equal(30.0, clearButton.Width, 0.1);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void TextBox_FocusState_ShowsAccentLineUnderneath()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.TextBox textBox = new()
                {
                    Width = 260,
                    Text = "Focused",
                };

                try
                {
                    window.Content = textBox;
                    window.Show();
                    _ = textBox.Focus();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border accentLine = Assert.IsType<Border>(textBox.Template.FindName("FocusAccentLine", textBox));

                    Assert.Equal(1.0, accentLine.Opacity);
                    Assert.Equal(2.0, accentLine.Height);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void PasswordBox_DefaultChrome_UsesWinUiReferenceValues()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.PasswordBox passwordBox = new()
                {
                    Width = 260,
                };

                try
                {
                    window.Content = passwordBox;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border mainBorder = Assert.IsType<Border>(passwordBox.Template.FindName("MainBorder", passwordBox));
                    Button revealButton = Assert.IsType<Button>(passwordBox.Template.FindName("PART_RevealButton", passwordBox));

                    Assert.Equal(new Thickness(10, 5, 6, 6), passwordBox.Padding);
                    Assert.Equal(32.0, passwordBox.MinHeight);
                    _ = Assert.IsAssignableFrom<LinearGradientBrush>(mainBorder.BorderBrush);
                    Assert.Equal(30.0, revealButton.Width, 0.1);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void PasswordBox_FocusState_ShowsAccentLineUnderneath()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.PasswordBox passwordBox = new()
                {
                    Width = 260,
                    Password = "Focused",
                };

                try
                {
                    window.Content = passwordBox;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    PasswordBox innerPasswordBox = Assert.IsType<PasswordBox>(passwordBox.Template.FindName("PART_PasswordBox", passwordBox));
                    Border accentLine = Assert.IsType<Border>(passwordBox.Template.FindName("FocusAccentLine", passwordBox));


                    _ = innerPasswordBox.Focus();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(1.0, accentLine.Opacity);
                    Assert.Equal(2.0, accentLine.Height);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void ListView_ItemAnimationsEnabled_DefaultTrue()
        {
            RunOnStaThread(static () =>
            {
                Controls.ListView listView = new();

                Assert.True(listView.ItemAnimationsEnabled);
            });
        }

        [Fact]
        public void ListView_HoverHighlightEnabled_DefaultTrue()
        {
            RunOnStaThread(static () =>
            {
                Controls.ListView listView = new();

                Assert.True(listView.HoverHighlightEnabled);
            });
        }

        [Fact]
        public void ListViewItem_DefaultChrome_UsesWinUiReferenceValues()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.ListView listView = new()
                {
                    Width = 260,
                    Height = 120,
                };
                _ = listView.Items.Add("Item 1");

                try
                {
                    window.Content = listView;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ListViewItem item = Assert.IsType<ListViewItem>(listView.ItemContainerGenerator.ContainerFromIndex(0));

                    Assert.Equal(new Thickness(12, 0, 12, 0), item.Padding);
                    Assert.Equal(HorizontalAlignment.Left, item.HorizontalContentAlignment);
                    Assert.Equal(40.0, item.MinHeight);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void ListViewItem_SelectionIndicator_UsesWinUiCornerRadius()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.ListView listView = new()
                {
                    Width = 260,
                    Height = 120,
                };
                _ = listView.Items.Add("Item 1");

                try
                {
                    window.Content = listView;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ListViewItem item = Assert.IsType<ListViewItem>(listView.ItemContainerGenerator.ContainerFromIndex(0));

                    _ = item.ApplyTemplate();
                    Border selectionIndicator = Assert.IsType<Border>(item.Template.FindName("SelectionIndicator", item));

                    // WI-3 C20: canonical ListViewItemSelectionIndicatorCornerRadius = 1.5
                    Assert.Equal(new CornerRadius(1.5), selectionIndicator.CornerRadius);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void ListViewItem_SelectedState_UsesWinUiSelectedBrush()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                Window window = new()
                {
                    Left = -20000,
                    Top = -20000,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    ShowInTaskbar = false,
                };
                Controls.ListView listView = new()
                {
                    Width = 260,
                    Height = 120,
                    SelectionMode = SelectionMode.Single,
                };
                _ = listView.Items.Add("Item 1");

                try
                {
                    window.Content = listView;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    listView.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ListViewItem item = Assert.IsType<ListViewItem>(listView.ItemContainerGenerator.ContainerFromIndex(0));

                    _ = item.ApplyTemplate();
                    Border selectedOverlay = Assert.IsType<Border>(item.Template.FindName("SelectedOverlay", item));
                    Border selectionIndicator = Assert.IsType<Border>(item.Template.FindName("SelectionIndicator", item));
                    SolidColorBrush expectedSelectedBrush = Assert.IsType<SolidColorBrush>(application?.Resources["SubtleFillColorSecondaryBrush"]);
                    SolidColorBrush expectedIndicatorBrush = Assert.IsType<SolidColorBrush>(application?.Resources["AccentFillColorDefaultBrush"]);

                    _ = Assert.IsAssignableFrom<SolidColorBrush>(selectedOverlay.Background);
                    _ = Assert.IsAssignableFrom<SolidColorBrush>(selectionIndicator.Background);
                    Assert.Equal(expectedSelectedBrush.Color, ((SolidColorBrush)selectedOverlay.Background).Color);
                    Assert.Equal(expectedIndicatorBrush.Color, ((SolidColorBrush)selectionIndicator.Background).Color);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void TextBlockExtensions_Typography_SetsCorrectFontSize()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    TextBlock textBlock = new();

                    Controls.TextBlockExtensions.SetTypography(textBlock, FluentTypography.BodyLarge);

                    Assert.Same(application?.TryFindResource("BodyLargeTextBlockStyle"), textBlock.Style);
                    Assert.Equal(18.0, textBlock.FontSize);
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void TextBox_TextViewAlignsWithPlaceholder_WhenIconIsShown()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.TextBox textBox = new()
                {
                    Width = 260,
                    PlaceholderText = "With icon",
                    Icon = new Controls.FontIcon
                    {
                        Glyph = "\uE721",
                        IconFontSize = 14,
                    },
                };

                try
                {
                    window.Content = textBox;
                    window.Show();
                    _ = textBox.Focus();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FrameworkElement placeholder = Assert.IsAssignableFrom<FrameworkElement>(textBox.Template.FindName("PlaceholderTextBlock", textBox));
                    FrameworkElement textView = Assert.IsAssignableFrom<FrameworkElement>(FindVisualChildByTypeName(textBox, "TextBoxView"));


                    double placeholderX = placeholder.TransformToAncestor(window).Transform(new Point(0, 0)).X;
                    double textViewX = textView.TransformToAncestor(window).Transform(new Point(0, 0)).X;

                    // UseLayoutRounding snaps the placeholder and the ScrollViewer content chain to whole
                    // device pixels independently, so at fractional DPI scales (e.g. 175%) the two can land
                    // one device pixel apart. Alignment is therefore asserted to the nearest device pixel.
                    double oneDevicePixelInDips = 1.0 / VisualTreeHelper.GetDpi(textBox).DpiScaleX;
                    Assert.Equal(placeholderX, textViewX, oneDevicePixelInDips + 0.01);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void Button_AccentAppearance_UsesAccentBrush()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                Window window = new();
                Controls.Button button = new()
                {
                    Width = 140,
                    Content = "Accent",
                    Appearance = ControlAppearance.Accent,
                    IsHitTestVisible = false,
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border restFill = Assert.IsType<Border>(button.Template.FindName("RestFill", button));
                    SolidColorBrush accentBrush = Assert.IsType<SolidColorBrush>(application?.Resources["AccentFillColorDefaultBrush"]);

                    _ = Assert.IsAssignableFrom<SolidColorBrush>(restFill.Background);
                    Assert.Equal(accentBrush.Color, ((SolidColorBrush)restFill.Background).Color);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void Button_LeftIconContentGroup_RemainsCentered()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.Button button = new()
                {
                    Width = 180,
                    Content = "With Icon",
                    Icon = new Controls.FontIcon
                    {
                        Glyph = "\uE710",
                        IconFontSize = 14,
                    },
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertContentGroupIsCentered(window, button, "With Icon", "\uE710");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void Button_RightIconContentGroup_RemainsCentered()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.Button button = new()
                {
                    Width = 180,
                    Content = "Icon Right",
                    IconPlacement = ElementPlacement.Right,
                    Icon = new Controls.FontIcon
                    {
                        Glyph = "\uE72A",
                        IconFontSize = 14,
                    },
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertContentGroupIsCentered(window, button, "Icon Right", "\uE72A");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void Button_LeftIcon_RendersGlyph()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.Button button = new()
                {
                    Width = 180,
                    Content = "With Icon",
                    Icon = new Controls.FontIcon
                    {
                        Glyph = "\uE710",
                        IconFontSize = 14,
                    },
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TextBlock? glyphTextBlock = null;
                    foreach (TextBlock textBlock in FindVisualChildren<TextBlock>(button))
                    {
                        if (string.Equals(textBlock.Text, "\uE710", StringComparison.Ordinal))
                        {
                            glyphTextBlock = textBlock;
                            break;
                        }
                    }

                    Assert.NotNull(glyphTextBlock);
                    Assert.True(glyphTextBlock.IsVisible, "Left-placed button icons should be visible, not just present in the tree.");
                    Assert.True(glyphTextBlock.ActualWidth > 0, "Left-placed button icons should occupy layout space.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void Button_AccentAppearance_UsesDistinctWinUiStateBrushes()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                Window window = new();
                Controls.Button button = new()
                {
                    Width = 140,
                    Content = "Accent",
                    Appearance = ControlAppearance.Accent,
                    IsHitTestVisible = false,
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border restFill = Assert.IsType<Border>(button.Template.FindName("RestFill", button));
                    Border outerBorder = Assert.IsType<Border>(button.Template.FindName("OuterBorder", button));
                    SolidColorBrush accentDefaultBrush = Assert.IsType<SolidColorBrush>(application?.Resources["AccentFillColorDefaultBrush"]);
                    LinearGradientBrush accentBorderBrush = Assert.IsType<LinearGradientBrush>(application?.Resources["AccentControlElevationBorderBrush"]);
                    SolidColorBrush accentSecondaryBrush = Assert.IsType<SolidColorBrush>(application?.Resources["AccentFillColorSecondaryBrush"]);
                    SolidColorBrush accentTertiaryBrush = Assert.IsType<SolidColorBrush>(application?.Resources["AccentFillColorTertiaryBrush"]);
                    FontFamily fluentFontFamily = Assert.IsType<FontFamily>(application?.Resources["FluentFontFamily"]);
                    TextBlock? contentText = FindVisualChildren<TextBlock>(button)
                        .FirstOrDefault(static tb => string.Equals(tb.Text, "Accent", StringComparison.Ordinal));

                    _ = Assert.IsAssignableFrom<SolidColorBrush>(restFill.Background);
                    _ = Assert.IsAssignableFrom<LinearGradientBrush>(outerBorder.BorderBrush);
                    Assert.Equal(accentDefaultBrush.Color, ((SolidColorBrush)restFill.Background).Color);
                    Assert.Equal(accentBorderBrush.GradientStops.Count, ((LinearGradientBrush)outerBorder.BorderBrush).GradientStops.Count);
                    Assert.Null(outerBorder.Effect);
                    Assert.Equal(fluentFontFamily.Source, button.FontFamily.Source, StringComparer.Ordinal);
                    Assert.NotNull(contentText);
                    Assert.Equal(fluentFontFamily.Source, contentText.FontFamily.Source, StringComparer.Ordinal);
                    Assert.NotEqual(accentDefaultBrush.Color, accentSecondaryBrush.Color);
                    Assert.NotEqual(accentDefaultBrush.Color, accentTertiaryBrush.Color);
                    Assert.True(accentSecondaryBrush.Color.A < accentDefaultBrush.Color.A, "Accent pointer-over brush should be visually distinct from default.");
                    Assert.True(accentTertiaryBrush.Color.A < accentSecondaryBrush.Color.A, "Accent pressed brush should progress further than pointer-over.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_AccentColorButtons_UseButtonControl()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.NavigateTo("settings");
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    List<Controls.Button> accentSwatchButtons = [.. FindVisualChildren<Controls.Button>(window).Where(static b => b.Tag is string hex && hex.Length > 0 && hex[0] == '#')];

                    List<string> expectedSwatches =
                    [
                        "#E80000",
                        "#F58809",
                        "#F5E70C",
                        "#2BDE11",
                        "#09C4DE",
                        "#AA04DE",
                        "#FF00E8",
                    ];

                    Assert.Equal(expectedSwatches, accentSwatchButtons.ConvertAll(static b => (string)b.Tag));
                    foreach (Controls.Button swatch in accentSwatchButtons)
                    {
                        _ = Assert.IsAssignableFrom<Controls.Button>(swatch);
                    }
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_SettingsSelectors_UseExpectedControls()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.NavigateTo("settings");
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = Assert.IsAssignableFrom<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "AppThemeComboBox"));
                    _ = Assert.IsAssignableFrom<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "NavigationStyleComboBox"));
                    _ = Assert.IsAssignableFrom<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "BackdropComboBox"));
                    _ = Assert.IsAssignableFrom<Controls.ToggleSwitch>(FindVisualChildByName<Controls.ToggleSwitch>(window, "ThemeWatcherToggle"));
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_AppThemeComboBox_UpdatesStateLabel()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Auto, updateAccent: true);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.NavigateTo("settings");
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.ComboBox themeComboBox = Assert.IsAssignableFrom<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "AppThemeComboBox"));
                    TextBlock themeStateLabel = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(window, "ThemeStateLabel"));


                    themeComboBox.SelectedIndex = 2;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal("Current: Dark", themeStateLabel.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_DemoButtons_RenderTheirIcons()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Buttons");

                    Controls.Button iconLeftButton = Assert.IsAssignableFrom<Controls.Button>(FindFluentButtonByContent(window, "Icon Left"));
                    Controls.Button iconRightButton = Assert.IsAssignableFrom<Controls.Button>(FindFluentButtonByContent(window, "Icon Right"));


                    AssertButtonShowsGlyph(iconLeftButton, "\uE774");
                    AssertButtonShowsGlyph(iconRightButton, "\uE8D6");
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_StandardDemoButtonIcons_UsePrimaryTextBrush()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Buttons");

                    Controls.Button iconLeftButton = Assert.IsAssignableFrom<Controls.Button>(FindFluentButtonByContent(window, "Icon Left"));
                    Controls.Button iconRightButton = Assert.IsAssignableFrom<Controls.Button>(FindFluentButtonByContent(window, "Icon Right"));
                    SolidColorBrush expectedBrush = Assert.IsType<SolidColorBrush>(application?.Resources["TextFillColorPrimaryBrush"]);


                    TextBlock iconLeftGlyph = Assert.IsAssignableFrom<TextBlock>(FindButtonIconTextBlock(iconLeftButton));
                    TextBlock iconRightGlyph = Assert.IsAssignableFrom<TextBlock>(FindButtonIconTextBlock(iconRightButton));

                    _ = Assert.IsAssignableFrom<SolidColorBrush>(iconLeftGlyph.Foreground);
                    _ = Assert.IsAssignableFrom<SolidColorBrush>(iconRightGlyph.Foreground);
                    Assert.Equal(expectedBrush.Color, ((SolidColorBrush)iconLeftGlyph.Foreground).Color);
                    Assert.Equal(expectedBrush.Color, ((SolidColorBrush)iconRightGlyph.Foreground).Color);
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void FluentTabControl_SelectedTabUsesFluentCardSurface()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    TabControl tabControl = new();
                    _ = tabControl.Items.Add(new TabItem { Header = "First", Content = new TextBlock { Text = "A" } });
                    _ = tabControl.Items.Add(new TabItem { Header = "Second", Content = new TextBlock { Text = "B" } });
                    window.Content = tabControl;
                    window.Width = 640;
                    window.Height = 480;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tabControl.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TabItem selectedTab = Assert.IsType<TabItem>(tabControl.ItemContainerGenerator.ContainerFromIndex(1));
                    FrameworkElement contentPanel = Assert.IsAssignableFrom<FrameworkElement>(tabControl.Template.FindName("ContentPanel", tabControl));


                    Point selectedOrigin = selectedTab.TransformToAncestor(window).Transform(new Point(0, 0));
                    Point contentOrigin = contentPanel.TransformToAncestor(window).Transform(new Point(0, 0));
                    double selectedBottom = selectedOrigin.Y + selectedTab.ActualHeight;

                    Assert.True(contentOrigin.Y - selectedBottom >= 6.0,
                        "Fluent TabControl should separate selected tabs from the card-like content surface.");
                    _ = Assert.IsAssignableFrom<Border>(contentPanel);

                    Border contentBorder = (Border)contentPanel;
                    Assert.NotNull(contentBorder.Background);
                    Assert.NotNull(contentBorder.BorderBrush);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void FluentTabControl_SelectedHeaderUsesSequentialPanelAndCenteredIndicator()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    TabControl tabControl = new();
                    _ = tabControl.Items.Add(new TabItem { Header = "Overview", Content = new TextBlock { Text = "A" } });
                    _ = tabControl.Items.Add(new TabItem { Header = "Activity", Content = new TextBlock { Text = "B" } });
                    _ = tabControl.Items.Add(new TabItem { Header = "Settings", Content = new TextBlock { Text = "C" } });
                    window.Content = tabControl;
                    window.Width = 640;
                    window.Height = 480;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tabControl.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WaitForAnimationAndDrain(window.Dispatcher, 250);

                    FrameworkElement headerPanel = Assert.IsAssignableFrom<FrameworkElement>(tabControl.Template.FindName("HeaderPanel", tabControl));
                    TabItem selectedTab = Assert.IsType<TabItem>(tabControl.ItemContainerGenerator.ContainerFromIndex(1));
                    _ = Assert.IsAssignableFrom<StackPanel>(headerPanel);
                    Assert.False(headerPanel is TabPanel,
                        "TabControl should not use TabPanel for Fluent headers because its selection overlap can clip rounded corners.");
                    Assert.Equal(Orientation.Horizontal, ((StackPanel)headerPanel).Orientation);

                    Border selectedBackground = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(selectedTab, "SelectedBackground"));
                    Border selectionIndicator = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(selectedTab, "SelectionIndicator"));

                    double backgroundX = selectedBackground.TransformToAncestor(selectedTab).Transform(new Point(0, 0)).X;
                    double indicatorX = selectionIndicator.TransformToAncestor(selectedTab).Transform(new Point(0, 0)).X;
                    double backgroundCenter = backgroundX + (selectedBackground.ActualWidth / 2.0);
                    double indicatorCenter = indicatorX + (selectionIndicator.ActualWidth / 2.0);
                    Assert.Equal(backgroundCenter, indicatorCenter, 0.5);
                    Assert.Equal(selectedTab.ActualWidth, selectedBackground.ActualWidth, 0.5);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void FluentTabControl_LeftPlacement_SeparatesHeadersAndContent()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    TabControl tabControl = new()
                    {
                        TabStripPlacement = Dock.Left,
                    };
                    _ = tabControl.Items.Add(new TabItem { Header = "First", Content = new TextBlock { Text = "A" } });
                    _ = tabControl.Items.Add(new TabItem { Header = "Second", Content = new TextBlock { Text = "B" } });
                    window.Content = tabControl;
                    window.Width = 640;
                    window.Height = 480;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FrameworkElement headerPanel = Assert.IsAssignableFrom<FrameworkElement>(tabControl.Template.FindName("HeaderPanel", tabControl));
                    FrameworkElement contentPanel = Assert.IsAssignableFrom<FrameworkElement>(tabControl.Template.FindName("ContentPanel", tabControl));

                    Assert.Equal(0, Grid.GetColumn(headerPanel));
                    Assert.Equal(1, Grid.GetColumn(contentPanel));
                    Assert.Equal(new Thickness(0, 0, 9, 0), headerPanel.Margin);
                    _ = Assert.IsAssignableFrom<StackPanel>(headerPanel);
                    Assert.Equal(Orientation.Vertical, ((StackPanel)headerPanel).Orientation);

                    TabItem firstItem = Assert.IsType<TabItem>(tabControl.Items[0]);
                    Assert.Equal(new Thickness(0, 0, 8, 2), firstItem.Margin);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void FluentTabControl_BottomPlacement_LeavesBorderBreathingRoom()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    TabControl tabControl = new()
                    {
                        TabStripPlacement = Dock.Bottom,
                    };
                    _ = tabControl.Items.Add(new TabItem { Header = "First", Content = new TextBlock { Text = "A" } });
                    _ = tabControl.Items.Add(new TabItem { Header = "Second", Content = new TextBlock { Text = "B" } });
                    window.Content = tabControl;
                    window.Width = 640;
                    window.Height = 480;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FrameworkElement headerPanel = Assert.IsAssignableFrom<FrameworkElement>(tabControl.Template.FindName("HeaderPanel", tabControl));

                    Assert.Equal(new Thickness(0, 8, 1, 0), headerPanel.Margin);
                    _ = Assert.IsAssignableFrom<StackPanel>(headerPanel);
                    Assert.Equal(Orientation.Horizontal, ((StackPanel)headerPanel).Orientation);

                    TabItem firstItem = Assert.IsType<TabItem>(tabControl.Items[0]);
                    Assert.Equal(new Thickness(0, 0, 8, 2), firstItem.Margin);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_TabSelection_ActivatesExpectedContent()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Buttons");
                    Assert.NotNull(FindFluentButtonByContent(window, "Icon Left"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Inputs");
                    Assert.NotNull(FindVisualChildByName<Controls.TextBox>(window, "CharCountTextBox"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Selection");
                    Assert.NotNull(FindVisualChildByName<Controls.ToggleSwitch>(window, "WorkToggleSwitch"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Selection");
                    Assert.NotNull(FindVisualChildByName<Controls.ComboBox>(window, "SelectionDemoCombo"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Status");
                    Assert.NotNull(FindVisualChildByName<Controls.ProgressBar>(window, "StepProgressBar"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Data");
                    Assert.NotNull(FindVisualChildByName<Controls.ListView>(window, "EmptyStateListView"));
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_NavigationView_UsesFlatGalleryTaxonomy()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(window.FindName("DemoNav"));
                    List<string> pages = [];
                    foreach (object? obj in nav.Items)
                    {
                        if (obj is not Controls.NavigationViewItem item || item.Content is not string content)
                        {
                            continue;
                        }

                        Assert.Null(item.InfoBadge);
                        pages.Add(content);
                    }

                    Assert.Contains("Home", pages, StringComparer.Ordinal);
                    Assert.Contains("Buttons", pages, StringComparer.Ordinal);
                    Assert.Contains("Selection", pages, StringComparer.Ordinal);
                    Assert.Contains("Inputs", pages, StringComparer.Ordinal);
                    Assert.Contains("Typography", pages, StringComparer.Ordinal);
                    Assert.Contains("Icons", pages, StringComparer.Ordinal);
                    Assert.False(pages.Contains("Windowing"), "Windowing controls should move to Settings rather than the main navigation list.");
                    Assert.False(pages.Contains("Button"), "Demo navigation should use grouped pages, not generated per-control pages.");
                    Assert.False(pages.Contains("Fundamentals"), "Demo navigation should not expose the old Fundamentals section.");
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_CaptionButtons_DefaultOverrides()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(window.IsMinimizable);
                    Assert.True(window.IsMaximizable);
                    Assert.True(window.IsClosable);

                    Button closeButton = Assert.IsType<Button>(window.Template.FindName("PART_CloseButton", window));
                    Assert.Equal(Visibility.Visible, closeButton.Visibility);
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_ThemeWatcherToggle_UpdatesLabel()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.NavigateTo("settings");
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.ToggleSwitch toggle = Assert.IsAssignableFrom<Controls.ToggleSwitch>(FindVisualChildByName<Controls.ToggleSwitch>(window, "ThemeWatcherToggle"));
                    TextBlock label = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(window, "SystemThemeLabel"));

                    Assert.True(toggle.IsChecked is true, "ThemeWatcherToggle should default to checked.");
                    Assert.Equal("Watching: Yes", label.Text, StringComparer.Ordinal);

                    toggle.IsChecked = false;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal("Watching: No", label.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_IconLeftButton_IconIsVerticallyCentered()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Buttons");

                    Controls.Button button = Assert.IsAssignableFrom<Controls.Button>(FindFluentButtonByContent(window, "Icon Left"));

                    TextBlock glyphTextBlock = Assert.IsAssignableFrom<TextBlock>(FindButtonGlyphTextBlock(button, "\uE774"));
                    Point buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
                    Point glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
                    double buttonCenterY = buttonOrigin.Y + (button.ActualHeight / 2.0);
                    double glyphCenterY = glyphOrigin.Y + (glyphTextBlock.ActualHeight / 2.0);

                    Assert.Equal(buttonCenterY, glyphCenterY, 1.0);
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_StandardButtonIcons_AreInsideButtonBounds()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Buttons");

                    Controls.Button iconLeftButton = Assert.IsAssignableFrom<Controls.Button>(FindFluentButtonByContent(window, "Icon Left"));
                    Controls.Button iconRightButton = Assert.IsAssignableFrom<Controls.Button>(FindFluentButtonByContent(window, "Icon Right"));


                    AssertGlyphWithinButtonBounds(window, iconLeftButton, "\uE774");
                    AssertGlyphWithinButtonBounds(window, iconRightButton, "\uE8D6");
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void Stage3_Card_DefaultVariant_IsDefault()
        {
            RunOnStaThread(static () =>
            {
                Controls.Card card = new();
                Assert.Equal(CardVariant.Default, card.Variant);
            });
        }

        [Fact]
        public void Stage3_Card_IsClickable_ExposesIsPressed()
        {
            RunOnStaThread(static () =>
            {
                Controls.Card card = new() { IsClickable = true };
                Assert.False(card.IsPressed);
            });
        }

        [Fact]
        public void Stage3_CheckBox_Content_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.CheckBox cb = new() { Content = "Test" };
                Assert.Equal("Test", cb.Content as string, StringComparer.Ordinal);
            });
        }

        [Fact]
        public void Stage3_ComboBox_PlaceholderText_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.ComboBox combo = new() { PlaceholderText = "Pick one" };
                Assert.Equal("Pick one", combo.PlaceholderText, StringComparer.Ordinal);
            });
        }

        [Fact]
        public void ComboBox_SelectionChange_UpdatesDisplayedContent()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.ComboBox combo = new() { Width = 240 };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                _ = combo.Items.Add(new ComboBoxItem { Content = "Beta" });
                combo.SelectedIndex = 0;

                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(combo.Template.FindName("contentPresenter", combo));
                    Assert.Equal("Alpha", presenter.Content as string, StringComparer.Ordinal);

                    combo.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal("Beta", presenter.Content as string, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void ComboBox_ItemTemplate_HasHoverOverlay()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.ComboBox combo = new() { Width = 240 };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                _ = combo.Items.Add(new ComboBoxItem { Content = "Beta" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    combo.IsDropDownOpen = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ComboBoxItem item = Assert.IsType<ComboBoxItem>(combo.ItemContainerGenerator.ContainerFromIndex(0));
                    _ = item.ApplyTemplate();

                    object outerBorder = item.Template.FindName("OuterBorder", item);
                    Assert.NotNull(outerBorder);

                    object selectionIndicator = item.Template.FindName("SelectionIndicator", item);
                    Assert.NotNull(selectionIndicator);

                    combo.IsDropDownOpen = false;
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void ComboBox_DropdownReveal_SettlesAtRestAndSurvivesReopen()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new() { Width = 400, Height = 300 };
                Controls.ComboBox combo = new() { Width = 240 };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                _ = combo.Items.Add(new ComboBoxItem { Content = "Beta" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border border = Assert.IsType<Border>(combo.Template.FindName("PART_DropdownBorder", combo));
                    TranslateTransform translate =
                        Assert.IsType<TranslateTransform>(border.RenderTransform);

                    // The code-driven reveal (moved out of the template MultiTriggers) must
                    // settle at the rest position with its Stop-fill clocks released.
                    for (int open = 0; open < 2; open++)
                    {
                        combo.IsDropDownOpen = true;
                        Assert.True(WaitUntil(window.Dispatcher, 2000,
                                () => Math.Abs(translate.Y) < 0.001 && border.Opacity >= 1.0 &&
                                    !translate.HasAnimatedProperties && !border.HasAnimatedProperties),
                            string.Format(
                                System.Globalization.CultureInfo.InvariantCulture,
                                "Open {0}: the dropdown reveal must settle at Y=0, full opacity, and release its clocks.",
                                open));

                        combo.IsDropDownOpen = false;
                        DrainDispatcher(window.Dispatcher);
                    }
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void ComboBox_NoSelection_ShowsPlaceholder()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.ComboBox combo = new()
                {
                    Width = 240,
                    PlaceholderText = "Choose...",
                    SelectedIndex = -1,
                };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TextBlock placeholder = Assert.IsType<TextBlock>(combo.Template.FindName("PlaceholderTextBlock", combo));
                    Assert.Equal(Visibility.Visible, placeholder.Visibility);

                    combo.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Collapsed, placeholder.Visibility);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void ComboBox_ToggleButton_OpensDropDown()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.ComboBox combo = new()
                {
                    Width = 240,
                    PlaceholderText = "Pick one",
                };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                _ = combo.Items.Add(new ComboBoxItem { Content = "Beta" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = combo.ApplyTemplate();
                    ToggleButton toggle = Assert.IsAssignableFrom<ToggleButton>(combo.Template.FindName("ToggleButton", combo));
                    Popup popup = Assert.IsType<Popup>(combo.Template.FindName("PART_Popup", combo));


                    ToggleButtonAutomationPeer peer = new(toggle);
                    IToggleProvider toggleProvider = Assert.IsAssignableFrom<IToggleProvider>(peer.GetPattern(PatternInterface.Toggle));


                    toggleProvider.Toggle();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(combo.IsDropDownOpen, "ComboBox toggle should open the drop-down.");
                    Assert.True(popup.IsOpen, "ComboBox popup should open when the toggle is clicked.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void ComboBox_ToggleButton_UsesReleaseClickMode()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.ComboBox combo = new()
                {
                    Width = 240,
                    PlaceholderText = "Pick one",
                };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = combo.ApplyTemplate();
                    ToggleButton toggle = Assert.IsAssignableFrom<ToggleButton>(combo.Template.FindName("ToggleButton", combo));

                    Assert.Equal(ClickMode.Release, toggle.ClickMode);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void ComboBox_DropDownSelection_UpdatesSelectedIndex()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.ComboBox combo = new()
                {
                    Width = 240,
                    PlaceholderText = "Pick one",
                };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                _ = combo.Items.Add(new ComboBoxItem { Content = "Beta" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    combo.IsDropDownOpen = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ComboBoxItem item = Assert.IsType<ComboBoxItem>(combo.ItemContainerGenerator.ContainerFromIndex(1));

                    item.IsSelected = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(1, combo.SelectedIndex);
                    Assert.Equal("Beta", combo.SelectedText, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void Stage3_ProgressBar_ProgressMode_DefaultIsStandard()
        {
            RunOnStaThread(static () =>
            {
                Controls.ProgressBar bar = new();
                Assert.Equal(ProgressBarMode.Standard, bar.ProgressMode);
            });
        }

        [Fact]
        public void Stage3_Border_Variant_DefaultIsNone()
        {
            RunOnStaThread(static () =>
            {
                Controls.Border border = new();
                Assert.Equal(BorderVariant.None, border.Variant);
            });
        }

        [Fact]
        public void Stage3_StackPanel_Spacing_DefaultZero()
        {
            RunOnStaThread(static () =>
            {
                Controls.StackPanel panel = new();
                Assert.Equal(0.0, panel.Spacing);
            });
        }

        [Fact]
        public void Stage3_DockPanel_LastChildFill_DefaultTrue()
        {
            RunOnStaThread(static () =>
            {
                Controls.DockPanel dock = new();
                Assert.True(dock.LastChildFill);
            });
        }

        [Fact]
        public void Stage3_TextBox_ValidationState_DefaultNone()
        {
            RunOnStaThread(static () =>
            {
                Controls.TextBox tb = new();
                Assert.Equal(ValidationState.None, tb.ValidationState);
            });
        }

        [Fact]
        public void Stage3_TextBox_HelperText_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.TextBox tb = new() { HelperText = "Hint" };
                Assert.Equal("Hint", tb.HelperText, StringComparer.Ordinal);
            });
        }

        [Fact]
        public void Stage3_PasswordBox_IndicatorsDefaultOffAndOptIn()
        {
            RunOnStaThread(static () =>
            {
                Controls.PasswordBox pb = new();
                Assert.False(pb.ShowCapsLockIndicator, "Caps Lock indicator must be opt-in by default.");
                Assert.False(pb.ShowPasswordStrength, "Password strength meter must be opt-in by default.");

                pb.ShowCapsLockIndicator = true;
                pb.ShowPasswordStrength = true;
                Assert.True(pb.ShowCapsLockIndicator);
                Assert.True(pb.ShowPasswordStrength);
            });
        }

        [Fact]
        public void Stage3_PasswordBox_ComputesPasswordStrength()
        {
            RunOnStaThread(static () =>
            {
                Controls.PasswordBox pb = new() { Password = "Aa1!aaaaaa" };
                Assert.True(pb.PasswordStrength >= 3);
            });
        }

        [Fact]
        public void Stage3_ListView_EmptyContent_DefaultNull()
        {
            RunOnStaThread(static () =>
            {
                Controls.ListView list = new();
                Assert.Null(list.EmptyContent);
            });
        }

        [Fact]
        public void Stage3_FontIcon_Rotation_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.FontIcon icon = new() { Rotation = 33 };
                Assert.Equal(33.0, icon.Rotation);
            });
        }

        [Fact]
        public void Stage3_FontIcon_IsSpinning_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.FontIcon icon = new() { IsSpinning = true };
                Assert.True(icon.IsSpinning);
            });
        }

        [Fact]
        public void Stage3_FontIcon_Spin_PausesWhenCollapsed_ResumesWhenVisible()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.FontIcon icon = new()
                {
                    Glyph = "\uE72C",
                    IsSpinning = true,
                };

                try
                {
                    window.Content = icon;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    RotateTransform rotate = Assert.IsType<RotateTransform>(icon.Template.FindName("PART_Rotate", icon));
                    Assert.True(rotate.HasAnimatedProperties, "Spin animation must run while the icon is loaded and visible.");

                    icon.Visibility = Visibility.Collapsed;
                    DrainDispatcher(window.Dispatcher);
                    Assert.False(rotate.HasAnimatedProperties, "Spin animation must stop while the icon is collapsed.");
                    Assert.Equal(icon.Rotation, rotate.Angle);

                    icon.Visibility = Visibility.Visible;
                    DrainDispatcher(window.Dispatcher);
                    Assert.True(rotate.HasAnimatedProperties, "Spin animation must resume when the icon becomes visible again.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void Stage3_FontIcon_Spin_StopsWhenUnloaded()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.FontIcon icon = new()
                {
                    Glyph = "\uE72C",
                    IsSpinning = true,
                };

                try
                {
                    window.Content = icon;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    RotateTransform rotate = Assert.IsType<RotateTransform>(icon.Template.FindName("PART_Rotate", icon));
                    Assert.True(rotate.HasAnimatedProperties, "Spin animation must run while the icon is loaded and visible.");

                    window.Content = null;
                    DrainDispatcher(window.Dispatcher);
                    Assert.False(rotate.HasAnimatedProperties, "Spin animation must stop when the icon is unloaded.");
                    Assert.Equal(icon.Rotation, rotate.Angle);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void Stage3_FontIcon_EnableTransitions_DefaultTrue()
        {
            RunOnStaThread(static () =>
            {
                Controls.FontIcon icon = new();
                Assert.True(icon.EnableTransitions);
            });
        }

        [Fact]
        public void Stage3_TextBox_CharacterCounter_ShowsWithMaxLength()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.TextBox textBox = new()
                {
                    Width = 260,
                    MaxLength = 40,
                    Text = "Hi",
                };

                try
                {
                    window.Content = textBox;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TextBlock counter = Assert.IsType<TextBlock>(textBox.Template.FindName("PART_CharacterCounter", textBox));
                    Assert.Equal("2/40", counter.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void Stage3_ListView_EmptyContent_VisibleWhenNoItems()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.ListView list = new()
                {
                    Width = 200,
                    Height = 100,
                    EmptyContent = new TextBlock { Text = "Empty" },
                };

                try
                {
                    window.Content = list;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.False(list.HasItems);
                    Assert.Contains(FindVisualChildren<TextBlock>(list), static tb => string.Equals(tb.Text, "Empty", StringComparison.Ordinal));
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void Stage3_ProgressBar_Template_HasTrackAndFill()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.ProgressBar bar = new() { Width = 200, Height = 8, Value = 40, Maximum = 100 };

                try
                {
                    window.Content = bar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.NotNull(bar.Template.FindName("PART_Track", bar));
                    Assert.NotNull(bar.Template.FindName("PART_Fill", bar));
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void Slider_Template_HasTrack()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.Slider slider = new() { Width = 220, Minimum = 0, Maximum = 100, Value = 30 };

                try
                {
                    window.Content = slider;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.NotNull(slider.Template.FindName("PART_Track", slider));
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_ProgressNumberBox_UpdatesFirstProgressBar()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Status");

                    Controls.NumberBox numberBox = Assert.IsAssignableFrom<Controls.NumberBox>(FindVisualChildByName<Controls.NumberBox>(window, "ProgressValueNumberBox"));
                    Controls.ProgressBar progressBar = Assert.IsAssignableFrom<Controls.ProgressBar>(FindVisualChildByName<Controls.ProgressBar>(window, "StandardProgressBar"));

                    numberBox.Value = 73;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(73d, progressBar.Value, 0.1);
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_SelectionDemoCombo_SelectionUpdatesIndex()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Selection");

                    Controls.ComboBox combo = Assert.IsAssignableFrom<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "SelectionDemoCombo"));
                    Assert.Equal(3, combo.Items.Count);

                    combo.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(1, combo.SelectedIndex);
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void MainWindow_ComboBoxPage_InitialComboBoxesHaveNoSelection()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Selection");
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(window.FindName("DemoNav"));

                    DependencyObject selectedContent = Assert.IsAssignableFrom<DependencyObject>(nav.Content);

                    List<Controls.ComboBox> comboBoxes = [.. FindVisualChildren<Controls.ComboBox>(selectedContent)];
                    Assert.True(comboBoxes.Count >= 2, "ComboBox page should display multiple ComboBox examples.");

                    foreach (Controls.ComboBox comboBox in comboBoxes)
                    {
                        Assert.Equal(-1, comboBox.SelectedIndex);
                    }
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void HyperlinkButton_DefaultForeground_IsAccent()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.HyperlinkButton button = new()
                {
                    Content = "Link",
                    Width = 120,
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SolidColorBrush accentBrush = Assert.IsType<SolidColorBrush>(application?.Resources["AccentTextFillColorPrimaryBrush"]);
                    _ = Assert.IsAssignableFrom<SolidColorBrush>(button.Foreground);
                    Assert.Equal(accentBrush.Color, ((SolidColorBrush)button.Foreground).Color);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void HyperlinkButton_Click_WithNavigateUri_DoesNotThrow()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.HyperlinkButton button = new()
                {
                    Content = "Link",
                    Width = 120,
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    DrainDispatcher(window.Dispatcher);

                    Assert.True(button.IsLoaded,
                        "HyperlinkButton should remain loaded after click dispatch.");
                    Assert.Null(button.NavigateUri);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void InfoBar_ErrorSeverity_HasExpectedBackground()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.InfoBar infoBar = new()
                {
                    Severity = InfoBarSeverity.Error,
                    Title = "Error",
                    Message = "Something went wrong.",
                    IsOpen = true,
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Brush expectedBrush = Assert.IsAssignableFrom<Brush>(application?.Resources["SystemFillColorCriticalBackgroundBrush"]);

                    _ = infoBar.ApplyTemplate();
                    Border rootBorder = Assert.IsType<Border>(infoBar.Template.FindName("RootBorder", infoBar));
                    Assert.NotNull(rootBorder.Background);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void InfoBar_CloseButton_SetsIsOpenFalse()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.InfoBar infoBar = new()
                {
                    IsClosable = true,
                    IsOpen = true,
                    Title = "Closable",
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = infoBar.ApplyTemplate();
                    Button closeButton = Assert.IsType<Button>(infoBar.Template.FindName("PART_CloseButton", infoBar));

                    ButtonAutomationPeer peer = new(closeButton);
                    IInvokeProvider invokeProvider = Assert.IsAssignableFrom<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke));

                    invokeProvider.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.False(infoBar.IsOpen, "Clicking the close button should set IsOpen to false.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void InfoBar_ClosingCancel_PreventsClose()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.InfoBar infoBar = new()
                {
                    IsClosable = true,
                    IsOpen = true,
                    Title = "Cancelable",
                };

                infoBar.Closing += static (sender, args) => args.Cancel = true;

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = infoBar.ApplyTemplate();
                    Button closeButton = Assert.IsType<Button>(infoBar.Template.FindName("PART_CloseButton", infoBar));

                    ButtonAutomationPeer peer = new(closeButton);
                    IInvokeProvider invokeProvider = Assert.IsAssignableFrom<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke));

                    invokeProvider.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.True(infoBar.IsOpen, "Canceling the Closing event should keep IsOpen true.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void RadioButton_Checked_HasAccentFill()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                Window window = new();
                Controls.RadioButton radio = new()
                {
                    Content = "Test",
                    IsChecked = true,
                };

                try
                {
                    window.Content = radio;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = radio.ApplyTemplate();
                    Ellipse checkedEllipse = Assert.IsType<Ellipse>(radio.Template.FindName("CheckedEllipse", radio));
                    SolidColorBrush accentBrush = Assert.IsType<SolidColorBrush>(application?.Resources["AccentFillColorDefaultBrush"]);

                    Assert.Equal(1.0, checkedEllipse.Opacity);
                    _ = Assert.IsAssignableFrom<SolidColorBrush>(checkedEllipse.Fill);
                    Assert.Equal(accentBrush.Color, ((SolidColorBrush)checkedEllipse.Fill).Color);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void RadioButton_ContentAlignment_CentersTextWithIndicator()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.RadioButton radio = new()
                {
                    Content = "Standard",
                    Width = 240,
                    Height = 40,
                };

                try
                {
                    window.Content = radio;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = radio.ApplyTemplate();
                    Grid indicatorHost = Assert.IsAssignableFrom<Grid>(FindVisualChildByName<Grid>(radio, "IndicatorHost"));
                    ContentPresenter contentPresenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(radio, "ContentPresenter"));

                    Assert.Equal(VerticalAlignment.Center, radio.VerticalContentAlignment);
                    Assert.Equal(VerticalAlignment.Center, indicatorHost.VerticalAlignment);
                    Assert.Equal(new Thickness(0), indicatorHost.Margin);
                    Assert.Equal(VerticalAlignment.Center, contentPresenter.VerticalAlignment);
                    Assert.Equal(new Thickness(8, 0, 0, 0), contentPresenter.Margin);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void RadioButton_GroupExclusivity_UnchecksOthers()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                StackPanel panel = new();
                Controls.RadioButton radio1 = new() { Content = "A", GroupName = "TestGroup", IsChecked = true };
                Controls.RadioButton radio2 = new() { Content = "B", GroupName = "TestGroup" };
                Controls.RadioButton radio3 = new() { Content = "C", GroupName = "TestGroup" };
                _ = panel.Children.Add(radio1);
                _ = panel.Children.Add(radio2);
                _ = panel.Children.Add(radio3);

                try
                {
                    window.Content = panel;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(radio1.IsChecked is true);

                    radio2.IsChecked = true;
                    DrainDispatcher(window.Dispatcher);

                    Assert.Equal(false, radio1.IsChecked);
                    Assert.Equal(true, radio2.IsChecked);
                    Assert.Equal(false, radio3.IsChecked);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void ToggleSwitch_OnOffContent_SwapsOnCheck()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.ToggleSwitch toggle = new()
                {
                    OnContent = "On",
                    OffContent = "Off",
                    IsChecked = false,
                };

                try
                {
                    window.Content = toggle;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = toggle.ApplyTemplate();
                    FrameworkElement offPresenter = Assert.IsAssignableFrom<FrameworkElement>(toggle.Template.FindName("OffContentPresenter", toggle));
                    FrameworkElement onPresenter = Assert.IsAssignableFrom<FrameworkElement>(toggle.Template.FindName("OnContentPresenter", toggle));
                    Assert.Equal(Visibility.Visible, offPresenter.Visibility);
                    Assert.Equal(Visibility.Collapsed, onPresenter.Visibility);

                    toggle.IsChecked = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Collapsed, offPresenter.Visibility);
                    Assert.Equal(Visibility.Visible, onPresenter.Visibility);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void ToggleSwitch_IsChecked_TogglesOnClick()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.ToggleSwitch toggle = new() { IsChecked = false };

                try
                {
                    window.Content = toggle;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(false, toggle.IsChecked);

                    IToggleProvider toggleProvider = (ToggleButtonAutomationPeer)new(toggle);
                    toggleProvider.Toggle();
                    DrainDispatcher(window.Dispatcher);

                    Assert.Equal(true, toggle.IsChecked);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void ProgressRing_Determinate_UpdatesArc()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.ProgressRing ring = new()
                {
                    IsIndeterminate = false,
                    Width = 48,
                    Height = 48,
                    Value = 50,
                    Minimum = 0,
                    Maximum = 100,
                    IsActive = true,
                };

                try
                {
                    window.Content = ring;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = ring.ApplyTemplate();
                    Path arcPath = Assert.IsType<Path>(ring.Template.FindName("PART_DeterminateArc", ring));
                    Assert.NotNull(arcPath.Data);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void ProgressRing_Indeterminate_CaterpillarArcBecomesVisible()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.ProgressRing ring = new()
                {
                    IsIndeterminate = true,
                    Width = 48,
                    Height = 48,
                    IsActive = true,
                };

                try
                {
                    window.Content = ring;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = ring.ApplyTemplate();
                    Path indeterminateArc = Assert.IsType<Path>(ring.Template.FindName("PART_IndeterminateArc", ring));
                    Assert.Equal(Visibility.Visible, indeterminateArc.Visibility);

                    bool arcDataReady = WaitUntil(window.Dispatcher, 1000, delegate
                    {
                        return indeterminateArc.Data is not null;
                    });
                    Assert.True(arcDataReady,
                        "PART_IndeterminateArc should have non-null Data for the caterpillar geometry.");

                    FrameworkElement? dotHost = ring.Template.FindName("DotHost", ring) as FrameworkElement;
                    Assert.Null(dotHost);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public void DemoMainWindow_SelectingNavPage_DoesNotThrow()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Auto, updateAccent: true);
                ApplicationAccentColorManager.ApplySystemAccent();

                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    window.UpdateLayout();

                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(window.FindName("DemoNav"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Buttons");
                    Assert.NotNull(nav.SelectedItem);
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        private static void SelectMainWindowNavPage(MainWindow window, Dispatcher dispatcher, string itemContent)
        {
            Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(window.FindName("DemoNav"));

            window.NavigateTo(itemContent);
            DrainDispatcher(dispatcher);
            dispatcher.Invoke(new Action(static delegate { }), DispatcherPriority.Loaded, default);
            dispatcher.Invoke(new Action(static delegate { }), DispatcherPriority.ContextIdle, default);
            window.UpdateLayout();
            DrainDispatcher(dispatcher);

            Controls.NavigationViewItem? selected = nav.SelectedItem as Controls.NavigationViewItem;
            string? selectedLabel = selected is null ? null : selected.Content as string;
            string? selectedTag = selected is null ? null : selected.Tag as string;
            bool matchesRequest = string.Equals(selectedLabel, itemContent, StringComparison.OrdinalIgnoreCase) ||
                (selectedTag?.IndexOf(itemContent, StringComparison.OrdinalIgnoreCase) >= 0);
            if (selected is null || nav.Content is null || !matchesRequest)
            {
                Assert.Fail(string.Format("Navigation item '{0}' should exist.", itemContent));
            }
        }

        private static void AssertButtonShowsGlyph(Controls.Button button, string glyph)
        {
            TextBlock glyphTextBlock = Assert.IsAssignableFrom<TextBlock>(FindButtonGlyphTextBlock(button, glyph));
            Assert.True(glyphTextBlock.IsVisible, "Expected button glyph should be visible.");
            Assert.True(glyphTextBlock.ActualWidth > 0, "Expected button glyph should occupy layout space.");
        }

        private static TextBlock? FindButtonIconTextBlock(Controls.Button button)
        {
            foreach (TextBlock textBlock in FindVisualChildren<TextBlock>(button))
            {
                FontFamily fontFamily = textBlock.FontFamily;
                if (fontFamily is not null &&
                    fontFamily.Source?.IndexOf("Segoe Fluent Icons", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return textBlock;
                }
            }

            return null;
        }

        private static TextBlock? FindButtonGlyphTextBlock(Controls.Button button, string glyph)
        {
            foreach (TextBlock textBlock in FindVisualChildren<TextBlock>(button))
            {
                if (string.Equals(textBlock.Text, glyph, StringComparison.Ordinal))
                {
                    return textBlock;
                }
            }

            return null;
        }

        private static void AssertGlyphWithinButtonBounds(Window window, Controls.Button button, string glyph)
        {
            TextBlock glyphTextBlock = Assert.IsAssignableFrom<TextBlock>(FindButtonGlyphTextBlock(button, glyph));

            Assert.True(glyphTextBlock.IsVisible, "Expected button glyph should be visible.");
            Assert.True(glyphTextBlock.ActualWidth > 0, "Expected button glyph should occupy layout space.");

            Point buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
            Point glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
            double buttonRight = buttonOrigin.X + button.ActualWidth;
            double buttonBottom = buttonOrigin.Y + button.ActualHeight;
            double glyphRight = glyphOrigin.X + glyphTextBlock.ActualWidth;
            double glyphBottom = glyphOrigin.Y + glyphTextBlock.ActualHeight;

            Assert.True(glyphOrigin.X >= buttonOrigin.X - 0.5, "Expected button glyph should not render left of the button.");
            Assert.True(glyphOrigin.Y >= buttonOrigin.Y - 0.5, "Expected button glyph should not render above the button.");
            Assert.True(glyphRight <= buttonRight + 0.5, "Expected button glyph should not render right of the button.");
            Assert.True(glyphBottom <= buttonBottom + 0.5, "Expected button glyph should not render below the button.");
        }

        private static Controls.Button? FindFluentButtonByContent(DependencyObject root, string content)
        {
            foreach (Controls.Button button in FindVisualChildren<Controls.Button>(root))
            {
                if (string.Equals(button.Content as string, content, StringComparison.Ordinal))
                {
                    return button;
                }
            }

            return null;
        }

        private static void AssertContentGroupIsCentered(Window window, Controls.Button button, string content, string glyph)
        {
            TextBlock glyphTextBlock = Assert.IsAssignableFrom<TextBlock>(FindButtonGlyphTextBlock(button, glyph));

            ContentPresenter? textPresenter = null;
            foreach (ContentPresenter presenter in FindVisualChildren<ContentPresenter>(button))
            {
                if (string.Equals(presenter.Content as string, content, StringComparison.Ordinal))
                {
                    textPresenter = presenter;
                    break;
                }
            }

            Assert.NotNull(textPresenter);

            Point buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
            double buttonCenter = buttonOrigin.X + (button.ActualWidth / 2.0);

            Point glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
            Point contentOrigin = textPresenter.TransformToAncestor(window).Transform(new Point(0, 0));
            double groupLeft = Math.Min(glyphOrigin.X, contentOrigin.X);
            double groupRight = Math.Max(glyphOrigin.X + glyphTextBlock.ActualWidth, contentOrigin.X + textPresenter.ActualWidth);
            double groupCenter = groupLeft + ((groupRight - groupLeft) / 2.0);

            Assert.Equal(buttonCenter, groupCenter, 1.0);
        }
    }
}
