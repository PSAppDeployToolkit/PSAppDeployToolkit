// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.Diagnostics;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Input;
using Color = System.Windows.Media.Color;

namespace Wpf.Ui.SyntaxHighlight.Controls;

/// <summary>
/// Formats and display a fragment of the source code.
/// </summary>
public class CodeBlock : System.Windows.Controls.ContentControl
{
    private string _sourceCode = string.Empty;

    /// <summary>
    /// Property for <see cref="SyntaxContent"/>.
    /// </summary>
    public static readonly DependencyProperty SyntaxContentProperty = DependencyProperty.Register(
        nameof(SyntaxContent),
        typeof(object),
        typeof(CodeBlock),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Property for <see cref="ButtonCommand"/>.
    /// </summary>
    public static readonly DependencyProperty ButtonCommandProperty = DependencyProperty.Register(
        nameof(ButtonCommand),
        typeof(IRelayCommand),
        typeof(CodeBlock)
    );

    /// <summary>
    /// Formatted <see cref="System.Windows.Controls.ContentControl.Content"/>.
    /// </summary>
    public object SyntaxContent
    {
        get => GetValue(SyntaxContentProperty);
        internal set => SetValue(SyntaxContentProperty, value);
    }

    /// <summary>
    /// Command triggered after clicking the control button.
    /// </summary>
    public IRelayCommand ButtonCommand => (IRelayCommand)GetValue(ButtonCommandProperty);

    /// <summary>
    /// Creates new instance and assigns <see cref="ButtonCommand"/> default action.
    /// </summary>
    public CodeBlock()
    {
        SetValue(ButtonCommandProperty, new RelayCommand<string>(OnTemplateButtonClick));

        ApplicationThemeManager.Changed += ThemeOnChanged;
    }

    private void ThemeOnChanged(ApplicationTheme currentApplicationTheme, Color systemAccent)
    {
        UpdateSyntax();
    }

    /// <summary>
    /// This method is invoked when the Content property changes.
    /// </summary>
    /// <param name="oldContent">The old value of the Content property.</param>
    /// <param name="newContent">The new value of the Content property.</param>
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        UpdateSyntax();
    }

    protected virtual void UpdateSyntax()
    {
        _sourceCode = Highlighter.Clean(Content as string ?? string.Empty);

        var richTextBox = new RichTextBox()
        {
            IsTextSelectionEnabled = true,
            VerticalContentAlignment = VerticalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            HorizontalContentAlignment = HorizontalAlignment.Left
        };

        richTextBox.Document.Blocks.Clear();
        richTextBox.Document.Blocks.Add(Highlighter.FormatAsParagraph(_sourceCode));

        SyntaxContent = richTextBox;
    }

    private void OnTemplateButtonClick(string? _)
    {
        Debug.WriteLine($"INFO | CodeBlock source: \n{_sourceCode}", "Wpf.Ui.CodeBlock");

        try
        {
            Clipboard.Clear();
            Clipboard.SetText(_sourceCode);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }
}
