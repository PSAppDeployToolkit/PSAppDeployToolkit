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

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfGrid = System.Windows.Controls.Grid;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design circular avatar control that displays a profile photo, initials,
    /// or a placeholder glyph, with an optional badge.
    /// Authority: WinUI 3 PersonPicture.xaml + PersonPicture_themeresources.xaml.
    /// Visual states: Photo, Initials, NoPhotoOrInitials, Group (CommonStates);
    /// NoBadge, BadgeWithoutImageSource (BadgeStates).
    /// </summary>
    [TemplatePart(Name = PART_InitialsText, Type = typeof(WpfTextBlock))]
    [TemplatePart(Name = PART_ImageEllipse, Type = typeof(Ellipse))]
    [TemplatePart(Name = PART_BadgeGrid, Type = typeof(WpfGrid))]
    [TemplatePart(Name = PART_BadgeText, Type = typeof(WpfTextBlock))]
    [TemplateVisualState(GroupName = GroupCommonStates, Name = StatePhoto)]
    [TemplateVisualState(GroupName = GroupCommonStates, Name = StateInitials)]
    [TemplateVisualState(GroupName = GroupCommonStates, Name = StateNoPhotoOrInitials)]
    [TemplateVisualState(GroupName = GroupCommonStates, Name = StateGroup)]
    [TemplateVisualState(GroupName = GroupBadgeStates, Name = StateNoBadge)]
    [TemplateVisualState(GroupName = GroupBadgeStates, Name = StateBadgeWithoutImageSource)]
    public class PersonPicture : Control
    {
        // Template part names.
        private const string PART_InitialsText = "PART_InitialsText";
        private const string PART_ImageEllipse = "PART_ImageEllipse";
        private const string PART_BadgeGrid = "PART_BadgeGrid";
        private const string PART_BadgeText = "PART_BadgeText";

        // Visual state names.
        private const string GroupCommonStates = "CommonStates";
        private const string StatePhoto = "Photo";
        private const string StateInitials = "Initials";
        private const string StateNoPhotoOrInitials = "NoPhotoOrInitials";
        private const string StateGroup = "Group";

        // Badge visual states: when badge is shown with or without an image source, to adjust badge styling as needed.
        private const string GroupBadgeStates = "BadgeStates";
        private const string StateNoBadge = "NoBadge";
        private const string StateBadgeWithoutImageSource = "BadgeWithoutImageSource";

        // Segoe Fluent Icons glyphs for placeholder states.
        private const string GlyphContact = "\uE77B";
        private const string GlyphPeople = "\uE716";
        private static readonly char[] InitialsSeparators = [' ', '\t'];

        /// <summary>
        /// Initializes static members of the PersonPicture class and overrides the default style key to associate the
        /// control with its style.
        /// </summary>
        /// <remarks>This static constructor ensures that the PersonPicture control uses the correct
        /// default style as defined in the application's resources. This is necessary for custom controls to apply
        /// their styles properly in WPF.</remarks>
        static PersonPicture()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(PersonPicture),
                new FrameworkPropertyMetadata(typeof(PersonPicture)));
        }

        /// <summary>
        /// Identifies the <see cref="DisplayName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register(
                nameof(DisplayName),
                typeof(string),
                typeof(PersonPicture),
                new FrameworkPropertyMetadata(string.Empty, OnDisplayNameChanged));

        /// <summary>
        /// Gets or sets the person's display name. Up to two initials are derived automatically
        /// unless <see cref="Initials"/> is set explicitly.
        /// </summary>
        public string DisplayName
        {
            get => (string)GetValue(DisplayNameProperty);
            set => SetValue(DisplayNameProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Initials"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InitialsProperty =
            DependencyProperty.Register(
                nameof(Initials),
                typeof(string),
                typeof(PersonPicture),
                new FrameworkPropertyMetadata(string.Empty, OnInitialsChanged));

        /// <summary>
        /// Gets or sets explicit initials to display. When set, overrides the initials
        /// derived from <see cref="DisplayName"/>.
        /// </summary>
        public string Initials
        {
            get => (string)GetValue(InitialsProperty);
            set => SetValue(InitialsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ProfilePicture"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ProfilePictureProperty =
            DependencyProperty.Register(
                nameof(ProfilePicture),
                typeof(ImageSource),
                typeof(PersonPicture),
                new FrameworkPropertyMetadata(defaultValue: null, OnProfilePictureChanged));

        /// <summary>
        /// Gets or sets the profile photo image source.
        /// </summary>
        public ImageSource ProfilePicture
        {
            get => (ImageSource)GetValue(ProfilePictureProperty);
            set => SetValue(ProfilePictureProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsGroup"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsGroupProperty =
            DependencyProperty.Register(
                nameof(IsGroup),
                typeof(bool),
                typeof(PersonPicture),
                new FrameworkPropertyMetadata(defaultValue: false, OnIsGroupChanged));

        /// <summary>
        /// Gets or sets whether the avatar represents a group rather than an individual.
        /// When <see langword="true"/>, the Group (people) glyph is displayed.
        /// </summary>
        public bool IsGroup
        {
            get => (bool)GetValue(IsGroupProperty);
            set => SetValue(IsGroupProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BadgeNumber"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BadgeNumberProperty =
            DependencyProperty.Register(
                nameof(BadgeNumber),
                typeof(int),
                typeof(PersonPicture),
                new FrameworkPropertyMetadata(0, OnBadgeChanged));

        /// <summary>
        /// Gets or sets a numeric badge displayed in the bottom-right corner.
        /// Set to 0 (default) to hide the badge.
        /// </summary>
        public int BadgeNumber
        {
            get => (int)GetValue(BadgeNumberProperty);
            set => SetValue(BadgeNumberProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BadgeGlyph"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BadgeGlyphProperty =
            DependencyProperty.Register(
                nameof(BadgeGlyph),
                typeof(string),
                typeof(PersonPicture),
                new FrameworkPropertyMetadata(defaultValue: null, OnBadgeChanged));

        /// <summary>
        /// Gets or sets a Segoe Fluent Icons glyph shown in the badge.
        /// Takes precedence over <see cref="BadgeNumber"/> when both are set.
        /// Set to <see langword="null"/> (default) to show no glyph badge.
        /// </summary>
        public string BadgeGlyph
        {
            get => (string)GetValue(BadgeGlyphProperty);
            set => SetValue(BadgeGlyphProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _initialsText = GetTemplateChild(PART_InitialsText) as WpfTextBlock;
            _imageEllipse = GetTemplateChild(PART_ImageEllipse) as Ellipse;
            _badgeGrid = GetTemplateChild(PART_BadgeGrid) as WpfGrid;
            _badgeText = GetTemplateChild(PART_BadgeText) as WpfTextBlock;
            UpdateVisualState(useTransitions: false);
            UpdateBadge();
        }

        private static void OnDisplayNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PersonPicture)d).UpdateVisualState();
        }

        private static void OnInitialsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PersonPicture)d).UpdateVisualState();
        }

        private static void OnProfilePictureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PersonPicture)d).UpdateVisualState();
        }

        private static void OnIsGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PersonPicture)d).UpdateVisualState();
        }

        private static void OnBadgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PersonPicture)d).UpdateBadge();
        }

        private void UpdateVisualState(bool useTransitions = true)
        {
            if (IsGroup)
            {
                // Group: show people glyph
                if (_initialsText is not null)
                {
                    _initialsText.Text = GlyphPeople;
                    _initialsText.SetResourceReference(WpfTextBlock.FontFamilyProperty, "FluentFontFamily");
                }
                _ = VisualStateManager.GoToState(this, StateGroup, useTransitions);
                return;
            }
            if (ProfilePicture is not null)
            {
                // Photo state: fill the image ellipse
                _ = _imageEllipse?.Fill = new ImageBrush(ProfilePicture) { Stretch = Stretch.UniformToFill };

                _ = VisualStateManager.GoToState(this, StatePhoto, useTransitions);
                return;
            }
            if (GetInitials() is string initials)
            {
                // Initials state
                if (_initialsText is not null)
                {
                    _initialsText.Text = initials;
                    _initialsText.SetResourceReference(WpfTextBlock.FontFamilyProperty, "FluentFontFamily");
                }
                _ = VisualStateManager.GoToState(this, StateInitials, useTransitions);
                return;
            }

            // No photo or initials: show contact glyph (Segoe Fluent Icons U+E77B)
            if (_initialsText is not null)
            {
                _initialsText.Text = GlyphContact;
                _initialsText.FontFamily = new FontFamily("Segoe Fluent Icons");
            }
            _ = VisualStateManager.GoToState(this, StateNoPhotoOrInitials, useTransitions);
        }

        private void UpdateBadge()
        {
            bool hasBadge = !string.IsNullOrWhiteSpace(BadgeGlyph) || BadgeNumber > 0;
            _ = _badgeGrid?.Visibility = hasBadge ? Visibility.Visible : Visibility.Collapsed;
            if (_badgeText is not null)
            {
                if (!string.IsNullOrWhiteSpace(BadgeGlyph))
                {
                    _badgeText.Text = BadgeGlyph;
                    _badgeText.FontFamily = new FontFamily("Segoe Fluent Icons");
                    _badgeText.FontSize = 10;
                }
                else if (BadgeNumber > 0)
                {
                    _badgeText.Text = BadgeNumber > 99 ? "99+" : BadgeNumber.ToString(CultureInfo.CurrentCulture);
                    _badgeText.SetResourceReference(WpfTextBlock.FontFamilyProperty, "FluentFontFamily");
                    _badgeText.FontSize = BadgeNumber > 9 ? 8 : 10;
                }
                else
                {
                    _badgeText.Text = string.Empty;
                }
            }
            _ = VisualStateManager.GoToState(this, hasBadge ? StateBadgeWithoutImageSource : StateNoBadge, useTransitions: true);
        }

        private string? GetInitials()
        {
            // Explicit initials take precedence.
            if (!string.IsNullOrWhiteSpace(Initials))
            {
                return Initials.Length > 2 ? Initials[..2].ToUpperInvariant() : Initials.ToUpperInvariant();
            }

            // Derive from DisplayName: take first character of up to two words.
            string[] parts = (DisplayName ?? string.Empty).Trim().Split(InitialsSeparators, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1
                ? (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpperInvariant()
                : parts.Length == 1
                ? parts[0][0].ToString().ToUpperInvariant()
                : null;
        }

        /// <summary>
        /// Represents the WPF TextBlock control used to display user initials.
        /// </summary>
        private WpfTextBlock? _initialsText;

        /// <summary>
        /// Represents the ellipse shape used to display an image, or null if no ellipse is assigned.
        /// </summary>
        private Ellipse? _imageEllipse;

        /// <summary>
        /// Represents the WPF grid used to display the badge, or null if no badge grid is present.
        /// </summary>
        private WpfGrid? _badgeGrid;

        /// <summary>
        /// Represents the text block used to display a badge in the WPF user interface.
        /// </summary>
        private WpfTextBlock? _badgeText;
    }
}
