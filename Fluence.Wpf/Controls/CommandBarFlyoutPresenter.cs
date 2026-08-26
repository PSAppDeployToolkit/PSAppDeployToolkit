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
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Displays the commands of a <see cref="CommandBarFlyout"/> on the canonical Fluent flyout
    /// surface: a horizontal primary bar, a more button shown while secondary commands exist,
    /// and a collapsible overflow menu below the bar toggled via <see cref="IsExpanded"/>. The
    /// themed template lives in <c>Themes/Controls/CommandBarFlyout.xaml</c>.
    /// </summary>
    /// <remarks>
    /// Clicking any <see cref="AppBarButton"/> hosted in the presenter (other than the more
    /// button) dismisses the owning flyout after the button's normal <c>Click</c> handling,
    /// matching the WinUI command-dismiss behavior. The WinUI <c>AlwaysExpanded</c> option is
    /// omitted for v1, so the overflow collapses again whenever the flyout closes.
    /// </remarks>
    [TemplatePart(Name = PART_PrimaryItemsControl, Type = typeof(System.Windows.Controls.ItemsControl))]
    [TemplatePart(Name = PART_MoreButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PART_SecondaryItemsControl, Type = typeof(System.Windows.Controls.ItemsControl))]
    [TemplatePart(Name = PART_SecondaryHost, Type = typeof(FrameworkElement))]
    public class CommandBarFlyoutPresenter : System.Windows.Controls.Control
    {
        // Template part names.
        private const string PART_PrimaryItemsControl = "PART_PrimaryItemsControl";
        private const string PART_MoreButton = "PART_MoreButton";
        private const string PART_SecondaryItemsControl = "PART_SecondaryItemsControl";
        private const string PART_SecondaryHost = "PART_SecondaryHost";

        /// <summary>
        /// Initializes static members of the <see cref="CommandBarFlyoutPresenter"/> class and
        /// overrides the default style metadata so the themed template in Generic.xaml applies.
        /// </summary>
        static CommandBarFlyoutPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(CommandBarFlyoutPresenter),
                new FrameworkPropertyMetadata(typeof(CommandBarFlyoutPresenter)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBarFlyoutPresenter"/> class and
        /// subscribes a handled-too <see cref="ButtonBase.ClickEvent"/> handler so any command
        /// invoked inside the presenter also dismisses the owning flyout.
        /// </summary>
        public CommandBarFlyoutPresenter()
        {
            AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnFlyoutCommandClick), handledEventsToo: true);
        }

        /// <summary>
        /// Identifies the <see cref="IsExpanded"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(
                nameof(IsExpanded),
                typeof(bool),
                typeof(CommandBarFlyoutPresenter),
                new FrameworkPropertyMetadata(defaultValue: false));

        /// <summary>
        /// Gets or sets a value indicating whether the secondary (overflow) command area below
        /// the primary bar is visible. Toggled by the more button; reset to
        /// <see langword="false"/> whenever the owning flyout closes.
        /// </summary>
        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="CommandBarFlyout"/> whose command collections feed this
        /// presenter. Internal so the flyout can bind its cached presenter to itself.
        /// </summary>
        internal CommandBarFlyout? Owner
        {
            get;
            set
            {
                if (ReferenceEquals(field, value))
                {
                    return;
                }

                if (field is not null)
                {
                    field.SecondaryCommands.CollectionChanged -= OnSecondaryCommandsChanged;
                    field.Closed -= OnOwnerClosed;
                }

                field = value;
                if (field is not null)
                {
                    field.SecondaryCommands.CollectionChanged += OnSecondaryCommandsChanged;
                    field.Closed += OnOwnerClosed;
                }

                WireOwnerCollections();
            }
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            _moreButton?.Click -= OnMoreButtonClick;
            base.OnApplyTemplate();
            _primaryItemsControl = GetTemplateChild(PART_PrimaryItemsControl) as System.Windows.Controls.ItemsControl;
            _moreButton = GetTemplateChild(PART_MoreButton) as ButtonBase;
            _secondaryItemsControl = GetTemplateChild(PART_SecondaryItemsControl) as System.Windows.Controls.ItemsControl;
            _moreButton?.Click += OnMoreButtonClick;
            WireOwnerCollections();
        }

        /// <summary>
        /// Feeds the owner's command collections into the primary and secondary items controls
        /// and refreshes the more button visibility. Safe to call before the template applies.
        /// </summary>
        private void WireOwnerCollections()
        {
            _primaryItemsControl?.SetCurrentValue(
                System.Windows.Controls.ItemsControl.ItemsSourceProperty,
                Owner?.PrimaryCommands);
            _secondaryItemsControl?.SetCurrentValue(
                System.Windows.Controls.ItemsControl.ItemsSourceProperty,
                Owner?.SecondaryCommands);
            UpdateMoreButtonVisibility();
        }

        /// <summary>
        /// Shows the more button only while the owner has at least one secondary command,
        /// mirroring the WinUI CommandBarFlyout overflow affordance.
        /// </summary>
        private void UpdateMoreButtonVisibility()
        {
            bool hasSecondaryCommands = Owner?.SecondaryCommands.Count > 0;
            _moreButton?.SetCurrentValue(VisibilityProperty, hasSecondaryCommands ? Visibility.Visible : Visibility.Collapsed);
        }

        private void OnMoreButtonClick(object sender, RoutedEventArgs e)
        {
            SetCurrentValue(IsExpandedProperty, !IsExpanded);
        }

        private void OnFlyoutCommandClick(object sender, RoutedEventArgs e)
        {
            // The more button toggles the overflow instead of invoking a command, so it must
            // not dismiss the flyout. Every other AppBarButton (primary or secondary) hides
            // the flyout after its own Click handlers have run, per WinUI.
            if (e.OriginalSource is AppBarButton command && !ReferenceEquals(command, _moreButton))
            {
                Owner?.Hide();
            }
        }

        private void OnSecondaryCommandsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMoreButtonVisibility();
        }

        private void OnOwnerClosed(object? sender, EventArgs e)
        {
            // WinUI reopens a CommandBarFlyout in its collapsed state unless AlwaysExpanded is
            // set; AlwaysExpanded is omitted for v1, so the overflow always collapses on close.
            SetCurrentValue(IsExpandedProperty, value: false);
        }

        /// <summary>
        /// The primary bar items control (PART_PrimaryItemsControl), or null before the
        /// template applies.
        /// </summary>
        private System.Windows.Controls.ItemsControl? _primaryItemsControl;

        /// <summary>
        /// The overflow toggle button (PART_MoreButton), or null before the template applies.
        /// </summary>
        private ButtonBase? _moreButton;

        /// <summary>
        /// The overflow items control (PART_SecondaryItemsControl), or null before the
        /// template applies.
        /// </summary>
        private System.Windows.Controls.ItemsControl? _secondaryItemsControl;
    }
}
