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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.AutoSuggestBox"/> control.
    /// </summary>
    public partial class ControlTests
    {
        [TestMethod]
        public void AutoSuggestBox_DefaultStyle_AppliesTemplateParts()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(Controls.AutoSuggestBox)) as Style;
                Assert.IsNotNull(style, "A default Style must be registered for Fluence.Wpf.Controls.AutoSuggestBox.");

                Window window = new() { Width = 400, Height = 300 };
                Controls.AutoSuggestBox box = new() { PlaceholderText = "Search" };

                try
                {
                    window.Content = box;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = box.Template;
                    Assert.IsNotNull(template, "AutoSuggestBox must receive its themed template.");

                    Controls.TextBox? textBox = template.FindName("PART_TextBox", box) as Controls.TextBox;
                    Popup? popup = template.FindName("PART_SuggestionsPopup", box) as Popup;
                    Selector? list = template.FindName("PART_SuggestionsList", box) as Selector;

                    Assert.IsNotNull(textBox, "PART_TextBox must be a Fluence TextBox so the field matches the themed look.");
                    Assert.IsNotNull(popup, "PART_SuggestionsPopup must be present in the template.");
                    Assert.IsNotNull(list, "PART_SuggestionsList must be a Selector hosting the suggestions.");
                    _ = Assert.IsInstanceOfType<Controls.ListBox>(list,
                        "The default template should present suggestions through the Fluence ListBox.");
                    Assert.IsFalse(popup.StaysOpen, "The suggestion popup must be light-dismiss (StaysOpen=false).");
                    Assert.IsTrue(popup.AllowsTransparency, "The suggestion popup must allow transparency for the rounded surface.");
                    Assert.AreEqual("Search", textBox.PlaceholderText, "PlaceholderText must flow into the inner Fluence TextBox.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
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

                    Assert.IsNotNull(captured, "Setting Text programmatically must raise TextChanged.");
                    Assert.AreEqual(AutoSuggestionBoxTextChangeReason.ProgrammaticChange, captured.Reason,
                        "A programmatic Text change must report Reason=ProgrammaticChange.");
                    Assert.IsTrue(captured.CheckCurrent(),
                        "CheckCurrent must report true while the text is still the value that raised the event.");

                    Controls.TextBox? textBox = box.Template?.FindName("PART_TextBox", box) as Controls.TextBox;
                    Assert.IsNotNull(textBox, "PART_TextBox must be present in the template.");
                    Assert.AreEqual("fluent", textBox.Text, "A programmatic Text change must flow into the inner text box.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                    Assert.IsNotNull(textBox, "PART_TextBox must be present in the template.");

                    AutoSuggestBoxTextChangedEventArgs? captured = null;
                    box.TextChanged += (_, args) => captured = args;

                    // Editing the inner text box raises TextBox.TextChanged, which is the
                    // same path real keyboard input takes through the control wiring.
                    textBox.Text = "ap";
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsNotNull(captured, "Editing the inner text box must raise TextChanged.");
                    Assert.AreEqual(AutoSuggestionBoxTextChangeReason.UserInput, captured.Reason,
                        "An edit that originates in the text box must report Reason=UserInput.");
                    Assert.AreEqual("ap", box.Text, "The edit must be mirrored into AutoSuggestBox.Text.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                    Assert.IsNotNull(popup, "PART_SuggestionsPopup must be present in the template.");
                    Assert.IsNotNull(list, "PART_SuggestionsList must be present in the template.");

                    box.ItemsSource = (List<string>)["Apple", "Banana", "Cherry"];
                    box.IsSuggestionListOpen = true;

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "Setting IsSuggestionListOpen=true must open the suggestion popup.");
                    Assert.AreEqual(3, list.Items.Count, "ItemsSource must flow into the suggestion list.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                    Assert.IsNotNull(textBox, "PART_TextBox must be present in the template.");
                    Assert.IsNotNull(popup, "PART_SuggestionsPopup must be present in the template.");
                    Assert.IsNotNull(list, "PART_SuggestionsList must be present in the template.");

                    box.ItemsSource = (List<string>)["Apple", "Banana", "Cherry"];
                    box.IsSuggestionListOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The suggestion popup must open before the keyboard scenario.");

                    List<AutoSuggestionBoxTextChangeReason> reasons = [];
                    box.TextChanged += (_, args) => reasons.Add(args.Reason);
                    object? chosen = null;
                    box.SuggestionChosen += (_, args) => chosen = args.SelectedItem;
                    AutoSuggestBoxQuerySubmittedEventArgs? submitted = null;
                    box.QuerySubmitted += (_, args) => submitted = args;

                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(0, list.SelectedIndex, "Down must move the highlight onto the first suggestion.");

                    RaisePreviewKeyDown(textBox, window, Key.Enter);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual("Apple", chosen, "Enter on a highlighted suggestion must raise SuggestionChosen with it.");
                    Assert.IsNotNull(submitted, "Enter on a highlighted suggestion must raise QuerySubmitted.");
                    Assert.AreEqual("Apple", submitted.QueryText, "QueryText must carry the updated text.");
                    Assert.AreEqual("Apple", submitted.ChosenSuggestion, "ChosenSuggestion must carry the chosen item.");
                    Assert.AreEqual("Apple", box.Text, "UpdateTextOnSelect must write the suggestion into Text.");
                    Assert.IsTrue(reasons.Contains(AutoSuggestionBoxTextChangeReason.SuggestionChosen),
                        "Choosing a suggestion must raise TextChanged with Reason=SuggestionChosen.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !popup.IsOpen),
                        "Submitting a query must close the suggestion popup.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                    Assert.IsNotNull(textBox, "PART_TextBox must be present in the template.");

                    box.Text = "search term";
                    AutoSuggestBoxQuerySubmittedEventArgs? submitted = null;
                    box.QuerySubmitted += (_, args) => submitted = args;

                    RaisePreviewKeyDown(textBox, window, Key.Enter);
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsNotNull(submitted, "Enter must raise QuerySubmitted even without a highlighted suggestion.");
                    Assert.AreEqual("search term", submitted.QueryText, "QueryText must carry the current text.");
                    Assert.IsNull(submitted.ChosenSuggestion, "ChosenSuggestion must be null when no suggestion is highlighted.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                    Assert.IsNotNull(textBox, "PART_TextBox must be present in the template.");
                    Assert.IsNotNull(popup, "PART_SuggestionsPopup must be present in the template.");

                    box.ItemsSource = (List<string>)["Apple", "Banana", "Cherry"];
                    box.IsSuggestionListOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The suggestion popup must open before the Escape scenario.");

                    RaisePreviewKeyDown(textBox, window, Key.Escape);

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !popup.IsOpen),
                        "Escape must close the suggestion popup.");
                    Assert.IsFalse(box.IsSuggestionListOpen, "Escape must reset IsSuggestionListOpen.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                    Assert.IsNotNull(textBox, "PART_TextBox must be present in the template.");
                    Assert.IsNotNull(popup, "PART_SuggestionsPopup must be present in the template.");
                    Assert.IsNotNull(list, "PART_SuggestionsList must be present in the template.");

                    // Type "ap" (UserInput baseline), then open the list.
                    textBox.Text = "ap";
                    DrainDispatcher(window.Dispatcher);
                    box.ItemsSource = (List<string>)["Apple", "Banana", "Cherry"];
                    box.IsSuggestionListOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The suggestion popup must open before the navigation scenario.");

                    List<AutoSuggestionBoxTextChangeReason> reasons = [];
                    box.TextChanged += (_, args) => reasons.Add(args.Reason);
                    bool querySubmitted = false;
                    box.QuerySubmitted += (_, _) => querySubmitted = true;

                    // Moving the highlight previews each suggestion into the box.
                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(0, list.SelectedIndex, "Down must move the highlight onto the first suggestion.");
                    Assert.AreEqual("Apple", box.Text, "The highlighted suggestion must be previewed into Text.");
                    Assert.AreEqual("Apple", textBox.Text, "The preview must reach the inner text box.");

                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual("Banana", box.Text, "Each highlight move must preview the new suggestion.");

                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual("Cherry", box.Text, "The last suggestion must preview like the others.");

                    // Cycling past the end returns to no selection and restores the typed text.
                    RaisePreviewKeyDown(textBox, window, Key.Down);
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(-1, list.SelectedIndex, "Cycling past the end must clear the highlight.");
                    Assert.AreEqual("ap", box.Text, "Clearing the highlight must restore the original typed text.");

                    Assert.IsTrue(reasons.Count > 0, "The preview navigation must raise TextChanged.");
                    foreach (AutoSuggestionBoxTextChangeReason reason in reasons)
                    {
                        Assert.AreEqual(AutoSuggestionBoxTextChangeReason.SuggestionChosen, reason,
                            "Every preview text change must report Reason=SuggestionChosen so app filters do not re-run.");
                    }

                    Assert.IsFalse(querySubmitted, "Arrow-key navigation alone must not submit the query.");
                    Assert.IsTrue(popup.IsOpen, "Arrow-key navigation must keep the suggestion list open.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                    Assert.IsNotNull(textBox, "PART_TextBox must be present in the template.");
                    Assert.IsNotNull(queryButton, "PART_QueryButton must be present in the template while QueryIcon is set.");
                    Assert.AreSame(queryButton, textBox.Icon,
                        "The query button must be hosted in the text box icon slot.");
                    Assert.AreSame(box.QueryIcon, queryButton.Content,
                        "The query button must host the QueryIcon content.");

                    AutoSuggestBoxQuerySubmittedEventArgs? submitted = null;
                    box.QuerySubmitted += (_, args) => submitted = args;

                    queryButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsNotNull(submitted, "Clicking the query icon button must raise QuerySubmitted.");
                    Assert.AreEqual("search term", submitted.QueryText, "QueryText must carry the current text.");
                    Assert.IsNull(submitted.ChosenSuggestion,
                        "A query icon click submits without a chosen suggestion, like Enter.");

                    // Clearing QueryIcon removes the button from the icon slot entirely.
                    box.QueryIcon = null;
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsNull(textBox.Icon, "Clearing QueryIcon must clear the icon slot so no empty button is shown.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void AutoSuggestBox_SurfaceBrushes_ResolveAfterThemeCycle()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ThemeTestHelpers.ApplyStandardThemeCycle();

                Assert.IsNotNull(app?.TryFindResource("ControlFillColorDefaultBrush"),
                    "ControlFillColorDefaultBrush (text field fill) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("TextControlElevationBorderBrush"),
                    "TextControlElevationBorderBrush (text field stroke) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("SolidBackgroundFillColorTertiaryBrush"),
                    "SolidBackgroundFillColorTertiaryBrush (suggestion flyout fill) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("SurfaceStrokeColorFlyoutBrush"),
                    "SurfaceStrokeColorFlyoutBrush (suggestion flyout stroke) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("OverlayCornerRadius"),
                    "OverlayCornerRadius (suggestion flyout corner radius) must resolve after a full theme cycle.");
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
    }
}
