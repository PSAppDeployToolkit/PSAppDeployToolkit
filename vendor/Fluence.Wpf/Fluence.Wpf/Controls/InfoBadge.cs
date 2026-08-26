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

using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A small badge overlay that displays a numeric value, icon, or dot indicator.
    /// Typically attached to a <see cref="NavigationViewItem"/> or other control.
    /// </summary>
    [TemplateVisualState(GroupName = "DisplayKindStates", Name = "Dot")]
    [TemplateVisualState(GroupName = "DisplayKindStates", Name = "Icon")]
    [TemplateVisualState(GroupName = "DisplayKindStates", Name = "FontIcon")]
    [TemplateVisualState(GroupName = "DisplayKindStates", Name = "Value")]
    public class InfoBadge : ContentControl
    {
        /// <summary>
        /// Initializes static members of the InfoBadge class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the InfoBadge control uses its custom style by
        /// default. It is called automatically by the .NET runtime before any static members are accessed or any
        /// instances are created.</remarks>
        static InfoBadge()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(InfoBadge),
                new FrameworkPropertyMetadata(typeof(InfoBadge)));
        }

        /// <summary>
        /// Identifies the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(int),
                typeof(InfoBadge),
                new FrameworkPropertyMetadata(-1, OnValueChanged));

        /// <summary>
        /// Gets or sets the numeric value displayed. Set to -1 to show a dot instead.
        /// </summary>
        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BadgeStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BadgeStyleProperty =
            DependencyProperty.Register(
                nameof(BadgeStyle),
                typeof(InfoBadgeStyle),
                typeof(InfoBadge),
                new FrameworkPropertyMetadata(InfoBadgeStyle.Attention));

        /// <summary>
        /// Gets or sets the severity style of the badge.
        /// </summary>
        public InfoBadgeStyle BadgeStyle
        {
            get => (InfoBadgeStyle)GetValue(BadgeStyleProperty);
            set => SetValue(BadgeStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IconSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(
                nameof(IconSource),
                typeof(object),
                typeof(InfoBadge),
                new FrameworkPropertyMetadata(defaultValue: null, OnIconSourceChanged));

        /// <summary>
        /// Gets or sets an icon element to display inside the badge (overrides Value text).
        /// </summary>
        public object IconSource
        {
            get => GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateDisplayKindState(useTransitions: false);
        }

        private static void OnIconSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            InfoBadge badge = (InfoBadge)d;
            badge.Content = e.NewValue ?? (badge.Value >= 0 ? badge.Value.ToString(CultureInfo.CurrentCulture) : null);
            badge.UpdateDisplayKindState();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            InfoBadge badge = (InfoBadge)d;
            if (badge.IconSource is null)
            {
                int val = (int)e.NewValue;
                badge.Content = val >= 0 ? val.ToString(CultureInfo.CurrentCulture) : null;
                badge.UpdateDisplayKindState();
            }

        }

        private void UpdateDisplayKindState(bool useTransitions = true)
        {
            string state = IconSource is not null ? "Icon" : Value >= 0 ? "Value" : "Dot";
            _ = VisualStateManager.GoToState(this, state, useTransitions);
        }
    }
}
