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

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    /// <summary>
    /// Verifies the Selection gallery's bound and editable ComboBox examples.
    /// </summary>
    public sealed class GallerySelectionPageTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        [Fact]
        public Task BoundComboBoxSample_CopyableSourceContainsLiveHandlersAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GallerySelectionPage(), static window =>
            {
                DemoSampleControl sample = Assert.Single(DemoTestHost.FindVisualChildren<DemoSampleControl>(window),
                    static control => control.XamlSource.Contains("ItemsSourceDemoCombo", System.StringComparison.Ordinal));
                Assert.Contains("EditableDemoCombo", sample.XamlSource, System.StringComparison.Ordinal);
                Assert.Contains("ItemsSourceComboStateLabel", sample.XamlSource, System.StringComparison.Ordinal);
                Assert.Contains("EditableComboStateLabel", sample.XamlSource, System.StringComparison.Ordinal);
                Assert.Contains("private void AddComboBoxOptionButton_Click", sample.CSharpSource, System.StringComparison.Ordinal);
                Assert.Contains("private void ItemsSourceDemoCombo_SelectionChanged", sample.CSharpSource, System.StringComparison.Ordinal);
                Assert.Contains("private void EditableDemoCombo_SelectionChanged", sample.CSharpSource, System.StringComparison.Ordinal);
                Assert.Contains("private void EditableDemoCombo_DropDownClosed", sample.CSharpSource, System.StringComparison.Ordinal);
                Assert.Contains("private void EditableDemoCombo_LostKeyboardFocus", sample.CSharpSource, System.StringComparison.Ordinal);
                Assert.Contains("private void EditableDemoCombo_KeyDown", sample.CSharpSource, System.StringComparison.Ordinal);
                Assert.Contains("Dispatcher.BeginInvoke", sample.CSharpSource, System.StringComparison.Ordinal);
            });
        }

        [Fact]
        public Task ComboBoxItemsSource_UpdatesWithoutRebuildingPageAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GallerySelectionPage(), static window =>
            {
                Controls.ComboBox bound = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "ItemsSourceDemoCombo"));
                Controls.ComboBox editable = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "EditableDemoCombo"));
                Controls.Button add = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(window, "AddComboBoxOptionButton"));
                TextBlock output = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "ItemsSourceComboStateLabel"));

                Assert.Equal(3, bound.Items.Count);
                Assert.Same(bound.ItemsSource, editable.ItemsSource);
                add.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Equal(4, bound.Items.Count);
                Assert.Equal("Option 1", bound.Items[3]);
                Assert.Equal(4, editable.Items.Count);

                bound.SelectedItem = "Option 1";
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Equal("SelectedItem: Option 1", output.Text);
            });
        }

        [Fact]
        public Task EditableComboBox_CommitsTextAndKeepsTheOnlyFocusedUnderlineAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GallerySelectionPage(), static window =>
            {
                Controls.ComboBox bound = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "ItemsSourceDemoCombo"));
                Controls.ComboBox editable = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "EditableDemoCombo"));
                Controls.Button add = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(window, "AddComboBoxOptionButton"));
                TextBlock output = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "EditableComboStateLabel"));
                TextBox editor = Assert.IsType<TextBox>(editable.Template.FindName("PART_EditableTextBox", editable));
                ToggleButton toggle = Assert.IsType<ToggleButton>(editable.Template.FindName("ToggleButton", editable));
                Border editableBorder = Assert.IsType<Border>(editable.Template.FindName("MainBorder", editable));
                Border boundBorder = Assert.IsType<Border>(bound.Template.FindName("MainBorder", bound));

                Assert.True(bound.Focus());
                Assert.True(bound.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Same(add, Keyboard.FocusedElement);
                Assert.True(add.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Same(editor, Keyboard.FocusedElement);

                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.False(editable.IsTabStop);
                Assert.True(editor.IsTabStop);
                Assert.False(toggle.Focusable);
                Assert.Equal(new Thickness(1, 1, 1, 2), editableBorder.BorderThickness);
                Assert.Equal(new Thickness(1), boundBorder.BorderThickness);

                editor.Text = "Custom";
                _ = add.Focus();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Equal("Custom", editable.Text);
                Assert.Equal("Text: Custom; SelectedItem: none", output.Text);

                editable.SelectedItem = "Beta";
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Equal("Text: Beta; SelectedItem: Beta", output.Text);
                Assert.Equal("Beta", editable.Text);

                editable.SelectedIndex = -1;
                _ = editor.Focus();
                editor.Text = "Delta";
                editable.IsDropDownOpen = true;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.True(editable.IsDropDownOpen);
                PresentationSource source = PresentationSource.FromVisual(window)
                    ?? throw new System.InvalidOperationException("Window must have a presentation source for keyboard input.");
                KeyEventArgs enter = new(Keyboard.PrimaryDevice, source, 0, Key.Return)
                {
                    RoutedEvent = Keyboard.PreviewKeyDownEvent,
                    Source = editor,
                };
                editor.RaiseEvent(enter);
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.False(editable.IsDropDownOpen);
                Assert.Equal("Delta", editable.Text);
                Assert.Null(editable.SelectedItem);
                Assert.Equal("Text: Delta; SelectedItem: none", output.Text);
                Keyboard.ClearFocus();
            });
        }
    }
}
