using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PSADT.Extensions
{
    /// <summary>
    /// Provides extension methods for retrieving metadata from enumeration values, such as descriptions defined by
    /// attributes.
    /// </summary>
    internal static class EnumExtensions
    {
        /// <summary>
        /// Retrieves the description specified by the <see cref="DescriptionAttribute"/> for the given enumeration
        /// value.
        /// </summary>
        /// <remarks>This method requires that each enumeration value is decorated with a single <see
        /// cref="DescriptionAttribute"/> containing a non-empty description. It is intended for use with enums that
        /// follow this convention.</remarks>
        /// <param name="value">The enumeration value for which to obtain the description.</param>
        /// <returns>A string containing the description associated with the specified enumeration value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the enumeration value is not defined as a field, does not have exactly one <see
        /// cref="DescriptionAttribute"/>, or if the attribute's description is null, empty, or consists only of
        /// white-space characters.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetDescription(this Enum value)
        {
            return value.GetType().GetField(value.ToString()) is not FieldInfo fieldInfo
                ? throw new InvalidOperationException("The specified enum value is not defined or is a flag value.")
                : fieldInfo.GetCustomAttribute<DescriptionAttribute>() is not DescriptionAttribute descriptionAttribute
                ? throw new InvalidOperationException("The specified enum value does not have a defined Description attribute.")
                : descriptionAttribute.Description is not string description || string.IsNullOrWhiteSpace(description)
                ? throw new InvalidOperationException("The specified enum value does not have a valid Description attribute.")
                : description;
        }
    }
}
