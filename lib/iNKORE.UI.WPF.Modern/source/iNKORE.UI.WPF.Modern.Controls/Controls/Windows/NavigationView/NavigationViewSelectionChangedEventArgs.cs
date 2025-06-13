// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using iNKORE.UI.WPF.Modern.Media.Animation;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public sealed class NavigationViewSelectionChangedEventArgs : EventArgs
    {
        internal NavigationViewSelectionChangedEventArgs()
        {
        }

        public object SelectedItem { get; internal set; }
        public bool IsSettingsSelected { get; internal set; }

        public NavigationViewItemBase SelectedItemContainer { get; internal set; }
        public NavigationTransitionInfo RecommendedNavigationTransitionInfo { get; internal set; }
    }
}
