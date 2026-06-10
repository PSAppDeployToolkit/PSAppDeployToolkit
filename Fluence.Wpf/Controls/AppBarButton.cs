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
    /// Represents a templated command button for command bar surfaces such as
    /// <see cref="CommandBarFlyout"/>, mirroring the WinUI 3 <c>AppBarButton</c> contract.
    /// </summary>
    /// <remarks>
    /// The default style renders the compact primary-bar appearance: a 40x40 hit target with a
    /// centered <see cref="Icon"/> and the <see cref="Label"/> surfaced as a tooltip. Inside the
    /// overflow menu of a <see cref="CommandBarFlyout"/> the presenter applies the
    /// <c>CommandBarFlyoutSecondaryAppBarButtonStyle</c> resource, which renders the icon and
    /// label side by side in a full-width menu-item row.
    /// </remarks>
    public class AppBarButton : System.Windows.Controls.Button
    {
        /// <summary>
        /// Initializes static members of the <see cref="AppBarButton"/> class and overrides the
        /// default style metadata so the themed template in Generic.xaml applies.
        /// </summary>
        static AppBarButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(AppBarButton),
                new FrameworkPropertyMetadata(typeof(AppBarButton)));
        }

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(AppBarButton),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon shown on the button, typically a <see cref="FontIcon"/>.
        /// Arbitrary content is supported and is hosted in a centered content presenter.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Label"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(AppBarButton),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the text label that describes the command. The compact default style
        /// surfaces the label as a tooltip; the overflow style renders it next to the icon.
        /// </summary>
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
    }
}
