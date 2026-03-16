using System;
using System.Management.Automation;

namespace PSAppDeployToolkit.Attributes
{
    /// <summary>
    /// Transforms numerical values into a <see cref="TimeSpan"/> by treating the value as seconds.
    /// </summary>
    /// <remarks>
    /// This attribute ensures numeric input is interpreted as seconds rather than PowerShell's default tick behavior
    /// for implicit conversions to <see cref="TimeSpan"/>.
    /// </remarks>
    public sealed class TimeSpanTransformationAttribute : ArgumentTransformationAttribute
    {
        /// <summary>
        /// Transforms the input value into a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="engineIntrinsics">The PowerShell engine intrinsics.</param>
        /// <param name="inputData">The input value to transform.</param>
        /// <returns>A <see cref="TimeSpan"/> value derived from the input.</returns>
        /// <exception cref="ArgumentTransformationMetadataException">Thrown when the input cannot be transformed into a <see cref="TimeSpan"/>.</exception>
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            while (inputData is PSObject psObject)
            {
                inputData = psObject.BaseObject;
            }
            if (inputData is null)
            {
                throw new ArgumentNullException(null, "Cannot transform null to TimeSpan.");
            }
            if (inputData is TimeSpan timeSpan)
            {
                return timeSpan;
            }
            if (inputData is string valueAsString)
            {
                if (TimeSpan.TryParse(valueAsString, out TimeSpan parsedTimeSpan))
                {
                    return parsedTimeSpan;
                }
                if (long.TryParse(valueAsString, out long parsedIntegerSeconds))
                {
                    return TimeSpanFromSeconds(parsedIntegerSeconds);
                }
                if (double.TryParse(valueAsString, out double parsedNumericalSeconds))
                {
                    return TimeSpanFromSeconds(parsedNumericalSeconds);
                }
            }
            return !TryGetNumericalSeconds(inputData, out double seconds)
                ? throw new ArgumentException($"Cannot transform value of type '{inputData.GetType().FullName}' to TimeSpan.")
                : TimeSpanFromSeconds(seconds);
        }

        /// <summary>
        /// Attempts to extract a numerical value representing seconds from the specified input data.
        /// </summary>
        /// <param name="inputData">The input object to evaluate. Supported types are sbyte, byte, short, ushort, int, uint, long, ulong, float,
        /// double, and decimal.</param>
        /// <param name="seconds">When this method returns, contains the extracted number of seconds if the conversion succeeded; otherwise,
        /// zero.</param>
        /// <returns>true if the input data was successfully converted to a numerical value representing seconds; otherwise,
        /// false.</returns>
        private static bool TryGetNumericalSeconds(object inputData, out double seconds)
        {
            switch (inputData)
            {
                case sbyte value:
                    seconds = value;
                    return true;
                case byte value:
                    seconds = value;
                    return true;
                case short value:
                    seconds = value;
                    return true;
                case ushort value:
                    seconds = value;
                    return true;
                case int value:
                    seconds = value;
                    return true;
                case uint value:
                    seconds = value;
                    return true;
                case long value:
                    seconds = value;
                    return true;
                case ulong value:
                    seconds = value;
                    return true;
                case float value:
                    seconds = value;
                    return true;
                case double value:
                    seconds = value;
                    return true;
                case decimal value:
                    try
                    {
                        seconds = (double)value;
                        return true;
                    }
                    catch (OverflowException)
                    {
                        seconds = default;
                        return false;
                    }
                default:
                    seconds = default;
                    return false;
            }
        }

        /// <summary>
        /// Converts a specified number of seconds to a <see cref="TimeSpan"/> instance.
        /// </summary>
        /// <param name="seconds">The number of seconds to convert. May be fractional. Must be within the valid range for <see
        /// cref="TimeSpan.FromSeconds(double)"/>.</param>
        /// <returns>A <see cref="TimeSpan"/> that represents the specified number of seconds.</returns>
        /// <exception cref="ArgumentTransformationMetadataException">Thrown when <paramref name="seconds"/> is outside the valid range for <see cref="TimeSpan"/> or is not a
        /// valid value.</exception>
        private static TimeSpan TimeSpanFromSeconds(double seconds)
        {
            try
            {
                return TimeSpan.FromSeconds(seconds);
            }
            catch (Exception ex) when (ex is ArgumentException or OverflowException)
            {
                throw new ArgumentOutOfRangeException($"The value '{seconds}' cannot be represented as a TimeSpan in seconds.", ex);
            }
        }
    }
}
