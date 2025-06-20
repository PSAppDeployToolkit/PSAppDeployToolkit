// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using Microsoft.Xaml.Behaviors.Core;

    internal static class ComparisonLogic
    {
        /// <summary>
        /// This method evaluates operands. 
        /// </summary>
        /// <param name="leftOperand">Left operand from the LeftOperand property.</param>
        /// <param name="operatorType">Operator from Operator property.</param>
        /// <param name="rightOperand">Right operand from the RightOperand property.</param>
        /// <returns>Returns true if the condition is met; otherwise, returns false.</returns>
        internal static bool EvaluateImpl(object leftOperand, ComparisonConditionType operatorType, object rightOperand)
        {
            bool result = false;

            if (leftOperand != null)
            {
                Type leftType = leftOperand.GetType();

                if (rightOperand != null)
                {
                    TypeConverter typeConverter = TypeConverterHelper.GetTypeConverter(leftType);
                    rightOperand = TypeConverterHelper.DoConversionFrom(typeConverter, rightOperand);
                }
            }

            IComparable leftComparableOperand = leftOperand as IComparable;
            IComparable rightComparableOperand = rightOperand as IComparable;

            // If both operands are comparable, use arithmetic comparison
            if (leftComparableOperand != null && rightComparableOperand != null)
            {
                return ComparisonLogic.EvaluateComparable(leftComparableOperand, operatorType, rightComparableOperand);
            }

            switch (operatorType)
            {
                case ComparisonConditionType.Equal:
                    result = object.Equals(leftOperand, rightOperand);
                    break;
                case ComparisonConditionType.NotEqual:
                    result = !object.Equals(leftOperand, rightOperand);
                    break;

                case ComparisonConditionType.GreaterThan:
                case ComparisonConditionType.GreaterThanOrEqual:
                case ComparisonConditionType.LessThan:
                case ComparisonConditionType.LessThanOrEqual:
                    if (leftComparableOperand == null && rightComparableOperand == null)
                    {
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                                                            ExceptionStringTable.InvalidOperands,
                                                            leftOperand != null ? leftOperand.GetType().Name : "null",
                                                            rightOperand != null ? rightOperand.GetType().Name : "null",
                                                            operatorType.ToString()));
                    }
                    else if (leftComparableOperand == null)
                    {
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                                                            ExceptionStringTable.InvalidLeftOperand,
                                                            leftOperand != null ? leftOperand.GetType().Name : "null",
                                                            operatorType.ToString()));
                    }
                    else
                    {
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                                        ExceptionStringTable.InvalidRightOperand,
                                        rightOperand != null ? rightOperand.GetType().Name : "null",
                                        operatorType.ToString()));
                    }
            }
            return result;
        }

        /// <summary>
        /// Evaluates both operands that implement the IComparable interface.
        /// </summary>
        /// <param name="leftOperand">Left operand from the LeftOperand property.</param>
        /// <param name="operatorType">Operator from Operator property.</param>
        /// <param name="rightOperand">Right operand from the RightOperand property.</param>
        /// <returns>Returns true if the condition is met; otherwise, returns false.</returns>
        private static bool EvaluateComparable(IComparable leftOperand, ComparisonConditionType operatorType, IComparable rightOperand)
        {
            object convertedOperand = null;

            try
            {
                convertedOperand = Convert.ChangeType(rightOperand, leftOperand.GetType(), CultureInfo.CurrentCulture);
            }
            catch (FormatException)
            {
                // FormatException: Convert.ChangeType("hello", typeof(double), ...);
            }
            catch (InvalidCastException)
            {
                // InvalidCastException: Convert.ChangeType(4.0d, typeof(Rectangle), ...);
            }

            if (convertedOperand == null)
            {
                return operatorType == ComparisonConditionType.NotEqual;
            }

            int comparison = ((IComparable)leftOperand).CompareTo((IComparable)convertedOperand);
            bool result = false;

            switch (operatorType)
            {
                case ComparisonConditionType.Equal:
                    result = comparison == 0;
                    break;
                case ComparisonConditionType.GreaterThan:
                    result = comparison > 0;
                    break;
                case ComparisonConditionType.GreaterThanOrEqual:
                    result = comparison >= 0;
                    break;
                case ComparisonConditionType.LessThan:
                    result = comparison < 0;
                    break;
                case ComparisonConditionType.LessThanOrEqual:
                    result = comparison <= 0;
                    break;
                case ComparisonConditionType.NotEqual:
                    result = comparison != 0;
                    break;
            }
            return result;
        }
    }
}
