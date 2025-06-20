// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors
{
    using System;
    using System.Windows;
    using System.Diagnostics;
    using System.Windows.Media;
    using System.Globalization;

    /// <summary>
    /// Provides data about which objects were affected when resolving a name change.
    /// </summary>
    internal sealed class NameResolvedEventArgs : EventArgs
    {
        private object oldObject;
        private object newObject;

        public object OldObject
        {
            get { return oldObject; }
        }

        public object NewObject
        {
            get { return newObject; }
        }

        public NameResolvedEventArgs(object oldObject, object newObject)
        {
            this.oldObject = oldObject;
            this.newObject = newObject;
        }
    }

    /// <summary>
    /// Helper class to handle the logic of resolving a TargetName into a Target element
    /// based on the context provided by a host element.
    /// </summary>
    internal sealed class NameResolver
    {
        private string name;
        private FrameworkElement nameScopeReferenceElement;

        /// <summary>
        /// Occurs when the resolved element has changed.
        /// </summary>
        public event EventHandler<NameResolvedEventArgs> ResolvedElementChanged;

        /// <summary>
        /// Gets or sets the name of the element to attempt to resolve.
        /// </summary>
        /// <value>The name to attempt to resolve.</value>
        public string Name
        {
            get { return this.name; }
            set
            {
                // because the TargetName influences that Target returns, need to
                // store the Target value before we change it so we can detect if
                // it has actually changed
                DependencyObject oldObject = this.Object;
                this.name = value;
                this.UpdateObjectFromName(oldObject);
            }
        }

        /// <summary>
        /// The resolved object. Will return the reference element if TargetName is null or empty, or if a resolve has not been attempted.
        /// </summary>
        public DependencyObject Object
        {
            get
            {
                if (string.IsNullOrEmpty(this.Name) && this.HasAttempedResolve)
                {
                    return this.NameScopeReferenceElement;
                }
                return this.ResolvedObject;
            }
        }

        /// <summary>
        /// Gets or sets the reference element from which to perform the name resolution.
        /// </summary>
        /// <value>The reference element.</value>
        public FrameworkElement NameScopeReferenceElement
        {
            get { return this.nameScopeReferenceElement; }
            set
            {
                FrameworkElement oldHost = this.NameScopeReferenceElement;
                this.nameScopeReferenceElement = value;
                this.OnNameScopeReferenceElementChanged(oldHost);
            }
        }

        private FrameworkElement ActualNameScopeReferenceElement
        {
            get
            {
                if (this.NameScopeReferenceElement == null || !Interaction.IsElementLoaded(this.NameScopeReferenceElement))
                {
                    return null;
                }
                return GetActualNameScopeReference(this.NameScopeReferenceElement);
            }
        }

        private DependencyObject ResolvedObject
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the reference element load is pending.
        /// </summary>
        /// <value>
        /// 	<c>True</c> if [pending reference element load]; otherwise, <c>False</c>.
        /// </value>
        /// <remarks>
        /// If the Host has not been loaded, the name will not be resolved.
        /// In that case, delay the resolution and track that fact with this property.
        /// </remarks>
        private bool PendingReferenceElementLoad
        {
            get;
            set;
        }

        private bool HasAttempedResolve
        {
            get;
            set;
        }

        private void OnNameScopeReferenceElementChanged(FrameworkElement oldNameScopeReference)
        {
            if (this.PendingReferenceElementLoad)
            {
                oldNameScopeReference.Loaded -= new RoutedEventHandler(OnNameScopeReferenceLoaded);
                this.PendingReferenceElementLoad = false;
            }
            this.HasAttempedResolve = false;
            this.UpdateObjectFromName(this.Object);
        }

        /// <summary>
        /// Attempts to update the resolved object from the name within the context of the namescope reference element.
        /// </summary>
        /// <param name="oldObject">The old resolved object.</param>
        /// <remarks>
        /// Resets the existing target and attempts to resolve the current TargetName from the
        /// context of the current Host. If it cannot resolve from the context of the Host, it will
        /// continue up the visual tree until it resolves. If it has not resolved it when it reaches
        /// the root, it will set the Target to null and write a warning message to Debug output.
        /// </remarks>
        private void UpdateObjectFromName(DependencyObject oldObject)
        {
            DependencyObject newObject = null;

            // clear the cache
            this.ResolvedObject = null;

            if (this.NameScopeReferenceElement != null)
            {
                if (!Interaction.IsElementLoaded(this.NameScopeReferenceElement))
                {
                    // We had a debug message here, but it seems like too common a scenario
                    this.NameScopeReferenceElement.Loaded += new RoutedEventHandler(OnNameScopeReferenceLoaded);
                    this.PendingReferenceElementLoad = true;
                    return;
                }

                // update the target
                if (!string.IsNullOrEmpty(this.Name))
                {
                    FrameworkElement namescopeElement = this.ActualNameScopeReferenceElement;
                    if (namescopeElement != null)
                    {
                        newObject = namescopeElement.FindName(this.Name) as DependencyObject;
                    }

                    if (newObject == null)
                    {
                        Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, ExceptionStringTable.UnableToResolveTargetNameWarningMessage, this.Name));
                    }
                }
            }
            this.HasAttempedResolve = true;
            // note that this.Object will be null if it doesn't resolve
            this.ResolvedObject = newObject;
            if (oldObject != this.Object)
            {
                // this.Source may not be newTarget, if TargetName is null or empty
                this.OnObjectChanged(oldObject, this.Object);
            }
        }

        private void OnObjectChanged(DependencyObject oldTarget, DependencyObject newTarget)
        {
            if (this.ResolvedElementChanged != null)
            {
                this.ResolvedElementChanged(this, new NameResolvedEventArgs(oldTarget, newTarget));
            }
        }

        private FrameworkElement GetActualNameScopeReference(FrameworkElement initialReferenceElement)
        {
            Debug.Assert(Interaction.IsElementLoaded(initialReferenceElement));
            FrameworkElement nameScopeReference = initialReferenceElement;

            if (this.IsNameScope(initialReferenceElement))
            {
                nameScopeReference = initialReferenceElement.Parent as FrameworkElement ?? nameScopeReference;
            }
            return nameScopeReference;
        }

        private bool IsNameScope(FrameworkElement frameworkElement)
        {
            FrameworkElement parentElement = frameworkElement.Parent as FrameworkElement;
            if (parentElement != null)
            {
                // Logic behind this check is as follows:
                // Resolves in Child Scope  |  Resolves in Parent Scope  |  Should use Parent as namescope?
                //			no				|			no				 |			no
                //			yes				|			no				 |			no
                //			no				|			yes				 |			yes
                //			yes				|			yes				 |			yes*
                // * Note that if the resolved element is the same, it doesn't matter if we use the parent or child,
                //   so we choose the parent. If they are different, we've found a name collision across namescopes,
                //	 and our rule is to use the parent as the namescope in that case and discourage people from 
                //	 getting into this state by disallowing creation of targeted types on Control XAML root elements.
                // Hence, we only need to check if Name resolves in the parent scope to know if we need to use the parent.
                object resolvedInParentScope = parentElement.FindName(this.Name);
                return resolvedInParentScope != null;
            }
            return false;
        }

        private void OnNameScopeReferenceLoaded(object sender, RoutedEventArgs e)
        {
            this.PendingReferenceElementLoad = false;
            this.NameScopeReferenceElement.Loaded -= new RoutedEventHandler(OnNameScopeReferenceLoaded);
            this.UpdateObjectFromName(this.Object);
        }
    }
}
