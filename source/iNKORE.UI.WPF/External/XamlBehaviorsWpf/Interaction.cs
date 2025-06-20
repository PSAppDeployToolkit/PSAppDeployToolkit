// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;
    using System.Diagnostics;

    /// <summary>
    /// Static class that owns the Triggers and Behaviors attached properties. Handles propagation of AssociatedObject change notifications.
    /// </summary>
    public static class Interaction
    {
        /// <summary>
        /// Gets or sets a value indicating whether to run as if in design mode.
        /// </summary>
        /// <value>
        /// 	<c>True</c> if [should run in design mode]; otherwise, <c>False</c>.
        /// </value>
        /// <remarks>Not to be used outside unit tests.</remarks>
        internal static bool ShouldRunInDesignMode
        {
            get;
            set;
        }

        /// <summary>
        /// This property is used as the internal backing store for the public Triggers attached property.
        /// </summary>
        /// <remarks>
        /// This property is not exposed publicly. This forces clients to use the GetTriggers and SetTriggers methods to access the
        /// collection, ensuring the collection exists and is set before it is used.
        /// </remarks>
        private static readonly DependencyProperty TriggersProperty = DependencyProperty.RegisterAttached("ShadowTriggers",
                                                                                                                typeof(TriggerCollection),
                                                                                                                typeof(Interaction),
                                                                                                                new FrameworkPropertyMetadata(
                                                                                                                    new PropertyChangedCallback(OnTriggersChanged)));

        // Note that the parts of the xml document comments must be together in the source, even in the presense of #ifs
        /// <summary>
        /// This property is used as the internal backing store for the public Behaviors attached property.
        /// </summary>
        /// <remarks>
        /// This property is not exposed publicly. This forces clients to use the GetBehaviors and SetBehaviors methods to access the
        /// collection, ensuring the collection exists and is set before it is used.
        /// </remarks>
        private static readonly DependencyProperty BehaviorsProperty = DependencyProperty.RegisterAttached("ShadowBehaviors",
                                                                                                                        typeof(BehaviorCollection),
                                                                                                                        typeof(Interaction),
                                                                                                                        new FrameworkPropertyMetadata(
                                                                                                                            new PropertyChangedCallback(OnBehaviorsChanged)));


        /// <summary>
        /// Gets the TriggerCollection containing the triggers associated with the specified object.
        /// </summary>
        /// <param name="obj">The object from which to retrieve the triggers.</param>
        /// <returns>A TriggerCollection containing the triggers associated with the specified object.</returns>
        public static TriggerCollection GetTriggers(DependencyObject obj)
        {
            TriggerCollection triggerCollection = (TriggerCollection)obj.GetValue(Interaction.TriggersProperty);
            if (triggerCollection == null)
            {
                triggerCollection = new TriggerCollection();
                obj.SetValue(Interaction.TriggersProperty, triggerCollection);
            }
            return triggerCollection;
        }

        /// <summary>
        /// Gets the <see cref="BehaviorCollection"/> associated with a specified object.
        /// </summary>
        /// <param name="obj">The object from which to retrieve the <see cref="BehaviorCollection"/>.</param>
        /// <returns>A <see cref="BehaviorCollection"/> containing the behaviors associated with the specified object.</returns>
        public static BehaviorCollection GetBehaviors(DependencyObject obj)
        {
            BehaviorCollection behaviorCollection = (BehaviorCollection)obj.GetValue(Interaction.BehaviorsProperty);
            if (behaviorCollection == null)
            {
                behaviorCollection = new BehaviorCollection();
                obj.SetValue(Interaction.BehaviorsProperty, behaviorCollection);
            }
            return behaviorCollection;
        }

        /// <exception cref="InvalidOperationException">Cannot host the same BehaviorCollection on more than one object at a time.</exception>
        private static void OnBehaviorsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            BehaviorCollection oldCollection = (BehaviorCollection)args.OldValue;
            BehaviorCollection newCollection = (BehaviorCollection)args.NewValue;

            if (oldCollection != newCollection)
            {
                if (oldCollection != null && ((IAttachedObject)oldCollection).AssociatedObject != null)
                {
                    oldCollection.Detach();
                }

                if (newCollection != null && obj != null)
                {
                    if (((IAttachedObject)newCollection).AssociatedObject != null)
                    {
                        throw new InvalidOperationException(ExceptionStringTable.CannotHostBehaviorCollectionMultipleTimesExceptionMessage);
                    }

                    newCollection.Attach(obj);
                }
            }
        }

        /// <exception cref="InvalidOperationException">Cannot host the same TriggerCollection on more than one object at a time.</exception>
        private static void OnTriggersChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            TriggerCollection oldCollection = args.OldValue as TriggerCollection;
            TriggerCollection newCollection = args.NewValue as TriggerCollection;

            if (oldCollection != newCollection)
            {
                if (oldCollection != null && ((IAttachedObject)oldCollection).AssociatedObject != null)
                {
                    oldCollection.Detach();
                }

                if (newCollection != null && obj != null)
                {
                    if (((IAttachedObject)newCollection).AssociatedObject != null)
                    {
                        throw new InvalidOperationException(ExceptionStringTable.CannotHostTriggerCollectionMultipleTimesExceptionMessage);
                    }

                    newCollection.Attach(obj);
                }
            }
        }

        /// <summary>
        /// A helper function to take the place of FrameworkElement.IsLoaded, as this property is not available in Silverlight.
        /// </summary>
        /// <param name="element">The element of interest.</param>
        /// <returns>True if the element has been loaded; otherwise, False.</returns>
        internal static bool IsElementLoaded(FrameworkElement element)
        {
            return element.IsLoaded;
        }
    }
}
