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
using System.Windows.Controls;
using System.Windows.Markup;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Colors page group example card: a title, a one-line description and a centred example,
    /// mirroring the WinUI 3 Gallery <c language="csharp">ColorPageExample</c> control.
    /// </summary>
    /// <remarks>
    /// <see cref="Control.Background"/> paints the card and defaults to the quarternary solid
    /// background; <see cref="Control.Foreground"/> colours the title and description so the
    /// accent group can invert them, exactly as the WinUI Gallery does.
    /// </remarks>
    [ContentProperty(nameof(ExampleContent))]
    public partial class ColorPageExample : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(ColorPageExample),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="Description"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(ColorPageExample),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="ExampleContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExampleContentProperty =
            DependencyProperty.Register(
                nameof(ExampleContent),
                typeof(object),
                typeof(ColorPageExample),
                new FrameworkPropertyMetadata(defaultValue: null));

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorPageExample"/> class.
        /// </summary>
        public ColorPageExample()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the group title shown in the subtitle type style.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the one-line usage note shown under the title.
        /// </summary>
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>
        /// Gets or sets the live example element centred under the description.
        /// </summary>
        public object? ExampleContent
        {
            get => GetValue(ExampleContentProperty);
            set => SetValue(ExampleContentProperty, value);
        }
    }
}
