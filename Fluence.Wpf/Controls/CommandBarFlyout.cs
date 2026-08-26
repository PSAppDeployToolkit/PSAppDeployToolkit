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

using System.Collections.ObjectModel;
using System.Windows;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Represents a flyout that shows a horizontal bar of primary commands with an optional
    /// expandable overflow menu of secondary commands, mirroring the WinUI 3
    /// <c>CommandBarFlyout</c> control.
    /// </summary>
    /// <remarks>
    /// Commands are usually <see cref="AppBarButton"/> instances. Clicking a command inside the
    /// flyout runs its normal <c>Click</c> handling and then dismisses the flyout, matching the
    /// WinUI behavior. The flyout participates in the full <see cref="FlyoutBase"/> contract:
    /// it can be opened via <see cref="FlyoutBase.ShowAt"/> or attached to an element with
    /// <see cref="FlyoutBase.SetAttachedFlyout"/> and opened via
    /// <see cref="FlyoutBase.ShowAttachedFlyout"/>. The WinUI <c>AlwaysExpanded</c> option is
    /// omitted for v1; the flyout always opens collapsed and the overflow menu is toggled by
    /// the presenter's more button.
    /// </remarks>
    public class CommandBarFlyout : FlyoutBase
    {
        /// <summary>
        /// Gets the commands shown in the always-visible horizontal primary bar.
        /// </summary>
        public ObservableCollection<UIElement> PrimaryCommands { get; } = [];

        /// <summary>
        /// Gets the commands shown in the expandable overflow menu below the primary bar. The
        /// presenter's more button is only visible while this collection is non-empty.
        /// </summary>
        public ObservableCollection<UIElement> SecondaryCommands { get; } = [];

        /// <summary>
        /// Creates (or returns the cached) <see cref="CommandBarFlyoutPresenter"/> bound to this
        /// flyout's <see cref="PrimaryCommands"/> and <see cref="SecondaryCommands"/>.
        /// </summary>
        /// <returns>The presenter hosting the command bar.</returns>
        protected override FrameworkElement CreatePresenter()
        {
            _presenter ??= new CommandBarFlyoutPresenter
            {
                Owner = this,
            };

            return _presenter;
        }

        /// <summary>
        /// The cached presenter instance reused across show and hide cycles.
        /// </summary>
        private CommandBarFlyoutPresenter? _presenter;
    }
}
