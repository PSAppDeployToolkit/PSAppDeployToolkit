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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.AutoSuggestBox"/> control.
    /// </summary>
    public partial class ControlTests
    {
        [Fact]
        public Task AutoSuggestBox_DefaultStyle_AppliesTemplatePartsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style style = Assert.IsType<Style>(app.TryFindResource(typeof(Controls.AutoSuggestBox)));

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new() { PlaceholderText = "Search" };

                try
                {
                    window.Content = box;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(box.Template);

                    Controls.TextBox textBox = Assert.IsType<Controls.TextBox>(template.FindName("PART_TextBox", box));
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_SuggestionsPopup", box));
                    Selector list = Assert.IsAssignableFrom<Selector>(template.FindName("PART_SuggestionsList", box));

                    Controls.TextBox verifiedTextBox = textBox ?? throw new InvalidOperationException("PART_TextBox must be a Fluence TextBox so the field matches the themed look.");
                    _ = Assert.IsAssignableFrom<Controls.ListBox>(list);
                    Assert.False(popup.StaysOpen, "The suggestion popup must be light-dismiss (StaysOpen=false).");
                    Assert.True(popup.AllowsTransparency, "The suggestion popup must allow transparency for the rounded surface.");
                    Assert.Equal("Search", verifiedTextBox.PlaceholderText, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task AutoSuggestBox_ProgrammaticTextChange_RaisesTextChangedWithProgrammaticReasonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutoSuggestBoxTextChangedEventArgs? captured = null;
                    box.TextChanged += (_, args) => captured = args;

                    box.Text = "fluent";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.NotNull(captured);
                    Assert.Equal(AutoSuggestionBoxTextChangeReason.ProgrammaticChange, captured.Reason);
                    Assert.True(captured.CheckCurrent(),
                        "CheckCurrent must report true while the text is still the value that raised the event.");

                    Controls.TextBox textBox = Assert.IsType<Controls.TextBox>(box.Template?.FindName("PART_TextBox", box));
                    Assert.Equal("fluent", textBox.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task AutoSuggestBox_UserEditInTextBox_RaisesTextChangedWithUserInputReasonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TextBox textBox = Assert.IsType<Controls.TextBox>(box.Template?.FindName("PART_TextBox", box));

                    AutoSuggestBoxTextChangedEventArgs? captured = null;
                    box.TextChanged += (_, args) => captured = args;

                    // Editing the inner text box raises TextBox.TextChanged, which is the
                    // same path real keyboard input takes through the control wiring.
                    textBox.Text = "ap";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.NotNull(captured);
                    Assert.Equal(AutoSuggestionBoxTextChangeReason.UserInput, captured.Reason);
                    Assert.Equal("ap", box.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task AutoSuggestBox_IsSuggestionListOpen_ShowsPopupWithItemsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Popup popup = Assert.IsType<Popup>(box.Template?.FindName("PART_SuggestionsPopup", box));
                    Selector list = Assert.IsAssignableFrom<Selector>(box.Template?.FindName("PART_SuggestionsList", box));

                    box.ItemsSource = (List<string>)["Apple", "Banana", "Cherry"];
                    box.IsSuggestionListOpen = true;

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "Setting IsSuggestionListOpen=true must open the suggestion popup.");
                    Assert.Equal(3, list.Items.Count);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task AutoSuggestBox_ChooseSuggestionViaKeyboard_RaisesSuggestionChosenAndQuerySubmittedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TextBox textBox = Assert.IsType<Controls.TextBox>(box.Template?.FindName("PART_TextBox", box));
                    Popup popup = Assert.IsType<Popup>(box.Template?.FindName("PART_SuggestionsPopup", box));
                    Selector list = Assert.IsAssignableFrom<Selector>(box.Template?.FindName("PART_SuggestionsList", box));

                    box.ItemsSource = (List<string>)["Apple", "Banana", "Cherry"];
                    box.IsSuggestionListOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The suggestion popup must open before the keyboard scenario.");

                    List<AutoSuggestionBoxTextChangeReason> reasons = [];
                    box.TextChanged += (_, args) => reasons.Add(args.Reason);
                    object? chosen = null;
                    box.SuggestionChosen += (_, args) => chosen = args.SelectedItem;
                    AutoSuggestBoxQuerySubmittedEventArgs? submitted = null;
                    box.QuerySubmitted += (_, args) => submitted = args;

                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(0, list.SelectedIndex);

                    RaisePreviewKeyDown(textBox, window, Key.Enter);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal("Apple", chosen);
                    Assert.NotNull(submitted);
                    Assert.Equal("Apple", submitted.QueryText, StringComparer.Ordinal);
                    Assert.Equal("Apple", submitted.ChosenSuggestion);
                    Assert.Equal("Apple", box.Text, StringComparer.Ordinal);
                    Assert.True(reasons.Contains(AutoSuggestionBoxTextChangeReason.SuggestionChosen),
                        "Choosing a suggestion must raise TextChanged with Reason=SuggestionChosen.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !popup.IsOpen).ConfigureAwait(true),
                        "Submitting a query must close the suggestion popup.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task AutoSuggestBox_EnterWithoutSelection_RaisesQuerySubmittedWithCurrentTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TextBox textBox = Assert.IsType<Controls.TextBox>(box.Template?.FindName("PART_TextBox", box));

                    box.Text = "search term";
                    AutoSuggestBoxQuerySubmittedEventArgs? submitted = null;
                    box.QuerySubmitted += (_, args) => submitted = args;

                    RaisePreviewKeyDown(textBox, window, Key.Enter);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.NotNull(submitted);
                    Assert.Equal("search term", submitted.QueryText, StringComparer.Ordinal);
                    Assert.Null(submitted.ChosenSuggestion);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task AutoSuggestBox_Escape_ClosesSuggestionListAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TextBox textBox = Assert.IsType<Controls.TextBox>(box.Template?.FindName("PART_TextBox", box));
                    Popup popup = Assert.IsType<Popup>(box.Template?.FindName("PART_SuggestionsPopup", box));

                    box.ItemsSource = (List<string>)["Apple", "Banana", "Cherry"];
                    box.IsSuggestionListOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The suggestion popup must open before the Escape scenario.");

                    RaisePreviewKeyDown(textBox, window, Key.Escape);

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !popup.IsOpen).ConfigureAwait(true),
                        "Escape must close the suggestion popup.");
                    Assert.False(box.IsSuggestionListOpen, "Escape must reset IsSuggestionListOpen.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task AutoSuggestBox_ArrowKeys_PreviewHighlightedSuggestionAndRestoreTypedTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TextBox textBox = Assert.IsType<Controls.TextBox>(box.Template?.FindName("PART_TextBox", box));
                    Popup popup = Assert.IsType<Popup>(box.Template?.FindName("PART_SuggestionsPopup", box));
                    Selector list = Assert.IsAssignableFrom<Selector>(box.Template?.FindName("PART_SuggestionsList", box));

                    // Type "ap" (UserInput baseline), then open the list.
                    textBox.Text = "ap";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    box.ItemsSource = (List<string>)["Apple", "Banana", "Cherry"];
                    box.IsSuggestionListOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The suggestion popup must open before the navigation scenario.");

                    List<AutoSuggestionBoxTextChangeReason> reasons = [];
                    box.TextChanged += (_, args) => reasons.Add(args.Reason);
                    bool querySubmitted = false;
                    box.QuerySubmitted += (_, _) => querySubmitted = true;

                    // Moving the highlight previews each suggestion into the box.
                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(0, list.SelectedIndex);
                    Assert.Equal("Apple", box.Text, StringComparer.Ordinal);
                    Assert.Equal("Apple", textBox.Text, StringComparer.Ordinal);

                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Banana", box.Text, StringComparer.Ordinal);

                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Cherry", box.Text, StringComparer.Ordinal);

                    // Cycling past the end returns to no selection and restores the typed text.
                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(-1, list.SelectedIndex);
                    Assert.Equal("ap", box.Text, StringComparer.Ordinal);

                    Assert.True(reasons.Count > 0, "The preview navigation must raise TextChanged.");
                    foreach (AutoSuggestionBoxTextChangeReason reason in reasons)
                    {
                        Assert.Equal(AutoSuggestionBoxTextChangeReason.SuggestionChosen, reason);
                    }

                    Assert.False(querySubmitted, "Arrow-key navigation alone must not submit the query.");
                    Assert.True(popup.IsOpen, "Arrow-key navigation must keep the suggestion list open.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task AutoSuggestBox_QueryIconButton_SubmitsQueryAndHidesWhenIconNullAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new()
                {
                    QueryIcon = new Controls.FontIcon { Glyph = "\uE721" },
                    Text = "search term",
                };

                try
                {
                    window.Content = box;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TextBox textBox = Assert.IsType<Controls.TextBox>(box.Template?.FindName("PART_TextBox", box));
                    ButtonBase queryButton = Assert.IsAssignableFrom<ButtonBase>(box.Template?.FindName("PART_QueryButton", box));
                    Assert.Same(queryButton, textBox.Icon);
                    Assert.Same(box.QueryIcon, queryButton.Content);

                    AutoSuggestBoxQuerySubmittedEventArgs? submitted = null;
                    box.QuerySubmitted += (_, args) => submitted = args;

                    queryButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.NotNull(submitted);
                    Assert.Equal("search term", submitted.QueryText, StringComparer.Ordinal);
                    Assert.Null(submitted.ChosenSuggestion);

                    // Clearing QueryIcon removes the button from the icon slot entirely.
                    box.QueryIcon = null;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Null(textBox.Icon);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task AutoSuggestBox_SurfaceBrushes_ResolveAfterThemeCycleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ThemeTestHelpers.ApplyStandardThemeCycle();

                Assert.NotNull(app.TryFindResource("ControlFillColorDefaultBrush"));
                Assert.NotNull(app.TryFindResource("TextControlElevationBorderBrush"));
                Assert.NotNull(app.TryFindResource("SolidBackgroundFillColorTertiaryBrush"));
                Assert.NotNull(app.TryFindResource("SurfaceStrokeColorFlyoutBrush"));
                Assert.NotNull(app.TryFindResource("OverlayCornerRadius"));
            });
        }

        private static void RaisePreviewKeyDown(UIElement target, Window window, Key key)
        {
            target.RaiseEvent(new KeyEventArgs(
                Keyboard.PrimaryDevice,
                PresentationSource.FromVisual(window),
                0,
                key)
            {
                RoutedEvent = UIElement.PreviewKeyDownEvent,
            });
        }

        [Fact]
        public Task AutoSuggestBox_Header_BecomesAccessibleNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Controls.AutoSuggestBox box = new() { Header = "Search term" };
                    window.Content = box;
                    window.Width = 300;
                    window.Height = 120;
                    window.Show();
                    _ = box.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(box);
                    Assert.True(
                        string.Equals("Search term", peer.GetName(), StringComparison.Ordinal),
                        "AutoSuggestBox Header must be the accessible name when no explicit AutomationProperties.Name is set.");

                    box.SetValue(AutomationProperties.NameProperty, "Explicit");
                    Assert.True(
                        string.Equals("Explicit", peer.GetName(), StringComparison.Ordinal),
                        "Explicit AutomationProperties.Name must win over Header.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
