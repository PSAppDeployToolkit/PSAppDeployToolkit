// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;

    public static class DependencyObjectHelper
    {
        /// <summary>
        /// This method will use the VisualTreeHelper.GetParent method to do a depth first walk up 
        /// the visual tree and return all ancestors of the specified object, including the object itself.
        /// </summary>
        /// <param name="dependencyObject">The object in the visual tree to find ancestors of.</param>
        /// <returns>Returns itself an all ancestors in the visual tree.</returns>
        public static IEnumerable<DependencyObject> GetSelfAndAncestors(this DependencyObject dependencyObject)
        {
            // Walk up the visual tree looking for the element.
            while (dependencyObject != null)
            {
                yield return dependencyObject;
                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }
        }
    }
}
