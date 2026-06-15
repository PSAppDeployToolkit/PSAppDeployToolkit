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

namespace Fluence.Wpf
{
    internal static class TabKeyboardNavigation
    {
        private static bool _registered;

        internal static void EnsureRegistered()
        {
            if (_registered)
            {
                return;
            }

            EventManager.RegisterClassHandler(
                typeof(System.Windows.Controls.TabItem),
                UIElement.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(OnTabItemPreviewKeyDown));
            _registered = true;
        }

        private static void OnTabItemPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Tab)
            {
                return;
            }

            System.Windows.Input.ModifierKeys modifiers = System.Windows.Input.Keyboard.Modifiers;
            if ((modifiers & ~System.Windows.Input.ModifierKeys.Shift) != System.Windows.Input.ModifierKeys.None)
            {
                return;
            }

            if (sender is not System.Windows.Controls.TabItem tabItem)
            {
                return;
            }

            System.Windows.Controls.ItemsControl? owner =
                System.Windows.Controls.ItemsControl.ItemsControlFromItemContainer(tabItem);
            if (owner is not System.Windows.Controls.TabControl tabControl)
            {
                return;
            }

            int currentIndex = tabControl.ItemContainerGenerator.IndexFromContainer(tabItem);
            if (currentIndex < 0)
            {
                return;
            }

            int direction = modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift) ? -1 : 1;
            int nextIndex = currentIndex + direction;
            if (nextIndex < 0 || nextIndex >= tabControl.Items.Count)
            {
                return;
            }

            System.Windows.Controls.TabItem? nextTabItem =
                tabControl.ItemContainerGenerator.ContainerFromIndex(nextIndex) as System.Windows.Controls.TabItem;
            nextTabItem ??= tabControl.Items[nextIndex] as System.Windows.Controls.TabItem;
            if (nextTabItem is null)
            {
                return;
            }

            object item = tabControl.ItemContainerGenerator.ItemFromContainer(nextTabItem);
            tabControl.SelectedItem = item != DependencyProperty.UnsetValue ? item : nextTabItem;
            _ = nextTabItem.Focus();
            _ = System.Windows.Input.Keyboard.Focus(nextTabItem);
            e.Handled = true;
        }
    }
}
