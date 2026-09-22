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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Tests.Infrastructure
{
    /// <summary>
    /// The one set of visual-tree walkers and the one window teardown for the suite. Every test
    /// class brings these into scope with a using static Fluence.Wpf.Tests.Infrastructure.VisualTree
    /// directive so the call sites read exactly as they did when each partial carried its own
    /// private copy.
    /// </summary>
    internal static class VisualTree
    {
        /// <summary>
        /// Returns the first descendant of <paramref name="root"/> of type <typeparamref name="T"/>
        /// in the visual tree, depth-first, pre-order, or <see langword="null"/>.
        /// </summary>
        /// <typeparam name="T">The descendant type to find.</typeparam>
        /// <param name="root">The element to search below.</param>
        internal static T? FindVisualChild<T>(DependencyObject? root)
            where T : DependencyObject
        {
            if (root is null)
            {
                return null;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int index = 0; index < childCount; index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, index);
                if (child is T match)
                {
                    return match;
                }

                if (FindVisualChild<T>(child) is T visual)
                {
                    return visual;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the first descendant of <paramref name="root"/> of type <typeparamref name="T"/>
        /// whose <see cref="FrameworkElement.Name"/> is <paramref name="name"/>, or
        /// <see langword="null"/>.
        /// </summary>
        /// <typeparam name="T">The descendant type to find.</typeparam>
        /// <param name="root">The element to search below.</param>
        /// <param name="name">The template part name to match, ordinally.</param>
        internal static T? FindVisualChildByName<T>(DependencyObject? root, string name)
            where T : FrameworkElement
        {
            if (root is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int index = 0; index < childCount; index++)
            {
                if (VisualTreeHelper.GetChild(root, index) is FrameworkElement child
                    && string.Equals(child.Name, name, StringComparison.Ordinal)
                    && child is T match)
                {
                    return match;
                }

                T? found = FindVisualChildByName<T>(VisualTreeHelper.GetChild(root, index), name);
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the first node at or below <paramref name="root"/> whose runtime type name is
        /// <paramref name="typeName"/>, or <see langword="null"/>. Used where the type is internal
        /// to the library and cannot be named from the test assembly.
        /// </summary>
        /// <param name="root">The element to search at and below.</param>
        /// <param name="typeName">The simple type name to match, ordinally.</param>
        internal static DependencyObject? FindVisualChildByTypeName(DependencyObject? root, string typeName)
        {
            if (root is null)
            {
                return null;
            }

            if (string.Equals(root.GetType().Name, typeName, StringComparison.Ordinal))
            {
                return root;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int index = 0; index < childCount; index++)
            {
                DependencyObject? found = FindVisualChildByTypeName(VisualTreeHelper.GetChild(root, index), typeName);
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Enumerates every visual descendant of <paramref name="root"/> of type
        /// <typeparamref name="T"/>. Forwards to the canonical
        /// <see cref="WpfTestSta.FindVisualDescendants{T}(DependencyObject?)"/>. This walk covers
        /// the visual tree only, so an element realized only in the logical tree (for example an
        /// unselected <see cref="System.Windows.Controls.TabItem"/> content) is not found here.
        /// <see cref="DemoTestHost.FindVisualChildren{T}(DependencyObject?)"/>
        /// shares this method's name but walks logical and visual together; if an element may
        /// live only in the logical tree, reach for that overload or the underlying
        /// <see cref="WpfTestSta.FindLogicalAndVisualDescendants{T}(DependencyObject?)"/> instead.
        /// </summary>
        /// <typeparam name="T">The descendant type to enumerate.</typeparam>
        /// <param name="root">The element to search below.</param>
        internal static IEnumerable<T> FindVisualChildren<T>(DependencyObject? root)
            where T : DependencyObject
        {
            return WpfTestSta.FindVisualDescendants<T>(root);
        }

        /// <summary>
        /// Detaches, closes and drains a test window. Test bodies call this in a
        /// <see langword="finally"/> so an assertion failure still tears the window down.
        /// </summary>
        /// <param name="window">The window to close.</param>
        internal static void CloseWindowAndDrain(Window window)
        {
            window.Content = null;
            window.UpdateLayout();
            window.Close();
            WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
        }
    }
}
