using System;
using System.Linq;
using System.Reflection;

namespace PSADT.Tests.TestHelpers
{
    /// <summary>
    /// Reads the values an enumeration declares.
    /// </summary>
    /// <remarks>
    /// Read from the type's fields rather than through the generic <c language="csharp">Enum.IsDefined</c> or
    /// <c language="csharp">Enum.GetValues&lt;T&gt;</c>, neither of which is available on every target framework this project
    /// builds for.
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
            return [.. typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static).Select(static f => (TEnum)(f.GetRawConstantValue() ?? default(TEnum)))];
        }
    }
}
