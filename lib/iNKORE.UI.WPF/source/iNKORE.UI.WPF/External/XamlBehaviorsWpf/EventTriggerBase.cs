// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Reflection;
    using System.Windows;

    /// <summary>
    /// Represents a trigger that can listen to an element other than its AssociatedObject.
    /// </summary>
    /// <typeparam name="T">The type that this trigger can be associated with.</typeparam>
    /// <remarks>
    ///		EventTriggerBase extends TriggerBase to add knowledge of another object than the one it is attached to. 
    ///		This allows a user to attach a Trigger/Action pair to one element and invoke the Action in response to a 
    ///		change in another object somewhere else. Override OnSourceChanged to hook or unhook handlers on the source 
    ///		element, and OnAttached/OnDetaching for the associated element. The type of the Source element can be 
    ///		constrained by the generic type parameter. If you need control over the type of the 
    ///		AssociatedObject, set a TypeConstraintAttribute on your derived type.
    ///	</remarks>
    public abstract class EventTriggerBase<T> : EventTriggerBase where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventTriggerBase&lt;T&gt;"/> class.
        /// </summary>
        protected EventTriggerBase()
            : base(typeof(T))
        {
        }

        /// <summary>
        /// Gets the resolved source. If <c ref="SourceName"/> is not set or cannot be resolved, defaults to AssociatedObject.
        /// </summary>
        /// <value>The resolved source object.</value>
        /// <remarks>In general, this property should be used in place of AssociatedObject in derived classes.</remarks>
        public new T Source
        {
            get
            {
                return (T)base.Source;
            }
        }

        internal sealed override void OnSourceChangedImpl(object oldSource, object newSource)
        {
            base.OnSourceChangedImpl(oldSource, newSource);
            this.OnSourceChanged(oldSource as T, newSource as T);
        }

        /// <summary>
        /// Called when the source property changes.
        /// </summary>
        /// <remarks>Override this to hook functionality to and unhook functionality from the specified source, rather than the AssociatedObject.</remarks>
        /// <param name="oldSource">The old source.</param>
        /// <param name="newSource">The new source.</param>
        protected virtual void OnSourceChanged(T oldSource, T newSource)
        {
        }
    }

    /// <summary>
    /// Represents a trigger that can listen to an object other than its AssociatedObject.
    /// </summary>
    /// <remarks>This is an infrastructure class. Trigger authors should derive from EventTriggerBase&lt;T&gt; instead of this class.</remarks>
    public abstract class EventTriggerBase : TriggerBase
    {
        private Type sourceTypeConstraint;
        private bool isSourceChangedRegistered;
        private NameResolver sourceNameResolver;
        private MethodInfo eventHandlerMethodInfo;

        public static readonly DependencyProperty SourceObjectProperty = DependencyProperty.Register("SourceObject",
                                                                                                    typeof(object),
                                                                                                    typeof(EventTriggerBase),
                                                                                                    new PropertyMetadata(
                                                                                                        new PropertyChangedCallback(OnSourceObjectChanged)));


        public static readonly DependencyProperty SourceNameProperty = DependencyProperty.Register("SourceName",
                                                                                                    typeof(string),
                                                                                                    typeof(EventTriggerBase),
                                                                                                    new PropertyMetadata(
                                                                                                        new PropertyChangedCallback(OnSourceNameChanged)));

        /// <summary>
        /// Gets the type constraint of the associated object.
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
        /// Gets the source type constraint.
        /// </summary>
        /// <value>The source type constraint.</value>
        protected Type SourceTypeConstraint
        {
            get
            {
                return this.sourceTypeConstraint;
            }
        }

        /// <summary>
        /// Gets or sets the target object. If TargetObject is not set, the target will look for the object specified by TargetName. If an element referred to by TargetName cannot be found, the target will default to the AssociatedObject. This is a dependency property.
        /// </summary>
        /// <value>The target object.</value>
        public object SourceObject
        {
            get { return this.GetValue(SourceObjectProperty); }
            set { this.SetValue(SourceObjectProperty, value); }
        }

        /// <summary>
        /// Gets or sets the name of the element this EventTriggerBase listens for as a source. If the name is not set or cannot be resolved, the AssociatedObject will be used.  This is a dependency property.
        /// </summary>
        /// <value>The name of the source element.</value>
        public string SourceName
        {
            get { return (string)this.GetValue(SourceNameProperty); }
            set { this.SetValue(SourceNameProperty, value); }
        }

        /// <summary>
        /// Gets the resolved source. If <c ref="SourceName"/> is not set or cannot be resolved, defaults to AssociatedObject.
        /// </summary>
        /// <value>The resolved source object.</value>
        /// <remarks>In general, this property should be used in place of AssociatedObject in derived classes.</remarks>
        /// <exception cref="InvalidOperationException">The element pointed to by <c cref="Source"/> does not satisify the type constraint.</exception>
        public object Source
        {
            get
            {
                object source = this.AssociatedObject;

                if (this.SourceObject != null)
                {
                    source = this.SourceObject;
                }
                else if (this.IsSourceNameSet)
                {
                    source = this.SourceNameResolver.Object;
                    if (source != null && !this.SourceTypeConstraint.IsAssignableFrom(source.GetType()))
                    {
                        throw new InvalidOperationException(string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionStringTable.RetargetedTypeConstraintViolatedExceptionMessage,
                            this.GetType().Name,
                            source.GetType(),
                            this.SourceTypeConstraint,
                            "Source"));
                    }
                }
                return source;
            }
        }

        private NameResolver SourceNameResolver
        {
            get { return this.sourceNameResolver; }
        }

        private bool IsSourceChangedRegistered
        {
            get { return this.isSourceChangedRegistered; }
            set { this.isSourceChangedRegistered = value; }
        }

        private bool IsSourceNameSet
        {
            get
            {
                return !string.IsNullOrEmpty(this.SourceName) || this.ReadLocalValue(SourceNameProperty) != DependencyProperty.UnsetValue;
            }
        }

        private bool IsLoadedRegistered
        {
            get;
            set;
        }

        internal EventTriggerBase(Type sourceTypeConstraint)
            : base(typeof(DependencyObject))
        {
            this.sourceTypeConstraint = sourceTypeConstraint;
            this.sourceNameResolver = new NameResolver();
            this.RegisterSourceChanged();
        }

        /// <summary>
        /// Specifies the name of the Event this EventTriggerBase is listening for.
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "NikhilKo convinced us this was the right choice.")]
        protected abstract string GetEventName();

        /// <summary>
        /// Called when the event associated with this EventTriggerBase is fired. By default, this will invoke all actions on the trigger.
        /// </summary>
        /// <param name="eventArgs">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Override this to provide more granular control over when actions associated with this trigger will be invoked.</remarks>
        protected virtual void OnEvent(EventArgs eventArgs)
        {
            this.InvokeActions(eventArgs);
        }

        private void OnSourceChanged(object oldSource, object newSource)
        {
            if (this.AssociatedObject != null)
            {
                this.OnSourceChangedImpl(oldSource, newSource);
            }
        }

        /// <summary>
        /// Called when the source changes.
        /// </summary>
        /// <param name="oldSource">The old source.</param>
        /// <param name="newSource">The new source.</param>
        /// <remarks>This function should be overridden in derived classes to hook functionality to and unhook functionality from the changing source objects.</remarks>
        internal virtual void OnSourceChangedImpl(object oldSource, object newSource)
        {
            if (string.IsNullOrEmpty(this.GetEventName()))
            {
                return;
            }
            // we'll handle loaded elsewhere
            if (string.Compare(this.GetEventName(), "Loaded", StringComparison.Ordinal) != 0)
            {
                if (oldSource != null && this.SourceTypeConstraint.IsAssignableFrom(oldSource.GetType()))
                {
                    this.UnregisterEvent(oldSource, this.GetEventName());
                }

                if (newSource != null && this.SourceTypeConstraint.IsAssignableFrom(newSource.GetType()))
                {
                    this.RegisterEvent(newSource, this.GetEventName());
                }
            }
        }

        /// <summary>
        /// Called after the trigger is attached to an AssociatedObject.
        /// </summary>
        protected override void OnAttached()
        {
            // We can't resolve element names using a Behavior, as it isn't a FrameworkElement.
            // Hence, if we are Hosted on a Behavior, we need to resolve against the Behavior's
            // Host rather than our own. See comment in TargetedTriggerAction.
            // TODO jekelly 6/20/08: Ideally we could do a namespace walk, but SL doesn't expose
            //						 a way to do this. This solution only looks one level deep. 
            //						 A Behavior with a Behavior attached won't work. This is OK
            //						 for now, but should consider a more general solution if needed.
            base.OnAttached();
            DependencyObject newHost = this.AssociatedObject;
            Behavior newBehavior = newHost as Behavior;
            FrameworkElement newHostElement = newHost as FrameworkElement;

            this.RegisterSourceChanged();
            if (newBehavior != null)
            {
                newHost = ((IAttachedObject)newBehavior).AssociatedObject;
                newBehavior.AssociatedObjectChanged += new EventHandler(OnBehaviorHostChanged);
            }
            else if (this.SourceObject != null || newHostElement == null)
            {
                try
                {
                    this.OnSourceChanged(null, this.Source);
                }
                catch (InvalidOperationException)
                {
                    // If we're hosted on a DependencyObject, we don't have a name scope reference element.
                    // Hence, we'll fire off the source changed manually when we first attach. However, if we've
                    // been attached to something that doesn't meet the target type constraint, accessing Source
                    // will throw.
                }
            }
            else
            {
                this.SourceNameResolver.NameScopeReferenceElement = newHostElement;
            }

            bool isLoadedEvent = (string.Compare(this.GetEventName(), "Loaded", StringComparison.Ordinal) == 0);

            if (isLoadedEvent && newHostElement != null && !Interaction.IsElementLoaded(newHostElement))
            {
                this.RegisterLoaded(newHostElement);
            }
        }

        /// <summary>
        /// Called when the trigger is being detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            Behavior oldBehavior = this.AssociatedObject as Behavior;
            FrameworkElement oldElement = this.AssociatedObject as FrameworkElement;

            try
            {
                this.OnSourceChanged(this.Source, null);
            }
            catch (InvalidOperationException)
            {
                // We fire off the source changed manually when we detach. However, if we've been attached to 
                // something that doesn't meet the target type constraint, accessing Source will throw.
            }
            this.UnregisterSourceChanged();

            if (oldBehavior != null)
            {
                oldBehavior.AssociatedObjectChanged -= new EventHandler(OnBehaviorHostChanged);
            }

            this.SourceNameResolver.NameScopeReferenceElement = null;

            bool isLoadedEvent = (string.Compare(this.GetEventName(), "Loaded", StringComparison.Ordinal) == 0);

            if (isLoadedEvent && oldElement != null)
            {
                this.UnregisterLoaded(oldElement);
            }
        }

        private void OnBehaviorHostChanged(object sender, EventArgs e)
        {
            this.SourceNameResolver.NameScopeReferenceElement = ((IAttachedObject)sender).AssociatedObject as FrameworkElement;
        }

        private static void OnSourceObjectChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            EventTriggerBase eventTriggerBase = (EventTriggerBase)obj;
            object sourceNameObject = eventTriggerBase.SourceNameResolver.Object;
            if (args.NewValue == null)
            {
                eventTriggerBase.OnSourceChanged(args.OldValue, sourceNameObject);
            }
            else
            {
                if (args.OldValue == null && sourceNameObject != null)
                {
                    eventTriggerBase.UnregisterEvent(sourceNameObject, eventTriggerBase.GetEventName());
                }
                eventTriggerBase.OnSourceChanged(args.OldValue, args.NewValue);
            }
        }

        private static void OnSourceNameChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            EventTriggerBase trigger = (EventTriggerBase)obj;
            trigger.SourceNameResolver.Name = (string)args.NewValue;
        }

        private void RegisterSourceChanged()
        {
            if (!this.IsSourceChangedRegistered)
            {
                this.SourceNameResolver.ResolvedElementChanged += this.OnSourceNameResolverElementChanged;
                this.IsSourceChangedRegistered = true;
            }
        }

        private void UnregisterSourceChanged()
        {
            if (this.IsSourceChangedRegistered)
            {
                this.SourceNameResolver.ResolvedElementChanged -= this.OnSourceNameResolverElementChanged;
                this.IsSourceChangedRegistered = false;
            }
        }

        private void OnSourceNameResolverElementChanged(object sender, NameResolvedEventArgs e)
        {
            if (this.SourceObject == null)
            {
                this.OnSourceChanged(e.OldObject, e.NewObject);
            }
        }

        private void RegisterLoaded(FrameworkElement associatedElement)
        {
            Debug.Assert(this.eventHandlerMethodInfo == null);
            Debug.Assert(!this.IsLoadedRegistered, "Trying to register Loaded more than once.");
            if (!this.IsLoadedRegistered && associatedElement != null)
            {
                associatedElement.Loaded += this.OnEventImpl;
                this.IsLoadedRegistered = true;
            }
        }

        private void UnregisterLoaded(FrameworkElement associatedElement)
        {
            Debug.Assert(this.eventHandlerMethodInfo == null);
            if (this.IsLoadedRegistered && associatedElement != null)
            {
                associatedElement.Loaded -= this.OnEventImpl;
                this.IsLoadedRegistered = false;
            }
        }

        /// <exception cref="ArgumentException">Could not find eventName on the Target.</exception>
        private void RegisterEvent(object obj, string eventName)
        {
            Debug.Assert(this.eventHandlerMethodInfo == null && string.Compare(eventName, "Loaded", StringComparison.Ordinal) != 0);
            Type targetType = obj.GetType();
            EventInfo eventInfo = targetType.GetEvent(eventName);
            if (eventInfo == null)
            {
                if (this.SourceObject != null)
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                                                                ExceptionStringTable.EventTriggerCannotFindEventNameExceptionMessage,
                                                                eventName,
                                                                obj.GetType().Name));
                }
                else
                {
                    return;
                }
            }

            if (!EventTriggerBase.IsValidEvent(eventInfo))
            {
                if (this.SourceObject != null)
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                                                                ExceptionStringTable.EventTriggerBaseInvalidEventExceptionMessage,
                                                                eventName,
                                                                obj.GetType().Name));
                }
                else
                {
                    return;
                }
            }
            this.eventHandlerMethodInfo = typeof(EventTriggerBase).GetMethod("OnEventImpl", BindingFlags.NonPublic | BindingFlags.Instance);
            eventInfo.AddEventHandler(obj, Delegate.CreateDelegate(eventInfo.EventHandlerType, this, this.eventHandlerMethodInfo));
        }

        private static bool IsValidEvent(EventInfo eventInfo)
        {
            Type eventHandlerType = eventInfo.EventHandlerType;
            if (typeof(Delegate).IsAssignableFrom(eventInfo.EventHandlerType))
            {
                MethodInfo invokeMethod = eventHandlerType.GetMethod("Invoke");
                ParameterInfo[] parameters = invokeMethod.GetParameters();
                return parameters.Length == 2 && typeof(object).IsAssignableFrom(parameters[0].ParameterType) && typeof(EventArgs).IsAssignableFrom(parameters[1].ParameterType);
            }
            return false;
        }

        private void UnregisterEvent(object obj, string eventName)
        {
            if (string.Compare(eventName, "Loaded", StringComparison.Ordinal) == 0)
            {
                FrameworkElement frameworkElement = obj as FrameworkElement;
                if (frameworkElement != null)
                {
                    this.UnregisterLoaded(frameworkElement);
                }
            }
            else
            {
                this.UnregisterEventImpl(obj, eventName);
            }
        }

        private void UnregisterEventImpl(object obj, string eventName)
        {
            Type targetType = obj.GetType();
            Debug.Assert(this.eventHandlerMethodInfo != null || targetType.GetEvent(eventName) == null);

            if (this.eventHandlerMethodInfo == null)
            {
                return;
            }

            EventInfo eventInfo = targetType.GetEvent(eventName);
            Debug.Assert(eventInfo != null, "Should not try to unregister an event that we successfully registered");
            eventInfo.RemoveEventHandler(obj, Delegate.CreateDelegate(eventInfo.EventHandlerType, this, this.eventHandlerMethodInfo));
            this.eventHandlerMethodInfo = null;
        }

        private void OnEventImpl(object sender, EventArgs eventArgs)
        {
            this.OnEvent(eventArgs);
        }

        internal void OnEventNameChanged(string oldEventName, string newEventName)
        {
            if (this.AssociatedObject != null)
            {
                FrameworkElement associatedElement = this.Source as FrameworkElement;

                if (associatedElement != null && string.Compare(oldEventName, "Loaded", StringComparison.Ordinal) == 0)
                {
                    this.UnregisterLoaded(associatedElement);
                }
                else if (!string.IsNullOrEmpty(oldEventName))
                {
                    this.UnregisterEvent(this.Source, oldEventName);
                }
                if (associatedElement != null && string.Compare(newEventName, "Loaded", StringComparison.Ordinal) == 0)
                {
                    this.RegisterLoaded(associatedElement);
                }
                else if (!string.IsNullOrEmpty(newEventName))
                {
                    this.RegisterEvent(this.Source, newEventName);
                }
            }
        }
    }
}
