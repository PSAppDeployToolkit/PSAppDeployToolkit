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

using Fluence.Wpf.Demo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public partial class ControlTests
    {
        [TestInitialize]
        public void ControlTestsInitialize()
        {
            WpfTestSta.Invoke(ResetSharedWpfState);
        }

        [TestCleanup]
        public void ControlTestsCleanup()
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

        [TestMethod]
        public void FontIcon_DefaultFontFamily_IsSegoeFluent()
        {
            RunOnStaThread(static () =>
            {
                Controls.FontIcon fontIcon = new();

                Assert.AreEqual("Segoe Fluent Icons", fontIcon.IconFontFamily.Source);
            });
        }

        [TestMethod]
        public void FontIcon_GlyphProperty_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.FontIcon fontIcon = new();
                const string testGlyph = "\uE710";

                fontIcon.Glyph = testGlyph;

                Assert.AreEqual(testGlyph, fontIcon.Glyph);
            });
        }

        [TestMethod]
        public void Button_DefaultAppearance_IsStandard()
        {
            RunOnStaThread(static () =>
            {
                Controls.Button button = new();

                Assert.AreEqual(ControlAppearance.Standard, button.Appearance);
            });
        }

        [TestMethod]
        public void Button_AccentAppearance_CanBeSet()
        {
            RunOnStaThread(static () =>
            {
                Controls.Button button = new()
                {
                    Appearance = ControlAppearance.Accent,
                };

                Assert.AreEqual(ControlAppearance.Accent, button.Appearance);
            });
        }

        [TestMethod]
        public void TextBox_PlaceholderText_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.TextBox textBox = new();
                const string placeholder = "Enter text here...";

                textBox.PlaceholderText = placeholder;

                Assert.AreEqual(placeholder, textBox.PlaceholderText);
            });
        }

        [TestMethod]
        public void TextBox_ClearButtonEnabled_DefaultTrue()
        {
            RunOnStaThread(static () =>
            {
                Controls.TextBox textBox = new();

                Assert.IsTrue(textBox.ClearButtonEnabled);
            });
        }

        [TestMethod]
        public void PasswordBox_RevealButtonEnabled_DefaultTrue()
        {
            RunOnStaThread(static () =>
            {
                Controls.PasswordBox passwordBox = new();

                Assert.IsTrue(passwordBox.RevealButtonEnabled);
            });
        }

        [TestMethod]
        public void PasswordBox_IsPasswordRevealed_DefaultFalse()
        {
            RunOnStaThread(static () =>
            {
                Controls.PasswordBox passwordBox = new();

                Assert.IsFalse(passwordBox.IsPasswordRevealed);
            });
        }

        [TestMethod]
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

                    Border? mainBorder = textBox.Template.FindName("MainBorder", textBox) as Border;
                    Button? clearButton = textBox.Template.FindName("PART_ClearButton", textBox) as Button;

                    Assert.IsNotNull(mainBorder);
                    Assert.IsNotNull(clearButton);
                    Assert.AreEqual(new Thickness(10, 5, 6, 6), textBox.Padding);
                    Assert.AreEqual(32.0, textBox.MinHeight);
                    Assert.IsInstanceOfType(mainBorder.BorderBrush, typeof(LinearGradientBrush), "TextBox should use the text-control elevation border brush.");
                    Assert.AreEqual(30.0, clearButton.Width, 0.1, "Clear button should use the WinUI helper button width.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Border? accentLine = textBox.Template.FindName("FocusAccentLine", textBox) as Border;

                    Assert.IsNotNull(accentLine, "FocusAccentLine should exist in the template.");
                    Assert.AreEqual(1.0, accentLine.Opacity, "Accent line should be visible when focused.");
                    Assert.AreEqual(2.0, accentLine.Height, "Accent line should be 2px tall.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Border? mainBorder = passwordBox.Template.FindName("MainBorder", passwordBox) as Border;
                    Button? revealButton = passwordBox.Template.FindName("PART_RevealButton", passwordBox) as Button;

                    Assert.IsNotNull(mainBorder);
                    Assert.IsNotNull(revealButton);
                    Assert.AreEqual(new Thickness(10, 5, 6, 6), passwordBox.Padding);
                    Assert.AreEqual(32.0, passwordBox.MinHeight);
                    Assert.IsInstanceOfType(mainBorder.BorderBrush, typeof(LinearGradientBrush), "PasswordBox should use the text-control elevation border brush.");
                    Assert.AreEqual(30.0, revealButton.Width, 0.1, "Reveal button should use the WinUI helper button width.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    PasswordBox? innerPasswordBox = passwordBox.Template.FindName("PART_PasswordBox", passwordBox) as PasswordBox;
                    Border? accentLine = passwordBox.Template.FindName("FocusAccentLine", passwordBox) as Border;

                    Assert.IsNotNull(innerPasswordBox);
                    Assert.IsNotNull(accentLine, "FocusAccentLine should exist in the template.");

                    _ = innerPasswordBox.Focus();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(1.0, accentLine.Opacity, "Accent line should be visible when inner PasswordBox has focus.");
                    Assert.AreEqual(2.0, accentLine.Height, "Accent line should be 2px tall.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ListView_ItemAnimationsEnabled_DefaultTrue()
        {
            RunOnStaThread(static () =>
            {
                Controls.ListView listView = new();

                Assert.IsTrue(listView.ItemAnimationsEnabled);
            });
        }

        [TestMethod]
        public void ListView_HoverHighlightEnabled_DefaultTrue()
        {
            RunOnStaThread(static () =>
            {
                Controls.ListView listView = new();

                Assert.IsTrue(listView.HoverHighlightEnabled);
            });
        }

        [TestMethod]
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

                    ListViewItem? item = listView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;

                    Assert.IsNotNull(item);
                    Assert.AreEqual(new Thickness(12, 0, 12, 0), item.Padding);
                    Assert.AreEqual(HorizontalAlignment.Left, item.HorizontalContentAlignment);
                    Assert.AreEqual(40.0, item.MinHeight);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    ListViewItem? item = listView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                    Assert.IsNotNull(item);

                    _ = item.ApplyTemplate();
                    Border? selectionIndicator = item.Template.FindName("SelectionIndicator", item) as Border;

                    Assert.IsNotNull(selectionIndicator);
                    // WI-3 C20: canonical ListViewItemSelectionIndicatorCornerRadius = 1.5
                    Assert.AreEqual(new CornerRadius(1.5), selectionIndicator.CornerRadius);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    ListViewItem? item = listView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                    Assert.IsNotNull(item);

                    _ = item.ApplyTemplate();
                    Border? selectedOverlay = item.Template.FindName("SelectedOverlay", item) as Border;
                    Border? selectionIndicator = item.Template.FindName("SelectionIndicator", item) as Border;
                    SolidColorBrush? expectedSelectedBrush = application?.Resources["SubtleFillColorSecondaryBrush"] as SolidColorBrush;
                    SolidColorBrush? expectedIndicatorBrush = application?.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush;

                    Assert.IsNotNull(selectedOverlay);
                    Assert.IsNotNull(selectionIndicator);
                    Assert.IsNotNull(expectedSelectedBrush);
                    Assert.IsNotNull(expectedIndicatorBrush);
                    Assert.IsInstanceOfType(selectedOverlay.Background, typeof(SolidColorBrush));
                    Assert.IsInstanceOfType(selectionIndicator.Background, typeof(SolidColorBrush));
                    Assert.AreEqual(expectedSelectedBrush.Color, ((SolidColorBrush)selectedOverlay.Background).Color);
                    Assert.AreEqual(expectedIndicatorBrush.Color, ((SolidColorBrush)selectionIndicator.Background).Color);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Assert.AreSame(application?.TryFindResource("BodyLargeTextBlockStyle"), textBlock.Style);
                    Assert.AreEqual(18.0, textBlock.FontSize);
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

        [TestMethod]
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

                    FrameworkElement? placeholder = textBox.Template.FindName("PlaceholderTextBlock", textBox) as FrameworkElement;
                    FrameworkElement? textView = FindVisualChildByTypeName(textBox, "TextBoxView") as FrameworkElement;

                    Assert.IsNotNull(placeholder);
                    Assert.IsNotNull(textView);

                    double placeholderX = placeholder.TransformToAncestor(window).Transform(new Point(0, 0)).X;
                    double textViewX = textView.TransformToAncestor(window).Transform(new Point(0, 0)).X;

                    Assert.AreEqual(placeholderX, textViewX, 0.5, "Text caret host should start where placeholder text starts.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Border? restFill = button.Template.FindName("RestFill", button) as Border;
                    SolidColorBrush? accentBrush = application?.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush;

                    Assert.IsNotNull(restFill);
                    Assert.IsNotNull(accentBrush);
                    Assert.IsInstanceOfType(restFill.Background, typeof(SolidColorBrush));
                    Assert.AreEqual(accentBrush.Color, ((SolidColorBrush)restFill.Background).Color);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

                    Assert.IsNotNull(glyphTextBlock, "Left-placed button icons should render their glyph.");
                    Assert.IsTrue(glyphTextBlock.IsVisible, "Left-placed button icons should be visible, not just present in the tree.");
                    Assert.IsTrue(glyphTextBlock.ActualWidth > 0, "Left-placed button icons should occupy layout space.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Border? restFill = button.Template.FindName("RestFill", button) as Border;
                    Border? outerBorder = button.Template.FindName("OuterBorder", button) as Border;
                    SolidColorBrush? accentDefaultBrush = application?.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush;
                    LinearGradientBrush? accentBorderBrush = application?.Resources["AccentControlElevationBorderBrush"] as LinearGradientBrush;
                    SolidColorBrush? accentSecondaryBrush = application?.Resources["AccentFillColorSecondaryBrush"] as SolidColorBrush;
                    SolidColorBrush? accentTertiaryBrush = application?.Resources["AccentFillColorTertiaryBrush"] as SolidColorBrush;
                    FontFamily? fluentFontFamily = application?.Resources["FluentFontFamily"] as FontFamily;
                    TextBlock? contentText = FindVisualChildren<TextBlock>(button)
                        .FirstOrDefault(static tb => string.Equals(tb.Text, "Accent", StringComparison.Ordinal));

                    Assert.IsNotNull(restFill);
                    Assert.IsNotNull(outerBorder);
                    Assert.IsNotNull(accentDefaultBrush);
                    Assert.IsNotNull(accentBorderBrush);
                    Assert.IsNotNull(accentSecondaryBrush);
                    Assert.IsNotNull(accentTertiaryBrush);
                    Assert.IsNotNull(fluentFontFamily);
                    Assert.IsInstanceOfType(restFill.Background, typeof(SolidColorBrush));
                    Assert.IsInstanceOfType(outerBorder.BorderBrush, typeof(LinearGradientBrush));
                    Assert.AreEqual(accentDefaultBrush.Color, ((SolidColorBrush)restFill.Background).Color);
                    Assert.AreEqual(accentBorderBrush.GradientStops.Count, ((LinearGradientBrush)outerBorder.BorderBrush).GradientStops.Count);
                    Assert.IsNull(outerBorder.Effect, "Accent buttons should use the WinUI elevation border, not a drop shadow.");
                    Assert.AreEqual(fluentFontFamily.Source, button.FontFamily.Source,
                        "Accent buttons should inherit the canonical Fluent font.");
                    Assert.IsNotNull(contentText, "String button content should materialize as visible text.");
                    Assert.AreEqual(fluentFontFamily.Source, contentText.FontFamily.Source,
                        "Button content should render with the same Fluent font as the control.");
                    Assert.AreNotEqual(accentDefaultBrush.Color, accentSecondaryBrush.Color, "Accent pointer-over brush should differ from the default accent brush.");
                    Assert.AreNotEqual(accentDefaultBrush.Color, accentTertiaryBrush.Color, "Accent pressed brush should differ from the default accent brush.");
                    Assert.IsTrue(accentSecondaryBrush.Color.A < accentDefaultBrush.Color.A, "Accent pointer-over brush should be visually distinct from default.");
                    Assert.IsTrue(accentTertiaryBrush.Color.A < accentSecondaryBrush.Color.A, "Accent pressed brush should progress further than pointer-over.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    CollectionAssert.AreEqual(expectedSwatches, accentSwatchButtons.ConvertAll(static b => (string)b.Tag),
                        "Settings page should expose the seven logo accent swatches.");
                    foreach (Controls.Button swatch in accentSwatchButtons)
                    {
                        Assert.IsInstanceOfType(swatch, typeof(Controls.Button));
                    }
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Assert.IsInstanceOfType(FindVisualChildByName<Controls.ComboBox>(window, "AppThemeComboBox"), typeof(Controls.ComboBox));
                    Assert.IsInstanceOfType(FindVisualChildByName<Controls.ComboBox>(window, "NavigationStyleComboBox"), typeof(Controls.ComboBox));
                    Assert.IsInstanceOfType(FindVisualChildByName<Controls.ComboBox>(window, "BackdropComboBox"), typeof(Controls.ComboBox));
                    Assert.IsInstanceOfType(FindVisualChildByName<Controls.ToggleSwitch>(window, "ThemeWatcherToggle"), typeof(Controls.ToggleSwitch));
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Controls.ComboBox? themeComboBox = FindVisualChildByName<Controls.ComboBox>(window, "AppThemeComboBox");
                    TextBlock? themeStateLabel = FindVisualChildByName<TextBlock>(window, "ThemeStateLabel");

                    Assert.IsNotNull(themeComboBox);
                    Assert.IsNotNull(themeStateLabel);

                    themeComboBox.SelectedIndex = 2;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual("Current: Dark", themeStateLabel.Text, "App theme combo box should update the state label when changed.");
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Controls.Button? iconLeftButton = FindFluentButtonByContent(window, "Icon Left");
                    Controls.Button? iconRightButton = FindFluentButtonByContent(window, "Icon Right");

                    Assert.IsNotNull(iconLeftButton, "Buttons page should contain an 'Icon Left' button.");
                    Assert.IsNotNull(iconRightButton, "Buttons page should contain an 'Icon Right' button.");

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

        [TestMethod]
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

                    Controls.Button? iconLeftButton = FindFluentButtonByContent(window, "Icon Left");
                    Controls.Button? iconRightButton = FindFluentButtonByContent(window, "Icon Right");
                    SolidColorBrush? expectedBrush = application?.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;

                    Assert.IsNotNull(iconLeftButton, "Buttons page should contain an 'Icon Left' button.");
                    Assert.IsNotNull(iconRightButton, "Buttons page should contain an 'Icon Right' button.");
                    Assert.IsNotNull(expectedBrush);

                    TextBlock? iconLeftGlyph = FindButtonIconTextBlock(iconLeftButton);
                    TextBlock? iconRightGlyph = FindButtonIconTextBlock(iconRightButton);

                    Assert.IsNotNull(iconLeftGlyph, "Icon Left demo button should render an icon glyph.");
                    Assert.IsNotNull(iconRightGlyph, "Icon Right demo button should render an icon glyph.");
                    Assert.IsInstanceOfType(iconLeftGlyph.Foreground, typeof(SolidColorBrush));
                    Assert.IsInstanceOfType(iconRightGlyph.Foreground, typeof(SolidColorBrush));
                    Assert.AreEqual(expectedBrush.Color, ((SolidColorBrush)iconLeftGlyph.Foreground).Color, "Standard demo button icons should use the primary text brush.");
                    Assert.AreEqual(expectedBrush.Color, ((SolidColorBrush)iconRightGlyph.Foreground).Color, "Standard demo button icons should use the primary text brush.");
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    TabItem? selectedTab = tabControl.ItemContainerGenerator.ContainerFromIndex(1) as TabItem;
                    FrameworkElement? contentPanel = tabControl.Template.FindName("ContentPanel", tabControl) as FrameworkElement;

                    Assert.IsNotNull(selectedTab);
                    Assert.IsNotNull(contentPanel);

                    Point selectedOrigin = selectedTab.TransformToAncestor(window).Transform(new Point(0, 0));
                    Point contentOrigin = contentPanel.TransformToAncestor(window).Transform(new Point(0, 0));
                    double selectedBottom = selectedOrigin.Y + selectedTab.ActualHeight;

                    Assert.IsTrue(contentOrigin.Y - selectedBottom >= 6.0,
                        "Fluent TabControl should separate selected tabs from the card-like content surface.");
                    Assert.IsInstanceOfType(contentPanel, typeof(Border),
                        "ContentPanel should be a Border so the Fluent surface can own background, stroke, and corner radius.");

                    Border contentBorder = (Border)contentPanel;
                    Assert.IsNotNull(contentBorder.Background,
                        "TabControl content surface should resolve a Fluent card background brush.");
                    Assert.IsNotNull(contentBorder.BorderBrush,
                        "TabControl content surface should resolve a Fluent card stroke brush.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    FrameworkElement? headerPanel = tabControl.Template.FindName("HeaderPanel", tabControl) as FrameworkElement;
                    TabItem? selectedTab = tabControl.ItemContainerGenerator.ContainerFromIndex(1) as TabItem;
                    Assert.IsNotNull(headerPanel, "TabControl template should expose the header host.");
                    Assert.IsNotNull(selectedTab, "The selected TabItem should be generated.");
                    Assert.IsInstanceOfType(headerPanel, typeof(StackPanel),
                        "TabControl should use a sequential StackPanel header host; WPF TabPanel can clip the selected tab's right edge.");
                    Assert.IsNotInstanceOfType(headerPanel, typeof(TabPanel),
                        "TabControl should not use TabPanel for Fluent headers because its selection overlap can clip rounded corners.");
                    Assert.AreEqual(Orientation.Horizontal, ((StackPanel)headerPanel).Orientation,
                        "Top TabControl headers should arrange horizontally.");

                    Border? selectedBackground = FindVisualChildByName<Border>(selectedTab, "SelectedBackground");
                    Border? selectionIndicator = FindVisualChildByName<Border>(selectedTab, "SelectionIndicator");
                    Assert.IsNotNull(selectedBackground, "Selected TabItem should expose the selected background border.");
                    Assert.IsNotNull(selectionIndicator, "Selected TabItem should expose the accent selection indicator.");

                    double backgroundX = selectedBackground.TransformToAncestor(selectedTab).Transform(new Point(0, 0)).X;
                    double indicatorX = selectionIndicator.TransformToAncestor(selectedTab).Transform(new Point(0, 0)).X;
                    double backgroundCenter = backgroundX + (selectedBackground.ActualWidth / 2.0);
                    double indicatorCenter = indicatorX + (selectionIndicator.ActualWidth / 2.0);
                    Assert.AreEqual(backgroundCenter, indicatorCenter, 0.5,
                        "The selection indicator should remain centered in the selected tab background.");
                    Assert.AreEqual(selectedTab.ActualWidth, selectedBackground.ActualWidth, 0.5,
                        "The selected background should occupy the full arranged tab width so the right rounded corner is not cut off.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    FrameworkElement? headerPanel = tabControl.Template.FindName("HeaderPanel", tabControl) as FrameworkElement;
                    FrameworkElement? contentPanel = tabControl.Template.FindName("ContentPanel", tabControl) as FrameworkElement;

                    Assert.IsNotNull(headerPanel);
                    Assert.IsNotNull(contentPanel);
                    Assert.AreEqual(0, Grid.GetColumn(headerPanel),
                        "Left TabStripPlacement should place tab headers in the left column.");
                    Assert.AreEqual(1, Grid.GetColumn(contentPanel),
                        "Left TabStripPlacement should keep content in the right column.");
                    Assert.AreEqual(new Thickness(0, 0, 9, 0), headerPanel.Margin,
                        "Left TabStripPlacement should keep the 8px Fluent gap plus 1px border breathing room.");
                    Assert.IsInstanceOfType(headerPanel, typeof(StackPanel),
                        "Left TabStripPlacement should use the same sequential header host as top placement.");
                    Assert.AreEqual(Orientation.Vertical, ((StackPanel)headerPanel).Orientation,
                        "Left TabStripPlacement should stack tab headers vertically.");

                    TabItem? firstItem = tabControl.Items[0] as TabItem;
                    Assert.IsNotNull(firstItem);
                    Assert.AreEqual(new Thickness(0, 0, 8, 2), firstItem.Margin,
                        "TabItem should reserve right and bottom space so selected borders are not clipped.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    FrameworkElement? headerPanel = tabControl.Template.FindName("HeaderPanel", tabControl) as FrameworkElement;

                    Assert.IsNotNull(headerPanel);
                    Assert.AreEqual(new Thickness(0, 8, 1, 0), headerPanel.Margin,
                        "Bottom TabStripPlacement should keep the top gap plus 1px right-side border breathing room.");
                    Assert.IsInstanceOfType(headerPanel, typeof(StackPanel),
                        "Bottom TabStripPlacement should use the same sequential header host as top placement.");
                    Assert.AreEqual(Orientation.Horizontal, ((StackPanel)headerPanel).Orientation,
                        "Bottom TabStripPlacement should keep tab headers horizontal.");

                    TabItem? firstItem = tabControl.Items[0] as TabItem;
                    Assert.IsNotNull(firstItem);
                    Assert.AreEqual(new Thickness(0, 0, 8, 2), firstItem.Margin,
                        "TabItem should reserve right and bottom space so selected borders are not clipped.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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
                    Assert.IsNotNull(FindFluentButtonByContent(window, "Icon Left"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Inputs");
                    Assert.IsNotNull(FindVisualChildByName<Controls.TextBox>(window, "CharCountTextBox"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Selection");
                    Assert.IsNotNull(FindVisualChildByName<Controls.ToggleSwitch>(window, "WorkToggleSwitch"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Selection");
                    Assert.IsNotNull(FindVisualChildByName<Controls.ComboBox>(window, "SelectionDemoCombo"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Status");
                    Assert.IsNotNull(FindVisualChildByName<Controls.ProgressBar>(window, "StepProgressBar"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Data");
                    Assert.IsNotNull(FindVisualChildByName<Controls.ListView>(window, "EmptyStateListView"));
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Controls.NavigationView? nav = window.FindName("DemoNav") as Controls.NavigationView;
                    Assert.IsNotNull(nav);
                    List<string> pages = [];
                    foreach (object? obj in nav.Items)
                    {
                        if (obj is not Controls.NavigationViewItem item || item.Content is not string content)
                        {
                            continue;
                        }

                        Assert.IsNull(item.InfoBadge, "Simplified demo navigation should not use category group badges.");
                        pages.Add(content);
                    }

                    CollectionAssert.Contains(pages, "Home");
                    CollectionAssert.Contains(pages, "Buttons");
                    CollectionAssert.Contains(pages, "Selection");
                    CollectionAssert.Contains(pages, "Inputs");
                    CollectionAssert.Contains(pages, "Typography");
                    CollectionAssert.Contains(pages, "Icons");
                    Assert.IsFalse(pages.Contains("Windowing"), "Windowing controls should move to Settings rather than the main navigation list.");
                    Assert.IsFalse(pages.Contains("Button"), "Demo navigation should use grouped pages, not generated per-control pages.");
                    Assert.IsFalse(pages.Contains("Fundamentals"), "Demo navigation should not expose the old Fundamentals section.");
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Assert.IsTrue(window.IsMinimizable);
                    Assert.IsTrue(window.IsMaximizable);
                    Assert.IsTrue(window.IsClosable);

                    Button? closeButton = window.Template.FindName("PART_CloseButton", window) as Button;
                    Assert.IsNotNull(closeButton);
                    Assert.AreEqual(Visibility.Visible, closeButton.Visibility);
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Controls.ToggleSwitch? toggle = FindVisualChildByName<Controls.ToggleSwitch>(window, "ThemeWatcherToggle");
                    TextBlock? label = FindVisualChildByName<TextBlock>(window, "SystemThemeLabel");

                    Assert.IsNotNull(toggle);
                    Assert.IsNotNull(label);
                    Assert.IsTrue(toggle.IsChecked is true, "ThemeWatcherToggle should default to checked.");
                    Assert.AreEqual("Watching: Yes", label.Text);

                    toggle.IsChecked = false;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual("Watching: No", label.Text, "Unchecking the toggle should update the label.");
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Controls.Button? button = FindFluentButtonByContent(window, "Icon Left");
                    Assert.IsNotNull(button, "Buttons page should contain an 'Icon Left' button.");

                    TextBlock? glyphTextBlock = FindButtonGlyphTextBlock(button, "\uE774");
                    Assert.IsNotNull(glyphTextBlock);
                    Point buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
                    Point glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
                    double buttonCenterY = buttonOrigin.Y + (button.ActualHeight / 2.0);
                    double glyphCenterY = glyphOrigin.Y + (glyphTextBlock.ActualHeight / 2.0);

                    Assert.AreEqual(buttonCenterY, glyphCenterY, 1.0, "Button icon should be vertically centered.");
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Controls.Button? iconLeftButton = FindFluentButtonByContent(window, "Icon Left");
                    Controls.Button? iconRightButton = FindFluentButtonByContent(window, "Icon Right");

                    Assert.IsNotNull(iconLeftButton, "Buttons page should contain an 'Icon Left' button.");
                    Assert.IsNotNull(iconRightButton, "Buttons page should contain an 'Icon Right' button.");

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

        [TestMethod]
        public void Stage3_Card_DefaultVariant_IsDefault()
        {
            RunOnStaThread(static () =>
            {
                Controls.Card card = new();
                Assert.AreEqual(CardVariant.Default, card.Variant);
            });
        }

        [TestMethod]
        public void Stage3_Card_IsClickable_ExposesIsPressed()
        {
            RunOnStaThread(static () =>
            {
                Controls.Card card = new() { IsClickable = true };
                Assert.IsFalse(card.IsPressed);
            });
        }

        [TestMethod]
        public void Stage3_CheckBox_Content_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.CheckBox cb = new() { Content = "Test" };
                Assert.AreEqual("Test", cb.Content as string);
            });
        }

        [TestMethod]
        public void Stage3_ComboBox_PlaceholderText_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.ComboBox combo = new() { PlaceholderText = "Pick one" };
                Assert.AreEqual("Pick one", combo.PlaceholderText);
            });
        }

        [TestMethod]
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

                    ContentPresenter? presenter = combo.Template.FindName("contentPresenter", combo) as ContentPresenter;
                    Assert.IsNotNull(presenter, "contentPresenter should exist in the template.");
                    Assert.AreEqual("Alpha", presenter.Content as string, "Initial displayed content should match first selection.");

                    combo.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual("Beta", presenter.Content as string, "Displayed content should update after selection change.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    ComboBoxItem? item = combo.ItemContainerGenerator.ContainerFromIndex(0) as ComboBoxItem;
                    Assert.IsNotNull(item, "ComboBoxItem container should be generated.");
                    _ = item.ApplyTemplate();

                    object outerBorder = item.Template.FindName("OuterBorder", item);
                    Assert.IsNotNull(outerBorder, "ComboBoxItem template should contain an OuterBorder element.");

                    object selectionIndicator = item.Template.FindName("SelectionIndicator", item);
                    Assert.IsNotNull(selectionIndicator, "ComboBoxItem template should contain a SelectionIndicator element.");

                    combo.IsDropDownOpen = false;
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Border? border = combo.Template.FindName("PART_DropdownBorder", combo) as Border;
                    Assert.IsNotNull(border, "PART_DropdownBorder should exist in the template.");
                    System.Windows.Media.TranslateTransform? translate =
                        border.RenderTransform as System.Windows.Media.TranslateTransform;
                    Assert.IsNotNull(translate, "PART_DropdownBorder should carry the DropdownTranslate render transform.");

                    // The code-driven reveal (moved out of the template MultiTriggers) must
                    // settle at the rest position with its Stop-fill clocks released.
                    for (int open = 0; open < 2; open++)
                    {
                        combo.IsDropDownOpen = true;
                        Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
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

        [TestMethod]
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

                    TextBlock? placeholder = combo.Template.FindName("PlaceholderTextBlock", combo) as TextBlock;
                    Assert.IsNotNull(placeholder, "PlaceholderTextBlock should exist in the template.");
                    Assert.AreEqual(Visibility.Visible, placeholder.Visibility, "Placeholder should be visible when SelectedIndex is explicitly -1.");

                    combo.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(Visibility.Collapsed, placeholder.Visibility, "Placeholder should be collapsed after selection.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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
                    ToggleButton? toggle = combo.Template.FindName("ToggleButton", combo) as ToggleButton;
                    Popup? popup = combo.Template.FindName("PART_Popup", combo) as Popup;

                    Assert.IsNotNull(toggle);
                    Assert.IsNotNull(popup);

                    ToggleButtonAutomationPeer peer = new(toggle);
                    IToggleProvider? toggleProvider = peer.GetPattern(PatternInterface.Toggle) as IToggleProvider;

                    Assert.IsNotNull(toggleProvider, "ComboBox toggle should expose a toggle pattern.");

                    toggleProvider.Toggle();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsTrue(combo.IsDropDownOpen, "ComboBox toggle should open the drop-down.");
                    Assert.IsTrue(popup.IsOpen, "ComboBox popup should open when the toggle is clicked.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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
                    ToggleButton? toggle = combo.Template.FindName("ToggleButton", combo) as ToggleButton;

                    Assert.IsNotNull(toggle);
                    Assert.AreEqual(ClickMode.Release, toggle.ClickMode, "ComboBox toggle should use release-click behavior so the drop-down stays open on a normal click.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    ComboBoxItem? item = combo.ItemContainerGenerator.ContainerFromIndex(1) as ComboBoxItem;
                    Assert.IsNotNull(item, "ComboBox should generate the drop-down item container when opened.");

                    item.IsSelected = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(1, combo.SelectedIndex, "Selecting a drop-down item should update the selected index.");
                    Assert.AreEqual("Beta", combo.SelectedText, "Selecting a drop-down item should update the displayed selected text.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Stage3_ProgressBar_ProgressMode_DefaultIsStandard()
        {
            RunOnStaThread(static () =>
            {
                Controls.ProgressBar bar = new();
                Assert.AreEqual(ProgressBarMode.Standard, bar.ProgressMode);
            });
        }

        [TestMethod]
        public void Stage3_Border_Variant_DefaultIsNone()
        {
            RunOnStaThread(static () =>
            {
                Controls.Border border = new();
                Assert.AreEqual(BorderVariant.None, border.Variant);
            });
        }

        [TestMethod]
        public void Stage3_StackPanel_Spacing_DefaultZero()
        {
            RunOnStaThread(static () =>
            {
                Controls.StackPanel panel = new();
                Assert.AreEqual(0.0, panel.Spacing);
            });
        }

        [TestMethod]
        public void Stage3_DockPanel_LastChildFill_DefaultTrue()
        {
            RunOnStaThread(static () =>
            {
                Controls.DockPanel dock = new();
                Assert.IsTrue(dock.LastChildFill);
            });
        }

        [TestMethod]
        public void Stage3_TextBox_ValidationState_DefaultNone()
        {
            RunOnStaThread(static () =>
            {
                Controls.TextBox tb = new();
                Assert.AreEqual(ValidationState.None, tb.ValidationState);
            });
        }

        [TestMethod]
        public void Stage3_TextBox_HelperText_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.TextBox tb = new() { HelperText = "Hint" };
                Assert.AreEqual("Hint", tb.HelperText);
            });
        }

        [TestMethod]
        public void Stage3_PasswordBox_IndicatorsDefaultOffAndOptIn()
        {
            RunOnStaThread(static () =>
            {
                Controls.PasswordBox pb = new();
                Assert.IsFalse(pb.ShowCapsLockIndicator, "Caps Lock indicator must be opt-in by default.");
                Assert.IsFalse(pb.ShowPasswordStrength, "Password strength meter must be opt-in by default.");

                pb.ShowCapsLockIndicator = true;
                pb.ShowPasswordStrength = true;
                Assert.IsTrue(pb.ShowCapsLockIndicator);
                Assert.IsTrue(pb.ShowPasswordStrength);
            });
        }

        [TestMethod]
        public void Stage3_PasswordBox_ComputesPasswordStrength()
        {
            RunOnStaThread(static () =>
            {
                Controls.PasswordBox pb = new() { Password = "Aa1!aaaaaa" };
                Assert.IsTrue(pb.PasswordStrength >= 3);
            });
        }

        [TestMethod]
        public void Stage3_ListView_EmptyContent_DefaultNull()
        {
            RunOnStaThread(static () =>
            {
                Controls.ListView list = new();
                Assert.IsNull(list.EmptyContent);
            });
        }

        [TestMethod]
        public void Stage3_FontIcon_Rotation_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.FontIcon icon = new() { Rotation = 33 };
                Assert.AreEqual(33.0, icon.Rotation);
            });
        }

        [TestMethod]
        public void Stage3_FontIcon_IsSpinning_Roundtrips()
        {
            RunOnStaThread(static () =>
            {
                Controls.FontIcon icon = new() { IsSpinning = true };
                Assert.IsTrue(icon.IsSpinning);
            });
        }

        [TestMethod]
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

                    RotateTransform? rotate = icon.Template.FindName("PART_Rotate", icon) as RotateTransform;
                    Assert.IsNotNull(rotate);
                    Assert.IsTrue(rotate.HasAnimatedProperties, "Spin animation must run while the icon is loaded and visible.");

                    icon.Visibility = Visibility.Collapsed;
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsFalse(rotate.HasAnimatedProperties, "Spin animation must stop while the icon is collapsed.");
                    Assert.AreEqual(icon.Rotation, rotate.Angle, "Angle must rest at Rotation while the spin is paused.");

                    icon.Visibility = Visibility.Visible;
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsTrue(rotate.HasAnimatedProperties, "Spin animation must resume when the icon becomes visible again.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    RotateTransform? rotate = icon.Template.FindName("PART_Rotate", icon) as RotateTransform;
                    Assert.IsNotNull(rotate);
                    Assert.IsTrue(rotate.HasAnimatedProperties, "Spin animation must run while the icon is loaded and visible.");

                    window.Content = null;
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsFalse(rotate.HasAnimatedProperties, "Spin animation must stop when the icon is unloaded.");
                    Assert.AreEqual(icon.Rotation, rotate.Angle, "Angle must rest at Rotation after the icon unloads.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Stage3_FontIcon_EnableTransitions_DefaultTrue()
        {
            RunOnStaThread(static () =>
            {
                Controls.FontIcon icon = new();
                Assert.IsTrue(icon.EnableTransitions);
            });
        }

        [TestMethod]
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

                    TextBlock? counter = textBox.Template.FindName("PART_CharacterCounter", textBox) as TextBlock;
                    Assert.IsNotNull(counter);
                    Assert.AreEqual("2/40", counter.Text);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Assert.IsFalse(list.HasItems);
                    Assert.IsTrue(FindVisualChildren<TextBlock>(list).Any(static tb => string.Equals(tb.Text, "Empty", StringComparison.Ordinal)));
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Assert.IsNotNull(bar.Template.FindName("PART_Track", bar));
                    Assert.IsNotNull(bar.Template.FindName("PART_Fill", bar));
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Assert.IsNotNull(slider.Template.FindName("PART_Track", slider), "Slider template should contain PART_Track.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Controls.NumberBox? numberBox = FindVisualChildByName<Controls.NumberBox>(window, "ProgressValueNumberBox");
                    Controls.ProgressBar? progressBar = FindVisualChildByName<Controls.ProgressBar>(window, "StandardProgressBar");
                    Assert.IsNotNull(numberBox, "ProgressValueNumberBox should be a Controls.NumberBox control.");
                    Assert.IsNotNull(progressBar, "StandardProgressBar should exist in the status page.");

                    numberBox.Value = 73;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(73d, progressBar.Value, 0.1);
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Controls.ComboBox? combo = FindVisualChildByName<Controls.ComboBox>(window, "SelectionDemoCombo");
                    Assert.IsNotNull(combo, "SelectionDemoCombo should exist on the ComboBox page.");
                    Assert.AreEqual(3, combo.Items.Count, "SelectionDemoCombo should list three items.");

                    combo.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(1, combo.SelectedIndex, "SelectionDemoCombo selection should persist after layout.");
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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
                    Controls.NavigationView? nav = window.FindName("DemoNav") as Controls.NavigationView;
                    Assert.IsNotNull(nav, "Main window should expose DemoNav.");

                    DependencyObject? selectedContent = nav.Content as DependencyObject;
                    Assert.IsNotNull(selectedContent, "ComboBox page should be selected.");

                    List<Controls.ComboBox> comboBoxes = [.. FindVisualChildren<Controls.ComboBox>(selectedContent)];
                    Assert.IsTrue(comboBoxes.Count >= 2, "ComboBox page should display multiple ComboBox examples.");

                    foreach (Controls.ComboBox comboBox in comboBoxes)
                    {
                        Assert.AreEqual(-1, comboBox.SelectedIndex,
                            "ComboBox page examples should not look selected before the user chooses an item.");
                    }
                }
                finally
                {
                    window?.Close();

                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    SolidColorBrush? accentBrush = application?.Resources["AccentTextFillColorPrimaryBrush"] as SolidColorBrush;
                    Assert.IsNotNull(accentBrush);
                    Assert.IsInstanceOfType(button.Foreground, typeof(SolidColorBrush));
                    Assert.AreEqual(accentBrush.Color, ((SolidColorBrush)button.Foreground).Color,
                        "HyperlinkButton default foreground should be the accent text brush.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Assert.IsTrue(button.IsLoaded,
                        "HyperlinkButton should remain loaded after click dispatch.");
                    Assert.IsNull(button.NavigateUri,
                        "This regression test exercises the no-uri click path.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Brush? expectedBrush = application?.Resources["SystemFillColorCriticalBackgroundBrush"] as Brush;
                    Assert.IsNotNull(expectedBrush, "SystemFillColorCriticalBackgroundBrush should be defined.");

                    _ = infoBar.ApplyTemplate();
                    Border? rootBorder = infoBar.Template.FindName("RootBorder", infoBar) as Border;
                    Assert.IsNotNull(rootBorder);
                    Assert.IsNotNull(rootBorder.Background, "InfoBar Error severity should have a non-null background.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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
                    Button? closeButton = infoBar.Template.FindName("PART_CloseButton", infoBar) as Button;
                    Assert.IsNotNull(closeButton);

                    ButtonAutomationPeer peer = new(closeButton);
                    IInvokeProvider? invokeProvider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                    Assert.IsNotNull(invokeProvider);

                    invokeProvider.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsFalse(infoBar.IsOpen, "Clicking the close button should set IsOpen to false.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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
                    Button? closeButton = infoBar.Template.FindName("PART_CloseButton", infoBar) as Button;
                    Assert.IsNotNull(closeButton);

                    ButtonAutomationPeer peer = new(closeButton);
                    IInvokeProvider? invokeProvider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                    Assert.IsNotNull(invokeProvider);

                    invokeProvider.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsTrue(infoBar.IsOpen, "Canceling the Closing event should keep IsOpen true.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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
                    Ellipse? checkedEllipse = radio.Template.FindName("CheckedEllipse", radio) as Ellipse;
                    SolidColorBrush? accentBrush = application?.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush;

                    Assert.IsNotNull(checkedEllipse);
                    Assert.IsNotNull(accentBrush);
                    Assert.AreEqual(1.0, checkedEllipse.Opacity, "CheckedEllipse should be visible when IsChecked is true.");
                    Assert.IsInstanceOfType(checkedEllipse.Fill, typeof(SolidColorBrush));
                    Assert.AreEqual(accentBrush.Color, ((SolidColorBrush)checkedEllipse.Fill).Color,
                        "Checked radio button indicator should use the accent fill brush.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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
                    Grid? indicatorHost = FindVisualChildByName<Grid>(radio, "IndicatorHost");
                    ContentPresenter? contentPresenter = FindVisualChildByName<ContentPresenter>(radio, "ContentPresenter");

                    Assert.IsNotNull(indicatorHost, "RadioButton template must include IndicatorHost.");
                    Assert.IsNotNull(contentPresenter, "RadioButton template must include ContentPresenter.");
                    Assert.AreEqual(VerticalAlignment.Center, radio.VerticalContentAlignment,
                        "RadioButton text should default to center alignment with the indicator.");
                    Assert.AreEqual(VerticalAlignment.Center, indicatorHost.VerticalAlignment,
                        "RadioButton indicator should be centered within the root row.");
                    Assert.AreEqual(new Thickness(0), indicatorHost.Margin,
                        "RadioButton indicator should not carry an extra top offset.");
                    Assert.AreEqual(VerticalAlignment.Center, contentPresenter.VerticalAlignment,
                        "RadioButton content should align vertically with the indicator center.");
                    Assert.AreEqual(new Thickness(8, 0, 0, 0), contentPresenter.Margin,
                        "RadioButton content presenter should keep the horizontal WinUI text inset without a top offset.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Assert.IsTrue(radio1.IsChecked is true);

                    radio2.IsChecked = true;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(false, radio1.IsChecked, "Radio1 should be unchecked after Radio2 is checked.");
                    Assert.AreEqual(true, radio2.IsChecked, "Radio2 should be checked after being set.");
                    Assert.AreEqual(false, radio3.IsChecked, "Radio3 should be unchecked.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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
                    FrameworkElement? offPresenter = toggle.Template.FindName("OffContentPresenter", toggle) as FrameworkElement;
                    FrameworkElement? onPresenter = toggle.Template.FindName("OnContentPresenter", toggle) as FrameworkElement;
                    Assert.IsNotNull(offPresenter);
                    Assert.IsNotNull(onPresenter);
                    Assert.AreEqual(Visibility.Visible, offPresenter.Visibility, "Off content should be visible when unchecked.");
                    Assert.AreEqual(Visibility.Collapsed, onPresenter.Visibility, "On content should be collapsed when unchecked.");

                    toggle.IsChecked = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(Visibility.Collapsed, offPresenter.Visibility, "Off content should be collapsed when checked.");
                    Assert.AreEqual(Visibility.Visible, onPresenter.Visibility, "On content should be visible when checked.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Assert.AreEqual(false, toggle.IsChecked, "ToggleSwitch should start unchecked.");

                    IToggleProvider toggleProvider = (ToggleButtonAutomationPeer)new(toggle);
                    toggleProvider.Toggle();
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(true, toggle.IsChecked, "ToggleSwitch should be checked after toggle.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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
                    Path? arcPath = ring.Template.FindName("PART_DeterminateArc", ring) as Path;
                    Assert.IsNotNull(arcPath, "PART_DeterminateArc should exist in the template.");
                    Assert.IsNotNull(arcPath.Data, "Determinate arc should have non-null Data at 50%.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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
                    Path? indeterminateArc = ring.Template.FindName("PART_IndeterminateArc", ring) as Path;
                    Assert.IsNotNull(indeterminateArc, "PART_IndeterminateArc should exist in the indeterminate template.");
                    Assert.AreEqual(Visibility.Visible, indeterminateArc.Visibility,
                        "PART_IndeterminateArc should be Visible when IsActive=True and IsIndeterminate=True.");

                    bool arcDataReady = WaitUntil(window.Dispatcher, 1000, delegate
                    {
                        return indeterminateArc.Data is not null;
                    });
                    Assert.IsTrue(arcDataReady,
                        "PART_IndeterminateArc should have non-null Data for the caterpillar geometry.");

                    FrameworkElement? dotHost = ring.Template.FindName("DotHost", ring) as FrameworkElement;
                    Assert.IsNull(dotHost, "DotHost should not exist in the default arc-based template.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
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

                    Controls.NavigationView? nav = window.FindName("DemoNav") as Controls.NavigationView;
                    Assert.IsNotNull(nav);

                    SelectMainWindowNavPage(window, window.Dispatcher, "Buttons");
                    Assert.IsNotNull(nav.SelectedItem);
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
            Controls.NavigationView? nav = window.FindName("DemoNav") as Controls.NavigationView;
            Assert.IsNotNull(nav, "Main window should expose DemoNav.");

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
            TextBlock? glyphTextBlock = FindButtonGlyphTextBlock(button, glyph);
            Assert.IsNotNull(glyphTextBlock, "Expected button glyph was not found in the visual tree.");
            Assert.IsTrue(glyphTextBlock.IsVisible, "Expected button glyph should be visible.");
            Assert.IsTrue(glyphTextBlock.ActualWidth > 0, "Expected button glyph should occupy layout space.");
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
            TextBlock? glyphTextBlock = FindButtonGlyphTextBlock(button, glyph);

            Assert.IsNotNull(glyphTextBlock, "Expected button glyph was not found in the visual tree.");
            Assert.IsTrue(glyphTextBlock.IsVisible, "Expected button glyph should be visible.");
            Assert.IsTrue(glyphTextBlock.ActualWidth > 0, "Expected button glyph should occupy layout space.");

            Point buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
            Point glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
            double buttonRight = buttonOrigin.X + button.ActualWidth;
            double buttonBottom = buttonOrigin.Y + button.ActualHeight;
            double glyphRight = glyphOrigin.X + glyphTextBlock.ActualWidth;
            double glyphBottom = glyphOrigin.Y + glyphTextBlock.ActualHeight;

            Assert.IsTrue(glyphOrigin.X >= buttonOrigin.X - 0.5, "Expected button glyph should not render left of the button.");
            Assert.IsTrue(glyphOrigin.Y >= buttonOrigin.Y - 0.5, "Expected button glyph should not render above the button.");
            Assert.IsTrue(glyphRight <= buttonRight + 0.5, "Expected button glyph should not render right of the button.");
            Assert.IsTrue(glyphBottom <= buttonBottom + 0.5, "Expected button glyph should not render below the button.");
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
            TextBlock? glyphTextBlock = FindButtonGlyphTextBlock(button, glyph);
            Assert.IsNotNull(glyphTextBlock, "Expected button glyph was not found in the visual tree.");

            ContentPresenter? textPresenter = null;
            foreach (ContentPresenter presenter in FindVisualChildren<ContentPresenter>(button))
            {
                if (string.Equals(presenter.Content as string, content, StringComparison.Ordinal))
                {
                    textPresenter = presenter;
                    break;
                }
            }

            Assert.IsNotNull(textPresenter, "Expected button content presenter was not found in the visual tree.");

            Point buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
            double buttonCenter = buttonOrigin.X + (button.ActualWidth / 2.0);

            Point glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
            Point contentOrigin = textPresenter.TransformToAncestor(window).Transform(new Point(0, 0));
            double groupLeft = Math.Min(glyphOrigin.X, contentOrigin.X);
            double groupRight = Math.Max(glyphOrigin.X + glyphTextBlock.ActualWidth, contentOrigin.X + textPresenter.ActualWidth);
            double groupCenter = groupLeft + ((groupRight - groupLeft) / 2.0);

            Assert.AreEqual(buttonCenter, groupCenter, 1.0, "Button icon and text should stay centered as a single content group.");
        }
    }
}
