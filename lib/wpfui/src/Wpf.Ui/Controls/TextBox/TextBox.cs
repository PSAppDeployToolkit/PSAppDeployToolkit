// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Diagnostics;
using System.Windows.Controls;
using Wpf.Ui.Converters;
using Wpf.Ui.Input;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Extended <see cref="System.Windows.Controls.TextBox"/> with additional parameters like <see cref="PlaceholderText"/>.
/// </summary>
public class TextBox : System.Windows.Controls.TextBox
{
    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(IconElement),
        typeof(TextBox),
        new PropertyMetadata(null, null, IconElement.Coerce)
    );

    /// <summary>Identifies the <see cref="IconPlacement"/> dependency property.</summary>
    public static readonly DependencyProperty IconPlacementProperty = DependencyProperty.Register(
        nameof(IconPlacement),
        typeof(ElementPlacement),
        typeof(TextBox),
        new PropertyMetadata(ElementPlacement.Left)
    );

    /// <summary>Identifies the <see cref="PlaceholderText"/> dependency property.</summary>
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
        nameof(PlaceholderText),
        typeof(string),
        typeof(TextBox),
        new PropertyMetadata(string.Empty)
    );

    /// <summary>Identifies the <see cref="PlaceholderEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty PlaceholderEnabledProperty = DependencyProperty.Register(
        nameof(PlaceholderEnabled),
        typeof(bool),
        typeof(TextBox),
        new PropertyMetadata(true, OnPlaceholderEnabledChanged)
    );

    /// <summary>Identifies the <see cref="CurrentPlaceholderEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentPlaceholderEnabledProperty = DependencyProperty.Register(
        nameof(CurrentPlaceholderEnabled),
        typeof(bool),
        typeof(TextBox),
        new PropertyMetadata(true)
    );

    /// <summary>Identifies the <see cref="ClearButtonEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty ClearButtonEnabledProperty = DependencyProperty.Register(
        nameof(ClearButtonEnabled),
        typeof(bool),
        typeof(TextBox),
        new PropertyMetadata(true)
    );

    /// <summary>Identifies the <see cref="ShowClearButton"/> dependency property.</summary>
    public static readonly DependencyProperty ShowClearButtonProperty = DependencyProperty.Register(
        nameof(ShowClearButton),
        typeof(bool),
        typeof(TextBox),
        new PropertyMetadata(false)
    );

    /// <summary>Identifies the <see cref="IsTextSelectionEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsTextSelectionEnabledProperty = DependencyProperty.Register(
        nameof(IsTextSelectionEnabled),
        typeof(bool),
        typeof(TextBox),
        new PropertyMetadata(false)
    );

    /// <summary>Identifies the <see cref="TemplateButtonCommand"/> dependency property.</summary>
    public static readonly DependencyProperty TemplateButtonCommandProperty = DependencyProperty.Register(
        nameof(TemplateButtonCommand),
        typeof(IRelayCommand),
        typeof(TextBox),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Gets or sets displayed <see cref="IconElement"/>.
    /// </summary>
    public IconElement? Icon
    {
        get => (IconElement?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets which side the icon should be placed on.
    /// </summary>
    public ElementPlacement IconPlacement
    {
        get => (ElementPlacement)GetValue(IconPlacementProperty);
        set => SetValue(IconPlacementProperty, value);
    }

    /// <summary>
    /// Gets or sets placeholder text.
    /// </summary>
    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to enable the placeholder text.
    /// </summary>
    public bool PlaceholderEnabled
    {
        get => (bool)GetValue(PlaceholderEnabledProperty);
        set => SetValue(PlaceholderEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to display the placeholder text.
    /// </summary>
    public bool CurrentPlaceholderEnabled
    {
        get => (bool)GetValue(CurrentPlaceholderEnabledProperty);
        protected set => SetValue(CurrentPlaceholderEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to enable the clear button.
    /// </summary>
    public bool ClearButtonEnabled
    {
        get => (bool)GetValue(ClearButtonEnabledProperty);
        set => SetValue(ClearButtonEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the clear button when <see cref="TextBox"/> is focused.
    /// </summary>
    public bool ShowClearButton
    {
        get => (bool)GetValue(ShowClearButtonProperty);
        protected set => SetValue(ShowClearButtonProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether text selection is enabled.
    /// </summary>
    public bool IsTextSelectionEnabled
    {
        get => (bool)GetValue(IsTextSelectionEnabledProperty);
        set => SetValue(IsTextSelectionEnabledProperty, value);
    }

    /// <summary>
    /// Gets the command triggered when clicking the button.
    /// </summary>
    public IRelayCommand TemplateButtonCommand => (IRelayCommand)GetValue(TemplateButtonCommandProperty);

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBox"/> class.
    /// </summary>
    public TextBox()
    {
        SetValue(TemplateButtonCommandProperty, new RelayCommand<string>(OnTemplateButtonClick));
        CurrentPlaceholderEnabled = PlaceholderEnabled;
    }

    /// <inheritdoc />
    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);

        SetPlaceholderTextVisibility();

        RevealClearButton();
    }

    protected void SetPlaceholderTextVisibility()
    {
        if (PlaceholderEnabled)
        {
            if (CurrentPlaceholderEnabled && Text.Length > 0)
            {
                SetCurrentValue(CurrentPlaceholderEnabledProperty, false);
            }

            if (!CurrentPlaceholderEnabled && Text.Length < 1)
            {
                SetCurrentValue(CurrentPlaceholderEnabledProperty, true);
            }
        }
        else if (CurrentPlaceholderEnabled)
        {
            SetCurrentValue(CurrentPlaceholderEnabledProperty, false);
        }
    }

    /// <inheritdoc />
    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);

        CaretIndex = Text.Length;

        RevealClearButton();
    }

    /// <inheritdoc />
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        HideClearButton();
    }

    /// <summary>
    /// Reveals the clear button by <see cref="ShowClearButton"/> property.
    /// </summary>
    protected void RevealClearButton()
    {
        if (ClearButtonEnabled && IsKeyboardFocusWithin)
        {
            SetCurrentValue(ShowClearButtonProperty, Text.Length > 0);
        }
    }

    /// <summary>
    /// Hides the clear button by <see cref="ShowClearButton"/> property.
    /// </summary>
    protected void HideClearButton()
    {
        if (ClearButtonEnabled && !IsKeyboardFocusWithin && ShowClearButton)
        {
            SetCurrentValue(ShowClearButtonProperty, false);
        }
    }

    /// <summary>
    /// Triggered when the user clicks the clear text button.
    /// </summary>
    protected virtual void OnClearButtonClick()
    {
        if (Text.Length > 0)
        {
            SetCurrentValue(TextProperty, string.Empty);
        }
    }

    /// <summary>
    /// Triggered by clicking a button in the control template.
    /// </summary>
    protected virtual void OnTemplateButtonClick(string? parameter)
    {
        Debug.WriteLine($"INFO: {typeof(TextBox)} button clicked", "Wpf.Ui.TextBox");

        OnClearButtonClick();
    }

    private static void OnPlaceholderEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox control)
        {
            return;
        }

        control.OnPlaceholderEnabledChanged();
    }

    protected virtual void OnPlaceholderEnabledChanged()
    {
        SetPlaceholderTextVisibility();
    }
}
