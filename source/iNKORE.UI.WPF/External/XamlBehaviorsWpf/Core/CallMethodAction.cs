// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors.Core
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using Microsoft.Xaml.Behaviors;

    /// <summary>
    /// Calls a method on a specified object when invoked.
    /// </summary>
    public class CallMethodAction : TriggerAction<DependencyObject>
    {
        private List<MethodDescriptor> methodDescriptors;

        public static readonly DependencyProperty TargetObjectProperty = DependencyProperty.Register("TargetObject", typeof(object), typeof(CallMethodAction), new PropertyMetadata(OnTargetObjectChanged));
        public static readonly DependencyProperty MethodNameProperty = DependencyProperty.Register("MethodName", typeof(string), typeof(CallMethodAction), new PropertyMetadata(OnMethodNameChanged));

        /// <summary>
        /// The object that exposes the method of interest. This is a dependency property.
        /// </summary>
        public object TargetObject
        {
            get { return (object)this.GetValue(TargetObjectProperty); }
            set { this.SetValue(TargetObjectProperty, value); }
        }

        /// <summary>
        /// The name of the method to invoke. This is a dependency property.
        /// </summary>
        public string MethodName
        {
            get { return (string)this.GetValue(MethodNameProperty); }
            set { this.SetValue(MethodNameProperty, value); }
        }

        public CallMethodAction()
        {
            this.methodDescriptors = new List<MethodDescriptor>();
        }

        private object Target
        {
            get
            {
                return this.TargetObject ?? this.AssociatedObject;
            }
        }

        /// <summary>
        /// Invokes the action.
        /// </summary>
        /// <param name="parameter">The parameter of the action. If the action does not require a parameter, the parameter may be set to a null reference.</param>
        ///// <exception cref="ArgumentException">A method with <c cref="MethodName"/> could not be found on the <c cref="TargetObject"/>.</exception>
        protected override void Invoke(object parameter)
        {
            if (this.AssociatedObject != null)
            {
                MethodDescriptor methodDescriptor = this.FindBestMethod(parameter);
                if (methodDescriptor != null)
                {
                    ParameterInfo[] parameters = methodDescriptor.Parameters;

                    // todo jekelly: reconcile these restrictions with spec questions (see below)
                    if (parameters.Length == 0)
                    {
                        methodDescriptor.MethodInfo.Invoke(this.Target, null);
                    }
                    else if (parameters.Length == 2 && this.AssociatedObject != null && parameter != null)
                    {
                        if (parameters[0].ParameterType.IsAssignableFrom(this.AssociatedObject.GetType())
                            && parameters[1].ParameterType.IsAssignableFrom(parameter.GetType()))
                        {

                            methodDescriptor.MethodInfo.Invoke(this.Target, new object[] { this.AssociatedObject, parameter });
                        }
                    }
                }
                else if (this.TargetObject != null)
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                        ExceptionStringTable.CallMethodActionValidMethodNotFoundExceptionMessage,
                        this.MethodName,
                        this.TargetObject.GetType().Name));
                }
            }
        }

        /// <summary>
        /// Called after the action is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>Override this to hook up functionality to the AssociatedObject.</remarks>
        protected override void OnAttached()
        {
            base.OnAttached();
            this.UpdateMethodInfo();
        }

        /// <summary>
        /// Called when the action is getting detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        /// <remarks>Override this to unhook functionality from the AssociatedObject.</remarks>
        protected override void OnDetaching()
        {
            this.methodDescriptors.Clear();
            base.OnDetaching();
        }

        private MethodDescriptor FindBestMethod(object parameter)
        {
            Type parameterType = (parameter == null) ? null : parameter.GetType();

            return this.methodDescriptors.FirstOrDefault((methodDescriptor) =>
                {
                    // todo jekelly: Need spec clarification on if we want to call an (object, EventArgs) overload if there is no parameter or void() sig. Currently, no. (see above)
                    return !methodDescriptor.HasParameters ||
                        (parameter != null &&
                        methodDescriptor.SecondParameterType.IsAssignableFrom(parameter.GetType()));
                });
        }

        private void UpdateMethodInfo()
        {
            this.methodDescriptors.Clear();

            if (this.Target != null && !string.IsNullOrEmpty(this.MethodName))
            {
                Type targetType = this.Target.GetType();
                MethodInfo[] methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    if (!this.IsMethodValid(method))
                    {
                        continue;
                    }

                    ParameterInfo[] methodParams = method.GetParameters();

                    if (!CallMethodAction.AreMethodParamsValid(methodParams))
                    {
                        continue;
                    }

                    this.methodDescriptors.Add(new MethodDescriptor(method, methodParams));
                }

                this.methodDescriptors = this.methodDescriptors.OrderByDescending((methodDescriptor) =>
                {
                    int distanceFromBaseClass = 0;

                    if (methodDescriptor.HasParameters)
                    {
                        Type typeWalker = methodDescriptor.SecondParameterType;
                        while (typeWalker != typeof(EventArgs))
                        {
                            distanceFromBaseClass++;
                            typeWalker = typeWalker.BaseType;
                        }
                    }
                    return methodDescriptor.ParameterCount + distanceFromBaseClass;
                }).ToList();
            }
        }

        private bool IsMethodValid(MethodInfo method)
        {
            if (!string.Equals(method.Name, this.MethodName, StringComparison.Ordinal))
            {
                return false;
            }

            if (method.ReturnType != typeof(void))
            {
                return false;
            }

            return true;
        }

        private static bool AreMethodParamsValid(ParameterInfo[] methodParams)
        {
            if (methodParams.Length == 2)
            {
                if (methodParams[0].ParameterType != typeof(object))
                {
                    return false;
                }

                if (!typeof(EventArgs).IsAssignableFrom(methodParams[1].ParameterType))
                {
                    return false;
                }
            }
            else if (methodParams.Length != 0)
            {
                return false;
            }

            return true;
        }

        private static void OnMethodNameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            CallMethodAction callMethodAction = (CallMethodAction)sender;
            callMethodAction.UpdateMethodInfo();
        }

        private static void OnTargetObjectChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            CallMethodAction callMethodAction = (CallMethodAction)sender;
            callMethodAction.UpdateMethodInfo();
        }

        private class MethodDescriptor
        {
            public MethodInfo MethodInfo
            {
                get;
                private set;
            }

            public bool HasParameters
            {
                get { return this.Parameters.Length > 0; }
            }

            public int ParameterCount
            {
                get { return this.Parameters.Length; }
            }

            public ParameterInfo[] Parameters
            {
                get;
                private set;
            }

            public Type SecondParameterType
            {
                get
                {
                    if (this.Parameters.Length >= 2)
                    {
                        return this.Parameters[1].ParameterType;
                    }
                    return null;
                }
            }

            public MethodDescriptor(MethodInfo methodInfo, ParameterInfo[] methodParams)
            {
                this.MethodInfo = methodInfo;
                this.Parameters = methodParams;
            }
        }
    }
}
