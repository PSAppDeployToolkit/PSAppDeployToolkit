// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors.Layout
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Shapes;
    using Microsoft.Xaml.Behaviors.Core;
    using Microsoft.Xaml.Behaviors;

    /// <summary>
    /// This enumerated type indicates whether a FluidMoveBehavior applies to the element to which it is attached, or to the children of that element.
    /// "Self" is useful when there is a single element that should behave in a special manner; "Children" is useful when the same behavior should apply to all
    /// children of a WrapPanel or to the ItemsHost panel of an ItemsControl.
    /// </summary>
    public enum FluidMoveScope
    {
        Self,
        Children
    }

    /// <summary>
    /// This enumerated type indicates whether an element is identified by itself, or by its DataContext.
    /// DataContext identification allows movement from one data-driven location to another.
    /// </summary>
    public enum TagType
    {
        Element,
        DataContext
    }

    public abstract class FluidMoveBehaviorBase : Behavior<FrameworkElement>
    {
        /// <summary>
        /// Indicates whether the behavior applies just to this element, or to all children of the element (if the element is a Panel).
        /// </summary>
        public FluidMoveScope AppliesTo
        {
            get { return (FluidMoveScope)this.GetValue(AppliesToProperty); }
            set { this.SetValue(AppliesToProperty, value); }
        }
        /// <summary>
        /// Dependency property for the scope of the behavior. See FluidMoveScope for more details.
        /// </summary>
        public static readonly DependencyProperty AppliesToProperty = DependencyProperty.Register("AppliesTo", typeof(FluidMoveScope), typeof(FluidMoveBehaviorBase), new PropertyMetadata(FluidMoveScope.Self));

        /// <summary>
        /// Indicates whether the behavior is currently active.
        /// </summary>
        public bool IsActive
        {
            get { return (bool)this.GetValue(IsActiveProperty); }
            set { this.SetValue(IsActiveProperty, value); }
        }
        /// <summary>
        /// Dependency property for the active state of the behavior.
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register("IsActive", typeof(bool), typeof(FluidMoveBehaviorBase), new PropertyMetadata(true));

        /// <summary>
        /// Indicates whether to use the element as its own tag, or to use the binding on the element as the tag.
        /// </summary>
        public TagType Tag
        {
            get { return (TagType)this.GetValue(TagProperty); }
            set { this.SetValue(TagProperty, value); }
        }
        /// <summary>
        /// Dependency property that provides the ability to use the element as its own tag, or the binding on the element.
        /// </summary>
        public static readonly DependencyProperty TagProperty = DependencyProperty.Register("Tag", typeof(TagType), typeof(FluidMoveBehaviorBase), new PropertyMetadata(TagType.Element));

        /// <summary>
        /// Extra path to add to the binding when TagType is specified.
        /// </summary>
        public string TagPath
        {
            get { return (string)this.GetValue(TagPathProperty); }
            set { this.SetValue(TagPathProperty, value); }
        }
        /// <summary>
        /// Dependency property for the extra path to add to the binding when UsaBindingAsTag is true.
        /// </summary>
        public static readonly DependencyProperty TagPathProperty = DependencyProperty.Register("TagPath", typeof(string), typeof(FluidMoveBehaviorBase), new PropertyMetadata(String.Empty));

        /// <summary>
        /// Identity tag used to detect element motion between containers.
        /// </summary>
        protected static readonly DependencyProperty IdentityTagProperty = DependencyProperty.RegisterAttached("IdentityTag", typeof(object), typeof(FluidMoveBehaviorBase), new PropertyMetadata(null));
        protected static object GetIdentityTag(DependencyObject obj) { return obj.GetValue(IdentityTagProperty); }
        protected static void SetIdentityTag(DependencyObject obj, object value) { obj.SetValue(IdentityTagProperty, value); }

        /// <summary>
        /// Private structure that stores all relevant data pertaining to a tagged item.
        /// </summary>
        internal class TagData
        {
            public FrameworkElement Child { get; set; } // the element
            public FrameworkElement Parent { get; set; }// the parent
            public Rect ParentRect { get; set; }        // the parent-relative rect
            public Rect AppRect { get; set; }           // the app-relative rect
            public DateTime Timestamp { get; set; }     // the last time we saw the element
            public object InitialTag { get; set; }      // the tag to spawn from
        }

        internal static Dictionary<object, TagData> TagDictionary = new Dictionary<object, TagData>();

        // timer data to help purge objects we should no longer be tracking
        private static DateTime nextToLastPurgeTick = DateTime.MinValue;
        private static DateTime lastPurgeTick = DateTime.MinValue;
        private static TimeSpan minTickDelta = TimeSpan.FromSeconds(0.5);

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.LayoutUpdated += this.AssociatedObject_LayoutUpdated;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.LayoutUpdated -= this.AssociatedObject_LayoutUpdated;
        }

        private void AssociatedObject_LayoutUpdated(object sender, EventArgs e)
        {
            if (!this.IsActive)
            {
                return;
            }

            // if it's been long enough since our last purge, then let's kick one off. Since we can't control how often layout runs, and some
            // objects could reappear on the very next layout pass, we'll purge any tag who hasn't been seen since the purge tick before that.
            //
            // If we got a notification when elements were deleted, we would maintain a far shorter list of tags whose FEs were deleted since the last purge.
            // 
            // We might also be able to use a WeakReference solution here, but this one is pretty cheap as it only runs when Layout is running anyway.
            if (DateTime.Now - lastPurgeTick >= minTickDelta)
            {
                List<object> deadTags = null;

                foreach (KeyValuePair<object, TagData> pair in TagDictionary)
                {
                    if (pair.Value.Timestamp < nextToLastPurgeTick)
                    {
                        if (deadTags == null)
                        {
                            deadTags = new List<object>();
                        }
                        deadTags.Add(pair.Key);
                    }
                }

                if (deadTags != null)
                {
                    foreach (object tag in deadTags)
                    {
                        TagDictionary.Remove(tag);
                    }
                }

                nextToLastPurgeTick = lastPurgeTick;
                lastPurgeTick = DateTime.Now;
            }

            if (this.AppliesTo == FluidMoveScope.Self)
            {
                this.UpdateLayoutTransition(this.AssociatedObject);
            }
            else
            {
                Panel panel = this.AssociatedObject as Panel;
                if (panel != null)
                {
                    foreach (FrameworkElement child in panel.Children)
                    {
                        this.UpdateLayoutTransition(child);
                    }
                }
            }
        }

        private void UpdateLayoutTransition(FrameworkElement child)
        {
            if (child.Visibility == Visibility.Collapsed || !child.IsLoaded)
            {
                if (this.ShouldSkipInitialLayout)
                {
                    return;
                }
            }

            FrameworkElement root = GetVisualRoot(child);

            TagData newTagData = new TagData();
            newTagData.Parent = VisualTreeHelper.GetParent(child) as FrameworkElement;
            newTagData.ParentRect = ExtendedVisualStateManager.GetLayoutRect(child);
            newTagData.Child = child;
            newTagData.Timestamp = DateTime.Now;

            try
            {
                newTagData.AppRect = TranslateRect(newTagData.ParentRect, newTagData.Parent, root);
            }
            catch (System.ArgumentException)
            {
                if (this.ShouldSkipInitialLayout)
                {
                    return;
                }
            }

            this.EnsureTags(child);

            // Now, get the tag for the element. If there is no tag, the element is its own tag.
            object tag = GetIdentityTag(child);
            if (tag == null)
            {
                tag = child;
            }

            this.UpdateLayoutTransitionCore(child, root, tag, newTagData);
        }

        protected virtual bool ShouldSkipInitialLayout
        {
            get { return (this.Tag == TagType.DataContext); }
        }

        internal abstract void UpdateLayoutTransitionCore(FrameworkElement child, FrameworkElement root, object tag, TagData newTagData);

        protected virtual void EnsureTags(FrameworkElement child)
        {
            // If we are going to use a binding for the tag, make sure we have one set up.
            if (this.Tag == TagType.DataContext)
            {
                object tagValue = child.ReadLocalValue(IdentityTagProperty);
                if (!(tagValue is BindingExpression))
                {
                    child.SetBinding(IdentityTagProperty, new Binding(this.TagPath));
                }
            }
        }

        // Gets the visual root of an element, be it the RootVisual, Popup, what have you.
        private static FrameworkElement GetVisualRoot(FrameworkElement child)
        {
            while (true)
            {
                FrameworkElement parent = VisualTreeHelper.GetParent(child) as FrameworkElement;
                if (parent == null)
                {
                    return child;
                }
                // The WPF floating solution relies on the AdornerLayer - we have to make sure that is still available
                if (System.Windows.Documents.AdornerLayer.GetAdornerLayer(parent) == null)
                {
                    return child;
                }
                child = parent;
            }
        }

        // Helper function to translate a rect from one coordinate system to another.
        internal static Rect TranslateRect(Rect rect, FrameworkElement from, FrameworkElement to)
        {
            if (from == null || to == null)
            {
                return rect;
            }

            Point point = new Point(rect.Left, rect.Top);
            point = from.TransformToVisual(to).Transform(point);
            return new Rect(point.X, point.Y, rect.Width, rect.Height);
        }
    }

    public sealed class FluidMoveSetTagBehavior : FluidMoveBehaviorBase
    {
        internal override void UpdateLayoutTransitionCore(FrameworkElement child, FrameworkElement root, object tag, TagData newTagData)
        {
            TagData tagData;
            bool gotData = TagDictionary.TryGetValue(tag, out tagData);

            if (!gotData)
            {
                tagData = new TagData();
                TagDictionary.Add(tag, tagData);
            }

            tagData.ParentRect = newTagData.ParentRect;
            tagData.AppRect = newTagData.AppRect;
            tagData.Parent = newTagData.Parent;
            tagData.Child = newTagData.Child;
            tagData.Timestamp = newTagData.Timestamp;
        }
    }

    /// <summary>
    /// Behavior that watches an element (or a set of elements) for layout changes, and moves the element smoothly to the new position when needed.
    /// This behavior does not animate the size or visibility of an element; it only animates the offset of that element within its parent container.
    /// </summary>
    public sealed class FluidMoveBehavior : FluidMoveBehaviorBase
    {
        /// <summary>
        /// The duration of the move.
        /// </summary>
        public Duration Duration
        {
            get { return (Duration)this.GetValue(DurationProperty); }
            set { this.SetValue(DurationProperty, value); }
        }
        /// <summary>
        /// Dependency property for the duration of the move.
        /// </summary>
        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register("Duration", typeof(Duration), typeof(FluidMoveBehavior), new PropertyMetadata(new Duration(TimeSpan.FromSeconds(1.0))));

        /// <summary>
        /// Spawning point for this item.
        /// </summary>
        public TagType InitialTag
        {
            get { return (TagType)this.GetValue(InitialTagProperty); }
            set { this.SetValue(InitialTagProperty, value); }
        }
        /// <summary>
        /// Dependency property for the tag type to use just before the object is loaded.
        /// </summary>
        public static readonly DependencyProperty InitialTagProperty = DependencyProperty.Register("InitialTag", typeof(TagType), typeof(FluidMoveBehavior), new PropertyMetadata(TagType.Element));

        /// <summary>
        /// Extra path to add to the binding when TagType is specified.
        /// </summary>
        public string InitialTagPath
        {
            get { return (string)this.GetValue(InitialTagPathProperty); }
            set { this.SetValue(InitialTagPathProperty, value); }
        }
        /// <summary>
        /// Dependency property for the extra path to add to the binding when UsaBindingAsTag is true.
        /// </summary>
        public static readonly DependencyProperty InitialTagPathProperty = DependencyProperty.Register("InitialTagPath", typeof(string), typeof(FluidMoveBehavior), new PropertyMetadata(String.Empty));

        /// <summary>
        /// Identity tag used to detect element motion between containers.
        /// </summary>

        private static readonly DependencyProperty initialIdentityTagProperty = DependencyProperty.RegisterAttached("InitialIdentityTag", typeof(object), typeof(FluidMoveBehavior), new PropertyMetadata(null));
        private static object GetInitialIdentityTag(DependencyObject obj) { return obj.GetValue(initialIdentityTagProperty); }
        private static void SetInitialIdentityTag(DependencyObject obj, object value) { obj.SetValue(initialIdentityTagProperty, value); }

        /// <summary>
        /// Flag that says whether elements are allowed to float above their containers (in a Popup or Adorner) when changing containers.
        /// </summary>
        public bool FloatAbove
        {
            get { return (bool)this.GetValue(FloatAboveProperty); }
            set { this.SetValue(FloatAboveProperty, value); }
        }
        /// <summary>
        /// Dependency property for the FloatAbove flag.
        /// </summary>
        public static readonly DependencyProperty FloatAboveProperty = DependencyProperty.Register("FloatAbove", typeof(bool), typeof(FluidMoveBehavior), new PropertyMetadata(true));

        /// <summary>
        /// EasingFunction to use for the horizontal component of the move.
        /// </summary>
        public IEasingFunction EaseX
        {
            get { return (IEasingFunction)this.GetValue(EaseXProperty); }
            set { this.SetValue(EaseXProperty, value); }
        }
        /// <summary>
        /// Dependency property for the EasingFunction to use for the horizontal component of the move.
        /// </summary>
        public static readonly DependencyProperty EaseXProperty = DependencyProperty.Register("EaseX", typeof(IEasingFunction), typeof(FluidMoveBehavior), new PropertyMetadata(null));

        /// <summary>
        /// EasingFunction to use for the vertical component of the move.
        /// </summary>
        public IEasingFunction EaseY
        {
            get { return (IEasingFunction)this.GetValue(EaseYProperty); }
            set { this.SetValue(EaseYProperty, value); }
        }
        /// <summary>
        /// Dependency property for the EasingFunction to use for the vertical component of the move.
        /// </summary>
        public static readonly DependencyProperty EaseYProperty = DependencyProperty.Register("EaseY", typeof(IEasingFunction), typeof(FluidMoveBehavior), new PropertyMetadata(null));

        /// <summary>
        /// Remember the popup/adorner being used, in case of element motion between containers when FloatAbove is true.
        /// </summary>
        private static readonly DependencyProperty overlayProperty = DependencyProperty.RegisterAttached("Overlay", typeof(object), typeof(FluidMoveBehavior), new PropertyMetadata(null));
        private static object GetOverlay(DependencyObject obj) { return obj.GetValue(overlayProperty); }
        private static void SetOverlay(DependencyObject obj, object value) { obj.SetValue(overlayProperty, value); }

        /// <summary>
        /// Opacity cache used when floating a Popup.
        /// </summary>
        private static readonly DependencyProperty cacheDuringOverlayProperty = DependencyProperty.RegisterAttached("CacheDuringOverlay", typeof(object), typeof(FluidMoveBehavior), new PropertyMetadata(null));
        private static object GetCacheDuringOverlay(DependencyObject obj) { return obj.GetValue(cacheDuringOverlayProperty); }
        private static void SetCacheDuringOverlay(DependencyObject obj, object value) { obj.SetValue(cacheDuringOverlayProperty, value); }

        /// <summary>
        /// Marks the animation transform.
        /// </summary>
        private static readonly DependencyProperty hasTransformWrapperProperty = DependencyProperty.RegisterAttached("HasTransformWrapper", typeof(bool), typeof(FluidMoveBehavior), new PropertyMetadata(false));
        private static bool GetHasTransformWrapper(DependencyObject obj) { return (bool)obj.GetValue(hasTransformWrapperProperty); }
        private static void SetHasTransformWrapper(DependencyObject obj, bool value) { obj.SetValue(hasTransformWrapperProperty, value); }

        private static Dictionary<object, Storyboard> transitionStoryboardDictionary = new Dictionary<object, Storyboard>();

        protected override bool ShouldSkipInitialLayout
        {
            get
            {
                return base.ShouldSkipInitialLayout || (this.InitialTag == TagType.DataContext);
            }
        }

        protected override void EnsureTags(FrameworkElement child)
        {
            base.EnsureTags(child);

            // If we are going to use a binding for the tag, make sure we have one set up.
            if (this.InitialTag == TagType.DataContext)
            {
                object tagValue = child.ReadLocalValue(initialIdentityTagProperty);
                if (!(tagValue is BindingExpression))
                {
                    child.SetBinding(initialIdentityTagProperty, new Binding(this.InitialTagPath));
                }
            }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Trying to keep the number of function parameters down to a minimum.")]
        internal override void UpdateLayoutTransitionCore(FrameworkElement child, FrameworkElement root, object tag, TagData newTagData)
        {
            TagData tagData;
            Rect previousRect;
            bool parentChange = false;
            bool usingBeforeLoaded = false;
            object initialTag = GetInitialIdentityTag(child);

            // Locate the previous tag, and the parent-relative previous rect. The previous rect is computed using the app-relative rect if switching parents.
            // Note that we do not use the app-relative rect all time time, because when the parent itself moves, it accounts for all the motion and we do not have to.
            bool gotData = TagDictionary.TryGetValue(tag, out tagData);

            // if spawn point has changed then throw away the old one
            if (gotData && tagData.InitialTag != initialTag)
            {
                gotData = false;
                TagDictionary.Remove(tag);
            }
            if (!gotData)
            {
                TagData spawnData;

                if (initialTag != null && TagDictionary.TryGetValue(initialTag, out spawnData))
                {
                    previousRect = TranslateRect(spawnData.AppRect, root, newTagData.Parent);
                    parentChange = true;
                    usingBeforeLoaded = true;
                }
                else
                {
                    previousRect = Rect.Empty;
                }

                tagData = new TagData() { ParentRect = Rect.Empty, AppRect = Rect.Empty, Parent = newTagData.Parent, Child = child, Timestamp = DateTime.Now, InitialTag = initialTag };
                TagDictionary.Add(tag, tagData);
            }
            else if (tagData.Parent != VisualTreeHelper.GetParent(child))
            {
                previousRect = TranslateRect(tagData.AppRect, root, newTagData.Parent);
                parentChange = true;
            }
            else
            {
                previousRect = tagData.ParentRect;
            }

            FrameworkElement originalChild = child;

            if ((!FluidMoveBehavior.IsEmptyRect(previousRect) && !FluidMoveBehavior.IsEmptyRect(newTagData.ParentRect)) && (!IsClose(previousRect.Left, newTagData.ParentRect.Left) || !IsClose(previousRect.Top, newTagData.ParentRect.Top)) ||
                (child != tagData.Child && transitionStoryboardDictionary.ContainsKey(tag)))
            {
                Rect currentRect = previousRect;
                bool forceFloatAbove = false;

                // If this element was animating before, append its current transform to the start position and kill the old animation.
                // Note that in an overlay scenario, the animation is on the image in the overlay.
                Storyboard oldTransitionStoryboard = null;
                if (transitionStoryboardDictionary.TryGetValue(tag, out oldTransitionStoryboard))
                {
                    object tagOverlay = GetOverlay(tagData.Child);
                    AdornerContainer adornerContainer = (AdornerContainer)tagOverlay;

                    forceFloatAbove = (tagOverlay != null); // if floating before, we need to keep floating
                    FrameworkElement elementWithTransform = tagData.Child;

                    if (tagOverlay != null)
                    {
                        Canvas overlayCanvas = adornerContainer.Child as Canvas;
                        if (overlayCanvas != null)
                        {
                            elementWithTransform = overlayCanvas.Children[0] as FrameworkElement;
                        }
                    }

                    // if we're picking a specific starting point, don't append this transform
                    if (!usingBeforeLoaded)
                    {
                        Transform transform = GetTransform(elementWithTransform);
                        currentRect = transform.TransformBounds(currentRect);
                    }

                    transitionStoryboardDictionary.Remove(tag);
                    oldTransitionStoryboard.Stop();
                    oldTransitionStoryboard = null;
                    RemoveTransform(elementWithTransform);

                    if (tagOverlay != null)
                    {
                        System.Windows.Documents.AdornerLayer.GetAdornerLayer(root).Remove(adornerContainer);
                        TransferLocalValue(tagData.Child, FluidMoveBehavior.cacheDuringOverlayProperty, FrameworkElement.RenderTransformProperty);
                        SetOverlay(tagData.Child, null);
                    }
                }

                object overlay = null;

                // If we need to float this element, then we have to:
                // 1. Take a picture of it
                // 2. Put that picture in an Image in a popup
                // 3. Hide the original element (opacity=0 so we do not disturb layout)
                // 4. Animate the image
                // 5. Keep track of all the info we need to unwind this later
                if (forceFloatAbove || (parentChange && this.FloatAbove))
                {
                    Canvas canvas = new Canvas() { Width = newTagData.ParentRect.Width, Height = newTagData.ParentRect.Height, IsHitTestVisible = false };

                    Rectangle rectangle = new Rectangle() { Width = newTagData.ParentRect.Width, Height = newTagData.ParentRect.Height, IsHitTestVisible = false };
                    rectangle.Fill = new VisualBrush(child);
                    canvas.Children.Add(rectangle);
                    AdornerContainer adornerContainer = new AdornerContainer(child) { Child = canvas };
                    overlay = adornerContainer;

                    // remember this overlay so we can get info from it
                    SetOverlay(originalChild, overlay);

                    System.Windows.Documents.AdornerLayer adorners = System.Windows.Documents.AdornerLayer.GetAdornerLayer(root);
                    adorners.Add(adornerContainer);

                    // Note: Not using this approach currently because the bitmap is not ready yet
                    // To remove use of VisualBrush, have to fill in bitmap after a render
                    //RenderTargetBitmap bitmap = new RenderTargetBitmap((int)child.ActualWidth, (int)child.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    //bitmap.Render(parent);
                    //image.Source = bitmap;

                    // can't animate this or it will flash, have to set the value outright
                    TransferLocalValue(child, FrameworkElement.RenderTransformProperty, FluidMoveBehavior.cacheDuringOverlayProperty);
                    child.RenderTransform = new TranslateTransform(-10000, -10000);
                    canvas.RenderTransform = new TranslateTransform(10000, 10000);

                    // change value here so that the animations will be applied to the image
                    child = rectangle;
                }

                // OK, now build the actual animation
                Rect parentRect = newTagData.ParentRect;
                Storyboard transitionStoryboard = CreateTransitionStoryboard(child, usingBeforeLoaded, ref parentRect, ref currentRect);

                // Put this storyboard in the running dictionary so we can detect reentrancy
                transitionStoryboardDictionary.Add(tag, transitionStoryboard);

                transitionStoryboard.Completed += delegate (object sender, EventArgs e)
                {
                    Storyboard currentlyRunningStoryboard;
                    if (transitionStoryboardDictionary.TryGetValue(tag, out currentlyRunningStoryboard) && currentlyRunningStoryboard == transitionStoryboard)
                    {
                        transitionStoryboardDictionary.Remove(tag);
                        transitionStoryboard.Stop();
                        RemoveTransform(child);
                        child.InvalidateMeasure();

                        if (overlay != null)
                        {
                            System.Windows.Documents.AdornerLayer.GetAdornerLayer(root).Remove((AdornerContainer)overlay);
                            TransferLocalValue(originalChild, FluidMoveBehavior.cacheDuringOverlayProperty, FrameworkElement.RenderTransformProperty);
                            SetOverlay(originalChild, null);
                        }
                    }
                };

                transitionStoryboard.Begin();
            }

            // Store current tag status
            tagData.ParentRect = newTagData.ParentRect;
            tagData.AppRect = newTagData.AppRect;
            tagData.Parent = newTagData.Parent;
            tagData.Child = newTagData.Child;
            tagData.Timestamp = newTagData.Timestamp;
        }

        private Storyboard CreateTransitionStoryboard(FrameworkElement child, bool usingBeforeLoaded, ref Rect layoutRect, ref Rect currentRect)
        {
            Duration duration = this.Duration;
            Storyboard transitionStoryboard = new Storyboard();
            transitionStoryboard.Duration = duration;

            double xScaleFrom = (!usingBeforeLoaded || layoutRect.Width == 0.0) ? 1.0 : (currentRect.Width / layoutRect.Width);
            double yScaleFrom = (!usingBeforeLoaded || layoutRect.Height == 0.0) ? 1.0 : (currentRect.Height / layoutRect.Height);
            double xFrom = currentRect.Left - layoutRect.Left;
            double yFrom = currentRect.Top - layoutRect.Top;

            TransformGroup transform = new TransformGroup();
            transform.Children.Add(new ScaleTransform() { ScaleX = xScaleFrom, ScaleY = yScaleFrom });
            transform.Children.Add(new TranslateTransform() { X = xFrom, Y = yFrom });
            AddTransform(child, transform);

            string prefix = "(FrameworkElement.RenderTransform).";

            TransformGroup transformGroup = child.RenderTransform as TransformGroup;
            if (transformGroup != null && GetHasTransformWrapper(child))
            {
                prefix += "(TransformGroup.Children)[" + (transformGroup.Children.Count - 1) + "].";
            }

            if (usingBeforeLoaded)
            {
                if (xScaleFrom != 1.0)
                {
                    DoubleAnimation xScaleAnimation = new DoubleAnimation() { Duration = duration, From = xScaleFrom, To = 1.0 };
                    Storyboard.SetTarget(xScaleAnimation, child);
                    Storyboard.SetTargetProperty(xScaleAnimation, new PropertyPath(prefix + "(TransformGroup.Children)[0].(ScaleTransform.ScaleX)", new object[0]));
                    xScaleAnimation.EasingFunction = this.EaseX;
                    transitionStoryboard.Children.Add(xScaleAnimation);
                }

                if (yScaleFrom != 1.0)
                {
                    DoubleAnimation yScaleAnimation = new DoubleAnimation() { Duration = duration, From = yScaleFrom, To = 1.0 };
                    Storyboard.SetTarget(yScaleAnimation, child);
                    Storyboard.SetTargetProperty(yScaleAnimation, new PropertyPath(prefix + "(TransformGroup.Children)[0].(ScaleTransform.ScaleY)", new object[0]));
                    yScaleAnimation.EasingFunction = this.EaseY;
                    transitionStoryboard.Children.Add(yScaleAnimation);
                }
            }

            if (xFrom != 0.0)
            {
                DoubleAnimation xAnimation = new DoubleAnimation() { Duration = duration, From = xFrom, To = 0.0 };
                Storyboard.SetTarget(xAnimation, child);
                Storyboard.SetTargetProperty(xAnimation, new PropertyPath(prefix + "(TransformGroup.Children)[1].(TranslateTransform.X)", new object[0]));
                xAnimation.EasingFunction = this.EaseX;
                transitionStoryboard.Children.Add(xAnimation);
            }

            if (yFrom != 0.0)
            {
                DoubleAnimation yAnimation = new DoubleAnimation() { Duration = duration, From = yFrom, To = 0.0 };
                Storyboard.SetTarget(yAnimation, child);
                Storyboard.SetTargetProperty(yAnimation, new PropertyPath(prefix + "(TransformGroup.Children)[1].(TranslateTransform.Y)", new object[0]));
                yAnimation.EasingFunction = this.EaseY;
                transitionStoryboard.Children.Add(yAnimation);
            }

            return transitionStoryboard;
        }

        private static void AddTransform(FrameworkElement child, Transform transform)
        {
            TransformGroup transformGroup = child.RenderTransform as TransformGroup;

            if (transformGroup == null)
            {
                transformGroup = new TransformGroup();
                transformGroup.Children.Add(child.RenderTransform);
                child.RenderTransform = transformGroup;
                SetHasTransformWrapper(child, true);
            }

            transformGroup.Children.Add(transform);
        }

        private static Transform GetTransform(FrameworkElement child)
        {
            TransformGroup transformGroup = child.RenderTransform as TransformGroup;
            if (transformGroup != null && transformGroup.Children.Count > 0)
            {
                return transformGroup.Children[transformGroup.Children.Count - 1];
            }
            else
            {
                return new TranslateTransform();
            }
        }

        private static void RemoveTransform(FrameworkElement child)
        {
            TransformGroup transformGroup = child.RenderTransform as TransformGroup;

            if (transformGroup != null)
            {
                if (GetHasTransformWrapper(child))
                {
                    child.RenderTransform = transformGroup.Children[0];
                    SetHasTransformWrapper(child, false);
                }
                else
                {
                    transformGroup.Children.RemoveAt(transformGroup.Children.Count - 1);
                }
            }
        }

        private static void TransferLocalValue(FrameworkElement element, DependencyProperty source, DependencyProperty dest)
        {
            object value = element.ReadLocalValue(source);

            BindingExpressionBase bindingExpressionBase = value as BindingExpressionBase;
            if (bindingExpressionBase != null)
            {
                element.SetBinding(dest, bindingExpressionBase.ParentBindingBase);
            }
            else if (value == DependencyProperty.UnsetValue)
            {
                element.ClearValue(dest);
            }
            else
            {
                element.SetValue(dest, element.GetAnimationBaseValue(source));
            }

            element.ClearValue(source);
        }

        private static bool IsClose(double a, double b)
        {
            return (Math.Abs((double)(a - b)) < 1E-07);
        }

        private static bool IsEmptyRect(Rect rect)
        {
            return ((rect.IsEmpty || double.IsNaN(rect.Left)) || double.IsNaN(rect.Top));
        }
    }

    /// <summary>
    /// Simple helper class to allow any UIElements to be used as an Adorner.
    /// </summary>
    public class AdornerContainer : System.Windows.Documents.Adorner
    {
        private UIElement child;

        public AdornerContainer(UIElement adornedElement) : base(adornedElement)
        {
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.child != null)
            {
                this.child.Arrange(new Rect(finalSize));
            }

            return finalSize;
        }

        public UIElement Child
        {
            get { return this.child; }
            set
            {
                this.AddVisualChild(value);
                this.child = value;
            }
        }

        protected override int VisualChildrenCount
        {
            get { return this.child == null ? 0 : 1; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return index == 0 && this.child != null ? this.child : base.GetVisualChild(index);
        }
    }
}