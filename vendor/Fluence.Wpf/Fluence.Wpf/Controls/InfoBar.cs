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
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Fluence.Wpf.Automation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// An inline notification bar for displaying status messages with severity levels.
    /// </summary>
    [TemplatePart(Name = PART_BannerPanel, Type = typeof(InfoBarPanel))]
    [TemplatePart(Name = PART_CloseButton, Type = typeof(System.Windows.Controls.Button))]
    public class InfoBar : ContentControl
    {
        // Template part names.
        private const string PART_BannerPanel = "PART_BannerPanel";
        private const string PART_CloseButton = "PART_CloseButton";

        /// <summary>
        /// Initializes static members of the InfoBar class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the InfoBar control uses its custom style by
        /// default. It is called automatically before any static members are accessed or any instances are
        /// created.</remarks>
        static InfoBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(InfoBar),
                new FrameworkPropertyMetadata(typeof(InfoBar)));
            AutomationProperties.LiveSettingProperty.OverrideMetadata(
                typeof(InfoBar),
                new FrameworkPropertyMetadata(AutomationLiveSetting.Polite));
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(OnAnnouncingPropertyChanged));

        /// <summary>
        /// Gets or sets the title text displayed in the info bar.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Message"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(OnAnnouncingPropertyChanged));

        /// <summary>
        /// Gets or sets the message text displayed in the info bar.
        /// </summary>
        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Severity"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SeverityProperty =
            DependencyProperty.Register(
                nameof(Severity),
                typeof(InfoBarSeverity),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(InfoBarSeverity.Informational, OnAnnouncingPropertyChanged));

        /// <summary>
        /// Gets or sets the severity level that determines the visual style of the info bar.
        /// </summary>
        public InfoBarSeverity Severity
        {
            get => (InfoBarSeverity)GetValue(SeverityProperty);
            set => SetValue(SeverityProperty, value);
        }

        /// <summary>
        /// Returns the Segoe Fluent Icons glyph that represents <paramref name="severity"/>. This is the
        /// single programmatic source for the severity glyphs; it mirrors the <c language="xaml">StandardIcon</c>
        /// severity triggers in Themes/Controls/InfoBar.xaml (WPF property triggers cannot call this method, so
        /// keep both in sync). These are the WinUI InfoBar*IconGlyph codes (InfoBar_themeresources.xaml), the
        /// glyph drawn on top of the IconBackground circle, not a standalone icon.
        /// </summary>
        /// <param name="severity">The severity to map.</param>
        /// <returns>A single-character glyph string in the Segoe Fluent Icons font.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="severity"/> is not a defined <see cref="InfoBarSeverity"/> value.</exception>
        public static string GetSeverityGlyph(InfoBarSeverity severity)
        {
            return severity switch
            {
                InfoBarSeverity.Informational => "",
                InfoBarSeverity.Success => "",
                InfoBarSeverity.Warning => "",
                InfoBarSeverity.Error => "",
                _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, message: null),
            };
        }

        /// <summary>
        /// Returns the theme brush resource key (for a <c language="xaml">DynamicResource</c> reference) that colors
        /// <paramref name="severity"/>. Mirrors the <c language="xaml">Severity</c> triggers in Themes/Controls/InfoBar.xaml.
        /// </summary>
        /// <param name="severity">The severity to map.</param>
        /// <returns>A brush key resolvable against the Fluence theme dictionaries.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="severity"/> is not a defined <see cref="InfoBarSeverity"/> value.</exception>
        public static string GetSeverityBrushKey(InfoBarSeverity severity)
        {
            return severity switch
            {
                InfoBarSeverity.Informational => "SystemFillColorAttentionBrush",
                InfoBarSeverity.Success => "SystemFillColorSuccessBrush",
                InfoBarSeverity.Warning => "SystemFillColorCautionBrush",
                InfoBarSeverity.Error => "SystemFillColorCriticalBrush",
                _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, message: null),
            };
        }

        /// <summary>
        /// Identifies the <see cref="IsOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: OnIsOpenChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the info bar is visible. The default is
        /// <see langword="false"/>, as WinUI's is: a bar is declared closed and opened when the
        /// condition it reports actually arises.
        /// </summary>
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsClosable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsClosableProperty =
            DependencyProperty.Register(
                nameof(IsClosable),
                typeof(bool),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets a value indicating whether the close button is displayed.
        /// </summary>
        public bool IsClosable
        {
            get => (bool)GetValue(IsClosableProperty);
            set => SetValue(IsClosableProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsIconVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsIconVisibleProperty =
            DependencyProperty.Register(
                nameof(IsIconVisible),
                typeof(bool),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets a value indicating whether the severity icon is displayed.
        /// </summary>
        public bool IsIconVisible
        {
            get => (bool)GetValue(IsIconVisibleProperty);
            set => SetValue(IsIconVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets a custom icon that overrides the default severity icon.
        /// </summary>
        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ActionButton"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActionButtonProperty =
            DependencyProperty.Register(
                nameof(ActionButton),
                typeof(object),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the content placed in the action button slot.
        /// </summary>
        public object ActionButton
        {
            get => GetValue(ActionButtonProperty);
            set => SetValue(ActionButtonProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius of the info bar.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CloseButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseButtonStyleProperty =
            DependencyProperty.Register(
                nameof(CloseButtonStyle),
                typeof(Style),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(defaultValue: null, OnCloseButtonStyleChanged));

        /// <summary>
        /// Gets or sets the style applied to the close button, WinUI's
        /// <c language="csharp">InfoBar.CloseButtonStyle</c>.
        /// </summary>
        public Style? CloseButtonStyle
        {
            get => (Style?)GetValue(CloseButtonStyleProperty);
            set => SetValue(CloseButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CloseButtonCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseButtonCommandProperty =
            DependencyProperty.Register(
                nameof(CloseButtonCommand),
                typeof(ICommand),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(defaultValue: null));

        /// <summary>
        /// Gets or sets the command invoked when the close button is clicked, WinUI's
        /// <c language="csharp">InfoBar.CloseButtonCommand</c>. The command runs in addition to the
        /// close itself: the bar still closes unless a <see cref="Closing"/> handler cancels it.
        /// </summary>
        public ICommand? CloseButtonCommand
        {
            get => (ICommand?)GetValue(CloseButtonCommandProperty);
            set => SetValue(CloseButtonCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CloseButtonCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseButtonCommandParameterProperty =
            DependencyProperty.Register(
                nameof(CloseButtonCommandParameter),
                typeof(object),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(defaultValue: null));

        /// <summary>
        /// Gets or sets the parameter passed to <see cref="CloseButtonCommand"/>.
        /// </summary>
        public object? CloseButtonCommandParameter
        {
            get => GetValue(CloseButtonCommandParameterProperty);
            set => SetValue(CloseButtonCommandParameterProperty, value);
        }

        /// <summary>
        /// Occurs when the close button is clicked, before the close itself runs, matching WinUI's
        /// <c language="csharp">InfoBar.CloseButtonClick</c>. WinUI passes no arguments of its own
        /// here, so neither does this. Cancel the close from <see cref="Closing"/>, not from here.
        /// </summary>
        public event EventHandler? CloseButtonClick;

        /// <summary>
        /// Occurs when the info bar opens, which is <see cref="IsOpen"/> going from
        /// <see langword="false"/> to <see langword="true"/>. WinUI raises the same event from the
        /// same transition. A <see cref="Closing"/> handler that cancels puts <see cref="IsOpen"/>
        /// back without the bar ever having closed, and that revert is not a fresh open, so it
        /// raises nothing here.
        /// </summary>
        public event EventHandler<InfoBarOpenedEventArgs>? Opened;

        /// <summary>
        /// Occurs before the info bar closes, whichever way the close started: the close button
        /// or <see cref="IsOpen"/> set to <see langword="false"/> in code. Set
        /// <see cref="InfoBarClosingEventArgs.Cancel"/> to <see langword="true"/> to prevent
        /// closing; <see cref="IsOpen"/> goes back to <see langword="true"/> and
        /// <see cref="Closed"/> is not raised.
        /// </summary>
        public event EventHandler<InfoBarClosingEventArgs>? Closing;

        /// <summary>
        /// Occurs after the info bar has closed.
        /// </summary>
        public event EventHandler<InfoBarClosedEventArgs>? Closed;

        /// <summary>
        /// Gets a value indicating whether the banner row stacked its title, message and action
        /// rather than putting them on one line. The panel decides that during measure, so this is
        /// only meaningful after a layout pass.
        /// </summary>
        internal bool IsBannerStacked => _bannerPanel?.IsVertical ?? false;

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new InfoBarAutomationPeer(this);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            _closeButton?.Click -= OnCloseButtonClick;
            base.OnApplyTemplate();
            _closeButton = GetTemplateChild(PART_CloseButton) as System.Windows.Controls.Button;
            _closeButton?.Click += OnCloseButtonClick;
            _bannerPanel = GetTemplateChild(PART_BannerPanel) as InfoBarPanel;
            ApplyCloseButtonStyle();
        }

        /// <summary>
        /// Puts <see cref="CloseButtonStyle"/> on the close button, or restores the template's own
        /// style when the property is cleared. Applied from code rather than from the template so
        /// an unset property leaves the default style in place instead of nulling it.
        /// </summary>
        private void ApplyCloseButtonStyle()
        {
            if (_closeButton is null)
            {
                return;
            }

            Style? style = CloseButtonStyle;
            if (style is null)
            {
                _closeButton.ClearValue(StyleProperty);
                return;
            }

            _closeButton.Style = style;
        }

        private static void OnCloseButtonStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((InfoBar)d).ApplyCloseButtonStyle();
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            InfoBar bar = (InfoBar)d;
            if ((bool)e.NewValue)
            {
                // A cancelled close puts IsOpen back without the bar ever having closed, so it
                // is not a fresh appearance: it neither announces a second time nor reports as an
                // open that never happened.
                if (!bar._revertingCancelledClose)
                {
                    bar.AnnounceLiveRegion();
                    bar.RaiseOpened();
                }

                return;
            }

            bar.RunClosePipeline();
        }

        /// <summary>
        /// Raises <see cref="Opened"/> from the instance itself, so the sender is the bar rather
        /// than whatever the static property callback was handed.
        /// </summary>
        private void RaiseOpened()
        {
            Opened?.Invoke(this, new InfoBarOpenedEventArgs());
        }

        /// <summary>
        /// Runs the whole close pipeline from the <see cref="IsOpen"/> changed callback, the way
        /// WinUI's InfoBar does (InfoBar.cpp OnIsOpenPropertyChanged): raises
        /// <see cref="Closing"/> with the reason the close was started for, puts
        /// <see cref="IsOpen"/> back to <see langword="true"/> when a handler cancels, and
        /// otherwise raises <see cref="Closed"/> exactly once with the same reason. Driving both
        /// events from the single property transition is what makes a handler's own
        /// <c language="text">IsOpen = false</c> a no-op rather than a second close: the property
        /// is already false by the time the handler runs.
        /// </summary>
        private void RunClosePipeline()
        {
            InfoBarCloseReason reason = _closeReason;
            _closeReason = InfoBarCloseReason.Programmatic;

            InfoBarClosingEventArgs closingArgs = new(reason);
            Closing?.Invoke(this, closingArgs);
            if (closingArgs.Cancel)
            {
                _revertingCancelledClose = true;
                try
                {
                    IsOpen = true;
                }
                finally
                {
                    _revertingCancelledClose = false;
                }

                return;
            }

            Closed?.Invoke(this, new InfoBarClosedEventArgs(reason));
        }

        private static void OnAnnouncingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            InfoBar bar = (InfoBar)d;
            if (bar.IsOpen)
            {
                bar.AnnounceLiveRegion();
            }
        }

        /// <summary>
        /// Raises <see cref="AutomationEvents.LiveRegionChanged"/> on this control's automation peer
        /// so Narrator announces the current content without moving focus.
        /// Uses only net472-safe APIs (no RaiseNotificationEvent).
        /// </summary>
        private void AnnounceLiveRegion()
        {
            if (!AutomationPeer.ListenerExists(AutomationEvents.LiveRegionChanged))
            {
                return;
            }

            // CreatePeerForElement is annotated non-null, so peer is provably non-null here (CA1508
            // rejects a redundant null guard); no NullReferenceException is possible.
            AutomationPeer peer = UIElementAutomationPeer.FromElement(this) ?? UIElementAutomationPeer.CreatePeerForElement(this);
            peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            OnCloseButtonClick();
        }

        /// <summary>
        /// Stamps <see cref="InfoBarCloseReason.CloseButton"/> as the reason for the close the
        /// button is about to start, then drives <see cref="IsOpen"/> to
        /// <see langword="false"/>. The <see cref="Closing"/> and <see cref="Closed"/> events are
        /// raised by the property's changed callback, so a cancelled close leaves the bar open
        /// and a completed one raises <see cref="Closed"/> once, with this reason.
        /// </summary>
        protected virtual void OnCloseButtonClick()
        {
            CloseButtonClick?.Invoke(this, EventArgs.Empty);

            ICommand? command = CloseButtonCommand;
            object? parameter = CloseButtonCommandParameter;
            if (command?.CanExecute(parameter) is true)
            {
                command.Execute(parameter);
            }

            _closeReason = InfoBarCloseReason.CloseButton;
            try
            {
                IsOpen = false;
            }
            finally
            {
                // A click on an already-closed bar changes nothing, so the pipeline never runs
                // and never consumes the stamped reason. Clearing it here stops that reason
                // leaking into the next, programmatic close.
                _closeReason = InfoBarCloseReason.Programmatic;
            }
        }

        /// <summary>
        /// Represents a reference to the close button control, or null if the button is not available.
        /// </summary>
        private System.Windows.Controls.Button? _closeButton;
        private InfoBarPanel? _bannerPanel;

        /// <summary>
        /// The reason to report on the next close. Stamped by <see cref="OnCloseButtonClick()"/>
        /// before it drives <see cref="IsOpen"/>, and consumed (and reset) by
        /// <see cref="RunClosePipeline"/>. A close nothing stamped is programmatic.
        /// </summary>
        private InfoBarCloseReason _closeReason = InfoBarCloseReason.Programmatic;

        /// <summary>
        /// True while <see cref="RunClosePipeline"/> is putting <see cref="IsOpen"/> back after a
        /// cancelled close, so the reopen is not announced as a fresh appearance.
        /// </summary>
        private bool _revertingCancelledClose;
    }
}
