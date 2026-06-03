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
    /// Pure-logic helper that computes the baseline visibility and enabled state for each
    /// caption button as a function of <see cref="ResizeMode"/> and <see cref="WindowState"/>.
    /// These baselines are then blended with the explicit per-DP overrides
    /// (<see cref="FluenceWindow.IsMinimizeButtonVisible"/> etc.) in
    /// <c>FluenceWindow.UpdateCaptionButtons</c>.
    /// </summary>
    internal static class CaptionButtonChrome
    {
        /// <summary>
        /// Computes the baseline visibility and enabled state for the Minimize button.
        /// </summary>
        /// <remarks>
        /// The Minimize button is collapsed (and disabled) when <paramref name="resizeMode"/> is
        /// <see cref="ResizeMode.NoResize"/>, because minimizing is not meaningful for a window
        /// that cannot be interactively resized to its pre-minimize size. For all other resize
        /// modes the button is visible and enabled.
        /// </remarks>
        /// <param name="resizeMode">The window's current <see cref="ResizeMode"/>.</param>
        /// <param name="visibility">
        ///   On return, the baseline <see cref="Visibility"/> for the Minimize button.
        /// </param>
        /// <param name="isEnabled">
        ///   On return, <see langword="true"/> when the Minimize button should be interactive.
        /// </param>
        internal static void GetMinimizeChrome(
            ResizeMode resizeMode,
            out Visibility visibility,
            out bool isEnabled)
        {
            isEnabled = resizeMode != ResizeMode.NoResize;
            visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Computes the baseline visibility and enabled state for the Close button.
        /// </summary>
        /// <remarks>
        /// The Close button is always visible and enabled at the baseline level. Per-window
        /// overrides (<see cref="FluenceWindow.IsCloseButtonVisible"/> and
        /// <see cref="FluenceWindow.IsClosable"/>) can suppress it after this baseline is applied.
        /// </remarks>
        /// <param name="visibility">
        ///   On return, always <see cref="Visibility.Visible"/>.
        /// </param>
        /// <param name="isEnabled">
        ///   On return, always <see langword="true"/>.
        /// </param>
        internal static void GetCloseChrome(out Visibility visibility, out bool isEnabled)
        {
            visibility = Visibility.Visible;
            isEnabled = true;
        }

        /// <summary>
        /// Computes the baseline visibility and enabled state for the Maximize and Restore buttons.
        /// </summary>
        /// <remarks>
        /// Only one of Maximize and Restore is visible at any time (they occupy the same slot).
        /// Both are collapsed when <paramref name="resizeMode"/> is
        /// <see cref="ResizeMode.NoResize"/>. When <paramref name="windowState"/> is
        /// <see cref="WindowState.Maximized"/> the Restore button is shown and the Maximize button
        /// is hidden; otherwise the Maximize button is shown. The enabled state follows the same
        /// gating: <see cref="ResizeMode.NoResize"/> and <see cref="ResizeMode.CanMinimize"/> both
        /// disable both buttons.
        /// </remarks>
        /// <param name="resizeMode">The window's current <see cref="ResizeMode"/>.</param>
        /// <param name="windowState">The window's current <see cref="WindowState"/>.</param>
        /// <param name="maximizeVisibility">
        ///   On return, the baseline <see cref="Visibility"/> for the Maximize button.
        /// </param>
        /// <param name="restoreVisibility">
        ///   On return, the baseline <see cref="Visibility"/> for the Restore button.
        /// </param>
        /// <param name="maximizeEnabled">
        ///   On return, <see langword="true"/> when the Maximize button should be interactive.
        /// </param>
        /// <param name="restoreEnabled">
        ///   On return, <see langword="true"/> when the Restore button should be interactive.
        /// </param>
        internal static void GetMaximizeRestoreChrome(
            ResizeMode resizeMode,
            WindowState windowState,
            out Visibility maximizeVisibility,
            out Visibility restoreVisibility,
            out bool maximizeEnabled,
            out bool restoreEnabled)
        {
            GetMaximizeRestoreLayout(resizeMode, windowState, out maximizeVisibility, out restoreVisibility);

            if (resizeMode is not ResizeMode.NoResize and not ResizeMode.CanMinimize)
            {
                maximizeEnabled = maximizeVisibility == Visibility.Visible;
                restoreEnabled = restoreVisibility == Visibility.Visible;
            }
            else
            {
                maximizeEnabled = false;
                restoreEnabled = false;
            }
        }

        /// <summary>
        /// Computes the Maximize/Restore button visibility layout without the enabled state.
        /// </summary>
        /// <param name="resizeMode">The window's current <see cref="ResizeMode"/>.</param>
        /// <param name="windowState">The window's current <see cref="WindowState"/>.</param>
        /// <param name="maximizeVisibility">
        ///   On return, the <see cref="Visibility"/> for the Maximize button.
        /// </param>
        /// <param name="restoreVisibility">
        ///   On return, the <see cref="Visibility"/> for the Restore button.
        /// </param>
        private static void GetMaximizeRestoreLayout(
            ResizeMode resizeMode,
            WindowState windowState,
            out Visibility maximizeVisibility,
            out Visibility restoreVisibility)
        {
            if (resizeMode == ResizeMode.NoResize)
            {
                maximizeVisibility = Visibility.Collapsed;
                restoreVisibility = Visibility.Collapsed;
                return;
            }

            if (windowState == WindowState.Maximized)
            {
                maximizeVisibility = Visibility.Collapsed;
                restoreVisibility = Visibility.Visible;
            }
            else
            {
                maximizeVisibility = Visibility.Visible;
                restoreVisibility = Visibility.Collapsed;
            }
        }
    }
}
