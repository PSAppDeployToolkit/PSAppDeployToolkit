// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors.Core
{
    using System.Windows;

    /// <summary>
    /// Represents one ternary condition.
    /// </summary>
    public class ComparisonCondition : Freezable
    {
        public static readonly DependencyProperty LeftOperandProperty = DependencyProperty.Register("LeftOperand", typeof(object), typeof(ComparisonCondition), new PropertyMetadata(null));
        public static readonly DependencyProperty OperatorProperty = DependencyProperty.Register("Operator", typeof(ComparisonConditionType), typeof(ComparisonCondition), new PropertyMetadata(ComparisonConditionType.Equal));
        public static readonly DependencyProperty RightOperandProperty = DependencyProperty.Register("RightOperand", typeof(object), typeof(ComparisonCondition), new PropertyMetadata(null));

        #region Freezable
        protected override Freezable CreateInstanceCore()
        {
            return new ComparisonCondition();
        }
        #endregion

        /// <summary>
        /// Gets or sets the left operand.
        /// </summary>
        public object LeftOperand
        {
            get { return GetValue(LeftOperandProperty); }
            set { SetValue(LeftOperandProperty, value); }
        }
        /// <summary>
        /// Gets or sets the right operand.
        /// </summary>
        public object RightOperand
        {
            get { return GetValue(RightOperandProperty); }
            set { SetValue(RightOperandProperty, value); }
        }
        /// <summary>
        /// Gets or sets the comparison operator. 
        /// </summary>
        public ComparisonConditionType Operator
        {
            get { return (ComparisonConditionType)GetValue(OperatorProperty); }
            set { SetValue(OperatorProperty, value); }
        }

        /// <summary>
        /// Method that evaluates the condition. Note that this method can throw ArgumentException if the operator is
        /// incompatible with the type. For instance, operators LessThan, LessThanOrEqual, GreaterThan, and GreaterThanOrEqual
        /// require both operators to implement IComparable. 
        /// </summary>
        /// <returns>Returns true if the condition has been met; otherwise, returns false.</returns>
        public bool Evaluate()
        {
            this.EnsureBindingUpToDate();
            return ComparisonLogic.EvaluateImpl(this.LeftOperand, this.Operator, this.RightOperand);
        }

        /// <summary>
        /// Ensure that any binding on DP operands are up-to-date.  
        /// </summary>
        private void EnsureBindingUpToDate()
        {
            DataBindingHelper.EnsureBindingUpToDate(this, LeftOperandProperty);
            DataBindingHelper.EnsureBindingUpToDate(this, OperatorProperty);
            DataBindingHelper.EnsureBindingUpToDate(this, RightOperandProperty);
        }
    }
}