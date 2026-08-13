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

using Fluence.Wpf.Automation;
using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

// IMPORTANT: every reference to TextBox / ListBox / ListBoxItem in this file MUST be
// fully qualified (System.Windows.Controls.TextBox, System.Windows.Controls.ListBox,
// System.Windows.Controls.ListBoxItem). The Fluence.Wpf.Controls namespace defines its
// own TextBox / ListBox / ListBoxItem subclasses, and because this file sits inside
// that namespace, any unqualified reference resolves to the Fluence subclass. The
// template part contract is typed against the stock WPF base types so both the default
// template (which hosts the Fluence controls) and custom templates resolve correctly.
// See NumberBox.cs for the same pattern.
namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A text input control that shows a light-dismiss list of suggestions while the user
    /// types, mirroring the WinUI 3 <c>AutoSuggestBox</c>. The application drives filtering
    /// by handling <see cref="TextChanged"/> and updating <see cref="ItemsSource"/>; the
    /// control opens the suggestion list while it has keyboard focus and suggestions exist.
    /// </summary>
    [TemplatePart(Name = PART_TextBox, Type = typeof(System.Windows.Controls.TextBox))]
    [TemplatePart(Name = PART_SuggestionsPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PART_SuggestionsList, Type = typeof(Selector))]
    [TemplatePart(Name = PART_QueryButton, Type = typeof(ButtonBase))]
    public class AutoSuggestBox : Control
    {
        // Template part names. These must match the names used in the default control template.
        private const string PART_TextBox = "PART_TextBox";
        private const string PART_SuggestionsPopup = "PART_SuggestionsPopup";
        private const string PART_SuggestionsList = "PART_SuggestionsList";
        private const string PART_QueryButton = "PART_QueryButton";

        /// <summary>
        /// Initializes static members of the AutoSuggestBox class and overrides the default
        /// style metadata so the control picks up its themed template from Generic.xaml.
        /// </summary>
        static AutoSuggestBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(AutoSuggestBox),
                new FrameworkPropertyMetadata(typeof(AutoSuggestBox)));
        }

        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(AutoSuggestBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnTextPropertyChanged));

        /// <summary>
        /// Gets or sets the text shown in the text box portion of the control.
        /// Setting this property programmatically raises <see cref="TextChanged"/> with
        /// <see cref="AutoSuggestionBoxTextChangeReason.ProgrammaticChange"/>.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemsSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(AutoSuggestBox),
                new FrameworkPropertyMetadata(defaultValue: null, OnItemsSourceChanged));

        /// <summary>
        /// Gets or sets the collection of suggestions shown in the suggestion list.
        /// Replace this collection from a <see cref="TextChanged"/> handler to filter
        /// suggestions as the user types.
        /// </summary>
        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsSuggestionListOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSuggestionListOpenProperty =
            DependencyProperty.Register(
                nameof(IsSuggestionListOpen),
                typeof(bool),
                typeof(AutoSuggestBox),
                new FrameworkPropertyMetadata(defaultValue: false));

        /// <summary>
        /// Gets or sets whether the suggestion list popup is open. The control opens the
        /// list while it has keyboard focus and <see cref="ItemsSource"/> has items, and
        /// closes it on light dismiss, focus loss, Escape, or query submission.
        /// </summary>
        public bool IsSuggestionListOpen
        {
            get => (bool)GetValue(IsSuggestionListOpenProperty);
            set => SetValue(IsSuggestionListOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TextMemberPath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextMemberPathProperty =
            DependencyProperty.Register(
                nameof(TextMemberPath),
                typeof(string),
                typeof(AutoSuggestBox),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the property path on a suggestion item that supplies the display
        /// text and, when <see cref="UpdateTextOnSelect"/> is enabled, the value written
        /// into <see cref="Text"/> when the suggestion is chosen. When empty, the item's
        /// string representation is used.
        /// </summary>
        public string TextMemberPath
        {
            get => (string)GetValue(TextMemberPathProperty);
            set => SetValue(TextMemberPathProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(AutoSuggestBox),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the placeholder text displayed when the text box is empty.
        /// </summary>
        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaxSuggestionListHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxSuggestionListHeightProperty =
            DependencyProperty.Register(
                nameof(MaxSuggestionListHeight),
                typeof(double),
                typeof(AutoSuggestBox),
                new FrameworkPropertyMetadata(380.0));

        /// <summary>
        /// Gets or sets the maximum height of the suggestion list popup.
        /// </summary>
        public double MaxSuggestionListHeight
        {
            get => (double)GetValue(MaxSuggestionListHeightProperty);
            set => SetValue(MaxSuggestionListHeightProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="UpdateTextOnSelect"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UpdateTextOnSelectProperty =
            DependencyProperty.Register(
                nameof(UpdateTextOnSelect),
                typeof(bool),
                typeof(AutoSuggestBox),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets whether choosing a suggestion updates <see cref="Text"/> with the
        /// value resolved through <see cref="TextMemberPath"/>.
        /// </summary>
        public bool UpdateTextOnSelect
        {
            get => (bool)GetValue(UpdateTextOnSelectProperty);
            set => SetValue(UpdateTextOnSelectProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="QueryIcon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty QueryIconProperty =
            DependencyProperty.Register(
                nameof(QueryIcon),
                typeof(object),
                typeof(AutoSuggestBox),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the icon shown at the right edge of the text box, typically a
        /// search glyph. The default template hosts it in a clickable subtle button in the
        /// text box icon slot; clicking it submits the current text through the same
        /// <see cref="QuerySubmitted"/> pipeline as the Enter key. No button is shown while
        /// the value is <see langword="null"/>.
        /// </summary>
        public object? QueryIcon
        {
            get => GetValue(QueryIconProperty);
            set => SetValue(QueryIconProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Header"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(AutoSuggestBox),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the optional header content shown above the text box.
        /// </summary>
        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Occurs after the text changes. Inspect
        /// <see cref="AutoSuggestBoxTextChangedEventArgs.Reason"/> to distinguish user
        /// input, programmatic changes, and suggestion-driven updates.
        /// </summary>
        public event EventHandler<AutoSuggestBoxTextChangedEventArgs>? TextChanged;

        /// <summary>
        /// Occurs when the user chooses a suggestion from the suggestion list, before
        /// <see cref="Text"/> is updated and <see cref="QuerySubmitted"/> is raised.
        /// </summary>
        public event EventHandler<AutoSuggestBoxSuggestionChosenEventArgs>? SuggestionChosen;

        /// <summary>
        /// Occurs when the user submits a query with the Enter key or by choosing a
        /// suggestion from the suggestion list.
        /// </summary>
        public event EventHandler<AutoSuggestBoxQuerySubmittedEventArgs>? QuerySubmitted;

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new AutoSuggestBoxAutomationPeer(this);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _textBox?.TextChanged -= OnTextBoxTextChanged;
            _suggestionsList?.PreviewMouseLeftButtonUp -= OnSuggestionsListPreviewMouseLeftButtonUp;
            _popup?.Closed -= OnPopupClosed;
            _queryButton?.Click -= OnQueryButtonClick;

            _textBox = GetTemplateChild(PART_TextBox) as System.Windows.Controls.TextBox;
            _popup = GetTemplateChild(PART_SuggestionsPopup) as Popup;
            _suggestionsList = GetTemplateChild(PART_SuggestionsList) as Selector;
            _queryButton = GetTemplateChild(PART_QueryButton) as ButtonBase;

            _textBox?.TextChanged += OnTextBoxTextChanged;
            _suggestionsList?.PreviewMouseLeftButtonUp += OnSuggestionsListPreviewMouseLeftButtonUp;
            _popup?.Closed += OnPopupClosed;
            _queryButton?.Click += OnQueryButtonClick;

            SyncTextBoxText(Text);
        }

        /// <inheritdoc />
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);

            // The shell itself is not a useful focus target; forward focus to the inner
            // text box so callers can simply call Focus() on the AutoSuggestBox.
            if (ReferenceEquals(e.NewFocus, this) && _textBox is not null)
            {
                _ = _textBox.Focus();
            }
        }

        /// <inheritdoc />
        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);

            if ((bool)e.NewValue)
            {
                UpdateSuggestionListState();
            }
            else
            {
                SetCurrentValue(IsSuggestionListOpenProperty, value: false);
            }
        }

        /// <inheritdoc />
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Handled)
            {
                return;
            }

            if (e.Key is Key.Down && IsSuggestionListOpen)
            {
                MoveSuggestionSelection(1);
                e.Handled = true;
            }
            else if (e.Key is Key.Up && IsSuggestionListOpen)
            {
                MoveSuggestionSelection(-1);
                e.Handled = true;
            }
            else if (e.Key is Key.Enter)
            {
                OnEnterKeyPressed();
                e.Handled = true;
            }
            else if (e.Key is Key.Escape && IsSuggestionListOpen)
            {
                SetCurrentValue(IsSuggestionListOpenProperty, value: false);
                e.Handled = true;
            }
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AutoSuggestBox box = (AutoSuggestBox)d;
            box.SyncTextBoxText((e.NewValue as string) ?? string.Empty);
            box.RaiseTextChanged(box._pendingChangeReason ?? AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AutoSuggestBox)d).UpdateSuggestionListState();
        }

        /// <summary>
        /// Resolves a dotted property <paramref name="path"/> against <paramref name="item"/>
        /// using reflection, returning <see langword="null"/> when any segment is missing.
        /// </summary>
        /// <param name="item">The object to evaluate the path against.</param>
        /// <param name="path">The dotted property path to evaluate.</param>
        /// <returns>The value of the property at the end of the path, or <see langword="null"/> if any segment is missing.</returns>
        private static object? EvaluatePath(object item, string path)
        {
            object? current = item;
            foreach (string segment in path.Split('.'))
            {
                if (current is null)
                {
                    return null;
                }

                PropertyInfo? property = current.GetType().GetProperty(segment);
                if (property is null)
                {
                    return null;
                }

                current = property.GetValue(current, index: null);
            }

            return current;
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressTextBoxSync || _textBox is null)
            {
                return;
            }

            // A genuine user edit replaces any keyboard-highlight preview baseline.
            _isPreviewingSuggestion = false;

            _pendingChangeReason = AutoSuggestionBoxTextChangeReason.UserInput;
            try
            {
                SetCurrentValue(TextProperty, _textBox.Text);
            }
            finally
            {
                _pendingChangeReason = null;
            }

            UpdateSuggestionListState();
        }

        /// <summary>
        /// Submits the current text when the query icon button is clicked, running the same
        /// <see cref="QuerySubmitted"/> pipeline as the Enter key without a chosen suggestion.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnQueryButtonClick(object sender, RoutedEventArgs e)
        {
            SubmitQuery(chosenSuggestion: null);
        }

        private void OnSuggestionsListPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_suggestionsList is null || e.OriginalSource is not DependencyObject source)
            {
                return;
            }

            if (ItemsControl.ContainerFromElement(_suggestionsList, source) is not System.Windows.Controls.ListBoxItem container)
            {
                return;
            }

            object item = _suggestionsList.ItemContainerGenerator.ItemFromContainer(container);
            if (ReferenceEquals(item, DependencyProperty.UnsetValue))
            {
                return;
            }

            ChooseSuggestion(item);
            _ = _textBox?.Focus();
            e.Handled = true;
        }

        private void OnPopupClosed(object? sender, EventArgs e)
        {
            _isPreviewingSuggestion = false;
            SetCurrentValue(IsSuggestionListOpenProperty, value: false);
            if (_suggestionsList is not null && _suggestionsList.SelectedIndex != -1)
            {
                _suggestionsList.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// Handles the Enter key: chooses the highlighted suggestion when one is selected,
        /// otherwise submits the current text without a chosen suggestion.
        /// </summary>
        private void OnEnterKeyPressed()
        {
            object? selected = IsSuggestionListOpen ? _suggestionsList?.SelectedItem : null;
            if (selected is not null)
            {
                ChooseSuggestion(selected);
            }
            else
            {
                SubmitQuery(chosenSuggestion: null);
            }
        }

        /// <summary>
        /// Raises <see cref="SuggestionChosen"/>, updates <see cref="Text"/> from the
        /// suggestion when <see cref="UpdateTextOnSelect"/> is enabled (raising
        /// <see cref="TextChanged"/> with
        /// <see cref="AutoSuggestionBoxTextChangeReason.SuggestionChosen"/>), then submits
        /// the query with the chosen suggestion.
        /// </summary>
        /// <param name="suggestion">The chosen suggestion.</param>
        private void ChooseSuggestion(object suggestion)
        {
            AutoSuggestBoxSuggestionChosenEventArgs chosenArgs = new() { SelectedItem = suggestion };
            SuggestionChosen?.Invoke(this, chosenArgs);

            if (UpdateTextOnSelect)
            {
                SetTextWithReason(GetTextFromSuggestion(suggestion), AutoSuggestionBoxTextChangeReason.SuggestionChosen);
            }

            SubmitQuery(suggestion);
        }

        /// <summary>
        /// Raises <see cref="QuerySubmitted"/> with the current text and closes the
        /// suggestion list.
        /// </summary>
        /// <param name="chosenSuggestion">The chosen suggestion, or <see langword="null"/> if the query is being submitted without a suggestion.</param>
        private void SubmitQuery(object? chosenSuggestion)
        {
            AutoSuggestBoxQuerySubmittedEventArgs args = new()
            {
                QueryText = Text,
                ChosenSuggestion = chosenSuggestion,
            };
            QuerySubmitted?.Invoke(this, args);
            SetCurrentValue(IsSuggestionListOpenProperty, value: false);
        }

        /// <summary>
        /// Sets <see cref="Text"/> while tagging the resulting <see cref="TextChanged"/>
        /// notification with the given <paramref name="reason"/>.
        /// </summary>
        /// <param name="text">The new text.</param>
        /// <param name="reason">The reason for the text change.</param>
        private void SetTextWithReason(string text, AutoSuggestionBoxTextChangeReason reason)
        {
            _pendingChangeReason = reason;
            try
            {
                SetCurrentValue(TextProperty, text);
            }
            finally
            {
                _pendingChangeReason = null;
            }
        }

        /// <summary>
        /// Pushes <paramref name="text"/> into the template text box without bouncing the
        /// change back through <see cref="OnTextBoxTextChanged"/>.
        /// </summary>
        /// <param name="text">The text to synchronize with the template text box.</param>
        private void SyncTextBoxText(string text)
        {
            if (_textBox is null || string.Equals(_textBox.Text, text, StringComparison.Ordinal))
            {
                return;
            }

            _suppressTextBoxSync = true;
            try
            {
                _textBox.Text = text;
                _textBox.CaretIndex = text.Length;
            }
            finally
            {
                _suppressTextBoxSync = false;
            }
        }

        /// <summary>
        /// Raises <see cref="TextChanged"/> with the given reason, capturing the current
        /// text so <see cref="AutoSuggestBoxTextChangedEventArgs.CheckCurrent"/> works.
        /// </summary>
        /// <param name="reason">The reason for the text change.</param>
        private void RaiseTextChanged(AutoSuggestionBoxTextChangeReason reason)
        {
            AutoSuggestBoxTextChangedEventArgs args = new() { Reason = reason };
            args.Capture(this, Text);
            TextChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Opens the suggestion list when the control has keyboard focus and suggestions
        /// exist; always closes it when no suggestions exist. An explicitly opened list is
        /// left open so programmatic <see cref="IsSuggestionListOpen"/> control wins while
        /// suggestions are available.
        /// </summary>
        private void UpdateSuggestionListState()
        {
            if (!HasSuggestions())
            {
                SetCurrentValue(IsSuggestionListOpenProperty, value: false);
                return;
            }

            if (IsKeyboardFocusWithin)
            {
                SetCurrentValue(IsSuggestionListOpenProperty, value: true);
            }
        }

        /// <summary>
        /// Returns whether <see cref="ItemsSource"/> contains at least one suggestion.
        /// </summary>
        private bool HasSuggestions()
        {
            IEnumerable? itemsSource = ItemsSource;
            if (itemsSource is null)
            {
                return false;
            }

            if (itemsSource is ICollection collection)
            {
                return collection.Count > 0;
            }

            IEnumerator enumerator = itemsSource.GetEnumerator();
            try
            {
                return enumerator.MoveNext();
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }

        /// <summary>
        /// Moves the suggestion list selection by <paramref name="delta"/>, cycling through
        /// no selection like WinUI: stepping past either end clears the selection so the
        /// user returns to their typed text. While <see cref="UpdateTextOnSelect"/> is
        /// enabled, the highlighted suggestion's text is previewed into the box (raising
        /// <see cref="TextChanged"/> with
        /// <see cref="AutoSuggestionBoxTextChangeReason.SuggestionChosen"/>) and the original
        /// typed text is restored when the highlight cycles back to no selection.
        /// </summary>
        /// <param name="delta">The number of steps to move the selection.</param>
        private void MoveSuggestionSelection(int delta)
        {
            if (_suggestionsList is null)
            {
                return;
            }

            int count = _suggestionsList.Items.Count;
            if (count is 0)
            {
                return;
            }

            int index = _suggestionsList.SelectedIndex;
            index = delta > 0
                ? (index >= count - 1 ? -1 : index + 1)
                : (index < 0 ? count - 1 : index - 1);
            _suggestionsList.SelectedIndex = index;

            if (index >= 0 && _suggestionsList is System.Windows.Controls.ListBox listBox)
            {
                listBox.ScrollIntoView(listBox.Items[index]);
            }

            PreviewHighlightedSuggestion(index);
        }

        /// <summary>
        /// Applies the keyboard-highlight text preview for the suggestion at
        /// <paramref name="index"/>: the typed text is captured once when the highlight first
        /// enters the list, each highlighted suggestion is previewed into <see cref="Text"/>,
        /// and the typed text is restored when the highlight cycles back to no selection.
        /// No-op while <see cref="UpdateTextOnSelect"/> is disabled, matching WinUI.
        /// </summary>
        /// <param name="index">The index of the highlighted suggestion.</param>
        private void PreviewHighlightedSuggestion(int index)
        {
            if (!UpdateTextOnSelect)
            {
                return;
            }

            if (index >= 0)
            {
                object? item = _suggestionsList?.Items[index];
                if (item is null)
                {
                    return;
                }

                if (!_isPreviewingSuggestion)
                {
                    _typedText = Text;
                    _isPreviewingSuggestion = true;
                }

                SetTextWithReason(GetTextFromSuggestion(item), AutoSuggestionBoxTextChangeReason.SuggestionChosen);
            }
            else if (_isPreviewingSuggestion)
            {
                _isPreviewingSuggestion = false;
                SetTextWithReason(_typedText, AutoSuggestionBoxTextChangeReason.SuggestionChosen);
            }
        }

        /// <summary>
        /// Resolves the text written into <see cref="Text"/> when a suggestion is chosen,
        /// honoring <see cref="TextMemberPath"/> and falling back to the item's string
        /// representation.
        /// </summary>
        /// <param name="suggestion">The chosen suggestion.</param>
        private string GetTextFromSuggestion(object suggestion)
        {
            string textMemberPath = TextMemberPath;
            object? value = string.IsNullOrWhiteSpace(textMemberPath)
                ? suggestion
                : EvaluatePath(suggestion, textMemberPath) ?? suggestion;
            return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
        }

        /// <summary>
        /// The template text box hosting user input.
        /// </summary>
        private System.Windows.Controls.TextBox? _textBox;

        /// <summary>
        /// The light-dismiss popup hosting the suggestion list.
        /// </summary>
        private Popup? _popup;

        /// <summary>
        /// The selector presenting the suggestions.
        /// </summary>
        private Selector? _suggestionsList;

        /// <summary>
        /// The clickable query icon button hosted in the text box icon slot, when present.
        /// </summary>
        private ButtonBase? _queryButton;

        /// <summary>
        /// The text the user had typed before the keyboard highlight entered the suggestion
        /// list; restored when the highlight cycles back to no selection.
        /// </summary>
        private string _typedText = string.Empty;

        /// <summary>
        /// Whether the box is currently previewing a keyboard-highlighted suggestion, so the
        /// typed text is captured only once per navigation pass.
        /// </summary>
        private bool _isPreviewingSuggestion;

        /// <summary>
        /// Guards against re-entrancy while pushing <see cref="Text"/> into the template
        /// text box.
        /// </summary>
        private bool _suppressTextBoxSync;

        /// <summary>
        /// The reason to report for the in-flight <see cref="Text"/> change; defaults to
        /// <see cref="AutoSuggestionBoxTextChangeReason.ProgrammaticChange"/> when unset.
        /// </summary>
        private AutoSuggestionBoxTextChangeReason? _pendingChangeReason;
    }
}
