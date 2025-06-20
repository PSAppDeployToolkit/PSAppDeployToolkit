// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors.Core
{
    using Microsoft.Xaml.Behaviors.Media;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Effects;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// ExtendedVisualStateManager is a custom VisualStateManager that can smooth out the animation of layout properties.
    /// With this custom VisualStateManager, states can include changes to properties like Grid.Column, can change element heights to or from Auto, and so on.
    /// These changes will be smoothed out over time using the GeneratedDuration and GeneratedEasingFunction of the appropriate transition.
    /// See the "VisualStateManager overrides" region below for a general description of the algorithm.
    /// </summary>
    public class ExtendedVisualStateManager : VisualStateManager
    {

        internal class WrapperCanvas : Canvas
        {
            public Rect OldRect { get; set; }
            public Rect NewRect { get; set; }
            public Dictionary<DependencyProperty, object> LocalValueCache { get; set; }
            public Visibility DestinationVisibilityCache { get; set; }

            public double SimulationProgress
            {
                get { return (double)GetValue(SimulationProgressProperty); }
                set { SetValue(SimulationProgressProperty, value); }
            }

            internal static readonly DependencyProperty SimulationProgressProperty = DependencyProperty.Register("SimulationProgress", typeof(double), typeof(WrapperCanvas), new PropertyMetadata(0d, SimulationProgressChanged));

            private static void SimulationProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                WrapperCanvas wrapper = d as WrapperCanvas;
                double progress = (double)e.NewValue;

                if (wrapper != null && wrapper.Children.Count > 0)
                {
                    FrameworkElement child = wrapper.Children[0] as FrameworkElement;

                    child.Width = Math.Max(0, wrapper.OldRect.Width * progress + wrapper.NewRect.Width * (1 - progress));
                    child.Height = Math.Max(0, wrapper.OldRect.Height * progress + wrapper.NewRect.Height * (1 - progress));
                    Canvas.SetLeft(child, progress * (wrapper.OldRect.Left - wrapper.NewRect.Left));
                    Canvas.SetTop(child, progress * (wrapper.OldRect.Top - wrapper.NewRect.Top));
                }
            }
        }

        public static bool IsRunningFluidLayoutTransition { get { return LayoutTransitionStoryboard != null; } }

        #region Data attached to VSM
        /// <summary>
        /// OriginalValueRecord remembers the original value of a property that was changed in a state.
        /// </summary>
        internal class OriginalLayoutValueRecord
        {
            public FrameworkElement Element { get; set; }
            public DependencyProperty Property { get; set; }
            public object Value { get; set; }
        }

        /// <summary>
        /// A VisualStateGroup that can use FluidLayout or not.
        /// </summary>
        public static readonly DependencyProperty UseFluidLayoutProperty = DependencyProperty.RegisterAttached("UseFluidLayout", typeof(bool), typeof(ExtendedVisualStateManager), new PropertyMetadata(false));
        public static bool GetUseFluidLayout(DependencyObject obj) { return (bool)obj.GetValue(UseFluidLayoutProperty); }
        public static void SetUseFluidLayout(DependencyObject obj, bool value) { obj.SetValue(UseFluidLayoutProperty, value); }

        /// <summary>
        /// Visibility is shadowed by a custom attached property at runtime.
        /// </summary>
        public static readonly DependencyProperty RuntimeVisibilityPropertyProperty = DependencyProperty.RegisterAttached("RuntimeVisibilityProperty", typeof(DependencyProperty), typeof(ExtendedVisualStateManager), new PropertyMetadata(null));
        public static DependencyProperty GetRuntimeVisibilityProperty(DependencyObject obj) { return (DependencyProperty)obj.GetValue(RuntimeVisibilityPropertyProperty); }
        public static void SetRuntimeVisibilityProperty(DependencyObject obj, DependencyProperty value) { obj.SetValue(RuntimeVisibilityPropertyProperty, value); }

        /// <summary>
        /// A VisualStateGroup keeps a list of these original values in an attached property.
        /// </summary>
        internal static readonly DependencyProperty OriginalLayoutValuesProperty = DependencyProperty.RegisterAttached("OriginalLayoutValues", typeof(List<OriginalLayoutValueRecord>), typeof(ExtendedVisualStateManager), new PropertyMetadata(null));
        internal static List<OriginalLayoutValueRecord> GetOriginalLayoutValues(DependencyObject obj) { return (List<OriginalLayoutValueRecord>)obj.GetValue(OriginalLayoutValuesProperty); }
        internal static void SetOriginalLayoutValues(DependencyObject obj, List<OriginalLayoutValueRecord> value) { obj.SetValue(OriginalLayoutValuesProperty, value); }

        /// <summary>
        /// For every state, the layout-specific properties get extracted and then are attached to the state. These properties are removed from the state itself.
        /// </summary>
        internal static readonly DependencyProperty LayoutStoryboardProperty = DependencyProperty.RegisterAttached("LayoutStoryboard", typeof(Storyboard), typeof(ExtendedVisualStateManager), new PropertyMetadata(null));
        internal static Storyboard GetLayoutStoryboard(DependencyObject obj) { return (Storyboard)obj.GetValue(LayoutStoryboardProperty); }
        internal static void SetLayoutStoryboard(DependencyObject obj, Storyboard value) { obj.SetValue(LayoutStoryboardProperty, value); }

        /// <summary>
        /// Remember the current state.
        /// </summary>
        internal static readonly DependencyProperty CurrentStateProperty = DependencyProperty.RegisterAttached("CurrentState", typeof(VisualState), typeof(ExtendedVisualStateManager), new PropertyMetadata(null));
        internal static VisualState GetCurrentState(DependencyObject obj) { return (VisualState)obj.GetValue(CurrentStateProperty); }
        internal static void SetCurrentState(DependencyObject obj, VisualState value) { obj.SetValue(CurrentStateProperty, value); }

        /// <summary>
        /// The TransitionEffect to use when the state changes.
        /// </summary>
        public static readonly DependencyProperty TransitionEffectProperty = DependencyProperty.RegisterAttached("TransitionEffect", typeof(TransitionEffect), typeof(ExtendedVisualStateManager), new PropertyMetadata(null));
        public static TransitionEffect GetTransitionEffect(DependencyObject obj) { return (TransitionEffect)obj.GetValue(TransitionEffectProperty); }
        public static void SetTransitionEffect(DependencyObject obj, TransitionEffect value) { obj.SetValue(TransitionEffectProperty, value); }

        /// <summary>
        /// The TransitionEffectStoryboard in use during the state change.
        /// </summary>
        internal static readonly DependencyProperty TransitionEffectStoryboardProperty = DependencyProperty.RegisterAttached("TransitionEffectStoryboard", typeof(Storyboard), typeof(ExtendedVisualStateManager), new PropertyMetadata(null));
        internal static Storyboard GetTransitionEffectStoryboard(DependencyObject obj) { return (Storyboard)obj.GetValue(TransitionEffectStoryboardProperty); }
        internal static void SetTransitionEffectStoryboard(DependencyObject obj, Storyboard value) { obj.SetValue(TransitionEffectStoryboardProperty, value); }

        /// <summary>
        /// The cached background in use during the state change.
        /// </summary>
        internal static readonly DependencyProperty DidCacheBackgroundProperty = DependencyProperty.RegisterAttached("DidCacheBackground", typeof(bool), typeof(ExtendedVisualStateManager), new PropertyMetadata(false));
        internal static bool GetDidCacheBackground(DependencyObject obj) { return (bool)obj.GetValue(DidCacheBackgroundProperty); }
        internal static void SetDidCacheBackground(DependencyObject obj, bool value) { obj.SetValue(DidCacheBackgroundProperty, value); }

        /// <summary>
        /// The cached background in use during the state change.
        /// </summary>
        internal static readonly DependencyProperty CachedBackgroundProperty = DependencyProperty.RegisterAttached("CachedBackground", typeof(object), typeof(ExtendedVisualStateManager), new PropertyMetadata(null));
        internal static object GetCachedBackground(DependencyObject obj) { return obj.GetValue(CachedBackgroundProperty); }
        internal static void SetCachedBackground(DependencyObject obj, object value) { obj.SetValue(CachedBackgroundProperty, value); }

        /// <summary>
        /// The cached background in use during the state change.
        /// </summary>
        internal static readonly DependencyProperty CachedEffectProperty = DependencyProperty.RegisterAttached("CachedEffect", typeof(Effect), typeof(ExtendedVisualStateManager), new PropertyMetadata(null));
        internal static Effect GetCachedEffect(DependencyObject obj) { return (Effect)obj.GetValue(CachedEffectProperty); }
        internal static void SetCachedEffect(DependencyObject obj, Effect value) { obj.SetValue(CachedEffectProperty, value); }
        #endregion

        #region Static data pertaining to the active layout transition
        /// <summary>
        /// This is the set of elements that are currently in motion.
        /// </summary>
        private static List<FrameworkElement> MovingElements;

        /// <summary>
        /// This is the storyboard that is animating the transition.
        /// </summary>
        private static Storyboard LayoutTransitionStoryboard;
        #endregion

        #region Definition of layout properties
        /// <summary>
        /// This list contains all the known layout properties.
        /// </summary>
        private static List<DependencyProperty> LayoutProperties = new List<DependencyProperty>()
        {
            Grid.ColumnProperty,
            Grid.ColumnSpanProperty,
            Grid.RowProperty,
            Grid.RowSpanProperty,
            Canvas.LeftProperty,
            Canvas.TopProperty,
            FrameworkElement.WidthProperty,
            FrameworkElement.HeightProperty,
            FrameworkElement.MinWidthProperty,
            FrameworkElement.MinHeightProperty,
            FrameworkElement.MaxWidthProperty,
            FrameworkElement.MaxHeightProperty,
            FrameworkElement.MarginProperty,
            FrameworkElement.HorizontalAlignmentProperty,
            FrameworkElement.VerticalAlignmentProperty,
            UIElement.VisibilityProperty,
            StackPanel.OrientationProperty,
        };

        private static List<DependencyProperty> ChildAffectingLayoutProperties = new List<DependencyProperty>()
        {
            StackPanel.OrientationProperty,
        };

        private static bool IsVisibilityProperty(DependencyProperty property)
        {
            // no need to check owner type - We've already filtered in LayoutPropertyFromTimeline
            return property == UIElement.VisibilityProperty || property.Name == "RuntimeVisibility";
        }

        [SuppressMessage("Microsoft.Globalization", "CA1309", Justification = "Strings are postfixes of class names and not localizable")]
        private static DependencyProperty LayoutPropertyFromTimeline(Timeline timeline, bool forceRuntimeProperty)
        {
            PropertyPath path = Storyboard.GetTargetProperty(timeline);

            if (path == null || path.PathParameters == null || path.PathParameters.Count == 0)
            {
                return null;
            }

            DependencyProperty property = path.PathParameters[0] as DependencyProperty;

            if (property != null)
            {
                if (property.Name == "RuntimeVisibility" && property.OwnerType.Name.EndsWith("DesignTimeProperties", StringComparison.Ordinal))
                {
                    if (!LayoutProperties.Contains(property))
                    {
                        LayoutProperties.Add(property);
                    }
                    return forceRuntimeProperty ? property : UIElement.VisibilityProperty;
                }
                else if (property.Name == "RuntimeWidth" && property.OwnerType.Name.EndsWith("DesignTimeProperties", StringComparison.Ordinal))
                {
                    if (!LayoutProperties.Contains(property))
                    {
                        LayoutProperties.Add(property);
                    }
                    return forceRuntimeProperty ? property : FrameworkElement.WidthProperty;
                }
                else if (property.Name == "RuntimeHeight" && property.OwnerType.Name.EndsWith("DesignTimeProperties", StringComparison.Ordinal))
                {
                    if (!LayoutProperties.Contains(property))
                    {
                        LayoutProperties.Add(property);
                    }
                    return forceRuntimeProperty ? property : FrameworkElement.HeightProperty;
                }
                else if (LayoutProperties.Contains(property))
                {
                    return property;
                }
            }

            return null;
        }
        #endregion

        private bool changingState = false;

        #region VisualStateManager overrides

        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "1#", Justification = "Better to share the implementation here than to match the parameter name.")]
        protected override bool GoToStateCore(FrameworkElement control, FrameworkElement stateGroupsRoot, string stateName, VisualStateGroup group, VisualState state, bool useTransitions)
        {
            //
            // Reminder that a layout transition may already be running; several of these functions keep track of the current value of MovingElements
            // so that they can account for the fact that these elements have unusual layout positions right now.
            //

            Storyboard layoutStoryboard;

            // On WPF 4 there's an open bug (882549) where platform controls reassert all states every measure, and at designtime this is a problem because the CommonStates
            // can't possibly be right. Fix is to inhibit a state change when we are in the middle of setting up a FluidLayout change.
            if (this.changingState)
            {
                return false;
            }

            if (group == null || state == null)
            {
                return false;
            }

            //
            // Keep our own copy of the current state
            //
            VisualState previousState = GetCurrentState(group);

            if (previousState == state)
            {
                return true;
            }

            //
            // Find the transition that should be used
            //
            VisualTransition transition = FindTransition(group, previousState, state);

            bool animateWithTransitionEffect = PrepareTransitionEffectImage(stateGroupsRoot, useTransitions, transition);

            //
            // If this group is not using Fluid Layout, then get out
            //
            if (!GetUseFluidLayout(group))
            {
                return this.TransitionEffectAwareGoToStateCore(control, stateGroupsRoot, stateName, group, state, useTransitions, transition, animateWithTransitionEffect, previousState);
            }

            //
            // Get all layout properties out of the state's storyboard. This is only performed once per state thanks to an attached property on the state.
            //
            layoutStoryboard = ExtractLayoutStoryboard(state);

            //
            // Make sure that we have a place to store the original values for anything that we might overwrite
            //
            List<OriginalLayoutValueRecord> originalValueRecords = GetOriginalLayoutValues(group);
            if (originalValueRecords == null)
            {
                originalValueRecords = new List<OriginalLayoutValueRecord>();
                SetOriginalLayoutValues(group, originalValueRecords);
            }

            //
            // Take the easy road if we don't have to animate - this is a compressed version of what's below
            //
            if (!useTransitions)
            {
                if (LayoutTransitionStoryboard != null)
                {
                    StopAnimations();
                }
                bool returnValue = this.TransitionEffectAwareGoToStateCore(control, stateGroupsRoot, stateName, group, state, useTransitions, transition, animateWithTransitionEffect, previousState);

                SetLayoutStoryboardProperties(control, stateGroupsRoot, layoutStoryboard, originalValueRecords);
                return returnValue;
            }

            if (layoutStoryboard.Children.Count == 0 && originalValueRecords.Count == 0)
            {
                return this.TransitionEffectAwareGoToStateCore(control, stateGroupsRoot, stateName, group, state, useTransitions, transition, animateWithTransitionEffect, previousState);
            }

            try
            {
                this.changingState = true;

                // Force layout to be updated first - helps with OnLoaded() animations.
                stateGroupsRoot.UpdateLayout();

                //
                // Enumerate elements in the state (and the previous state), then expand that list to contain anything that might move
                // as a result of these elements changing size:
                //   - siblings of elements in the list
                //   - parents of elements in the list
                //   - siblings of parents in the list
                //   - grandparents and their siblings
                //   - etc.
                //   - no need to travel *down* the tree, if a parent changes size then the children will move
                //
                List<FrameworkElement> targetElements = FindTargetElements(control, stateGroupsRoot, layoutStoryboard, originalValueRecords, MovingElements);

                //
                // Get the parent-relative rect of every element in the list, and the original effective opacity (= opacity * visibility, more or less)
                //  - Assume that every Visibility change is an intended animation, unlike the work we do to filter the set of elements that actually moved
                //
                Dictionary<FrameworkElement, Rect> oldRects = GetRectsOfTargets(targetElements, MovingElements);
                Dictionary<FrameworkElement, double> oldOpacities = GetOldOpacities(control, stateGroupsRoot, layoutStoryboard, originalValueRecords, MovingElements);

                //
                // Now that we've captured the current situation, stop the previous transition before going to the new state
                //
                if (LayoutTransitionStoryboard != null)
                {
                    stateGroupsRoot.LayoutUpdated -= new EventHandler(control_LayoutUpdated);
                    StopAnimations();
                    stateGroupsRoot.UpdateLayout();
                }

                //
                // Go to the new state; jump immediately to the layout changes
                //
                this.TransitionEffectAwareGoToStateCore(control, stateGroupsRoot, stateName, group, state, useTransitions, transition, animateWithTransitionEffect, previousState);
                SetLayoutStoryboardProperties(control, stateGroupsRoot, layoutStoryboard, originalValueRecords);

                //
                // UpdateLayout
                //
                stateGroupsRoot.UpdateLayout();

                //
                // Get the parent-relative rect of every element in the list
                //  - Note: Do not need the new visibility since we can just read the property
                //
                Dictionary<FrameworkElement, Rect> newRects = GetRectsOfTargets(targetElements, null);

                //
                // Compute the set of elements from the list whose rects changed
                //
                MovingElements = new List<FrameworkElement>();

                foreach (FrameworkElement target in targetElements)
                {
                    if (oldRects[target] != newRects[target])
                    {
                        MovingElements.Add(target);
                    }
                }

                //
                // Add the elements whose opacity is changing, so we can change opacity on the wrapper and not the element
                //
                foreach (FrameworkElement visibilityElement in oldOpacities.Keys)
                {
                    if (!MovingElements.Contains(visibilityElement))
                    {
                        MovingElements.Add(visibilityElement);
                    }
                }

                //
                // Freeze these elements at their current location, otherwise detach from layout
                //  - For now, wrap each of these elements in a Canvas
                //
                WrapMovingElementsInCanvases(MovingElements, oldRects, newRects);

                stateGroupsRoot.LayoutUpdated += new EventHandler(control_LayoutUpdated);

                //
                // Animate the size/location of these elements from old rect to new rect
                //  - NOT with scale transforms, though translate transforms are OK
                //  - changing the size of the element should call measure/arrange on any of its children that are not themselves detached from layout
                //
                LayoutTransitionStoryboard = CreateLayoutTransitionStoryboard(transition, MovingElements, oldOpacities);

                LayoutTransitionStoryboard.Completed += (EventHandler)delegate (object sender, EventArgs args)
                {
                    stateGroupsRoot.LayoutUpdated -= new EventHandler(control_LayoutUpdated);
                    StopAnimations();
                };

                LayoutTransitionStoryboard.Begin();
            }
            finally
            {
                this.changingState = false;
            }

            return true;
        }
        #endregion

        #region Private static helpers
        private static void control_LayoutUpdated(object sender, EventArgs e)
        {
            if (LayoutTransitionStoryboard != null)
            {
                foreach (FrameworkElement movingElement in MovingElements)
                {
                    WrapperCanvas parentCanvas = movingElement.Parent as WrapperCanvas;
                    if (parentCanvas != null)
                    {
                        Rect currentRect = GetLayoutRect(parentCanvas);
                        Rect newRect = parentCanvas.NewRect;

                        TranslateTransform translate = parentCanvas.RenderTransform as TranslateTransform;
                        double oldOffsetX = translate == null ? 0 : translate.X;
                        double oldOffsetY = translate == null ? 0 : translate.Y;
                        double newOffsetX = newRect.Left - currentRect.Left;
                        double newOffsetY = newRect.Top - currentRect.Top;

                        if (oldOffsetX != newOffsetX || oldOffsetY != newOffsetY)
                        {
                            if (translate == null)
                            {
                                translate = new TranslateTransform();
                                parentCanvas.RenderTransform = translate;
                            }

                            translate.X = newOffsetX;
                            translate.Y = newOffsetY;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stop the animation and replace the layout changes that were made to support that animation.
        /// </summary>
        private static void StopAnimations()
        {
            if (LayoutTransitionStoryboard != null)
            {
                LayoutTransitionStoryboard.Stop();
                LayoutTransitionStoryboard = null;
            }

            if (MovingElements != null)
            {
                UnwrapMovingElementsFromCanvases(MovingElements);
                MovingElements = null;
            }
        }

        private class DummyEasingFunction : EasingFunctionBase
        {
            public double DummyValue
            {
                get { return (double)GetValue(DummyValueProperty); }
                set { SetValue(DummyValueProperty, value); }
            }

            public static readonly DependencyProperty DummyValueProperty = DependencyProperty.Register("DummyValue", typeof(double), typeof(DummyEasingFunction), new PropertyMetadata(0.0));

            protected override Freezable CreateInstanceCore()
            {
                return new DummyEasingFunction();
            }

            public DummyEasingFunction()
            {
            }

            protected override double EaseInCore(double normalizedTime)
            {
                return this.DummyValue;
            }
        }

        private static bool PrepareTransitionEffectImage(FrameworkElement stateGroupsRoot, bool useTransitions, VisualTransition transition)
        {
            TransitionEffect effect = transition == null ? null : ExtendedVisualStateManager.GetTransitionEffect(transition);
            bool animateWithTransitionEffect = false;

            // Are we using TransitionEffects?
            if (effect != null)
            {
                effect = (TransitionEffect)effect.CloneCurrentValue();

                // If we're going to be animating, take a snapshot
                if (useTransitions)
                {
                    animateWithTransitionEffect = true;

                    int bitmapWidth = (int)Math.Max(1, stateGroupsRoot.ActualWidth);
                    int bitmapHeight = (int)Math.Max(1, stateGroupsRoot.ActualHeight);

                    RenderTargetBitmap current = new RenderTargetBitmap(bitmapWidth, bitmapHeight, 96, 96, PixelFormats.Pbgra32);
                    current.Render(stateGroupsRoot);

                    ImageBrush oldImageBrush = new ImageBrush();
                    oldImageBrush.ImageSource = current;
                    effect.OldImage = oldImageBrush;
                }

                // Also, cancel the previous animation if there was one
                Storyboard oldTransitionEffectStoryboard = GetTransitionEffectStoryboard(stateGroupsRoot);
                if (oldTransitionEffectStoryboard != null)
                {
                    oldTransitionEffectStoryboard.Stop();
                    FinishTransitionEffectAnimation(stateGroupsRoot);
                }

                if (useTransitions)
                {
                    TransferLocalValue(stateGroupsRoot, FrameworkElement.EffectProperty, CachedEffectProperty);
                    stateGroupsRoot.Effect = effect;
                }
            }
            return animateWithTransitionEffect;
        }

        private bool TransitionEffectAwareGoToStateCore(FrameworkElement control, FrameworkElement stateGroupsRoot, string stateName, VisualStateGroup group, VisualState state, bool useTransitions, VisualTransition transition, bool animateWithTransitionEffect, VisualState previousState)
        {
            IEasingFunction oldGeneratedEasingFunction = null;

            if (animateWithTransitionEffect)
            {
                // We need to set up a dummy easing function to keep GoToState from sending a StateChanged event.
                // The dummy easing function is intended to freeze the animation at the *end* of the transition,
                // because from a *property* perspective, everything happens instantly.
                // However, if animating to opacity 0, then we have to stop just before the end, because if the opacity
                // reaches zero then there will be no surface on which to draw.
                oldGeneratedEasingFunction = transition.GeneratedEasingFunction;
                transition.GeneratedEasingFunction = new DummyEasingFunction() { DummyValue = (FinishesWithZeroOpacity(control, stateGroupsRoot, state, previousState) ? 0.01 : 0) };
            }

            bool returnValue = base.GoToStateCore(control, stateGroupsRoot, stateName, group, state, useTransitions);

            if (animateWithTransitionEffect)
            {
                // Now, put things back, and kick off the TransitionEffect if the state actually changed
                transition.GeneratedEasingFunction = oldGeneratedEasingFunction;

                if (returnValue)
                {
                    AnimateTransitionEffect(stateGroupsRoot, transition);
                }
            }

            SetCurrentState(group, state);

            return returnValue;
        }

        private static bool FinishesWithZeroOpacity(FrameworkElement control, FrameworkElement stateGroupsRoot, VisualState state, VisualState previousState)
        {
            // first, check the new state
            if (state.Storyboard != null)
            {
                foreach (Timeline timeline in state.Storyboard.Children)
                {
                    if (!TimelineIsAnimatingRootOpacity(timeline, control, stateGroupsRoot))
                    {
                        continue;
                    }

                    bool gotValue;
                    object value = GetValueFromTimeline(timeline, out gotValue);

                    return (gotValue && value is double && (double)value == 0);
                }
            }

            // if we got here, then see if it's mentioned in the old state, and if so, the new value will be the base
            if (previousState != null && previousState.Storyboard != null)
            {
                foreach (Timeline timeline in previousState.Storyboard.Children)
                {
                    if (!TimelineIsAnimatingRootOpacity(timeline, control, stateGroupsRoot))
                    {
                        continue;
                    }
                }

                double baseOpacity = (double)stateGroupsRoot.GetAnimationBaseValue(UIElement.OpacityProperty);
                return (baseOpacity == 0);
            }

            // if it's not mentioned in either state, then let's just check the current opacity
            return (stateGroupsRoot.Opacity == 0);
        }

        private static bool TimelineIsAnimatingRootOpacity(Timeline timeline, FrameworkElement control, FrameworkElement stateGroupsRoot)
        {
            if (GetTimelineTarget(control, stateGroupsRoot, timeline) != stateGroupsRoot)
            {
                return false;
            }

            PropertyPath path = Storyboard.GetTargetProperty(timeline);

            return path != null && path.PathParameters != null && path.PathParameters.Count != 0 && path.PathParameters[0] == UIElement.OpacityProperty;
        }

        private static void AnimateTransitionEffect(FrameworkElement stateGroupsRoot, VisualTransition transition)
        {
            TransitionEffect effect = stateGroupsRoot.Effect as TransitionEffect;

            DoubleAnimation da = new DoubleAnimation();
            da.Duration = transition.GeneratedDuration;
            da.EasingFunction = transition.GeneratedEasingFunction;
            da.From = 0;
            da.To = 1;

            Storyboard sb = new Storyboard();
            sb.Duration = transition.GeneratedDuration;
            sb.Children.Add(da);

            // On WPF, can't seem to address the Effect directly.
            Storyboard.SetTarget(da, stateGroupsRoot);
            Storyboard.SetTargetProperty(da, new PropertyPath("(0).(1)", new DependencyProperty[] { FrameworkElement.EffectProperty, TransitionEffect.ProgressProperty }));

            // If the background is null, make it transparent so that the effect will be drawn in the correct area.
            Panel rootPanel = stateGroupsRoot as Panel;

            if (rootPanel != null && rootPanel.Background == null)
            {
                {
                    // ObjectAnimationUsingKeyFrames does not work for some reason - set it ASAP
                    SetDidCacheBackground(rootPanel, true);
                    TransferLocalValue(rootPanel, Panel.BackgroundProperty, CachedBackgroundProperty);
                    rootPanel.Background = Brushes.Transparent;
                }
            }

            sb.Completed += delegate (object sender, EventArgs e)
            {
                Storyboard currentTransitionEffectStoryboard = GetTransitionEffectStoryboard(stateGroupsRoot);
                if (currentTransitionEffectStoryboard == sb)
                {
                    FinishTransitionEffectAnimation(stateGroupsRoot);
                }
            };
            SetTransitionEffectStoryboard(stateGroupsRoot, sb);
            sb.Begin();
        }

        private static void FinishTransitionEffectAnimation(FrameworkElement stateGroupsRoot)
        {
            SetTransitionEffectStoryboard(stateGroupsRoot, null);

            TransferLocalValue(stateGroupsRoot, CachedEffectProperty, FrameworkElement.EffectProperty);

            if (GetDidCacheBackground(stateGroupsRoot))
            {
                TransferLocalValue(stateGroupsRoot, CachedBackgroundProperty, Panel.BackgroundProperty);
                SetDidCacheBackground(stateGroupsRoot, false);
            }
        }

        /// <summary>
        /// Locate the transition that VisualStateManager will use to animate the change, so that the layout animation can match the duration and easing.
        /// </summary>
        /// <param name="group">The group in which the transition is taking place.</param>
        /// <param name="previousState">The state that you are coming from.</param>
        /// <param name="state">The state you are going to.</param>
        /// <returns>The transition</returns>
        private static VisualTransition FindTransition(VisualStateGroup group, VisualState previousState, VisualState state)
        {
            string previousStateName = (previousState != null ? previousState.Name : string.Empty);
            string stateName = (state != null ? state.Name : string.Empty);

            int bestMatchScore = -1;
            VisualTransition bestTransition = null;

            if (group.Transitions != null)
            {
                foreach (VisualTransition transition in group.Transitions)
                {
                    int matchScore = 0;
                    if (transition.From == previousStateName)
                    {
                        matchScore++;
                    }
                    else if (!string.IsNullOrEmpty(transition.From))
                    {
                        continue;
                    }
                    if (transition.To == stateName)
                    {
                        matchScore += 2;
                    }
                    else if (!string.IsNullOrEmpty(transition.To))
                    {
                        continue;
                    }
                    if (matchScore > bestMatchScore)
                    {
                        bestMatchScore = matchScore;
                        bestTransition = transition;
                    }
                }
            }
            return bestTransition;
        }

        /// <summary>
        /// Remove all layout-affecting properties from the Storyboard for the state and cache them in an attached property.
        /// </summary>
        /// <param name="state">The state you are moving to.</param>
        /// <returns>A Storyboard containing the layout properties in that state.</returns>
        private static Storyboard ExtractLayoutStoryboard(VisualState state)
        {
            Storyboard layoutStoryboard = null;

            if (state.Storyboard != null)
            {
                layoutStoryboard = GetLayoutStoryboard(state.Storyboard);
                if (layoutStoryboard == null)
                {
                    layoutStoryboard = new Storyboard();

                    for (int i = state.Storyboard.Children.Count - 1; i >= 0; i--)
                    {
                        Timeline timeline = state.Storyboard.Children[i];

                        if (LayoutPropertyFromTimeline(timeline, false) != null)
                        {
                            state.Storyboard.Children.RemoveAt(i);
                            layoutStoryboard.Children.Add(timeline);
                        }
                    }

                    SetLayoutStoryboard(state.Storyboard, layoutStoryboard);
                }
            }
            return layoutStoryboard != null ? layoutStoryboard : new Storyboard();
        }

        /// <summary>
        /// The set of target elements is the set of all elements that might have moved in a layout transition. This set is the closure of:
        ///  - Elements with layout properties animated in the state.
        ///  - Siblings of elements in the set.
        ///  - Parents of elements in the set.
        ///  
        /// Subsequent code will check these rectangles both before and after the layout change.
        /// </summary>
        /// <param name="control">The control whose layout is changing state.</param>
        /// <param name="layoutStoryboard">The storyboard containing the layout changes.</param>
        /// <param name="originalValueRecords">Any previous values from previous state navigations that might be reverted.</param>
        /// <param name="movingElements">The set of elements currently in motion, if there is a state change transition ongoing.</param>
        /// <returns>The full set of elements whose layout may have changed.</returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "This is done in a single pass for performance reasons.")]
        private static List<FrameworkElement> FindTargetElements(FrameworkElement control, FrameworkElement templateRoot, Storyboard layoutStoryboard, List<OriginalLayoutValueRecord> originalValueRecords, List<FrameworkElement> movingElements)
        {
            List<FrameworkElement> targets = new List<FrameworkElement>();

            // Of course, any elements currently in motion need to be inspected
            if (movingElements != null)
            {
                targets.AddRange(movingElements);
            }

            // Also, any element with a layout property being animated
            foreach (Timeline timeline in layoutStoryboard.Children)
            {
                FrameworkElement target = (FrameworkElement)GetTimelineTarget(control, templateRoot, timeline);

                if (target != null)
                {
                    if (!targets.Contains(target))
                    {
                        targets.Add(target);
                    }

                    if (ChildAffectingLayoutProperties.Contains(LayoutPropertyFromTimeline(timeline, false)))
                    {
                        Panel panel = target as Panel;
                        if (panel != null)
                        {
                            foreach (FrameworkElement child in panel.Children)
                            {
                                if (!targets.Contains(child) && !(child is WrapperCanvas))
                                {
                                    targets.Add(child);
                                }
                            }
                        }
                    }
                }
            }

            // and any elements that were animated in previous states
            foreach (OriginalLayoutValueRecord originalValueRecord in originalValueRecords)
            {
                if (!targets.Contains(originalValueRecord.Element))
                {
                    targets.Add(originalValueRecord.Element);
                }

                if (ChildAffectingLayoutProperties.Contains(originalValueRecord.Property))
                {
                    Panel panel = originalValueRecord.Element as Panel;
                    if (panel != null)
                    {
                        foreach (FrameworkElement child in panel.Children)
                        {
                            if (!targets.Contains(child) && !(child is WrapperCanvas))
                            {
                                targets.Add(child);
                            }
                        }
                    }
                }
            }

            // Now do the expansion. Note that targets.Count increases as we move through the list.
            for (int i = 0; i < targets.Count; i++)
            {
                FrameworkElement target = targets[i];
                FrameworkElement parent = VisualTreeHelper.GetParent(target) as FrameworkElement;

                if (movingElements != null && movingElements.Contains(target) && parent is WrapperCanvas)
                {
                    // If this element is being moved currently, then we've wrapped it in a Canvas.
                    // We don't want to track the Canvas wrapper, so parent up once more.
                    parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
                }

                // If we found a parent, then we can add that parent and all siblings
                if (parent != null)
                {
                    if (!targets.Contains(parent))
                    {
                        targets.Add(parent);
                    }

                    for (int j = 0; j < VisualTreeHelper.GetChildrenCount(parent); j++)
                    {
                        FrameworkElement sibling = VisualTreeHelper.GetChild(parent, j) as FrameworkElement;

                        if (sibling != null && !targets.Contains(sibling) && !(sibling is WrapperCanvas))
                        {
                            targets.Add(sibling);
                        }
                    }
                }
            }

            return targets;
        }

        private static object GetTimelineTarget(FrameworkElement control, FrameworkElement templateRoot, Timeline timeline)
        {
            string targetName = Storyboard.GetTargetName(timeline);
            if (string.IsNullOrEmpty(targetName))
            {
                return null;
            }
            if (control is UserControl)
            {
                return control.FindName(targetName);
            }
            else
            {
                return templateRoot.FindName(targetName);
            }
        }

        /// <summary>
        /// Gets a set of rectangles for all the elements in the target list.
        /// </summary>
        /// <param name="targets">The set of elements to consider.</param>
        /// <param name="movingElements">The set of elements currently in motion.</param>
        /// <returns>A Dictionary mapping elements to their Rects.</returns>
        private static Dictionary<FrameworkElement, Rect> GetRectsOfTargets(List<FrameworkElement> targets, List<FrameworkElement> movingElements)
        {
            Dictionary<FrameworkElement, Rect> rects = new Dictionary<FrameworkElement, Rect>();

            foreach (FrameworkElement target in targets)
            {
                Rect rect;

                if (movingElements != null && movingElements.Contains(target) && (target.Parent is WrapperCanvas))
                {
                    // If this element is being moved currently, then we've wrapped it in a Canvas.
                    // Incorporate the Canvas into the layout calculations
                    WrapperCanvas parentCanvas = target.Parent as WrapperCanvas;
                    rect = GetLayoutRect(parentCanvas);
                    TranslateTransform renderTransform = parentCanvas.RenderTransform as TranslateTransform;
                    double left = Canvas.GetLeft(target);
                    double top = Canvas.GetTop(target);
                    rect = new Rect(rect.Left + (double.IsNaN(left) ? 0 : left) + (renderTransform == null ? 0 : renderTransform.X), rect.Top + (double.IsNaN(top) ? 0 : top) + (renderTransform == null ? 0 : renderTransform.Y), target.ActualWidth, target.ActualHeight);
                }
                else
                {
                    rect = GetLayoutRect(target);
                }
                rects.Add(target, rect);
            }

            return rects;
        }

        /// <summary>
        /// Get the layout rectangle of an element, by getting the layout slot and then computing which portion of the slot is being used.
        /// </summary>
        /// <param name="element">The element whose layout Rect will be retrieved.</param>
        /// <returns>The layout Rect of that element.</returns>
        internal static Rect GetLayoutRect(FrameworkElement element)
        {
            double actualWidth = element.ActualWidth;
            double actualHeight = element.ActualHeight;

            // Use RenderSize here because that works for SL Image and MediaElement - the other uses fo ActualWidth/Height are correct even for these element types
            if ((element is Image || element is MediaElement))
            {
                if (element.Parent is Canvas)
                {
                    actualWidth = double.IsNaN(element.Width) ? actualWidth : element.Width;
                    actualHeight = double.IsNaN(element.Height) ? actualHeight : element.Height;
                }
                else
                {
                    actualWidth = element.RenderSize.Width;
                    actualHeight = element.RenderSize.Height;
                }
            }

            actualWidth = element.Visibility == Visibility.Collapsed ? 0 : actualWidth;
            actualHeight = element.Visibility == Visibility.Collapsed ? 0 : actualHeight;
            Thickness margin = element.Margin;

            Rect slotRect = LayoutInformation.GetLayoutSlot(element);

            double left = 0.0;
            double top = 0.0;

            switch (element.HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    left = slotRect.Left + margin.Left;
                    break;

                case HorizontalAlignment.Center:
                    left = ((((slotRect.Left + margin.Left) + slotRect.Right) - margin.Right) / 2.0) - (actualWidth / 2.0);
                    break;

                case HorizontalAlignment.Right:
                    left = (slotRect.Right - margin.Right) - actualWidth;
                    break;

                case HorizontalAlignment.Stretch:
                    left = Math.Max((double)(slotRect.Left + margin.Left), (double)(((((slotRect.Left + margin.Left) + slotRect.Right) - margin.Right) / 2.0) - (actualWidth / 2.0)));
                    break;
            }

            switch (element.VerticalAlignment)
            {
                case VerticalAlignment.Top:
                    top = slotRect.Top + margin.Top;
                    break;

                case VerticalAlignment.Center:
                    top = ((((slotRect.Top + margin.Top) + slotRect.Bottom) - margin.Bottom) / 2.0) - (actualHeight / 2.0);
                    break;

                case VerticalAlignment.Bottom:
                    top = (slotRect.Bottom - margin.Bottom) - actualHeight;
                    break;

                case VerticalAlignment.Stretch:
                    top = Math.Max((double)(slotRect.Top + margin.Top), (double)(((((slotRect.Top + margin.Top) + slotRect.Bottom) - margin.Bottom) / 2.0) - (actualHeight / 2.0)));
                    break;
            }

            return new Rect(left, top, actualWidth, actualHeight);
        }

        /// <summary>
        /// Get the opacities of elements at the time of the state change, instead of visibilities, because the state change may be in process and the current value is the most important.
        /// </summary>
        /// <param name="control">The control whose state is changing.</param>
        /// <param name="layoutStoryboard">The storyboard with the layout properties.</param>
        /// <param name="originalValueRecords">The set of original values.</param>
        /// <returns></returns>
        private static Dictionary<FrameworkElement, double> GetOldOpacities(FrameworkElement control, FrameworkElement templateRoot, Storyboard layoutStoryboard, List<OriginalLayoutValueRecord> originalValueRecords, List<FrameworkElement> movingElements)
        {
            Dictionary<FrameworkElement, double> oldOpacities = new Dictionary<FrameworkElement, double>();

            // first walk through any elements currently in motion - note that this may include elements from an orthogonal stategroup
            if (movingElements != null)
            {
                foreach (FrameworkElement movingElement in movingElements)
                {
                    WrapperCanvas wrapper = movingElement.Parent as WrapperCanvas;

                    if (wrapper != null)
                    {
                        oldOpacities.Add(movingElement, wrapper.Opacity);
                    }
                }
            }

            // then walk through the elements whose visibility we'll be replacing
            for (int i = originalValueRecords.Count - 1; i >= 0; i--)
            {
                OriginalLayoutValueRecord originalValueRecord = originalValueRecords[i];
                if (IsVisibilityProperty(originalValueRecord.Property))
                {
                    double oldOpacity;

                    if (!oldOpacities.TryGetValue(originalValueRecord.Element, out oldOpacity))
                    {
                        oldOpacity = ((Visibility)(originalValueRecord.Element.GetValue(originalValueRecord.Property)) == Visibility.Visible ? 1.0 : 0.0);
                        oldOpacities.Add(originalValueRecord.Element, oldOpacity);
                    }
                }
            }

            // now look at the storyboard itself
            foreach (Timeline timeline in layoutStoryboard.Children)
            {
                FrameworkElement target = (FrameworkElement)GetTimelineTarget(control, templateRoot, timeline);

                DependencyProperty property = LayoutPropertyFromTimeline(timeline, true);

                if (target != null && IsVisibilityProperty(property))
                {
                    double oldOpacity;
                    if (!oldOpacities.TryGetValue(target, out oldOpacity))
                    {
                        oldOpacity = ((Visibility)target.GetValue(property) == Visibility.Visible ? 1.0 : 0.0);
                        oldOpacities.Add(target, oldOpacity);
                    }
                }
            }

            return oldOpacities;
        }

        /// <summary>
        /// Go through the layout Storyboard and set all the properties by using SetValue to enable calling UpdateLayout without
        /// ticking the timeline, which would cause a render.
        /// All values that are overwritten will be stored in the collection of OriginalValueRecords so that they can be replaced later.
        /// </summary>
        /// <param name="control">The control whose state is changing.</param>
        /// <param name="layoutStoryboard">The Storyboard holding the layout properties.</param>
        /// <param name="originalValueRecords">The store of original values.</param>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is done in a single pass for performance reasons.")]
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "This is done in a single pass for performance reasons.")]
        private static void SetLayoutStoryboardProperties(FrameworkElement control, FrameworkElement templateRoot, Storyboard layoutStoryboard, List<OriginalLayoutValueRecord> originalValueRecords)
        {
            // Restore all the values first - not strictly necessary, but likely just as fast as computing which records don't appear in the current state.
            foreach (OriginalLayoutValueRecord originalValueRecord in originalValueRecords)
            {
                ReplaceCachedLocalValueHelper(originalValueRecord.Element, originalValueRecord.Property, originalValueRecord.Value);
            }
            originalValueRecords.Clear();

            // Now, set all the values
            foreach (Timeline timeline in layoutStoryboard.Children)
            {
                FrameworkElement target = (FrameworkElement)GetTimelineTarget(control, templateRoot, timeline);
                DependencyProperty property = LayoutPropertyFromTimeline(timeline, true);

                if (target != null && property != null)
                {
                    object value;
                    bool gotValue;

                    value = GetValueFromTimeline(timeline, out gotValue);

                    // If we found a useful value, store the original value and set the new one
                    if (gotValue)
                    {
                        originalValueRecords.Add(new OriginalLayoutValueRecord { Element = target, Property = property, Value = CacheLocalValueHelper(target, property) });
                        target.SetValue(property, value);
                    }
                }
            }
        }

        private static object GetValueFromTimeline(Timeline timeline, out bool gotValue)
        {
            ObjectAnimationUsingKeyFrames objectAnimationUsingKeyFrames = timeline as ObjectAnimationUsingKeyFrames;
            if (objectAnimationUsingKeyFrames != null)
            {
                gotValue = true;
                return objectAnimationUsingKeyFrames.KeyFrames[0].Value;
            }
            else
            {
                DoubleAnimationUsingKeyFrames doubleAnimationUsingKeyFrames = timeline as DoubleAnimationUsingKeyFrames;
                if (doubleAnimationUsingKeyFrames != null)
                {
                    gotValue = true;
                    return doubleAnimationUsingKeyFrames.KeyFrames[0].Value;
                }
                else
                {
                    DoubleAnimation doubleAnimation = timeline as DoubleAnimation;
                    if (doubleAnimation != null)
                    {
                        gotValue = true;
                        return doubleAnimation.To;
                    }
                    else
                    {
                        ThicknessAnimationUsingKeyFrames thicknessAnimationUsingKeyFrames = timeline as ThicknessAnimationUsingKeyFrames;
                        if (thicknessAnimationUsingKeyFrames != null)
                        {
                            gotValue = true;
                            return thicknessAnimationUsingKeyFrames.KeyFrames[0].Value;
                        }
                        else
                        {
                            ThicknessAnimation thicknessAnimation = timeline as ThicknessAnimation;
                            if (thicknessAnimation != null)
                            {
                                gotValue = true;
                                return thicknessAnimation.To;
                            }
                            else
                            {
                                Int32AnimationUsingKeyFrames int32AnimationUsingKeyFrames = timeline as Int32AnimationUsingKeyFrames;
                                if (int32AnimationUsingKeyFrames != null)
                                {
                                    gotValue = true;
                                    return int32AnimationUsingKeyFrames.KeyFrames[0].Value;
                                }
                                else
                                {
                                    Int32Animation int32Animation = timeline as Int32Animation;
                                    if (int32Animation != null)
                                    {
                                        gotValue = true;
                                        return int32Animation.To;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            gotValue = false;
            return null;
        }

        /// <summary>
        /// Take all the elements that will be moving as a result of the layout animation, and wrap them in Canvas panels so that
        /// they do not affect their sibling elements.
        /// </summary>
        /// <param name="movingElements">The set of elements that will be moving.</param>
        private static void WrapMovingElementsInCanvases(List<FrameworkElement> movingElements, Dictionary<FrameworkElement, Rect> oldRects, Dictionary<FrameworkElement, Rect> newRects)
        {
            foreach (FrameworkElement movedElement in movingElements)
            {
                FrameworkElement parent = VisualTreeHelper.GetParent(movedElement) as FrameworkElement;
                WrapperCanvas parentCanvas = new WrapperCanvas();
                parentCanvas.OldRect = oldRects[movedElement];
                parentCanvas.NewRect = newRects[movedElement];

                // Force the current DataContext directly on the parent to keep any rebuild of children from happening during these transitory reparenting steps
                object localDataContext = CacheLocalValueHelper(movedElement, FrameworkElement.DataContextProperty);
                movedElement.DataContext = movedElement.DataContext;

                //parentCanvas.Background = new SolidColorBrush(Color.FromArgb(32, 255, 0, 0));

                bool addedCanvas = true;

                Panel panel = parent as Panel;
                if (panel != null && !panel.IsItemsHost)
                {
                    int index = panel.Children.IndexOf(movedElement);
                    panel.Children.RemoveAt(index);
                    panel.Children.Insert(index, parentCanvas);
                }
                else
                {
                    Decorator decorator = parent as Decorator;
                    if (decorator != null)
                    {
                        decorator.Child = parentCanvas;
                    }
                    else
                    {
                        addedCanvas = false;
                    }
                }

                if (addedCanvas)
                {
                    parentCanvas.Children.Add(movedElement);

                    CopyLayoutProperties(movedElement, parentCanvas, false);

                    // Take the forced DataContext off so it can be updated later by someone else
                    ReplaceCachedLocalValueHelper(movedElement, FrameworkElement.DataContextProperty, localDataContext);
                }
            }
        }

        /// <summary>
        /// Take all the elements that have been moving as a result of the layout animation, and unwrap them from their Canvas panels.
        /// </summary>
        /// <param name="movingElements">The set of elements that have been moving.</param>
        private static void UnwrapMovingElementsFromCanvases(List<FrameworkElement> movingElements)
        {
            foreach (FrameworkElement movedElement in movingElements)
            {
                WrapperCanvas parentCanvas = movedElement.Parent as WrapperCanvas;

                if (parentCanvas == null)
                {
                    continue;
                }

                // Force the current DataContext directly on the parent to keep any rebuild of children from happening during these transitory reparenting steps
                object localDataContext = CacheLocalValueHelper(movedElement, FrameworkElement.DataContextProperty);
                movedElement.DataContext = movedElement.DataContext;

                FrameworkElement parent = VisualTreeHelper.GetParent(parentCanvas) as FrameworkElement;

                parentCanvas.Children.Remove(movedElement);

                Panel panel = parent as Panel;
                if (panel != null)
                {
                    int index = panel.Children.IndexOf(parentCanvas);
                    panel.Children.RemoveAt(index);
                    panel.Children.Insert(index, movedElement);
                }
                else
                {
                    Decorator decorator = parent as Decorator;
                    if (decorator != null)
                    {
                        decorator.Child = movedElement;
                    }
                }

                CopyLayoutProperties(parentCanvas, movedElement, true);

                // Take the forced DataContext off so it can be updated later by someone else
                ReplaceCachedLocalValueHelper(movedElement, FrameworkElement.DataContextProperty, localDataContext);
            }
        }

        /// <summary>
        /// Copy the layout properties from the source element to the target element, clearing them from the source.
        /// </summary>
        /// <param name="source">The source of the layout properties.</param>
        /// <param name="target">The destination of the layout properties.</param>
        private static void CopyLayoutProperties(FrameworkElement source, FrameworkElement target, bool restoring)
        {
            WrapperCanvas parentCanvas = (restoring ? source : target) as WrapperCanvas;

            if (parentCanvas.LocalValueCache == null)
            {
                parentCanvas.LocalValueCache = new Dictionary<DependencyProperty, object>();
            }

            foreach (DependencyProperty property in LayoutProperties)
            {
                if (!ChildAffectingLayoutProperties.Contains(property))
                {
                    if (restoring)
                    {
                        // restore is easy - replace local values from the cache we built earlier
                        ReplaceCachedLocalValueHelper(target, property, parentCanvas.LocalValueCache[property]);
                    }
                    else
                    {
                        // store is harder, because we want to cache both the local value (which may be a binding)
                        // and the computed value (because we need the object to behave as an appropriate stand-in for the original object).

                        // We'll also be clearing the value from the source; since the target is a new object, it has a useful copy of the
                        // default value already. Get it. Note that we can't just call ClearValue() because in WPF that means to replace with
                        // the template default.
                        object defaultValue = target.GetValue(property);

                        // cache the local value
                        object localValue = CacheLocalValueHelper(source, property);
                        parentCanvas.LocalValueCache[property] = localValue;

                        // copy the current computed value. Note that we move Visibility to a shadow property because we want the Visibility
                        // of the wrapper to always be Visible so that any opacity animation can be seen.
                        if (IsVisibilityProperty(property))
                        {
                            parentCanvas.DestinationVisibilityCache = (Visibility)source.GetValue(property);
                        }
                        else
                        {
                            target.SetValue(property, source.GetValue(property));
                        }

                        // This is better than ClearValue - ClearValue will replace the template defaults on WPF
                        source.SetValue(property, defaultValue);
                    }
                }
            }
        }

        /// <summary>
        /// Create the actual Storyboard that will be used to animate the transition. Use all previously calculated results.
        /// </summary>
        /// <param name="duration">The duration of the animation.</param>
        /// <param name="ease">The easing function to be used in the animation.</param>
        /// <param name="movingElements">The set of elements that will be moving.</param>
        /// <param name="oldOpacities">The old opacities of the elements whose visibility properties are changing.</param>
        /// <returns>The Storyboard.</returns>
        private static Storyboard CreateLayoutTransitionStoryboard(VisualTransition transition, List<FrameworkElement> movingElements, Dictionary<FrameworkElement, double> oldOpacities)
        {
            Duration duration = transition != null ? transition.GeneratedDuration : new Duration(TimeSpan.Zero);
            IEasingFunction ease = transition != null ? transition.GeneratedEasingFunction : null;

            Storyboard layoutTransitionStoryboard = new Storyboard();
            layoutTransitionStoryboard.Duration = duration;

            // Handle the positions of elements that are on the move
            foreach (FrameworkElement movingElement in movingElements)
            {
                WrapperCanvas parentCanvas = movingElement.Parent as WrapperCanvas;

                if (parentCanvas == null)
                {
                    continue;
                }

                {
                    DoubleAnimation animation = new DoubleAnimation() { From = 1, To = 0, Duration = duration };
                    animation.EasingFunction = ease;
                    Storyboard.SetTarget(animation, parentCanvas);
                    Storyboard.SetTargetProperty(animation, new PropertyPath(WrapperCanvas.SimulationProgressProperty));
                    layoutTransitionStoryboard.Children.Add(animation);

                    // prime the pump
                    parentCanvas.SimulationProgress = 1;
                }

                Rect newRect = parentCanvas.NewRect;

                // Do-nothing animation to override the width/height of the Canvas without stomping the values, so that the Canvas unwrap will replace the right ones
                // Needed in some cases because the width/height may be Auto and the Canvas would shrink to zero size
                if (!IsClose(parentCanvas.Width, newRect.Width))
                {
                    DoubleAnimation animation = new DoubleAnimation() { From = newRect.Width, To = newRect.Width, Duration = duration };
                    Storyboard.SetTarget(animation, parentCanvas);
                    Storyboard.SetTargetProperty(animation, new PropertyPath(FrameworkElement.WidthProperty));
                    layoutTransitionStoryboard.Children.Add(animation);
                }

                if (!IsClose(parentCanvas.Height, newRect.Height))
                {
                    DoubleAnimation animation = new DoubleAnimation() { From = newRect.Height, To = newRect.Height, Duration = duration };
                    Storyboard.SetTarget(animation, parentCanvas);
                    Storyboard.SetTargetProperty(animation, new PropertyPath(FrameworkElement.HeightProperty));
                    layoutTransitionStoryboard.Children.Add(animation);
                }

                if (parentCanvas.DestinationVisibilityCache == Visibility.Collapsed)
                {
                    Thickness margin = parentCanvas.Margin;

                    if (!IsClose(margin.Left, 0) || !IsClose(margin.Top, 0) || !IsClose(margin.Right, 0) || !IsClose(margin.Bottom, 0))
                    {
                        ObjectAnimationUsingKeyFrames animation = new ObjectAnimationUsingKeyFrames() { Duration = duration };
                        DiscreteObjectKeyFrame keyFrame = new DiscreteObjectKeyFrame() { KeyTime = TimeSpan.Zero, Value = new Thickness() };
                        animation.KeyFrames.Add(keyFrame);
                        Storyboard.SetTarget(animation, parentCanvas);
                        Storyboard.SetTargetProperty(animation, new PropertyPath(FrameworkElement.MarginProperty));
                        layoutTransitionStoryboard.Children.Add(animation);
                    }

                    if (!IsClose(parentCanvas.MinWidth, 0))
                    {
                        DoubleAnimation animation = new DoubleAnimation() { From = 0, To = 0, Duration = duration };
                        Storyboard.SetTarget(animation, parentCanvas);
                        Storyboard.SetTargetProperty(animation, new PropertyPath(FrameworkElement.MinWidthProperty));
                        layoutTransitionStoryboard.Children.Add(animation);
                    }

                    if (!IsClose(parentCanvas.MinHeight, 0))
                    {
                        DoubleAnimation animation = new DoubleAnimation() { From = 0, To = 0, Duration = duration };
                        Storyboard.SetTarget(animation, parentCanvas);
                        Storyboard.SetTargetProperty(animation, new PropertyPath(FrameworkElement.MinHeightProperty));
                        layoutTransitionStoryboard.Children.Add(animation);
                    }
                }
            }

            // Handle the opacity/visibility of elements that are changing visibility
            foreach (FrameworkElement visibilityElement in oldOpacities.Keys)
            {
                WrapperCanvas parentCanvas = visibilityElement.Parent as WrapperCanvas;

                if (parentCanvas == null)
                {
                    continue;
                }

                double oldOpacity = oldOpacities[visibilityElement];
                double newOpacity = (parentCanvas.DestinationVisibilityCache == Visibility.Visible ? 1.0 : 0.0);

                // Animate the opacity to smooth out the visibility change
                if (!IsClose(oldOpacity, 1) || !IsClose(newOpacity, 1))
                {
                    DoubleAnimation animation = new DoubleAnimation() { From = oldOpacity, To = newOpacity, Duration = duration };
                    animation.EasingFunction = ease;
                    Storyboard.SetTarget(animation, parentCanvas);
                    Storyboard.SetTargetProperty(animation, new PropertyPath(FrameworkElement.OpacityProperty));
                    layoutTransitionStoryboard.Children.Add(animation);
                }
            }

            return layoutTransitionStoryboard;
        }

        private static void TransferLocalValue(FrameworkElement element, DependencyProperty sourceProperty, DependencyProperty destProperty)
        {
            object localValue = CacheLocalValueHelper(element, sourceProperty);
            ReplaceCachedLocalValueHelper(element, destProperty, localValue);
        }

        private static object CacheLocalValueHelper(DependencyObject dependencyObject, DependencyProperty property)
        {
            return dependencyObject.ReadLocalValue(property);
        }

        private static void ReplaceCachedLocalValueHelper(FrameworkElement element, DependencyProperty property, object value)
        {
            if (value == DependencyProperty.UnsetValue)
            {
                element.ClearValue(property);
                return;
            }

            BindingExpressionBase bindingExpressionBase = value as BindingExpressionBase;
            if (bindingExpressionBase != null)
            {
                element.SetBinding(property, bindingExpressionBase.ParentBindingBase);
            }
            else
            {
                element.SetValue(property, value);
            }
        }

        private static bool IsClose(double a, double b)
        {
            return (Math.Abs((double)(a - b)) < 1E-07);
        }
        #endregion
    }
}
