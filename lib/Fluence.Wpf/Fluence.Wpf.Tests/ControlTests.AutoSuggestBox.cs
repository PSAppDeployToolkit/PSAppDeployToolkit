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
        public void AutoSuggestBox_DefaultStyle_AppliesTemplateParts()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(Controls.AutoSuggestBox)) as Style;
                Assert.NotNull(style);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new() { PlaceholderText = "Search" };

                try
                {
                    window.Content = box;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = box.Template;
                    Assert.NotNull(template);

                    Controls.TextBox? textBox = template.FindName("PART_TextBox", box) as Controls.TextBox;
                    Popup? popup = template.FindName("PART_SuggestionsPopup", box) as Popup;
                    Selector? list = template.FindName("PART_SuggestionsList", box) as Selector;

                    Assert.NotNull(textBox);
                    Controls.TextBox verifiedTextBox = textBox ?? throw new InvalidOperationException("PART_TextBox must be a Fluence TextBox so the field matches the themed look.");
                    Assert.NotNull(popup);
                    Assert.NotNull(list);
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
        public void AutoSuggestBox_ProgrammaticTextChange_RaisesTextChangedWithProgrammaticReason()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutoSuggestBoxTextChangedEventArgs? captured = null;
                    box.TextChanged += (_, args) => captured = args;

                    box.Text = "fluent";
                    DrainDispatcher(window.Dispatcher);

                    Assert.NotNull(captured);
                    Assert.Equal(AutoSuggestionBoxTextChangeReason.ProgrammaticChange, captured.Reason);
                    Assert.True(captured.CheckCurrent(),
                        "CheckCurrent must report true while the text is still the value that raised the event.");

                    Controls.TextBox? textBox = box.Template?.FindName("PART_TextBox", box) as Controls.TextBox;
                    Assert.NotNull(textBox);
                    if (textBox is null)
                    {
                        throw new Xunit.Sdk.XunitException("PART_TextBox must be present in the template.");
                    }
                    Assert.Equal("fluent", textBox.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void AutoSuggestBox_UserEditInTextBox_RaisesTextChangedWithUserInputReason()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TextBox? textBox = box.Template?.FindName("PART_TextBox", box) as Controls.TextBox;
                    Assert.NotNull(textBox);

                    AutoSuggestBoxTextChangedEventArgs? captured = null;
                    box.TextChanged += (_, args) => captured = args;

                    // Editing the inner text box raises TextBox.TextChanged, which is the
                    // same path real keyboard input takes through the control wiring.
                    textBox.Text = "ap";
                    DrainDispatcher(window.Dispatcher);

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
        public void AutoSuggestBox_IsSuggestionListOpen_ShowsPopupWithItems()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Popup? popup = box.Template?.FindName("PART_SuggestionsPopup", box) as Popup;
                    Selector? list = box.Template?.FindName("PART_SuggestionsList", box) as Selector;
                    Assert.NotNull(popup);
                    Assert.NotNull(list);

                    box.ItemsSource = (List<string>)["Apple", "Banana", "Cherry"];
                    box.IsSuggestionListOpen = true;

                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
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
        public void AutoSuggestBox_ChooseSuggestionViaKeyboard_RaisesSuggestionChosenAndQuerySubmitted()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TextBox? textBox = box.Template?.FindName("PART_TextBox", box) as Controls.TextBox;
                    Popup? popup = box.Template?.FindName("PART_SuggestionsPopup", box) as Popup;
                    Selector? list = box.Template?.FindName("PART_SuggestionsList", box) as Selector;
                    Assert.NotNull(textBox);
                    Assert.NotNull(popup);
                    Assert.NotNull(list);

                    box.ItemsSource = (List<string>)["Apple", "Banana", "Cherry"];
                    box.IsSuggestionListOpen = true;
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The suggestion popup must open before the keyboard scenario.");

                    List<AutoSuggestionBoxTextChangeReason> reasons = [];
                    box.TextChanged += (_, args) => reasons.Add(args.Reason);
                    object? chosen = null;
                    box.SuggestionChosen += (_, args) => chosen = args.SelectedItem;
                    AutoSuggestBoxQuerySubmittedEventArgs? submitted = null;
                    box.QuerySubmitted += (_, args) => submitted = args;

                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    DrainDispatcher(window.Dispatcher);
                    Assert.Equal(0, list.SelectedIndex);

                    RaisePreviewKeyDown(textBox, window, Key.Enter);
                    DrainDispatcher(window.Dispatcher);

                    Assert.Equal("Apple", chosen);
                    Assert.NotNull(submitted);
                    Assert.Equal("Apple", submitted.QueryText, StringComparer.Ordinal);
                    Assert.Equal("Apple", submitted.ChosenSuggestion);
                    Assert.Equal("Apple", box.Text, StringComparer.Ordinal);
                    Assert.True(reasons.Contains(AutoSuggestionBoxTextChangeReason.SuggestionChosen),
                        "Choosing a suggestion must raise TextChanged with Reason=SuggestionChosen.");
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => !popup.IsOpen),
                        "Submitting a query must close the suggestion popup.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void AutoSuggestBox_EnterWithoutSelection_RaisesQuerySubmittedWithCurrentText()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TextBox? textBox = box.Template?.FindName("PART_TextBox", box) as Controls.TextBox;
                    Assert.NotNull(textBox);

                    box.Text = "search term";
                    AutoSuggestBoxQuerySubmittedEventArgs? submitted = null;
                    box.QuerySubmitted += (_, args) => submitted = args;

                    RaisePreviewKeyDown(textBox, window, Key.Enter);
                    DrainDispatcher(window.Dispatcher);

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
        public void AutoSuggestBox_Escape_ClosesSuggestionList()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TextBox? textBox = box.Template?.FindName("PART_TextBox", box) as Controls.TextBox;
                    Popup? popup = box.Template?.FindName("PART_SuggestionsPopup", box) as Popup;
                    Assert.NotNull(textBox);
                    Assert.NotNull(popup);

                    box.ItemsSource = (List<string>)["Apple", "Banana", "Cherry"];
                    box.IsSuggestionListOpen = true;
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The suggestion popup must open before the Escape scenario.");

                    RaisePreviewKeyDown(textBox, window, Key.Escape);

                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => !popup.IsOpen),
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
        public void AutoSuggestBox_ArrowKeys_PreviewHighlightedSuggestionAndRestoreTypedText()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new();

                try
                {
                    window.Content = box;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TextBox? textBox = box.Template?.FindName("PART_TextBox", box) as Controls.TextBox;
                    Popup? popup = box.Template?.FindName("PART_SuggestionsPopup", box) as Popup;
                    Selector? list = box.Template?.FindName("PART_SuggestionsList", box) as Selector;
                    Assert.NotNull(textBox);
                    Assert.NotNull(popup);
                    Assert.NotNull(list);

                    // Type "ap" (UserInput baseline), then open the list.
                    textBox.Text = "ap";
                    DrainDispatcher(window.Dispatcher);
                    box.ItemsSource = (List<string>)["Apple", "Banana", "Cherry"];
                    box.IsSuggestionListOpen = true;
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The suggestion popup must open before the navigation scenario.");

                    List<AutoSuggestionBoxTextChangeReason> reasons = [];
                    box.TextChanged += (_, args) => reasons.Add(args.Reason);
                    bool querySubmitted = false;
                    box.QuerySubmitted += (_, _) => querySubmitted = true;

                    // Moving the highlight previews each suggestion into the box.
                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    DrainDispatcher(window.Dispatcher);
                    Assert.Equal(0, list.SelectedIndex);
                    Assert.Equal("Apple", box.Text, StringComparer.Ordinal);
                    Assert.Equal("Apple", textBox.Text, StringComparer.Ordinal);

                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Banana", box.Text, StringComparer.Ordinal);

                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Cherry", box.Text, StringComparer.Ordinal);

                    // Cycling past the end returns to no selection and restores the typed text.
                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    DrainDispatcher(window.Dispatcher);
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
        public void AutoSuggestBox_QueryIconButton_SubmitsQueryAndHidesWhenIconNull()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new()
                {
                    QueryIcon = new Controls.FontIcon { Glyph = "" },
                    Text = "search term",
                };

                try
                {
                    window.Content = box;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TextBox? textBox = box.Template?.FindName("PART_TextBox", box) as Controls.TextBox;
                    ButtonBase? queryButton = box.Template?.FindName("PART_QueryButton", box) as ButtonBase;
                    Assert.NotNull(textBox);
                    Assert.NotNull(queryButton);
                    Assert.Same(queryButton, textBox.Icon);
                    Assert.Same(box.QueryIcon, queryButton.Content);

                    AutoSuggestBoxQuerySubmittedEventArgs? submitted = null;
                    box.QuerySubmitted += (_, args) => submitted = args;

                    queryButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    DrainDispatcher(window.Dispatcher);

                    Assert.NotNull(submitted);
                    Assert.Equal("search term", submitted.QueryText, StringComparer.Ordinal);
                    Assert.Null(submitted.ChosenSuggestion);

                    // Clearing QueryIcon removes the button from the icon slot entirely.
                    box.QueryIcon = null;
                    DrainDispatcher(window.Dispatcher);
                    Assert.Null(textBox.Icon);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void AutoSuggestBox_SurfaceBrushes_ResolveAfterThemeCycle()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ThemeTestHelpers.ApplyStandardThemeCycle();

                Assert.NotNull(app?.TryFindResource("ControlFillColorDefaultBrush"));
                Assert.NotNull(app?.TryFindResource("TextControlElevationBorderBrush"));
                Assert.NotNull(app?.TryFindResource("SolidBackgroundFillColorTertiaryBrush"));
                Assert.NotNull(app?.TryFindResource("SurfaceStrokeColorFlyoutBrush"));
                Assert.NotNull(app?.TryFindResource("OverlayCornerRadius"));
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
        public void AutoSuggestBox_Header_BecomesAccessibleName()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);

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
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
