// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors
{
    using System;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    /// <summary>
    /// Provides design tools information about what <see cref="TriggerBase"/> to instantiate for a given action or command.
    /// </summary>
    [CLSCompliant(false)]
    [AttributeUsage(AttributeTargets.Class |
                    AttributeTargets.Property,
                    AllowMultiple = true)]
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "FxCop is complaining about our single parameter override")]
    public sealed class DefaultTriggerAttribute : Attribute
    {
        private Type targetType;
        private Type triggerType;
        private object[] parameters;

        /// <summary>
        /// Gets the type that this DefaultTriggerAttribute applies to.
        /// </summary>
        /// <value>The type this DefaultTriggerAttribute applies to.</value>
        public Type TargetType
        {
            get { return this.targetType; }
        }

        /// <summary>
        /// Gets the type of the <see cref="TriggerBase"/> to instantiate.
        /// </summary>
        /// <value>The type of the <see cref="TriggerBase"/> to instantiate.</value>
        public Type TriggerType
        {
            get { return this.triggerType; }
        }

        /// <summary>
        /// Gets the parameters to pass to the <see cref="TriggerBase"/> constructor.
        /// </summary>
        /// <value>The parameters to pass to the <see cref="TriggerBase"/> constructor.</value>
        public IEnumerable Parameters
        {
            get { return this.parameters; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTriggerAttribute"/> class.
        /// </summary>
        /// <param name="targetType">The type this attribute applies to.</param>
        /// <param name="triggerType">The type of <see cref="TriggerBase"/> to instantiate.</param>
        /// <param name="parameter">A single argument for the specified <see cref="TriggerBase"/>.</param>
        /// <exception cref="ArgumentException"><c cref="triggerType"/> is not derived from TriggerBase.</exception>
        /// <remarks>This constructor is useful if the specifed <see cref="TriggerBase"/> has a single argument. The
        /// resulting code will be CLS compliant.</remarks>
        public DefaultTriggerAttribute(Type targetType, Type triggerType, object parameter) :
            this(targetType, triggerType, new object[] { parameter })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTriggerAttribute"/> class.
        /// </summary>
        /// <param name="targetType">The type this attribute applies to.</param>
        /// <param name="triggerType">The type of <see cref="TriggerBase"/> to instantiate.</param>
        /// <param name="parameters">The constructor arguments for the specified <see cref="TriggerBase"/>.</param>
        /// <exception cref="ArgumentException"><c cref="triggerType"/> is not derived from TriggerBase.</exception>
        public DefaultTriggerAttribute(Type targetType, Type triggerType, params object[] parameters)
        {
            if (!typeof(TriggerBase).IsAssignableFrom(triggerType))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                                                            ExceptionStringTable.DefaultTriggerAttributeInvalidTriggerTypeSpecifiedExceptionMessage,
                                                            triggerType.Name));
            }

            // todo jekelly: validate that targetType is a valid target for the trigger specified by triggerType

            this.targetType = targetType;
            this.triggerType = triggerType;
            this.parameters = parameters;
        }

        /// <summary>
        /// Instantiates this instance.
        /// </summary>
        /// <returns>The <see cref="TriggerBase"/> specified by the DefaultTriggerAttribute.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Activator.CreateInstance could be calling user code which we don't want to bring us down.")]
        public TriggerBase Instantiate()
        {
            object trigger = null;
            try
            {
                trigger = Activator.CreateInstance(this.TriggerType, this.parameters);
            }
            catch
            {
            }
            return (TriggerBase)trigger;
        }
    }
}
