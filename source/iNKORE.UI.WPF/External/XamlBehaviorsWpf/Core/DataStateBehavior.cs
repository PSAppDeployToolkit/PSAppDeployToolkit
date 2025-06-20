// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using Microsoft.Xaml.Behaviors;

    /// <summary>
    /// Toggles between two states based on a conditional statement.
    /// </summary>
    public class DataStateBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty BindingProperty = DependencyProperty.Register("Binding", typeof(object), typeof(DataStateBehavior), new PropertyMetadata(OnBindingChanged));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(DataStateBehavior), new PropertyMetadata(OnValueChanged));
        public static readonly DependencyProperty TrueStateProperty = DependencyProperty.Register("TrueState", typeof(string), typeof(DataStateBehavior), new PropertyMetadata(OnTrueStateChanged));
        public static readonly DependencyProperty FalseStateProperty = DependencyProperty.Register("FalseState", typeof(string), typeof(DataStateBehavior), new PropertyMetadata(OnFalseStateChanged));

        /// <summary>
        /// Gets or sets the binding that produces the property value of the data object. This is a dependency property.
        /// </summary>
        public object Binding
        {
            get { return this.GetValue(BindingProperty); }
            set { this.SetValue(BindingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value to be compared with the property value of the data object. This is a dependency property.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "This matches the structure of DataTrigger, which this is patterned after.")]
        public object Value
        {
            get { return this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// Gets or sets the name of the visual state to transition to when the condition is met. This is a dependency property.
        /// </summary>
        public string TrueState
        {
            get { return (string)this.GetValue(TrueStateProperty); }
            set { this.SetValue(TrueStateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the name of the visual state to transition to when the condition is not met. This is a dependency property.
        /// </summary>
        public string FalseState
        {
            get { return (string)this.GetValue(FalseStateProperty); }
            set { this.SetValue(FalseStateProperty, value); }
        }

        private FrameworkElement TargetObject
        {
            get
            {
                return VisualStateUtilities.FindNearestStatefulControl(this.AssociatedObject);
            }
        }

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>Override this to hook up functionality to the AssociatedObject.</remarks>
        protected override void OnAttached()
        {
            base.OnAttached();
            this.ValidateStateNamesDeferred();
        }

        private void ValidateStateNamesDeferred()
        {
            FrameworkElement parentElement = this.AssociatedObject.Parent as FrameworkElement;

            if (parentElement != null && IsElementLoaded(parentElement))
            {
                this.ValidateStateNames();
            }
            else
            {
                this.AssociatedObject.Loaded += (o, e) =>
                    {
                        this.ValidateStateNames();
                    };
            }
        }

        // todo jekelly: this is duplicated from Interaction.IsElementLoaded, find some way to share them
        /// <summary>
        /// A helper function to take the place of FrameworkElement.IsLoaded, as this property isn't available in Silverlight.
        /// </summary>
        /// <param name="element">The element of interest.</param>
        /// <returns>Returns true if the element has been loaded; otherwise, returns false.</returns>
        internal static bool IsElementLoaded(FrameworkElement element)
        {
            return element.IsLoaded;
        }

        private void ValidateStateNames()
        {
            this.ValidateStateName(this.TrueState);
            this.ValidateStateName(this.FalseState);
        }

        private void ValidateStateName(string stateName)
        {
            if (this.AssociatedObject != null)
            {
                if (!string.IsNullOrEmpty(stateName))
                {
                    foreach (VisualState state in this.TargetedVisualStates)
                    {
                        if (stateName == state.Name)
                        {
                            return;
                        }
                    }
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                        ExceptionStringTable.DataStateBehaviorStateNameNotFoundExceptionMessage,
                        stateName,
                        this.TargetObject != null ?
                            this.TargetObject.GetType().Name :
                            "null"));
                }
            }
        }

        private IEnumerable<VisualState> TargetedVisualStates
        {
            get
            {
                List<VisualState> states = new List<VisualState>();
                if (this.TargetObject != null)
                {
                    IList groups = VisualStateUtilities.GetVisualStateGroups(this.TargetObject);
                    foreach (VisualStateGroup group in groups)
                    {
                        foreach (VisualState state in group.States)
                        {
                            states.Add(state);
                        }
                    }
                }
                return states;
            }
        }

        private static void OnBindingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            DataStateBehavior dataStateBehavior = (DataStateBehavior)obj;
            dataStateBehavior.Evaluate();
        }

        private static void OnValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            DataStateBehavior dataStateBehavior = (DataStateBehavior)obj;
            dataStateBehavior.Evaluate();
        }

        private static void OnTrueStateChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            DataStateBehavior dataStateBehavior = (DataStateBehavior)obj;
            dataStateBehavior.ValidateStateName(dataStateBehavior.TrueState);
            dataStateBehavior.Evaluate();
        }

        private static void OnFalseStateChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            DataStateBehavior dataStateBehavior = (DataStateBehavior)obj;
            dataStateBehavior.ValidateStateName(dataStateBehavior.FalseState);
            dataStateBehavior.Evaluate();
        }

        private void Evaluate()
        {
            if (this.TargetObject != null)
            {
                string stateName = null;
                if (ComparisonLogic.EvaluateImpl(this.Binding, ComparisonConditionType.Equal, this.Value))
                {
                    stateName = this.TrueState;
                }
                else
                {
                    stateName = this.FalseState;
                }

                VisualStateUtilities.GoToState(this.TargetObject, stateName, true);
            }
        }
    }
}
