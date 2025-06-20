// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Globalization;

    /// <summary>
    /// Represents an action that can be targeted to affect an object other than its AssociatedObject.
    /// </summary>
    /// <typeparam name="T">The type constraint on the target.</typeparam>
    /// <remarks>
    ///		TargetedTriggerAction extends TriggerAction to add knowledge of another element than the one it is attached to. 
    ///		This allows a user to invoke the action on an element other than the one it is attached to in response to a 
    ///		trigger firing. Override OnTargetChanged to hook or unhook handlers on the target element, and OnAttached/OnDetaching 
    ///		for the associated element. The type of the Target element can be constrained by the generic type parameter. If 
    ///		you need control over the type of the AssociatedObject, set a TypeConstraintAttribute on your derived type.
    /// </remarks>
    public abstract class TargetedTriggerAction<T> : TargetedTriggerAction where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TargetedTriggerAction&lt;T&gt;"/> class.
        /// </summary>
        protected TargetedTriggerAction()
            : base(typeof(T))
        {
        }

        /// <summary>
        /// Gets the target object. If TargetName is not set or cannot be resolved, defaults to the AssociatedObject.
        /// </summary>
        /// <value>The target.</value>
        /// <remarks>In general, this property should be used in place of AssociatedObject in derived classes.</remarks>
        protected new T Target
        {
            get
            {
                return (T)base.Target;
            }
        }

        internal sealed override void OnTargetChangedImpl(object oldTarget, object newTarget)
        {
            base.OnTargetChangedImpl(oldTarget, newTarget);
            this.OnTargetChanged(oldTarget as T, newTarget as T);
        }

        /// <summary>
        /// Called when the target property changes.
        /// </summary>
        /// <remarks>Override this to hook and unhook functionality on the specified Target, rather than the AssociatedObject.</remarks>
        /// <param name="oldTarget">The old target.</param>
        /// <param name="newTarget">The new target.</param>
        protected virtual void OnTargetChanged(T oldTarget, T newTarget)
        {
        }
    }

    /// <summary>
    /// Represents an action that can be targeted to affect an object other than its AssociatedObject.
    /// </summary>
    /// <remarks>This is an infrastructure class. Action authors should derive from TargetedTriggerAction&lt;T&gt; instead of this class.</remarks>
    public abstract class TargetedTriggerAction : TriggerAction
    {
        private Type targetTypeConstraint;
        private bool isTargetChangedRegistered;
        private NameResolver targetResolver;

        public static readonly DependencyProperty TargetObjectProperty = DependencyProperty.Register("TargetObject",
                                                                                                    typeof(object),
                                                                                                    typeof(TargetedTriggerAction),
                                                                                                     new FrameworkPropertyMetadata(
                                                                                                        new PropertyChangedCallback(OnTargetObjectChanged)));

        public static readonly DependencyProperty TargetNameProperty = DependencyProperty.Register("TargetName",
                                                                                                    typeof(string),
                                                                                                    typeof(TargetedTriggerAction),
                                                                                                    new FrameworkPropertyMetadata(
                                                                                                        new PropertyChangedCallback(OnTargetNameChanged)));

        /// <summary>
        /// Gets or sets the target object. If TargetObject is not set, the target will look for the object specified by TargetName. If an element referred to by TargetName cannot be found, the target will default to the AssociatedObject. This is a dependency property.
        /// </summary>
        /// <value>The target object.</value>
        public object TargetObject
        {
            get { return this.GetValue(TargetObjectProperty); }
            set { this.SetValue(TargetObjectProperty, value); }
        }

        /// <summary>
        /// Gets or sets the name of the object this action targets. If Target is set, this property is ignored. If Target is not set and TargetName is not set or cannot be resolved, the target will default to the AssociatedObject. This is a dependency property.
        /// </summary>
        /// <value>The name of the target object.</value>
        public string TargetName
        {
            get { return (string)this.GetValue(TargetNameProperty); }
            set { this.SetValue(TargetNameProperty, value); }
        }

        /// <summary>
        /// Gets the target object. If TargetObject is set, returns TargetObject. Else, if TargetName is not set or cannot be resolved, defaults to the AssociatedObject.
        /// </summary>
        /// <value>The target object.</value>
        /// <remarks>In general, this property should be used in place of AssociatedObject in derived classes.</remarks>
        /// <exception cref="InvalidOperationException">The Target element does not satisfy the type constraint.</exception>
        protected object Target
        {
            get
            {
                object target = this.AssociatedObject;
                if (this.TargetObject != null)
                {
                    target = this.TargetObject;
                }
                else if (this.IsTargetNameSet)
                {
                    target = this.TargetResolver.Object;
                }

                if (target != null && !this.TargetTypeConstraint.IsAssignableFrom(target.GetType()))
                {
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionStringTable.RetargetedTypeConstraintViolatedExceptionMessage,
                        this.GetType().Name,
                        target.GetType(),
                        this.TargetTypeConstraint,
                        "Target"));
                }
                return target;
            }
        }

        /// <summary>
        /// Gets the associated object type constraint.
        /// </summary>
        /// <value>The associated object type constraint.</value>
        /// <remarks>Define a TypeConstraintAttribute on a derived type to constrain the types it may be attached to.</remarks>
        protected sealed override Type AssociatedObjectTypeConstraint
        {
            get
            {
                AttributeCollection attributes = TypeDescriptor.GetAttributes(this.GetType());
                TypeConstraintAttribute typeConstraintAttribute = attributes[typeof(TypeConstraintAttribute)] as TypeConstraintAttribute;

                if (typeConstraintAttribute != null)
                {
                    return typeConstraintAttribute.Constraint;
                }
                return typeof(DependencyObject);
            }
        }

        /// <summary>
        /// Gets the target type constraint.
        /// </summary>
        /// <value>The target type constraint.</value>
        protected Type TargetTypeConstraint
        {
            get
            {
                this.ReadPreamble();
                return this.targetTypeConstraint;
            }
        }

        private bool IsTargetNameSet
        {
            get
            {
                return !string.IsNullOrEmpty(this.TargetName) || this.ReadLocalValue(TargetNameProperty) != DependencyProperty.UnsetValue;
            }
        }

        private NameResolver TargetResolver
        {
            get { return this.targetResolver; }
        }

        private bool IsTargetChangedRegistered
        {
            get { return this.isTargetChangedRegistered; }
            set { this.isTargetChangedRegistered = value; }
        }

        internal TargetedTriggerAction(Type targetTypeConstraint)
            : base(typeof(DependencyObject))
        {
            this.targetTypeConstraint = targetTypeConstraint;
            this.targetResolver = new NameResolver();
            this.RegisterTargetChanged();
        }

        /// <summary>
        /// Called when the target changes.
        /// </summary>
        /// <param name="oldTarget">The old target.</param>
        /// <param name="newTarget">The new target.</param>
        /// <remarks>This function should be overriden in derived classes to hook and unhook functionality from the changing source objects.</remarks>
        internal virtual void OnTargetChangedImpl(object oldTarget, object newTarget)
        {
        }

        /// <summary>
        /// Called after the action is attached to an AssociatedObject.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            // We can't resolve element names using a Behavior, as it isn't a FrameworkElement.
            // Hence, if we are Hosted on a Behavior, we need to resolve against the Behavior's
            // Host rather than our own. See comment in EventTriggerBase.
            // TODO jekelly 6/20/08: Ideally we could do a namespace walk, but SL doesn't expose
            //						 a way to do this. This solution only looks one level deep. 
            //						 A Behavior with a Behavior attached won't work. This is OK
            //						 for now, but should consider a more general solution if needed.
            DependencyObject hostObject = this.AssociatedObject;
            Behavior newBehavior = hostObject as Behavior;

            this.RegisterTargetChanged();
            if (newBehavior != null)
            {
                hostObject = ((IAttachedObject)newBehavior).AssociatedObject;
                newBehavior.AssociatedObjectChanged += new EventHandler(OnBehaviorHostChanged);
            }
            this.TargetResolver.NameScopeReferenceElement = hostObject as FrameworkElement;
        }

        /// <summary>
        /// Called when the action is being detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        protected override void OnDetaching()
        {
            Behavior oldBehavior = this.AssociatedObject as Behavior;
            base.OnDetaching();
            this.OnTargetChangedImpl(this.TargetResolver.Object, null);
            this.UnregisterTargetChanged();

            if (oldBehavior != null)
            {
                oldBehavior.AssociatedObjectChanged -= new EventHandler(OnBehaviorHostChanged);
            }
            this.TargetResolver.NameScopeReferenceElement = null;
        }

        private void OnBehaviorHostChanged(object sender, EventArgs e)
        {
            this.TargetResolver.NameScopeReferenceElement = ((IAttachedObject)sender).AssociatedObject as FrameworkElement;
        }

        private void RegisterTargetChanged()
        {
            if (!this.IsTargetChangedRegistered)
            {
                this.TargetResolver.ResolvedElementChanged += new EventHandler<NameResolvedEventArgs>(OnTargetChanged);
                this.IsTargetChangedRegistered = true;
            }
        }

        private void UnregisterTargetChanged()
        {
            if (this.IsTargetChangedRegistered)
            {
                this.TargetResolver.ResolvedElementChanged -= new EventHandler<NameResolvedEventArgs>(OnTargetChanged);
                this.IsTargetChangedRegistered = false;
            }
        }

        private static void OnTargetObjectChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            TargetedTriggerAction targetedTriggerAction = (TargetedTriggerAction)obj;
            targetedTriggerAction.OnTargetChanged(obj, new NameResolvedEventArgs(args.OldValue, args.NewValue));
        }

        private static void OnTargetNameChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            TargetedTriggerAction targetedTriggerAction = (TargetedTriggerAction)obj;
            targetedTriggerAction.TargetResolver.Name = (string)args.NewValue;
        }

        private void OnTargetChanged(object sender, NameResolvedEventArgs e)
        {
            if (this.AssociatedObject != null)
            {
                this.OnTargetChangedImpl(e.OldObject, e.NewObject);
            }
        }
    }
}
