using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace PSADT.UserInterface.Tests.TestHelpers
{
    /// <summary>
    /// Reads the members an enum declares.
    /// </summary>
    /// <remarks>
    /// <see cref="Enum.GetValues(Type)"/> returns distinct values, so a member that is an alias of another
    /// disappears from it. These tests care about what is declared, which is what the fields give.
    /// </remarks>
    internal static class EnumValues
    {
        /// <summary>
        /// The values of every member the enum declares, aliases included, in declaration order.
        /// </summary>
        /// <typeparam name="TEnum">The enum to read.</typeparam>
        /// <returns>One value per declared member.</returns>
        public static TEnum[] Declared<TEnum>() where TEnum : struct, Enum
        {
            return [.. typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static).Select(static f => (TEnum)(f.GetRawConstantValue() ?? default(TEnum)))];
        }

        /// <summary>
        /// The names of every member the enum declares, in declaration order.
        /// </summary>
        /// <typeparam name="TEnum">The enum to read.</typeparam>
        /// <returns>One name per declared member.</returns>
        public static string[] DeclaredNames<TEnum>() where TEnum : struct, Enum
        {
            return [.. typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static).Select(static f => f.Name)];
        }

        /// <summary>
        /// The name and numeric value of every member the enum declares, in declaration order.
        /// </summary>
        /// <remarks>
        /// This is the shape each enum's test compares an expected table against. Naming and value are
        /// asserted together in one array so that a member renamed, renumbered, added, removed or
        /// reordered all fail the same single assertion, and the failure prints the whole enum rather
        /// than the first member that differs.
        /// <para>
        /// Widened to <see cref="ulong"/> so the same helper serves the enums backed by <see cref="int"/>
        /// and the three backed by <see cref="uint"/>. None of them declare a negative member; one that
        /// did would throw here rather than compare wrongly.
        /// </para>
        /// </remarks>
        /// <typeparam name="TEnum">The enum to read.</typeparam>
        /// <returns>One name and value pair per declared member.</returns>
        public static (string Name, ulong Value)[] DeclaredPairs<TEnum>() where TEnum : struct, Enum
        {
            return
            [
                .. typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Select(static f => (f.Name, Convert.ToUInt64(f.GetRawConstantValue(), CultureInfo.InvariantCulture))),
            ];
        }
    }
}
