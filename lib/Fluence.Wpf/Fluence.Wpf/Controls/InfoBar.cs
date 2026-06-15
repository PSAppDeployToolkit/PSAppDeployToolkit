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

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// An inline notification bar for displaying status messages with severity levels.
    /// </summary>
    [TemplatePart(Name = PART_CloseButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplateVisualState(GroupName = "SeverityLevels", Name = "Informational")]
    [TemplateVisualState(GroupName = "SeverityLevels", Name = "Success")]
    [TemplateVisualState(GroupName = "SeverityLevels", Name = "Warning")]
    [TemplateVisualState(GroupName = "SeverityLevels", Name = "Error")]
    public class InfoBar : ContentControl
    {
        // Template part names.
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
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

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
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

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
                new FrameworkPropertyMetadata(InfoBarSeverity.Informational, OnSeverityChanged));

        /// <summary>
        /// Gets or sets the severity level that determines the visual style of the info bar.
        /// </summary>
        public InfoBarSeverity Severity
        {
            get => (InfoBarSeverity)GetValue(SeverityProperty);
            set => SetValue(SeverityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets a value indicating whether the info bar is visible.
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
                new FrameworkPropertyMetadata(new CornerRadius(8)));

        /// <summary>
        /// Gets or sets the corner radius of the info bar.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Occurs before the info bar closes. Set <see cref="InfoBarClosingEventArgs.Cancel"/>
        /// to <see langword="true"/> to prevent closing.
        /// </summary>
        public event EventHandler<InfoBarClosingEventArgs>? Closing;

        /// <summary>
        /// Occurs after the info bar has closed.
        /// </summary>
        public event EventHandler? Closed;

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
            UpdateSeverityState(useTransitions: false);
        }

        private static void OnSeverityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((InfoBar)d).UpdateSeverityState(useTransitions: true);
        }

        /// <summary>
        /// Transitions the control to the visual state matching the current <see cref="Severity"/>.
        /// Called without transitions on initial template application; with transitions on runtime changes.
        /// </summary>
        /// <param name="useTransitions">Indicates whether to use visual transitions.</param>
        private void UpdateSeverityState(bool useTransitions)
        {
            _ = VisualStateManager.GoToState(this, Severity switch
            {
                InfoBarSeverity.Success => "Success",
                InfoBarSeverity.Warning => "Warning",
                InfoBarSeverity.Error => "Error",
                InfoBarSeverity.Informational or _ => "Informational",
            }, useTransitions);
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            OnCloseButtonClick();
        }

        /// <summary>
        /// Raises the <see cref="Closing"/> event. If not canceled, sets <see cref="IsOpen"/>
        /// to <see langword="false"/> and raises the <see cref="Closed"/> event.
        /// </summary>
        protected virtual void OnCloseButtonClick()
        {
            InfoBarClosingEventArgs args = new();
            Closing?.Invoke(this, args);
            if (!args.Cancel)
            {
                IsOpen = false;
                Closed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Represents a reference to the close button control, or null if the button is not available.
        /// </summary>
        private System.Windows.Controls.Button? _closeButton;
    }
}
