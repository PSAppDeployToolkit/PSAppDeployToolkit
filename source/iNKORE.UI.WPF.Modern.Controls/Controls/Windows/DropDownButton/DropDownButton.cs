// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Automation.Peers;
using iNKORE.UI.WPF.Modern.Common;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using iNKORE.UI.WPF.Modern.Controls.Primitives;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public class DropDownButton : Button
    {
        static DropDownButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownButton), new FrameworkPropertyMetadata(typeof(DropDownButton)));
        }

        public DropDownButton()
        {
        }

        #region CornerRadius

        public static readonly DependencyProperty CornerRadiusProperty =
            ControlHelper.CornerRadiusProperty.AddOwner(typeof(DropDownButton));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        #region ShowChevronArrow

        public static readonly DependencyProperty ShowChevronArrowProperty =
            DependencyProperty.Register(nameof(ShowChevronArrow), typeof(bool), typeof(DropDownButton), new PropertyMetadata(true));

        public bool ShowChevronArrow
        {
            get => (bool)GetValue(ShowChevronArrowProperty);
            set => SetValue(ShowChevronArrowProperty, value);
        }

        #endregion


        #region UseSystemFocusVisuals

        public static readonly DependencyProperty UseSystemFocusVisualsProperty =
            FocusVisualHelper.UseSystemFocusVisualsProperty.AddOwner(typeof(DropDownButton));

        public bool UseSystemFocusVisuals
        {
            get => (bool)GetValue(UseSystemFocusVisualsProperty);
            set => SetValue(UseSystemFocusVisualsProperty, value);
        }

        #endregion

        #region FocusVisualMargin

        public static readonly DependencyProperty FocusVisualMarginProperty =
            FocusVisualHelper.FocusVisualMarginProperty.AddOwner(typeof(DropDownButton));

        public Thickness FocusVisualMargin
        {
            get => (Thickness)GetValue(FocusVisualMarginProperty);
            set => SetValue(FocusVisualMarginProperty, value);
        }

        #endregion

        #region Flyout

        public static readonly DependencyProperty FlyoutProperty =
            FlyoutService.FlyoutProperty.AddOwner(
                typeof(DropDownButton),
                new FrameworkPropertyMetadata(OnFlyoutPropertyChanged));

        public FlyoutBase Flyout
        {
            get => (FlyoutBase)GetValue(FlyoutProperty);
            set => SetValue(FlyoutProperty, value);
        }

        private static void OnFlyoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DropDownButton)d).OnFlyoutPropertyChanged(e);
        }

        private void OnFlyoutPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is FlyoutBase oldFlyout)
            {
                oldFlyout.Opened -= OnFlyoutOpened;
                oldFlyout.Closed -= OnFlyoutClosed;
            }

            if (e.NewValue is FlyoutBase newFlyout)
            {
                newFlyout.Opened += OnFlyoutOpened;
                newFlyout.Closed += OnFlyoutClosed;
            }
        }

        #endregion

        #region FlyoutOpeningMode

        public static readonly DependencyProperty FlyoutOpeningModeProperty =
            FlyoutService.FlyoutOpeningModeProperty.AddOwner(
                typeof(DropDownButton));

        public FlyoutOpeningMode FlyoutOpeningMode
        {
            get => (FlyoutOpeningMode)GetValue(FlyoutOpeningModeProperty);
            set => SetValue(FlyoutOpeningModeProperty, value);
        }

        #endregion


        internal bool IsFlyoutOpen => m_isFlyoutOpen;

        internal void OpenFlyout()
        {
            Flyout?.ShowAt(this);
        }

        internal void CloseFlyout()
        {
            Flyout?.Hide();
        }

        private void OnFlyoutOpened(object sender, object e)
        {
            m_isFlyoutOpen = true;
            SharedHelpers.RaiseAutomationPropertyChangedEvent(this, ExpandCollapseState.Collapsed, ExpandCollapseState.Expanded);
        }

        private void OnFlyoutClosed(object sender, object e)
        {
            m_isFlyoutOpen = false;
            SharedHelpers.RaiseAutomationPropertyChangedEvent(this, ExpandCollapseState.Expanded, ExpandCollapseState.Collapsed);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new DropDownButtonAutomationPeer(this);
        }

        private bool m_isFlyoutOpen;
    }
}
