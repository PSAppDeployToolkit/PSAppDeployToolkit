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
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation.Peers;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A horizontal trail of crumbs mirroring the WinUI 3 <c>BreadcrumbBar</c>: each item
    /// renders as a clickable <see cref="BreadcrumbBarItem"/> followed by a chevron separator,
    /// with the last crumb shown as the current location (no trailing chevron, primary text,
    /// SemiBold).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Items come from <see cref="System.Windows.Controls.ItemsControl.ItemsSource"/> (or the
    /// inline items collection) and render through the normal ItemsControl mechanics, so
    /// <see cref="System.Windows.Controls.ItemsControl.DisplayMemberPath"/> and
    /// <see cref="System.Windows.Controls.ItemsControl.ItemTemplate"/> are respected. Subscribe
    /// to <see cref="ItemClicked"/> to navigate; the event is raised for every crumb including
    /// the last one, matching WinUI.
    /// </para>
    /// <para>
    /// WinUI collapses leading crumbs into an ellipsis crumb when the bar is width-constrained;
    /// that overflow collapse is a deliberate v1 omission here. The strip simply extends to its
    /// natural width and clips when constrained.
    /// </para>
    /// </remarks>
    public class BreadcrumbBar : System.Windows.Controls.ItemsControl
    {
        /// <summary>
        /// Initializes static members of the BreadcrumbBar class and overrides the default
        /// style metadata so the control picks up its themed template from Generic.xaml.
        /// </summary>
        static BreadcrumbBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(BreadcrumbBar),
                new FrameworkPropertyMetadata(typeof(BreadcrumbBar)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BreadcrumbBar"/> class and subscribes
        /// to child <see cref="BreadcrumbBarItem.Click"/> events for aggregation into
        /// <see cref="ItemClicked"/>.
        /// </summary>
        public BreadcrumbBar()
        {
            AddHandler(BreadcrumbBarItem.ClickEvent, new RoutedEventHandler(OnBreadcrumbItemClick));
        }

        /// <summary>
        /// Occurs when any crumb is clicked, including the last (current) one, matching the
        /// WinUI 3 BreadcrumbBar contract. The event args carry the clicked data item and its
        /// zero-based index in the items collection.
        /// </summary>
        public event EventHandler<BreadcrumbBarItemClickedEventArgs>? ItemClicked;

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new Automation.BreadcrumbBarAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new BreadcrumbBarItem();
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is BreadcrumbBarItem;
        }

        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            // Containers are generated lazily during layout, after OnItemsChanged has run, so
            // the freshly prepared container must trigger its own last-item refresh.
            UpdateLastItemState();
        }

        /// <inheritdoc />
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            // Adds, removes, moves, and resets can all change which crumb is last; refresh the
            // realized containers immediately (new containers refresh again when prepared).
            UpdateLastItemState();
        }

        private void OnBreadcrumbItemClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not BreadcrumbBarItem crumb)
            {
                return;
            }

            int index = ItemContainerGenerator.IndexFromContainer(crumb);
            if (index < 0)
            {
                return;
            }

            object item = ItemContainerGenerator.ItemFromContainer(crumb);
            if (item == DependencyProperty.UnsetValue)
            {
                item = crumb;
            }

            ItemClicked?.Invoke(this, new BreadcrumbBarItemClickedEventArgs(item, index));
        }

        /// <summary>
        /// Reapplies <see cref="BreadcrumbBarItem.IsLastItem"/> across all realized containers
        /// so exactly the crumb at the final index renders as the current location.
        /// </summary>
        private void UpdateLastItemState()
        {
            int lastIndex = Items.Count - 1;
            for (int index = 0; index <= lastIndex; index++)
            {
                if (ItemContainerGenerator.ContainerFromIndex(index) is BreadcrumbBarItem crumb)
                {
                    crumb.IsLastItem = index == lastIndex;
                }
            }
        }
    }
}
