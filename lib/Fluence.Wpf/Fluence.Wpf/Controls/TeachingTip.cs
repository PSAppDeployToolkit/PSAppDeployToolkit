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

using Fluence.Wpf.Automation;
using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A contextual tip surface with a title, subtitle, body content, and optional action and
    /// close buttons, mirroring the WinUI 3 <c>TeachingTip</c> control. The tip is hosted in an
    /// internal light-weight <see cref="Popup"/>: when <see cref="Target"/> is set the popup is
    /// anchored to it, centered on the target edge selected by <see cref="PreferredPlacement"/>;
    /// untargeted tips dock to the bottom-right corner of the active window content (or center
    /// when <see cref="PreferredPlacement"/> is explicitly Center) and hide the beak. The body
    /// uses the inherited <see cref="ContentControl.Content"/> and
    /// <see cref="ContentControl.ContentTemplate"/>.
    /// </summary>
    [TemplatePart(Name = PART_ActionButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PART_CloseButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PART_AlternateCloseButton, Type = typeof(ButtonBase))]
    public class TeachingTip : ContentControl
    {
        // Template part names.
        private const string PART_ActionButton = "PART_ActionButton";
        private const string PART_CloseButton = "PART_CloseButton";
        private const string PART_AlternateCloseButton = "PART_AlternateCloseButton";

        /// <summary>
        /// Initializes static members of the TeachingTip class and overrides the default
        /// style metadata so the control picks up its Fluent template from Generic.xaml.
        /// </summary>
        static TeachingTip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TeachingTip),
                new FrameworkPropertyMetadata(typeof(TeachingTip)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TeachingTip"/> class. The tip starts
        /// collapsed so a tip declared in page XAML renders nothing inline; it becomes visible
        /// once it is re-hosted in its popup the first time it opens. SetCurrentValue keeps an
        /// explicit consumer-set <see cref="UIElement.Visibility"/> authoritative.
        /// </summary>
        public TeachingTip()
        {
            SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(TeachingTip),
                new FrameworkPropertyMetadata(string.Empty, null, CoerceText));

        /// <summary>
        /// Gets or sets the title shown at the top of the tip. The title is hidden while the
        /// value is empty.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Subtitle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(
                nameof(Subtitle),
                typeof(string),
                typeof(TeachingTip),
                new FrameworkPropertyMetadata(string.Empty, null, CoerceText));

        /// <summary>
        /// Gets or sets the subtitle shown beneath <see cref="Title"/>. The subtitle is hidden
        /// while the value is empty.
        /// </summary>
        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Target"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(
                nameof(Target),
                typeof(FrameworkElement),
                typeof(TeachingTip),
                new FrameworkPropertyMetadata(null, OnPlacementInputChanged));

        /// <summary>
        /// Gets or sets the element the tip is anchored to. When <see langword="null"/> the tip
        /// centers over the active window content and the beak is hidden.
        /// </summary>
        public FrameworkElement? Target
        {
            get => (FrameworkElement?)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(TeachingTip),
                new FrameworkPropertyMetadata(false, OnIsOpenChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the tip is open. Setting
        /// <see langword="true"/> shows the tip in its host popup; setting
        /// <see langword="false"/> (or a light dismiss) closes it and raises
        /// <see cref="Closed"/>.
        /// </summary>
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PreferredPlacement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PreferredPlacementProperty =
            DependencyProperty.Register(
                nameof(PreferredPlacement),
                typeof(TeachingTipPlacementMode),
                typeof(TeachingTip),
                new FrameworkPropertyMetadata(TeachingTipPlacementMode.Auto, OnPlacementInputChanged));

        /// <summary>
        /// Gets or sets where the tip opens relative to <see cref="Target"/>. When a target is
        /// set, <see cref="TeachingTipPlacementMode.Auto"/> currently resolves to
        /// <see cref="TeachingTipPlacementMode.Bottom"/>. When <see cref="Target"/> is
        /// <see langword="null"/>, an untargeted <see cref="TeachingTipPlacementMode.Auto"/> tip
        /// docks to the bottom-right corner of the window content (the WinUI default position);
        /// any other explicit value centers the tip over the window content.
        /// </summary>
        public TeachingTipPlacementMode PreferredPlacement
        {
            get => (TeachingTipPlacementMode)GetValue(PreferredPlacementProperty);
            set => SetValue(PreferredPlacementProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsLightDismissEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsLightDismissEnabledProperty =
            DependencyProperty.Register(
                nameof(IsLightDismissEnabled),
                typeof(bool),
                typeof(TeachingTip),
                new FrameworkPropertyMetadata(false, OnIsLightDismissEnabledChanged));

        /// <summary>
        /// Gets or sets a value indicating whether clicking outside the tip dismisses it.
        /// Maps to the inverse of <see cref="Popup.StaysOpen"/> on the host popup.
        /// </summary>
        public bool IsLightDismissEnabled
        {
            get => (bool)GetValue(IsLightDismissEnabledProperty);
            set => SetValue(IsLightDismissEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ActionButtonContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActionButtonContentProperty =
            DependencyProperty.Register(
                nameof(ActionButtonContent),
                typeof(object),
                typeof(TeachingTip),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the content of the accent action button in the tip footer. The button
        /// is collapsed while the value is <see langword="null"/>.
        /// </summary>
        public object? ActionButtonContent
        {
            get => GetValue(ActionButtonContentProperty);
            set => SetValue(ActionButtonContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ActionButtonCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActionButtonCommandProperty =
            DependencyProperty.Register(
                nameof(ActionButtonCommand),
                typeof(ICommand),
                typeof(TeachingTip),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command executed when the action button is invoked, after the
        /// <see cref="ActionButtonClick"/> event has been raised.
        /// </summary>
        public ICommand? ActionButtonCommand
        {
            get => (ICommand?)GetValue(ActionButtonCommandProperty);
            set => SetValue(ActionButtonCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ActionButtonCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActionButtonCommandParameterProperty =
            DependencyProperty.Register(
                nameof(ActionButtonCommandParameter),
                typeof(object),
                typeof(TeachingTip),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the parameter passed to <see cref="ActionButtonCommand"/>.
        /// </summary>
        public object? ActionButtonCommandParameter
        {
            get => GetValue(ActionButtonCommandParameterProperty);
            set => SetValue(ActionButtonCommandParameterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CloseButtonContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseButtonContentProperty =
            DependencyProperty.Register(
                nameof(CloseButtonContent),
                typeof(object),
                typeof(TeachingTip),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the content of the close button in the tip footer, matching the WinUI
        /// close-affordance rules: the footer close button shows only while this value is set.
        /// While it is <see langword="null"/> and <see cref="IsLightDismissEnabled"/> is
        /// <see langword="false"/>, an alternate X close button is shown in the top-right
        /// corner of the tip instead; while it is <see langword="null"/> and light dismiss is
        /// enabled, the tip shows no close affordance at all.
        /// </summary>
        public object? CloseButtonContent
        {
            get => GetValue(CloseButtonContentProperty);
            set => SetValue(CloseButtonContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ActualPlacement"/> dependency property.
        /// </summary>
        private static readonly DependencyPropertyKey ActualPlacementPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualPlacement),
                typeof(TeachingTipPlacementMode),
                typeof(TeachingTip),
                new FrameworkPropertyMetadata(TeachingTipPlacementMode.Center));

        /// <summary>
        /// Identifies the <see cref="ActualPlacement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActualPlacementProperty =
            ActualPlacementPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the placement the tip resolved when it was last opened:
        /// <see cref="PreferredPlacement"/> with <see cref="TeachingTipPlacementMode.Auto"/>
        /// resolved to a concrete edge, or <see cref="TeachingTipPlacementMode.Center"/> for
        /// untargeted tips (which dock to the bottom-right corner of the window content but
        /// never show a beak). The template positions the beak on the edge facing the target
        /// and hides it for <see cref="TeachingTipPlacementMode.Center"/>.
        /// </summary>
        public TeachingTipPlacementMode ActualPlacement
        {
            get => (TeachingTipPlacementMode)GetValue(ActualPlacementProperty);
            private set => SetValue(ActualPlacementPropertyKey, value);
        }

        /// <summary>
        /// Occurs when the action button is invoked, before <see cref="ActionButtonCommand"/>
        /// executes. Invoking the action button does not close the tip.
        /// </summary>
        public event EventHandler? ActionButtonClick;

        /// <summary>
        /// Occurs when the close button is invoked, before the tip closes.
        /// </summary>
        public event EventHandler? CloseButtonClick;

        /// <summary>
        /// Occurs after the tip has closed, whether through <see cref="IsOpen"/>, the close
        /// button, or a light dismiss.
        /// </summary>
        public event EventHandler? Closed;

        /// <summary>
        /// Gets the popup that hosts the tip. Created lazily the first time the tip opens.
        /// Internal so tests can verify popup configuration.
        /// </summary>
        internal Popup? HostPopup { get; private set; }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TeachingTipAutomationPeer(this);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            _actionButton?.Click -= OnActionButtonClick;
            _closeButton?.Click -= OnCloseButtonClick;
            _alternateCloseButton?.Click -= OnCloseButtonClick;
            base.OnApplyTemplate();
            _actionButton = GetTemplateChild(PART_ActionButton) as ButtonBase;
            _closeButton = GetTemplateChild(PART_CloseButton) as ButtonBase;
            _alternateCloseButton = GetTemplateChild(PART_AlternateCloseButton) as ButtonBase;
            _actionButton?.Click += OnActionButtonClick;
            _closeButton?.Click += OnCloseButtonClick;
            _alternateCloseButton?.Click += OnCloseButtonClick;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Escape pressed inside the open tip dismisses it, mirroring the WinUI keyboard
        /// contract. The close runs through the <see cref="IsOpen"/> pipeline so
        /// <see cref="Closed"/> is raised as usual.
        /// </remarks>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (!e.Handled && e.Key == Key.Escape && IsOpen)
            {
                SetCurrentValue(IsOpenProperty, false);
                e.Handled = true;
            }
        }

        private static object CoerceText(DependencyObject d, object? baseValue)
        {
            return baseValue ?? string.Empty;
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TeachingTip tip = (TeachingTip)d;
            if ((bool)e.NewValue)
            {
                tip.OpenPopup();
            }
            else
            {
                tip.ClosePopup();
            }
        }

        private static void OnPlacementInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TeachingTip tip = (TeachingTip)d;
            if (tip.HostPopup is not null && tip.HostPopup.IsOpen)
            {
                tip.ApplyPlacement(tip.HostPopup);
            }
        }

        private static void OnIsLightDismissEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TeachingTip tip = (TeachingTip)d;
            _ = tip.HostPopup?.StaysOpen = !(bool)e.NewValue;
        }

        /// <summary>
        /// Maps the WinUI-style <see cref="TeachingTipPlacementMode"/> to the WPF popup side
        /// the tip opens on, feeding the shared edge-centering placement callback.
        /// <see cref="TeachingTipPlacementMode.Auto"/> maps to the bottom side;
        /// <see cref="TeachingTipPlacementMode.Center"/> never reaches this mapping because
        /// centered tips use the native <see cref="PlacementMode.Center"/>.
        /// </summary>
        /// <param name="placement">The requested tip placement.</param>
        /// <returns>The equivalent popup side.</returns>
        internal static PlacementMode MapPlacementSide(TeachingTipPlacementMode placement)
        {
            return placement switch
            {
                TeachingTipPlacementMode.Top => PlacementMode.Top,
                TeachingTipPlacementMode.Left => PlacementMode.Left,
                TeachingTipPlacementMode.Right => PlacementMode.Right,
                TeachingTipPlacementMode.Bottom or TeachingTipPlacementMode.Auto or TeachingTipPlacementMode.Center or _ =>
                    PlacementMode.Bottom,
            };
        }

        /// <summary>
        /// Resolves <see cref="TeachingTipPlacementMode.Auto"/> to the concrete edge used for
        /// the popup placement and the beak position.
        /// </summary>
        private static TeachingTipPlacementMode ResolvePlacement(TeachingTipPlacementMode preferred)
        {
            return preferred == TeachingTipPlacementMode.Auto
                ? TeachingTipPlacementMode.Bottom
                : preferred;
        }

        /// <summary>
        /// Resolves the placement target for an untargeted tip: the content of the active
        /// window first, then the active window itself, then the application main window.
        /// </summary>
        private static UIElement? ResolveFallbackPlacementTarget()
        {
            Application? application = Application.Current;
            if (application is null)
            {
                return null;
            }

            Window? active = null;
            foreach (Window window in application.Windows)
            {
                if (window.IsActive)
                {
                    active = window;
                    break;
                }
            }

            active ??= application.MainWindow;
            return active?.Content as UIElement ?? active;
        }

        /// <summary>
        /// Opens the host popup with the current placement and light-dismiss configuration.
        /// </summary>
        private void OpenPopup()
        {
            Popup popup = EnsurePopup();
            ApplyPlacement(popup);
            popup.StaysOpen = !IsLightDismissEnabled;
            if (!popup.IsOpen)
            {
                popup.IsOpen = true;
            }
        }

        /// <summary>
        /// Closes the host popup; <see cref="Closed"/> is raised from the popup's Closed event.
        /// </summary>
        private void ClosePopup()
        {
            if (HostPopup is not null && HostPopup.IsOpen)
            {
                HostPopup.IsOpen = false;
            }
        }

        /// <summary>
        /// Anchors the popup to <see cref="Target"/>, centered on the target edge selected by
        /// <see cref="PreferredPlacement"/> (via the shared edge-centering placement callback);
        /// explicit Center placement keeps the native centered popup. Untargeted tips dock to
        /// the bottom-right corner of the active window content per WinUI, or center when
        /// <see cref="PreferredPlacement"/> is explicitly Center, and record
        /// <see cref="TeachingTipPlacementMode.Center"/> so the beak stays hidden. The resolved
        /// placement is recorded in <see cref="ActualPlacement"/>.
        /// </summary>
        private void ApplyPlacement(Popup popup)
        {
            FrameworkElement? target = Target;
            if (target is not null)
            {
                popup.PlacementTarget = target;
                TeachingTipPlacementMode resolved = ResolvePlacement(PreferredPlacement);
                if (resolved == TeachingTipPlacementMode.Center)
                {
                    popup.CustomPopupPlacementCallback = null;
                    popup.Placement = PlacementMode.Center;
                }
                else
                {
                    popup.CustomPopupPlacementCallback = GetEdgePlacements;
                    popup.Placement = PlacementMode.Custom;
                }

                ActualPlacement = resolved;
            }
            else
            {
                popup.PlacementTarget = ResolveFallbackPlacementTarget();
                if (PreferredPlacement == TeachingTipPlacementMode.Auto)
                {
                    popup.CustomPopupPlacementCallback = GetBottomRightPlacements;
                    popup.Placement = PlacementMode.Custom;
                }
                else
                {
                    popup.CustomPopupPlacementCallback = null;
                    popup.Placement = PlacementMode.Center;
                }

                ActualPlacement = TeachingTipPlacementMode.Center;
            }
        }

        /// <summary>
        /// The popup's custom placement callback for targeted tips: centers the tip on the
        /// target edge selected by <see cref="PreferredPlacement"/>, sharing the
        /// <see cref="FlyoutBase.GetEdgeCenteredPlacements"/> math with the flyout family.
        /// </summary>
        private CustomPopupPlacement[] GetEdgePlacements(Size popupSize, Size targetSize, Point offset)
        {
            return FlyoutBase.GetEdgeCenteredPlacements(
                MapPlacementSide(ResolvePlacement(PreferredPlacement)),
                popupSize,
                targetSize,
                offset);
        }

        /// <summary>
        /// The popup's custom placement callback for untargeted tips: docks the tip to the
        /// bottom-right corner of the fallback placement target (the active window content),
        /// matching the WinUI default position for untargeted teaching tips.
        /// </summary>
        private static CustomPopupPlacement[] GetBottomRightPlacements(Size popupSize, Size targetSize, Point offset)
        {
            Point bottomRight = new(
                targetSize.Width - popupSize.Width + offset.X,
                targetSize.Height - popupSize.Height + offset.Y);
            return [new CustomPopupPlacement(bottomRight, PopupPrimaryAxis.Horizontal)];
        }

        /// <summary>
        /// Creates the host popup on first use and re-hosts the tip as the popup child,
        /// mirroring how <see cref="FlyoutBase"/> hosts its presenter. A tip declared inside a
        /// panel is detached from that panel before it becomes the popup child.
        /// </summary>
        /// <returns>The popup that hosts the tip.</returns>
        private Popup EnsurePopup()
        {
            if (HostPopup is null)
            {
                HostPopup = new Popup
                {
                    AllowsTransparency = true,
                    // The TeachingTip template owns the open reveal (a slide + fade storyboard
                    // on Loaded), so the popup must not add its own fade on top.
                    PopupAnimation = PopupAnimation.None,
                };
                HostPopup.Closed += OnPopupClosed;
            }

            if (!ReferenceEquals(HostPopup.Child, this))
            {
                DetachFromParent(this);
                HostPopup.Child = this;
                SetCurrentValue(VisibilityProperty, Visibility.Visible);
            }

            return HostPopup;
        }

        /// <summary>
        /// Detaches <paramref name="element"/> from its current parent so it can become the
        /// popup child. The logical parent is preferred because it owns the content slot (a
        /// ContentControl's content reports a ContentPresenter as its visual parent); the
        /// visual parent is the fallback for template-generated hosts. Unsupported parents
        /// fail fast instead of letting the popup throw WPF's generic re-parenting error.
        /// </summary>
        /// <param name="element">The element to detach.</param>
        /// <exception cref="InvalidOperationException">
        /// The parent is not a <see cref="System.Windows.Controls.Panel"/>, a
        /// <see cref="System.Windows.Controls.Decorator"/> (which includes Border), or a
        /// <see cref="ContentControl"/>.
        /// </exception>
        private static void DetachFromParent(FrameworkElement element)
        {
            DependencyObject? parent = element.Parent ?? VisualTreeHelper.GetParent(element);
            if (parent is null)
            {
                return;
            }

            if (parent is System.Windows.Controls.Panel panel)
            {
                panel.Children.Remove(element);
            }
            else if (parent is System.Windows.Controls.Decorator decorator)
            {
                decorator.Child = null;
            }
            else if (parent is ContentControl contentControl)
            {
                contentControl.Content = null;
            }
            else
            {
                throw new InvalidOperationException(
                    "TeachingTip could not detach itself from its parent (" + parent.GetType().FullName + ") to move into its popup. " +
                    "Declare the tip inside a Panel, Border, Decorator, or ContentControl, or remove it from its parent before opening it.");
            }
        }

        private void OnPopupClosed(object? sender, EventArgs e)
        {
            if (IsOpen)
            {
                // The popup closed outside the IsOpen pipeline (light dismiss); sync the
                // property without clobbering a potential binding. The re-entrant changed
                // callback finds the popup already closed and no-ops.
                SetCurrentValue(IsOpenProperty, false);
            }

            // Release the anchor so a closed tip does not pin the last placement target
            // (or the fallback window content) for its own lifetime.
            _ = HostPopup?.PlacementTarget = null;

            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void OnActionButtonClick(object sender, RoutedEventArgs e)
        {
            ActionButtonClick?.Invoke(this, EventArgs.Empty);
            ICommand? command = ActionButtonCommand;
            if (command is not null && command.CanExecute(ActionButtonCommandParameter))
            {
                command.Execute(ActionButtonCommandParameter);
            }
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            CloseButtonClick?.Invoke(this, EventArgs.Empty);
            SetCurrentValue(IsOpenProperty, false);
        }

        /// <summary>
        /// The action button wired from the template, when present.
        /// </summary>
        private ButtonBase? _actionButton;

        /// <summary>
        /// The close button wired from the template, when present.
        /// </summary>
        private ButtonBase? _closeButton;

        /// <summary>
        /// The alternate top-right X close button wired from the template, when present. It
        /// shares the <see cref="OnCloseButtonClick"/> pipeline with the footer close button.
        /// </summary>
        private ButtonBase? _alternateCloseButton;
    }
}
