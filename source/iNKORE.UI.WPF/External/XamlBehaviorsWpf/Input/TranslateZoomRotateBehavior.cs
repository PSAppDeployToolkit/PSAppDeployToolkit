// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors.Input
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using Microsoft.Xaml.Behaviors.Layout;
    using Microsoft.Xaml.Behaviors;

    /// <summary>
    /// Allows the user to use common touch gestures to translate, zoom, and rotate the attached object.
    /// </summary>
    public class TranslateZoomRotateBehavior : Behavior<FrameworkElement>
    {
        #region Fields

        private Transform cachedRenderTransform;

        // used for handling the mouse fallback behavior.
        private bool isDragging = false;
        // prevent us from trying to update the position when handling a mouse move
        private bool isAdjustingTransform = false;
        private Point lastMousePoint;

        // used to enforce min and max scale.
        private double lastScaleX = 1.0;
        private double lastScaleY = 1.0;
        private const double HardMinimumScale = 1e-6;
        #endregion

        #region Dependency properties

        public static readonly DependencyProperty SupportedGesturesProperty =
            DependencyProperty.Register("SupportedGestures", typeof(ManipulationModes), typeof(TranslateZoomRotateBehavior), new PropertyMetadata(ManipulationModes.All));

        public static readonly DependencyProperty TranslateFrictionProperty =
            DependencyProperty.Register("TranslateFriction", typeof(double), typeof(TranslateZoomRotateBehavior), new PropertyMetadata(0.0, frictionChanged, coerceFriction));

        public static readonly DependencyProperty RotationalFrictionProperty =
            DependencyProperty.Register("RotationalFriction", typeof(double), typeof(TranslateZoomRotateBehavior), new PropertyMetadata(0.0, frictionChanged, coerceFriction));

        public static readonly DependencyProperty ConstrainToParentBoundsProperty =
            DependencyProperty.Register("ConstrainToParentBounds", typeof(bool), typeof(TranslateZoomRotateBehavior), new PropertyMetadata(false));

        public static readonly DependencyProperty MinimumScaleProperty =
            DependencyProperty.Register("MinimumScale", typeof(double), typeof(TranslateZoomRotateBehavior), new PropertyMetadata(0.1));

        public static readonly DependencyProperty MaximumScaleProperty =
            DependencyProperty.Register("MaximumScale", typeof(double), typeof(TranslateZoomRotateBehavior), new PropertyMetadata(10.0));

        #endregion

        #region Public properties
        /// <summary>
        /// Gets or sets a value specifying which zooming and translation variants to support.
        /// </summary>
        public ManipulationModes SupportedGestures
        {
            get { return (ManipulationModes)this.GetValue(TranslateZoomRotateBehavior.SupportedGesturesProperty); }
            set { this.SetValue(TranslateZoomRotateBehavior.SupportedGesturesProperty, value); }
        }

        /// <summary>
        /// Gets or sets a number describing the rate at which the translation will decrease.
        /// </summary>
        public double TranslateFriction
        {
            get { return (double)this.GetValue(TranslateZoomRotateBehavior.TranslateFrictionProperty); }
            set { this.SetValue(TranslateZoomRotateBehavior.TranslateFrictionProperty, value); }
        }

        /// <summary>
        /// Gets or sets a number describing the rate at which the rotation will decrease.
        /// </summary>
        public double RotationalFriction
        {
            get { return (double)this.GetValue(TranslateZoomRotateBehavior.RotationalFrictionProperty); }
            set { this.SetValue(TranslateZoomRotateBehavior.RotationalFrictionProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value indicating whether the zoom and translate position of the attached object is limited by the bounds of the parent object.
        /// </summary>
        public bool ConstrainToParentBounds
        {
            get { return (bool)this.GetValue(TranslateZoomRotateBehavior.ConstrainToParentBoundsProperty); }
            set { this.SetValue(TranslateZoomRotateBehavior.ConstrainToParentBoundsProperty, value); }
        }

        /// <summary>
        /// Gets or sets a number indicating the minimum zoom value allowed.
        /// </summary>
        public double MinimumScale
        {
            get { return (double)this.GetValue(TranslateZoomRotateBehavior.MinimumScaleProperty); }
            set { this.SetValue(TranslateZoomRotateBehavior.MinimumScaleProperty, value); }
        }

        /// <summary>
        /// Gets or sets a number indicating the maximum zoom value allowed.
        /// </summary>
        public double MaximumScale
        {
            get { return (double)this.GetValue(TranslateZoomRotateBehavior.MaximumScaleProperty); }
            set { this.SetValue(TranslateZoomRotateBehavior.MaximumScaleProperty, value); }
        }

        #endregion

        #region PropertyChangedHandlers

        private static void frictionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            // this doesn't have to do anything, but is required to supply a CoerceValueCallback.
        }

        private static object coerceFriction(DependencyObject sender, object value)
        {
            double friction = (double)value;
            return Math.Max(0, Math.Min(1, friction));
        }


        #endregion

        #region Private properties

        private Transform RenderTransform
        {
            get
            {
                if (this.cachedRenderTransform == null || !object.ReferenceEquals(cachedRenderTransform, this.AssociatedObject.RenderTransform))
                {
                    Transform clonedTransform = MouseDragElementBehavior.CloneTransform(this.AssociatedObject.RenderTransform);
                    this.RenderTransform = clonedTransform;
                }
                return cachedRenderTransform;
            }
            set
            {
                if (this.cachedRenderTransform != value)
                {
                    this.cachedRenderTransform = value;
                    this.AssociatedObject.RenderTransform = value;
                }
            }
        }

        private Point RenderTransformOriginInElementCoordinates
        {
            get
            {
                return new Point(this.AssociatedObject.RenderTransformOrigin.X * this.AssociatedObject.ActualWidth,
                                this.AssociatedObject.RenderTransformOrigin.Y * this.AssociatedObject.ActualHeight);

            }
        }

        // This needs to take the render transform origin into account to get the proper transform value.
        private Matrix FullTransformValue
        {
            get
            {
                Point center = this.RenderTransformOriginInElementCoordinates;
                Matrix matrix = this.RenderTransform.Value;
                matrix.TranslatePrepend(-center.X, -center.Y);
                matrix.Translate(center.X, center.Y);
                return matrix;
            }
        }

        private MatrixTransform MatrixTransform
        {
            get
            {
                this.EnsureTransform();
                return (MatrixTransform)this.RenderTransform;
            }
        }

        private FrameworkElement ParentElement
        {
            get
            {
                return this.AssociatedObject.Parent as FrameworkElement;
            }
        }

        #endregion

        #region Private methods

        // This behavior always enforces a matrix transform.
        internal void EnsureTransform()
        {
            MatrixTransform transform = this.RenderTransform as MatrixTransform;
            if (transform == null || transform.IsFrozen)
            {
                if (this.RenderTransform != null)
                {
                    transform = new MatrixTransform(this.FullTransformValue);
                }
                else
                {
                    // can't use MatrixTransform.Identity because it is frozen.
                    transform = new MatrixTransform(Matrix.Identity);
                }
                this.RenderTransform = transform;
            }
            // The touch manipulation deltas need to be applied relative to the element's actual center.  
            // Keeping a render transform origin in place will cause the transform to be applied incorrectly, so we clear it.
            this.AssociatedObject.RenderTransformOrigin = new Point(0, 0);
        }

        internal void ApplyRotationTransform(double angle, Point rotationPoint)
        {
            // Need to use a temporary and set MatrixTransform.Matrix.
            // Modifying the matrix property directly will only affect a local copy, since Matrix is a value type.  
            Matrix matrix = this.MatrixTransform.Matrix;
            matrix.RotateAt(angle, rotationPoint.X, rotationPoint.Y);
            this.MatrixTransform.Matrix = matrix;
        }

        internal void ApplyScaleTransform(double scaleX, double scaleY, Point scalePoint)
        {
            // lastScale will not go below HardMinimumScale due to the checks below.  Thus, we can safely divide it to constrain the scale delta.
            // scale is the incremental scale, while lastScale is the current accumulated scale in the transform.  We want to constrain the incremental scale
            // so that the accumulated scale doesn't exceed min or max scale.  To prevent collapsing to a zero scale, we'll enforce a positive hard minimum scale.
            double newScaleX = scaleX * this.lastScaleX;
            newScaleX = Math.Min(Math.Max(Math.Max(TranslateZoomRotateBehavior.HardMinimumScale, this.MinimumScale), newScaleX), this.MaximumScale);
            scaleX = newScaleX / this.lastScaleX;
            this.lastScaleX = scaleX * this.lastScaleX;

            double newScaleY = scaleY * this.lastScaleY;
            newScaleY = Math.Min(Math.Max(Math.Max(TranslateZoomRotateBehavior.HardMinimumScale, this.MinimumScale), newScaleY), this.MaximumScale);
            scaleY = newScaleY / this.lastScaleY;
            this.lastScaleY = scaleY * this.lastScaleY;

            // Need to use a temporary and set MatrixTransform.Matrix.
            // Modifying the matrix property directly will only affect a local copy, since Matrix is a value type.  
            Matrix matrix = this.MatrixTransform.Matrix;
            matrix.ScaleAt(scaleX, scaleY, scalePoint.X, scalePoint.Y);
            this.MatrixTransform.Matrix = matrix;
        }

        internal void ApplyTranslateTransform(double x, double y)
        {
            // Need to use a temporary and set MatrixTransform.Matrix.
            // Modifying the matrix property directly will only affect a local copy, since Matrix is a value type.
            Matrix matrix = this.MatrixTransform.Matrix;
            matrix.Translate(x, y);
            this.MatrixTransform.Matrix = matrix;
        }

        private void ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            FrameworkElement manipulationContainer = this.ParentElement;
            // If the parent relationship goes through a popup(e.g. ComboBox/ComboBoxItem), then we need to use the element itself as the manipulation container, otherwise we'll crash(Expression 105258).
            if (manipulationContainer == null || !manipulationContainer.IsAncestorOf(this.AssociatedObject))
            {
                manipulationContainer = this.AssociatedObject;
            }
            e.ManipulationContainer = manipulationContainer;
            e.Mode = this.SupportedGestures;
            e.Handled = true;
        }

        private void ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            // deceleration is pixels per ms^2
            // the translate factor is in the range of [0,1], with 0 being no deceleration, and 1 being instant deceleration.
            // We use log because the curve has good characteristics over the input range.
            double translateFactor = this.TranslateFriction == 1 ? 1.0 : -.00666 * Math.Log(1 - this.TranslateFriction);
            double translateDeceleration = e.InitialVelocities.LinearVelocity.Length * translateFactor;

            e.TranslationBehavior = new InertiaTranslationBehavior()
            {
                InitialVelocity = e.InitialVelocities.LinearVelocity,
                DesiredDeceleration = Math.Max(translateDeceleration, 0)
            };

            double rotateFactor = this.RotationalFriction == 1 ? 1.0 : -.00666 * Math.Log(1 - this.RotationalFriction);
            double rotateDeceleration = Math.Abs(e.InitialVelocities.AngularVelocity) * rotateFactor;

            e.RotationBehavior = new InertiaRotationBehavior()
            {
                InitialVelocity = e.InitialVelocities.AngularVelocity,
                DesiredDeceleration = Math.Max(rotateDeceleration, 0)
            };

            e.Handled = true;
        }

        // This handles the manipulation data from the touch events.  Currently it assumes the zoom and rotation is applied through the center of the element.
        private void ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            this.EnsureTransform();
            ManipulationDelta currentDelta = e.DeltaManipulation;

            // Always use the element's center point as the origin of the manipulation deltas.
            Point origin = new Point(this.AssociatedObject.ActualWidth / 2, this.AssociatedObject.ActualHeight / 2);

            // Compute the manipulation center in element space.
            Point center = this.FullTransformValue.Transform(origin);

            this.ApplyScaleTransform(currentDelta.Scale.X, currentDelta.Scale.Y, center);
            this.ApplyRotationTransform(currentDelta.Rotation, center);
            this.ApplyTranslateTransform(currentDelta.Translation.X, currentDelta.Translation.Y);

            FrameworkElement container = (FrameworkElement)e.ManipulationContainer;
            // If constraining to bounds, and the element leaves its parent bounds, then stop the inertia.
            Rect parentBounds = new Rect(container.RenderSize);

            Rect childBounds = this.AssociatedObject.TransformToVisual(container).TransformBounds(new Rect(this.AssociatedObject.RenderSize));

            if (e.IsInertial && this.ConstrainToParentBounds && !parentBounds.Contains(childBounds))
            {
                e.Complete();
            }

            e.Handled = true;
        }

        // Mouse fallback for panning is implemented by tracking the mouse movement while the mouse is down.
        private void MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.AssociatedObject.CaptureMouse();
            this.AssociatedObject.MouseMove += this.AssociatedObject_MouseMove;
            this.AssociatedObject.LostMouseCapture += this.AssociatedObject_LostMouseCapture;
            e.Handled = true;
            this.lastMousePoint = e.GetPosition(this.AssociatedObject);
            this.isDragging = true;
        }

        // ends the mouse fallback for panning
        private void MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.AssociatedObject.ReleaseMouseCapture();
            e.Handled = true;
        }

        // capture can be lost by e.g. changing windows.
        private void AssociatedObject_LostMouseCapture(object sender, MouseEventArgs e)
        {
            this.isDragging = false;
            this.AssociatedObject.MouseMove -= this.AssociatedObject_MouseMove;
            this.AssociatedObject.LostMouseCapture -= this.AssociatedObject_LostMouseCapture;
        }

        // handle a mouse move by updating the object transform.
        private void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isDragging && !this.isAdjustingTransform)
            {
                this.isAdjustingTransform = true;
                Point newPoint = e.GetPosition(this.AssociatedObject);
                Vector delta = newPoint - this.lastMousePoint;
                if ((this.SupportedGestures & ManipulationModes.TranslateX) == 0)
                {
                    delta.X = 0;
                }
                if ((this.SupportedGestures & ManipulationModes.TranslateY) == 0)
                {
                    delta.Y = 0;
                }

                // Transform mouse movement into element space, taking the element's transform into account.
                Vector transformedDelta = this.FullTransformValue.Transform(delta);
                this.ApplyTranslateTransform(transformedDelta.X, transformedDelta.Y);
                // Need to get the position again, as it probably changed when updating the transform.
                this.lastMousePoint = e.GetPosition(this.AssociatedObject);
                this.isAdjustingTransform = false;
            }
        }

        #endregion

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>Override this to hook up functionality to the AssociatedObject.</remarks>
        protected override void OnAttached()
        {
            this.AssociatedObject.AddHandler(UIElement.ManipulationStartingEvent, new EventHandler<ManipulationStartingEventArgs>(this.ManipulationStarting), false /* handledEventsToo */);
            this.AssociatedObject.AddHandler(UIElement.ManipulationInertiaStartingEvent, new EventHandler<ManipulationInertiaStartingEventArgs>(this.ManipulationInertiaStarting), false /* handledEventsToo */);
            this.AssociatedObject.AddHandler(UIElement.ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(this.ManipulationDelta), false /* handledEventsToo */);
            this.AssociatedObject.IsManipulationEnabled = true;

            this.AssociatedObject.AddHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(this.MouseLeftButtonDown), false /* handledEventsToo */);
            this.AssociatedObject.AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(this.MouseLeftButtonUp), false /* handledEventsToo */);
        }

        /// <summary>
        /// Called when the behavior is getting detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        /// <remarks>Override this to unhook functionality from the AssociatedObject.</remarks>
        protected override void OnDetaching()
        {
            this.AssociatedObject.RemoveHandler(UIElement.ManipulationStartingEvent, new EventHandler<ManipulationStartingEventArgs>(this.ManipulationStarting));
            this.AssociatedObject.RemoveHandler(UIElement.ManipulationInertiaStartingEvent, new EventHandler<ManipulationInertiaStartingEventArgs>(this.ManipulationInertiaStarting));
            this.AssociatedObject.RemoveHandler(UIElement.ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(this.ManipulationDelta));
            this.AssociatedObject.IsManipulationEnabled = false;

            this.AssociatedObject.AddHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(this.MouseLeftButtonDown), false /* handledEventsToo */);
            this.AssociatedObject.AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(this.MouseLeftButtonUp), false /* handledEventsToo */);
        }
    }
}