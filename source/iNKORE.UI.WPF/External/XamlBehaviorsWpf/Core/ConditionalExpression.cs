// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors.Core
{
    using System.Windows;
    using System.Windows.Markup;

    /// <summary>
    /// Forward chaining.
    /// </summary>
    public enum ForwardChaining
    {
        And,
        Or
    }

    /// <summary>
    /// Represents a conditional expression that is set on a ConditionBehavior.Condition property. 
    /// Contains a list of conditions that gets evaluated in order to return true or false for ICondition.Evaluate().
    /// </summary>
    [ContentProperty("Conditions")]
    public class ConditionalExpression :
        Freezable,
        ICondition
    {
        public static readonly DependencyProperty ConditionsProperty = DependencyProperty.Register("Conditions", typeof(ConditionCollection), typeof(ConditionalExpression), new PropertyMetadata(null));
        public static readonly DependencyProperty ForwardChainingProperty = DependencyProperty.Register("ForwardChaining", typeof(ForwardChaining), typeof(ConditionalExpression), new PropertyMetadata(ForwardChaining.And));

        #region Freezable
        protected override Freezable CreateInstanceCore()
        {
            return new ConditionalExpression();
        }
        #endregion

        /// <summary>
        /// Gets or sets forward chaining for the conditions.
        /// If forward chaining is set to ForwardChaining.And, all conditions must be met.
        /// If forward chaining is set to ForwardChaining.Or, only one condition must be met.		
        /// </summary>
        public ForwardChaining ForwardChaining
        {
            get { return (ForwardChaining)GetValue(ForwardChainingProperty); }
            set { SetValue(ForwardChainingProperty, value); }
        }
        /// <summary>
        /// Return the Condition collections.
        /// </summary>
        public ConditionCollection Conditions
        {
            get { return (ConditionCollection)GetValue(ConditionsProperty); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionalExpression"/> class.
        /// </summary>
        public ConditionalExpression()
        {
            this.SetValue(ConditionalExpression.ConditionsProperty, new ConditionCollection());
        }

        /// <summary>
        /// Goes through the Conditions collection and evalutes each condition based on 
        /// ForwardChaining property.
        /// </summary>
        /// <returns>Returns true if conditions are met; otherwise, returns false.</returns>
        public bool Evaluate()
        {
            bool result = false;
            foreach (ComparisonCondition operation in this.Conditions)
            {
                result = operation.Evaluate();

                if (result == false && this.ForwardChaining == Core.ForwardChaining.And)
                {
                    return result;
                }

                if (result == true && this.ForwardChaining == Core.ForwardChaining.Or)
                {
                    return result;
                }
            }

            return result;
        }
    }
}
