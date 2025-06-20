// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Helpers
{
    /// <summary>
    /// Defines a collection of extensions methods for UI.
    /// </summary>
    public static class VisualTree
    {
        /// <summary>
        /// Find descendant <see cref="FrameworkElement"/> control using its name.
        /// </summary>
        /// <param name="element">Parent element.</param>
        /// <param name="name">Name of the control to find</param>
        /// <returns>Descendant control or null if not found.</returns>
        public static FrameworkElement FindDescendantByName(this DependencyObject element, string name)
        {
            if (element == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            if (name.Equals((element as FrameworkElement)?.Name, StringComparison.OrdinalIgnoreCase))
            {
                return element as FrameworkElement;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                var result = VisualTreeHelper.GetChild(element, i).FindDescendantByName(name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Find first descendant control of a specified type.
        /// </summary>
        /// <typeparam name="T">Type to search for.</typeparam>
        /// <param name="element">Parent element.</param>
        /// <returns>Descendant control or null if not found.</returns>
        public static T FindDescendant<T>(this DependencyObject element)
            where T : DependencyObject
        {
            T retValue = null;
            var childrenCount = VisualTreeHelper.GetChildrenCount(element);

            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                var type = child as T;
                if (type != null)
                {
                    retValue = type;
                    break;
                }

                retValue = child.FindDescendant<T>();

                if (retValue != null)
                {
                    break;
                }
            }

            return retValue;
        }

        /// <summary>
        /// Find first descendant control of a specified type.
        /// </summary>
        /// <param name="element">Parent element.</param>
        /// <param name="type">Type of descendant.</param>
        /// <returns>Descendant control or null if not found.</returns>
        public static object FindDescendant(this DependencyObject element, Type type)
        {
            object retValue = null;
            var childrenCount = VisualTreeHelper.GetChildrenCount(element);

            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                if (child.GetType() == type)
                {
                    retValue = child;
                    break;
                }

                retValue = child.FindDescendant(type);

                if (retValue != null)
                {
                    break;
                }
            }

            return retValue;
        }

        /// <summary>
        /// Find all descendant controls of the specified type.
        /// </summary>
        /// <typeparam name="T">Type to search for.</typeparam>
        /// <param name="element">Parent element.</param>
        /// <returns>Descendant controls or empty if not found.</returns>
        public static IEnumerable<T> FindDescendants<T>(this DependencyObject element)
            where T : DependencyObject
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(element);

            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                var type = child as T;
                if (type != null)
                {
                    yield return type;
                }

                foreach (T childofChild in child.FindDescendants<T>())
                {
                    yield return childofChild;
                }
            }
        }

        /// <summary>
        /// Find visual ascendant <see cref="FrameworkElement"/> control using its name.
        /// </summary>
        /// <param name="element">Parent element.</param>
        /// <param name="name">Name of the control to find</param>
        /// <returns>Descendant control or null if not found.</returns>
        public static FrameworkElement FindAscendantByName(this DependencyObject element, string name)
        {
            if (element == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var parent = VisualTreeHelper.GetParent(element);

            if (parent == null)
            {
                return null;
            }

            if (name.Equals((parent as FrameworkElement)?.Name, StringComparison.OrdinalIgnoreCase))
            {
                return parent as FrameworkElement;
            }

            return parent.FindAscendantByName(name);
        }

        /// <summary>
        /// Find first visual ascendant control of a specified type.
        /// </summary>
        /// <typeparam name="T">Type to search for.</typeparam>
        /// <param name="element">Child element.</param>
        /// <returns>Ascendant control or null if not found.</returns>
        public static T FindAscendant<T>(this DependencyObject element)
            where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(element);

            if (parent == null)
            {
                return null;
            }

            if (parent is T)
            {
                return parent as T;
            }

            return parent.FindAscendant<T>();
        }

        /// <summary>
        /// Find first visual ascendant control of a specified type.
        /// </summary>
        /// <param name="element">Child element.</param>
        /// <param name="type">Type of ascendant to look for.</param>
        /// <returns>Ascendant control or null if not found.</returns>
        public static object FindAscendant(this DependencyObject element, Type type)
        {
            var parent = VisualTreeHelper.GetParent(element);

            if (parent == null)
            {
                return null;
            }

            if (parent.GetType() == type)
            {
                return parent;
            }

            return parent.FindAscendant(type);
        }

        /// <summary>
        /// Find all visual ascendants for the element.
        /// </summary>
        /// <param name="element">Child element.</param>
        /// <returns>A collection of parent elements or null if none found.</returns>
        public static IEnumerable<DependencyObject> FindAscendants(this DependencyObject element)
        {
            var parent = VisualTreeHelper.GetParent(element);

            while (parent != null)
            {
                yield return parent;
                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        public static bool DetachFromParent(this FrameworkElement element, DependencyObject parent)
        {
            try
            {
                if (parent is Panel parent_Panel)
                {
                    parent_Panel.Children.Remove(element);
                    return true;
                }
                else if (parent is Decorator parent_Decorator)
                {
                    if(parent_Decorator.Child == element)
                    {
                        parent_Decorator.Child = null;
                        return true;
                    }
                }
                else if (parent is ContentControl parent_ContentControl)
                {
                    parent_ContentControl.Content = null;
                    return true;
                }
                else if (parent is ContentPresenter parent_ContentPresenter)
                {
                    parent_ContentPresenter.Content = null;
                    return true;
                }

                else if (parent is Popup parent_Popup)
                {
                    parent_Popup.Child = null;
                    return true;
                }
                else if (parent is ItemsControl parent_ItemsControl)
                {
                    if (parent_ItemsControl.Items.Contains(element))
                    {
                        parent_ItemsControl.Items.Remove(element);
                        return true;
                    }
                    if (parent_ItemsControl.Items.Contains(element.DataContext))
                    {
                        parent_ItemsControl.Items.Remove(element.DataContext);
                        return true;
                    }
                }
                else
                {
                    var parentType = parent.GetType();
                    var bindFlag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;
                    var props = new List<PropertyInfo>
                    {
                        parentType.GetProperty("Children", bindFlag),
                        parentType.GetProperty("Child", bindFlag),
                        parentType.GetProperty("Content", bindFlag),
                        parentType.GetProperty("Items", bindFlag)
                    };

                    bool isRemovalDone = false;

                    foreach (var prop in props)
                    {
                        switch (prop.Name.ToLower())
                        {
                            case "children":
                                var children = prop.GetValue(parent, null);
                                foreach(var method in children.GetType().GetMethods())
                                {
                                    if(method.Name.ToLower() == "remove")
                                    {
                                        method.Invoke(children, new object[] { element });
                                        isRemovalDone = true;
                                    }
                                }
                                break;
                            case "child":
                            case "content":
                                if (prop.GetValue(parent) == element)
                                {
                                    prop.SetValue(parent, null);
                                    isRemovalDone = true;
                                }
                                break;
                        }
                    }

                    if (isRemovalDone)
                    {
                        return true;
                    }
                }

            }
            catch
            {
                if (Debugger.IsAttached)
                    throw;
            }

            return false;
        }

        public static bool DetachFromLogicalParent(this FrameworkElement element)
        {
            var oldParent = element.Parent;
            DetachFromParent(element, oldParent);

            return element.Parent != oldParent;
        }
        public static bool DetachFromVisualParent(this FrameworkElement element)
        {
            var oldParent = VisualTreeHelper.GetParent(element);
            DetachFromParent(element, oldParent);

            return VisualTreeHelper.GetParent(element) != oldParent;
        }

    }
}
