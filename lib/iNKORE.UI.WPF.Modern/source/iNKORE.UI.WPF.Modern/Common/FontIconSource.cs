// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Modern.Common
{
    /// <summary>
    /// Represents an icon source that uses a glyph from the specified font.
    /// </summary>
    public class FontIconSource : IconSource, IFontIconClass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FontIconSource"/> class.
        /// </summary>
        public FontIconSource()
        {
        }

        /// <summary>
        /// Identifies the <see cref="FontFamily"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register(
                nameof(FontFamily),
                typeof(FontFamily),
                typeof(FontIconSource),
                new PropertyMetadata(new FontFamily(FontIcon.SegoeIconsFontFamilyName), FontFamilyProperty_ValueChanged));

        private static void FontFamilyProperty_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is IFontIconClass cls)
            {
                UpdateIconData(cls, false);
            }
        }

        /// <summary>
        /// Gets or sets the font used to display the icon glyph.
        /// </summary>
        /// <returns>
        /// The font used to display the icon glyph.
        /// </returns>
        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                nameof(FontSize),
                typeof(double),
                typeof(FontIconSource),
                new PropertyMetadata(16d));

        /// <summary>
        /// Gets or sets the size of the icon glyph.
        /// </summary>
        /// <returns>
        /// A non-negative value that specifies the font size, measured in pixels.
        /// </returns>
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FontStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontStyleProperty =
            DependencyProperty.Register(
                nameof(FontStyle),
                typeof(FontStyle),
                typeof(FontIconSource),
                new PropertyMetadata(FontStyles.Normal));

        /// <summary>
        /// Gets or sets the font style for the icon glyph.
        /// </summary>
        /// <returns>
        /// A named constant of the enumeration that specifies the style in which the icon glyph is rendered.
        /// The default is <see cref="FontStyles.Normal"/>.
        /// </returns>
        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FontWeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontWeightProperty =
            DependencyProperty.Register(
                nameof(FontWeight),
                typeof(FontWeight),
                typeof(FontIconSource),
                new PropertyMetadata(FontWeights.Normal));

        /// <summary>
        /// Gets or sets the thickness of the icon glyph.
        /// </summary>
        /// <returns>
        /// A value that specifies the thickness of the icon glyph.
        /// The default is <see cref="FontWeights.Normal"/>.
        /// </returns>
        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Glyph"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(
                nameof(Glyph),
                typeof(string),
                typeof(FontIconSource),
                new PropertyMetadata(string.Empty, GlyphProperty_ValueChanged));

        private static void GlyphProperty_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is IFontIconClass cls)
            {
                UpdateIconData(cls, false);
            }
        }

        /// <summary>
        /// Gets or sets the character code that identifies the icon glyph.
        /// </summary>
        /// <returns>
        /// The hexadecimal character code for the icon glyph.
        /// </returns>
        public string Glyph
        {
            get => (string)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(FontIconData?),
                typeof(FontIconSource),
                new PropertyMetadata(null, (d, e) => UpdateIconData(d as IFontIconClass, true)));

        /// <summary>
        /// Gets or sets the wrapped icon, which includes <see cref="Glyph"/> and <see cref="FontFamily"/>. You can get these instances from <see cref="iNKORE.UI.WPF.Modern.Common.IconKeys"/> namespace.
        /// If you are using Glyph and FontFamily property, this can be ignored.
        /// </summary>
        public FontIconData? Icon
        {
            get => (FontIconData?)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }


        public static bool UpdateIconData(IFontIconClass instance, bool preserveData)
        {
            bool isChanged = false;

            if (instance.Icon.HasValue)
            {
                var icon = instance.Icon.Value;

                if (instance.Glyph != icon.Glyph)
                {
                    if (preserveData)
                        instance.Glyph = icon.Glyph;
                    else
                    {
                        instance.Icon = null;
                        return true;
                    }
                    isChanged = true;
                }
                if (icon.FontFamily != null && icon.FontFamily != instance.FontFamily)
                {
                    if (preserveData)
                        instance.FontFamily = icon.FontFamily;
                    else
                    {
                        instance.Icon = null;
                        return true;
                    }

                    isChanged = true;
                }
            }

            return isChanged;
        }



        /// <inheritdoc/>
        protected override IconElement CreateIconElementCore()
        {
            FontIcon fontIcon = new FontIcon();

            fontIcon.Glyph = Glyph;
            fontIcon.FontSize = FontSize;
            var newForeground = Foreground;
            if (newForeground != null)
            {
                fontIcon.Foreground = newForeground;
            }

            if (FontFamily == null)
            {
                FontFamily = new FontFamily(FontIcon.SegoeIconsFontFamilyName);
            }
            fontIcon.FontFamily = FontFamily;

            fontIcon.FontWeight = FontWeight;
            fontIcon.FontStyle = FontStyle;

            return fontIcon;
        }
    }

    public interface IFontIconClass
    {
        FontFamily FontFamily { get; set; }

        string Glyph { get; set; }

        double FontSize { get; set; }

        Brush Foreground { get; set; }

        FontIconData? Icon { get; set; }
    }
}
