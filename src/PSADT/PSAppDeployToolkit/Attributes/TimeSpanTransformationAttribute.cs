using System;
using System.Globalization;
using System.Management.Automation;
using PSAppDeployToolkit.Utilities;

namespace PSAppDeployToolkit.Attributes
{
    /// <summary>
    /// Transforms numerical values into a <see cref="TimeSpan"/> by treating the value as seconds.
    /// </summary>
    /// <remarks>
    /// This attribute ensures numeric input is interpreted as seconds rather than PowerShell's default tick behavior
    /// for implicit conversions to <see cref="TimeSpan"/>.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3253:Constructor and destructor declarations should not be redundant", Justification = "This primary constructor is required for PowerShell.")]
    public sealed class TimeSpanTransformationAttribute() : ArgumentTransformationAttribute
    {
        /// <summary>
        /// Initializes a new instance of the TimeSpanTransformationAttribute class using the specified culture
        /// information.
        /// </summary>
        /// <param name="cultureInfo">The CultureInfo to use for parsing and formatting time span values. Cannot be null.</param>
        public TimeSpanTransformationAttribute(CultureInfo cultureInfo) : this()
        {
            CultureInfo = cultureInfo;
        }

        /// <summary>
        /// Transforms the input value into a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="engineIntrinsics">The PowerShell engine intrinsics.</param>
        /// <param name="inputData">The input value to transform.</param>
        /// <returns>A <see cref="TimeSpan"/> value derived from the input.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the input value is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the input value cannot be transformed into a TimeSpan.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0015:Specify the parameter name in ArgumentException", Justification = "We don't want a paramter name on these exceptions.")]
        public override object Transform(EngineIntrinsics engineIntrinsics, object? inputData)
        {
            if (!PowerShellUtilities.TryGetBaseObject(inputData, out inputData))
            {
                throw new ArgumentNullException(paramName: null, "Cannot transform null to TimeSpan.");
            }
            if (inputData is TimeSpan timeSpan)
            {
                return timeSpan;
            }
            if (inputData is string valueAsString)
            {
                if (TimeSpan.TryParse(valueAsString, CultureInfo, out TimeSpan parsedTimeSpan))
                {
                    return parsedTimeSpan;
                }
                if (long.TryParse(valueAsString, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedIntegerSeconds))
                {
                    return TimeSpan.FromSeconds(parsedIntegerSeconds);
                }
                if (double.TryParse(valueAsString, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsedNumericalSeconds))
                {
                    return TimeSpan.FromSeconds(parsedNumericalSeconds);
                }
            }
            return !TryGetNumericalSeconds(inputData, out double seconds)
                ? throw new ArgumentException($"Cannot transform value of type '{inputData.GetType().FullName}' to TimeSpan.")
                : TimeSpan.FromSeconds(seconds);
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
        /// Represents the culture-specific information associated with the current context.
        /// </summary>
        public CultureInfo CultureInfo { get; } = CultureInfo.CurrentCulture;
    }
}
