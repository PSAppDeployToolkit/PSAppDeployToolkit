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

using Fluence.Wpf.Controls;
using System;

namespace Fluence.Wpf.Demo
{
    internal sealed class DemoNavigationShellState
    {
        private readonly NavigationView _navigationView;
        private readonly TitleBar? _titleBar;
        private bool _lastAppliedExtendedTitleBar;

        internal DemoNavigationShellState(NavigationView navigationView, TitleBar? titleBar)
        {
            _navigationView = navigationView ?? throw new ArgumentNullException(nameof(navigationView));
            _titleBar = titleBar;
            UserBackButtonVisible = navigationView.IsBackButtonVisible;
            UserPaneToggleButtonVisible = navigationView.IsPaneToggleButtonVisible;
        }

        internal event EventHandler? Changed;

        internal NavigationViewPaneDisplayMode PaneDisplayMode => _navigationView.PaneDisplayMode;

        internal bool IsPaneOpen => _navigationView.IsPaneOpen;

        private bool IsApplyingChrome { get; set; }

        private bool UserBackButtonVisible { get; set; }

        private bool UserPaneToggleButtonVisible { get; set; }

        internal void SetBackEnabled(bool isBackEnabled)
        {
            if (_navigationView.IsBackEnabled != isBackEnabled)
            {
                _navigationView.IsBackEnabled = isBackEnabled;
            }
        }

        internal void SetPaneDisplayMode(NavigationViewPaneDisplayMode mode)
        {
            if (mode == NavigationViewPaneDisplayMode.Top)
            {
                SetPaneState(NavigationViewPaneDisplayMode.Top, isPaneOpen: true);
                RaiseChanged();
                return;
            }

            if (mode == NavigationViewPaneDisplayMode.Left)
            {
                SetPaneState(NavigationViewPaneDisplayMode.Left, isPaneOpen: true);
            }
            else
            {
                SetPaneState(mode, isPaneOpen: false);
            }

            RaiseChanged();
        }

        internal void ToggleLeftPane()
        {
            if (_navigationView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            {
                return;
            }

            if (_navigationView.PaneDisplayMode == NavigationViewPaneDisplayMode.Left)
            {
                _navigationView.IsPaneOpen = !_navigationView.IsPaneOpen;
                RaiseChanged();
                return;
            }

            SetPaneDisplayMode(NavigationViewPaneDisplayMode.Left);
        }

        internal void CaptureNavigationStateFromControl(bool extendsContentIntoTitleBar)
        {
            if (IsApplyingChrome)
            {
                return;
            }

            if (_navigationView.PaneDisplayMode != NavigationViewPaneDisplayMode.Top)
            {
                if (extendsContentIntoTitleBar)
                {
                    if (_navigationView.IsPaneToggleButtonVisible)
                    {
                        UserPaneToggleButtonVisible = true;
                    }
                }
                else
                {
                    UserPaneToggleButtonVisible = _navigationView.IsPaneToggleButtonVisible;
                }
            }

            RaiseChanged();
        }

        internal void CaptureBackVisibilityFromControl()
        {
            if (!IsApplyingChrome)
            {
                UserBackButtonVisible = _navigationView.IsBackButtonVisible;
            }
        }

        internal void ApplyChrome(bool extendsContentIntoTitleBar, bool shellTitleBarPresent)
        {
            IsApplyingChrome = true;
            try
            {
                if (shellTitleBarPresent)
                {
                    _navigationView.IsBackButtonVisible = false;
                }
                else if (_lastAppliedExtendedTitleBar)
                {
                    _navigationView.IsBackButtonVisible = UserBackButtonVisible;
                }

                if (extendsContentIntoTitleBar)
                {
                    _navigationView.IsPaneToggleButtonVisible = false;
                }
                else if (_lastAppliedExtendedTitleBar)
                {
                    _navigationView.IsPaneToggleButtonVisible = UserPaneToggleButtonVisible;
                }
            }
            finally
            {
                IsApplyingChrome = false;
            }

            if (_titleBar is not null)
            {
                _titleBar.IsBackButtonVisible = UserBackButtonVisible && _navigationView.IsBackEnabled;
                _titleBar.IsPaneToggleButtonVisible =
                    extendsContentIntoTitleBar &&
                    UserPaneToggleButtonVisible &&
                    _navigationView.PaneDisplayMode != NavigationViewPaneDisplayMode.Top;
            }

            _lastAppliedExtendedTitleBar = extendsContentIntoTitleBar;
        }

        private void SetPaneState(NavigationViewPaneDisplayMode mode, bool isPaneOpen)
        {
            if (mode == NavigationViewPaneDisplayMode.Left)
            {
                _navigationView.IsPaneOpen = isPaneOpen;
                _navigationView.PaneDisplayMode = mode;
                return;
            }

            _navigationView.PaneDisplayMode = mode;
            _navigationView.IsPaneOpen = isPaneOpen;
        }

        private void RaiseChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
