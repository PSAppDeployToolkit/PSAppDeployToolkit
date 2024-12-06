// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Represents an icon that uses an <see cref="System.Windows.Controls.Image"/> as its content.
/// </summary>
public class ImageIcon : IconElement
{
    /// <summary>Identifies the <see cref="Source"/> dependency property.</summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(ImageSource),
        typeof(ImageIcon),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
            OnSourceChanged
        )
    );

    /// <summary>
    /// Gets or sets the Source on this Image.
    /// </summary>
    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    protected System.Windows.Controls.Image? Image { get; set; }

    protected override UIElement InitializeChildren()
    {
        Image = new System.Windows.Controls.Image() { Source = Source, Stretch = Stretch.UniformToFill };

        return Image;
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ImageIcon self = (ImageIcon)d;

        if (self.Image is null)
        {
            return;
        }

        self.Image.SetCurrentValue(System.Windows.Controls.Image.SourceProperty, (ImageSource?)e.NewValue);
    }
}
