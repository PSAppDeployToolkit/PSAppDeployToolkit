using System;
using System.Linq;
using System.Reflection;

namespace PSAppDeployToolkit.Tests.TestHelpers
{
    /// <summary>
    /// Reads the values an enumeration declares.
    /// </summary>
    /// <remarks>
    /// Read from the type's fields rather than through the generic <c>Enum.GetValues&lt;T&gt;</c>, which is not
    /// available on every framework this project builds for.
    /// </remarks>
    internal static class EnumValues
    {
        /// <summary>
        /// The values declared by an enumeration.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration to read.</typeparam>
        /// <returns>Its declared values.</returns>
        internal static TEnum[] Declared<TEnum>() where TEnum : struct, Enum
        {
            return [.. typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static).Select(static field => (TEnum)(field.GetRawConstantValue() ?? default(TEnum)))];
        }

        /// <summary>
        /// The names declared by an enumeration, in declaration order.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration to read.</typeparam>
        /// <returns>Its declared names.</returns>
        internal static string[] DeclaredNames<TEnum>() where TEnum : struct, Enum
        {
            return [.. typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static).Select(static field => field.Name)];
        }
    }
}
