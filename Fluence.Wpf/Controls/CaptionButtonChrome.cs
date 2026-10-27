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
    /// Resolves effective caption-button visibility, enabled state, and shared command eligibility.
    /// A null visibility override leaves the <see cref="ResizeMode"/> baseline in effect.
    /// </summary>
    internal static class CaptionButtonChrome
    {
        /// <summary>
        /// Resolves Minimize chrome, including an explicit visibility override and capability flag.
        /// </summary>
        /// <param name="resizeMode">The window resize mode.</param>
        /// <param name="visibilityOverride">An explicit visibility, or null for the baseline.</param>
        /// <param name="isMinimizable">Whether minimizing is allowed.</param>
        /// <param name="visibility">The effective Minimize visibility.</param>
        /// <param name="isEnabled">Whether the Minimize button is enabled.</param>
        internal static void GetMinimizeChrome(
            ResizeMode resizeMode,
            Visibility? visibilityOverride,
            bool isMinimizable,
            out Visibility visibility,
            out bool isEnabled)
        {
            visibility = visibilityOverride ??
                (resizeMode is ResizeMode.NoResize ? Visibility.Collapsed : Visibility.Visible);
            isEnabled = visibility is Visibility.Visible &&
                CanMinimize(resizeMode, visibilityOverride, isMinimizable);
        }

        /// <summary>
        /// Resolves the active Maximize or Restore button for the window state.
        /// </summary>
        /// <param name="resizeMode">The window resize mode.</param>
        /// <param name="windowState">The current window state.</param>
        /// <param name="visibilityOverride">An explicit visibility, or null for the baseline.</param>
        /// <param name="isMaximizable">Whether maximizing and restoring are allowed.</param>
        /// <param name="maximizeVisibility">The effective Maximize visibility.</param>
        /// <param name="restoreVisibility">The effective Restore visibility.</param>
        /// <param name="maximizeEnabled">Whether the Maximize button is enabled.</param>
        /// <param name="restoreEnabled">Whether the Restore button is enabled.</param>
        internal static void GetMaximizeRestoreChrome(
            ResizeMode resizeMode,
            WindowState windowState,
            Visibility? visibilityOverride,
            bool isMaximizable,
            out Visibility maximizeVisibility,
            out Visibility restoreVisibility,
            out bool maximizeEnabled,
            out bool restoreEnabled)
        {
            bool isMaximized = windowState is WindowState.Maximized;
            Visibility activeVisibility;
            if (visibilityOverride is Visibility explicitVisibility)
            {
                // An invalid explicit value, like Collapsed, collapses both members of the pair.
                activeVisibility = explicitVisibility is Visibility.Visible or Visibility.Hidden
                    ? explicitVisibility
                    : Visibility.Collapsed;
            }
            else
            {
                activeVisibility = resizeMode is ResizeMode.NoResize
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }

            maximizeVisibility = isMaximized ? Visibility.Collapsed : activeVisibility;
            restoreVisibility = isMaximized ? activeVisibility : Visibility.Collapsed;

            bool canMaximize = CanMaximize(resizeMode, visibilityOverride, isMaximizable);
            maximizeEnabled = maximizeVisibility is Visibility.Visible && canMaximize;
            restoreEnabled = restoreVisibility is Visibility.Visible && canMaximize;
        }

        /// <summary>
        /// Resolves Close chrome from its visibility override and capability flag.
        /// </summary>
        /// <param name="visibilityOverride">An explicit visibility, or null for the baseline.</param>
        /// <param name="isClosable">Whether closing is allowed.</param>
        /// <param name="visibility">The effective Close visibility.</param>
        /// <param name="isEnabled">Whether the Close button is enabled.</param>
        internal static void GetCloseChrome(
            Visibility? visibilityOverride,
            bool isClosable,
            out Visibility visibility,
            out bool isEnabled)
        {
            visibility = visibilityOverride ?? Visibility.Visible;
            isEnabled = visibility is Visibility.Visible && isClosable;
        }

        /// <summary>
        /// Returns whether the Minimize command is eligible under the current window policy.
        /// </summary>
        /// <param name="resizeMode">The window resize mode.</param>
        /// <param name="visibilityOverride">An explicit visibility, or null for the baseline.</param>
        /// <param name="isMinimizable">Whether minimizing is allowed.</param>
        internal static bool CanMinimize(
            ResizeMode resizeMode,
            Visibility? visibilityOverride,
            bool isMinimizable)
        {
            return (resizeMode is not ResizeMode.NoResize || visibilityOverride is Visibility.Visible) &&
                isMinimizable;
        }

        /// <summary>
        /// Returns whether the Maximize and Restore commands are eligible under the current window policy.
        /// </summary>
        /// <param name="resizeMode">The window resize mode.</param>
        /// <param name="visibilityOverride">An explicit visibility, or null for the baseline.</param>
        /// <param name="isMaximizable">Whether maximizing and restoring are allowed.</param>
        internal static bool CanMaximize(
            ResizeMode resizeMode,
            Visibility? visibilityOverride,
            bool isMaximizable)
        {
            return (resizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip ||
                visibilityOverride is Visibility.Visible) && isMaximizable;
        }
    }
}
