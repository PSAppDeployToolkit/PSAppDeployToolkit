using System;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.CompilerServices;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// Transforms values into a <see cref="DateTime"/>.
    /// </summary>
    /// <remarks>
    /// String values are parsed using <see cref="CultureInfo.CurrentCulture"/> first, then
    /// <see cref="CultureInfo.InvariantCulture"/>. Numerical values are interpreted as days.
    /// </remarks>
    public sealed class DateTimeTransformationAttribute : ArgumentTransformationAttribute
    {
        /// <summary>
        /// Transforms the input value into a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="engineIntrinsics">The PowerShell engine intrinsics.</param>
        /// <param name="inputData">The input value to transform.</param>
        /// <returns>A <see cref="DateTime"/> value derived from the input.</returns>
        /// <exception cref="ArgumentTransformationMetadataException">Thrown when the input cannot be transformed into a <see cref="DateTime"/>.</exception>
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            if (inputData is null)
            {
                throw new ArgumentTransformationMetadataException("Cannot transform null to DateTime.");
            }
            while (inputData is PSObject psObject)
            {
                inputData = psObject.BaseObject;
            }
            if (inputData is null)
            {
                throw new ArgumentTransformationMetadataException("Cannot transform null to DateTime.");
            }
            if (inputData is DateTime dateTime)
            {
                return dateTime;
            }
            if (inputData is string valueAsString)
            {
                if (DateTime.TryParse(valueAsString, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime currentCultureDateTime))
                {
                    return currentCultureDateTime;
                }
                if (DateTime.TryParse(valueAsString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime invariantCultureDateTime))
                {
                    return invariantCultureDateTime;
                }
                if (TryParseNumericalDays(valueAsString, out double parsedDays))
                {
                    return DateTimeFromDays(parsedDays);
                }
            }
            return !TryGetNumericalDays(inputData, out double days)
                ? throw new ArgumentTransformationMetadataException($"Cannot transform value of type '{inputData.GetType().FullName}' to DateTime.")
                : DateTimeFromDays(days);
        }

        /// <summary>
        /// Attempts to parse the specified string as a number of days using the current culture or the invariant
        /// culture.
        /// </summary>
        /// <param name="value">The string representation of the number of days to parse.</param>
        /// <param name="days">When this method returns, contains the parsed number of days if the conversion succeeded, or zero if it
        /// failed.</param>
        /// <returns>true if the string was successfully parsed as a number of days; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryParseNumericalDays(string value, out double days)
        {
            return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out days) || double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out days);
        }

        /// <summary>
        /// Attempts to extract a numerical value representing days from the specified input data.
        /// </summary>
        /// <param name="inputData">The input object to evaluate.</param>
        /// <param name="days">When this method returns, contains the extracted number of days if the conversion succeeded; otherwise,
        /// zero.</param>
        /// <returns>true if the input data was successfully converted to a numerical value representing days; otherwise,
        /// false.</returns>
        private static bool TryGetNumericalDays(object inputData, out double days)
        {
            switch (inputData)
            {
                case sbyte value:
                    days = value;
                    return true;
                case byte value:
                    days = value;
                    return true;
                case short value:
                    days = value;
                    return true;
                case ushort value:
                    days = value;
                    return true;
                case int value:
                    days = value;
                    return true;
                case uint value:
                    days = value;
                    return true;
                case long value:
                    days = value;
                    return true;
                case ulong value:
                    days = value;
                    return true;
                case float value:
                    days = value;
                    return true;
                case double value:
                    days = value;
                    return true;
                case decimal value:
                    try
                    {
                        days = (double)value;
                        return true;
                    }
                    catch (OverflowException)
                    {
                        days = default;
                        return false;
                    }
                default:
                    days = default;
                    return false;
            }
        }

        /// <summary>
        /// Converts a specified number of days to a <see cref="DateTime"/> instance.
        /// </summary>
        /// <param name="days">The number of days to convert. May be fractional.</param>
        /// <returns>A <see cref="DateTime"/> that represents the specified number of days from <see cref="DateTime.MinValue"/>.</returns>
        /// <exception cref="ArgumentTransformationMetadataException">Thrown when <paramref name="days"/> is outside the valid range for <see cref="DateTime"/>.</exception>
        private static DateTime DateTimeFromDays(double days)
        {
            try
            {
                return DateTime.MinValue.AddDays(days);
            }
            catch (Exception ex) when (ex is ArgumentException or OverflowException)
            {
                throw new ArgumentTransformationMetadataException($"The value '{days}' cannot be represented as a DateTime in days.", ex);
            }
        }
    }
}
