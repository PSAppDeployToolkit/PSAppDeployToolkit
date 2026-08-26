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

using System.Windows;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design styled ToggleButton with accent checked state.
    /// </summary>
    public class ToggleButton : System.Windows.Controls.Primitives.ToggleButton
    {
        /// <summary>
        /// Initializes static members of the ToggleButton class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the ToggleButton control uses its own style by
        /// default, rather than inheriting the style from its base class. This is important for custom control theming
        /// in WPF.</remarks>
        static ToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ToggleButton),
                new FrameworkPropertyMetadata(typeof(ToggleButton)));
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(ToggleButton),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Identifies the <see cref="Appearance"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AppearanceProperty =
            DependencyProperty.Register(
                nameof(Appearance),
                typeof(ControlAppearance),
                typeof(ToggleButton),
                new FrameworkPropertyMetadata(ControlAppearance.Standard));

        /// <summary>
        /// Gets or sets the corner radius of the toggle button.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets or sets the visual appearance of the toggle button.
        /// </summary>
        /// <remarks>
        /// The default template renders the single canonical WinUI toggle visual for
        /// every <see cref="ControlAppearance"/> value: the checked state is the accent
        /// state, so a separate accent rest variant would make checked and unchecked
        /// indistinguishable. The property exists primarily for derived controls such as
        /// <see cref="DropDownButton"/>, which consume it in their own templates.
        /// </remarks>
        public ControlAppearance Appearance
        {
            get => (ControlAppearance)GetValue(AppearanceProperty);
            set => SetValue(AppearanceProperty, value);
        }
    }
}
