// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors.Core
{
    using System; //Do not remove this - as the cref in the comments breaks if this is not included.
    using System.Collections;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using Microsoft.Xaml.Behaviors;

    [DefaultTrigger(typeof(ButtonBase), typeof(Behaviors.EventTrigger), "Click")]
    [DefaultTrigger(typeof(TextBox), typeof(Behaviors.EventTrigger), "TextChanged")]
    [DefaultTrigger(typeof(RichTextBox), typeof(Behaviors.EventTrigger), "TextChanged")]
    [DefaultTrigger(typeof(ListBoxItem), typeof(Behaviors.EventTrigger), "Selected")]
    [DefaultTrigger(typeof(TreeViewItem), typeof(Behaviors.EventTrigger), "Selected")]
    [DefaultTrigger(typeof(Selector), typeof(Behaviors.EventTrigger), "SelectionChanged")]
    [DefaultTrigger(typeof(TreeView), typeof(Behaviors.EventTrigger), "SelectedItemChanged")]
    [DefaultTrigger(typeof(RangeBase), typeof(Behaviors.EventTrigger), "ValueChanged")]
    public abstract class PrototypingActionBase : TriggerAction<DependencyObject>
    {
        //	for unittests
        internal void TestInvoke(object parameter)
        {
            Invoke(parameter);
        }

        protected UserControl GetContainingScreen()
        {
            var userControlAncestors = this.AssociatedObject.GetSelfAndAncestors().OfType<UserControl>();

            // Find the first screen up the visual tree, so that we can invoke state on the proper UserControl
            var screen = userControlAncestors.FirstOrDefault(userControl => InteractionContext.IsScreen(userControl.GetType().FullName));

            // If we couldn't find a hosting screen, take the first UserControl we find.
            return screen ?? userControlAncestors.First();
        }
    }

    public sealed class ActivateStateAction : PrototypingActionBase
    {
        public static readonly DependencyProperty TargetScreenProperty = DependencyProperty.Register(
            "TargetScreen", typeof(string), typeof(ActivateStateAction), new PropertyMetadata(null));
        public static readonly DependencyProperty TargetStateProperty = DependencyProperty.Register(
            "TargetState", typeof(string), typeof(ActivateStateAction), new PropertyMetadata(null));
        public string TargetScreen
        {
            get { return GetValue(TargetScreenProperty) as string; }
            set { SetValue(TargetScreenProperty, value); }
        }

        public string TargetState
        {
            get { return GetValue(TargetStateProperty) as string; }
            set { SetValue(TargetStateProperty, value); }
        }

        protected override void Invoke(object parameter)
        {
            string screen = this.TargetScreen;

            if (string.IsNullOrEmpty(screen))
            {
                screen = this.GetContainingScreen().GetType().ToString();
            }

            InteractionContext.GoToState(screen, this.TargetState);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new ActivateStateAction();
        }
    }

    public sealed class NavigateToScreenAction : PrototypingActionBase
    {
        public static readonly DependencyProperty TargetScreenProperty = DependencyProperty.Register(
            "TargetScreen", typeof(string), typeof(NavigateToScreenAction), new PropertyMetadata(null));

        public string TargetScreen
        {
            get { return GetValue(TargetScreenProperty) as string; }
            set { SetValue(TargetScreenProperty, value as string); }
        }

        protected override void Invoke(object parameter)
        {
            Assembly libraryAssembly = null;

            UserControl hostControl = this.AssociatedObject.GetSelfAndAncestors().OfType<UserControl>().FirstOrDefault();
            if (hostControl != null)
            {
                libraryAssembly = hostControl.GetType().Assembly;
            }

            InteractionContext.GoToScreen(TargetScreen, libraryAssembly);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new NavigateToScreenAction();
        }
    }

    public sealed class NavigateBackAction : PrototypingActionBase
    {
        protected override void Invoke(object parameter)
        {
            InteractionContext.GoBack();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new NavigateBackAction();
        }
    }

    public sealed class NavigateForwardAction : PrototypingActionBase
    {
        protected override void Invoke(object parameter)
        {
            InteractionContext.GoForward();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new NavigateForwardAction();
        }
    }

    public sealed class PlaySketchFlowAnimationAction : PrototypingActionBase
    {
        public static readonly DependencyProperty TargetScreenProperty = DependencyProperty.Register(
            "TargetScreen", typeof(string), typeof(PlaySketchFlowAnimationAction), new PropertyMetadata(null));
        public static readonly DependencyProperty SketchFlowAnimationProperty = DependencyProperty.Register(
            "StateAnimation", typeof(string), typeof(PlaySketchFlowAnimationAction), new PropertyMetadata(null));

        public string TargetScreen
        {
            get { return GetValue(TargetScreenProperty) as string; }
            set { SetValue(TargetScreenProperty, value); }
        }

        public string SketchFlowAnimation
        {
            get { return GetValue(SketchFlowAnimationProperty) as string; }
            set { SetValue(SketchFlowAnimationProperty, value); }
        }

        protected override void Invoke(object parameter)
        {
            string screen = this.TargetScreen;

            if (string.IsNullOrEmpty(screen))
            {
                screen = this.GetContainingScreen().GetType().ToString();
            }

            InteractionContext.PlaySketchFlowAnimation(this.SketchFlowAnimation, screen);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new PlaySketchFlowAnimationAction();
        }
    }

    [DefaultTrigger(typeof(FrameworkElement), typeof(Behaviors.EventTrigger), "Loaded")]
    [DefaultTrigger(typeof(ButtonBase), typeof(Behaviors.EventTrigger), "Loaded")]
    public sealed class NavigationMenuAction : TargetedTriggerAction<FrameworkElement>
    {
        public static readonly DependencyProperty InactiveStateProperty = DependencyProperty.Register(
            "InactiveState",
            typeof(string),
            typeof(NavigationMenuAction),
            new PropertyMetadata(null));

        public static readonly DependencyProperty TargetScreenProperty = DependencyProperty.Register(
            "TargetScreen",
            typeof(string),
            typeof(NavigationMenuAction),
            new PropertyMetadata(null));

        public static readonly DependencyProperty ActiveStateProperty = DependencyProperty.Register(
            "ActiveState",
            typeof(string),
            typeof(NavigationMenuAction),
            new PropertyMetadata(null));

        public string TargetScreen
        {
            get { return (string)GetValue(TargetScreenProperty); }
            set { SetValue(TargetScreenProperty, value); }
        }

        public string ActiveState
        {
            get { return (string)GetValue(ActiveStateProperty); }
            set { SetValue(ActiveStateProperty, value); }
        }

        public string InactiveState
        {
            get { return (string)GetValue(InactiveStateProperty); }
            set { SetValue(InactiveStateProperty, value); }
        }

        private bool IsTargetObjectSet
        {
            get
            {
                bool isLocallySet = this.ReadLocalValue(TargetedTriggerAction.TargetObjectProperty) != DependencyProperty.UnsetValue;
                // if the value can be set indirectly (via trigger, style, etc), should also check ValueSource, but not a concern for behaviors right now.
                return isLocallySet;
            }
        }

        private FrameworkElement StateTarget { get; set; }

        /// <summary>
        /// Called when the target changes. If the TargetName property isn't set, this action has custom behavior.
        /// </summary>
        /// <param name="oldTarget"></param>
        /// <param name="newTarget"></param>
        /// <exception cref="InvalidOperationException">Could not locate an appropriate FrameworkElement with states.</exception>
        protected override void OnTargetChanged(FrameworkElement oldTarget, FrameworkElement newTarget)
        {
            base.OnTargetChanged(oldTarget, newTarget);

            FrameworkElement frameworkElement = null;

            if (string.IsNullOrEmpty(this.TargetName) && !this.IsTargetObjectSet)
            {
                VisualStateUtilities.TryFindNearestStatefulControl(this.AssociatedObject as FrameworkElement, out frameworkElement);
            }
            else
            {
                frameworkElement = this.Target;
            }

            this.StateTarget = frameworkElement;
        }

        protected override void Invoke(object parameter)
        {
            if (this.AssociatedObject != null)
            {
                this.InvokeImpl(this.StateTarget);
            }
        }

        internal void InvokeImpl(FrameworkElement stateTarget)
        {
            if (stateTarget != null &&
                !string.IsNullOrEmpty(this.ActiveState) &&
                !string.IsNullOrEmpty(this.InactiveState) &&
                !string.IsNullOrEmpty(this.TargetScreen))
            {
                UserControl screen = stateTarget
                        .GetSelfAndAncestors()
                        .OfType<UserControl>()
                        .FirstOrDefault(control => control.GetType().ToString() == this.TargetScreen);

                string stateName = this.InactiveState;
                if (screen != null)
                {
                    stateName = this.ActiveState;
                }

                if (!string.IsNullOrEmpty(stateName))
                {
                    ToggleButton toggleButton = stateTarget as ToggleButton;
                    if (toggleButton != null)
                    {
                        switch (stateName)
                        {
                            case "Checked":
                                toggleButton.IsChecked = true;
                                return;
                            case "Unchecked":
                                toggleButton.IsChecked = false;
                                return;
                        }
                    }
                    if (stateName == "Disabled")
                    {
                        stateTarget.IsEnabled = false;
                        return;
                    }
                    VisualStateUtilities.GoToState(stateTarget, stateName, true);
                }
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new NavigationMenuAction();
        }
    }

    /// <summary>
    /// Allows a user to remove the item from a ListBox ItemTemplate.
    /// </summary>
    public sealed class RemoveItemInListBoxAction : TriggerAction<FrameworkElement>
    {
        protected override void Invoke(object parameter)
        {
            ItemsControl items = this.ItemsControl;
            if (items != null)
            {
                if (items.ItemsSource != null)
                {
                    // Given the flexibility of databinding, we won't always have a collection where we can 
                    // remove an item.  But let's try a common scenario.
                    IList list = items.ItemsSource as IList;
                    if (list != null && !list.IsReadOnly && list.Contains(this.AssociatedObject.DataContext))
                    {
                        list.Remove(this.AssociatedObject.DataContext);
                    }
                }
                else
                {
                    ListBox listBox = this.ItemsControl as ListBox;
                    if (listBox != null)
                    {
                        ListBoxItem listBoxItem = this.ItemContainer;
                        if (listBoxItem != null)
                        {
                            listBox.Items.Remove(listBoxItem.Content);
                        }
                    }
                }
            }

        }

        private ListBoxItem ItemContainer
        {
            get
            {
                return (ListBoxItem)DependencyObjectHelper.GetSelfAndAncestors(this.AssociatedObject).FirstOrDefault(element => element is ListBoxItem);
            }
        }

        private ItemsControl ItemsControl
        {
            get
            {
                return (ItemsControl)DependencyObjectHelper.GetSelfAndAncestors(this.AssociatedObject).FirstOrDefault(element => element is ItemsControl);
            }
        }
    }
}
