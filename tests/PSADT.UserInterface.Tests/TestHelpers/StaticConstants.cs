using System;
using System.Linq;
using System.Reflection;

namespace PSADT.UserInterface.Tests.TestHelpers
{
    /// <summary>
    /// Reads the shared constants a type declares as public static fields.
    /// </summary>
    /// <remarks>
    /// The dialog result types spell their members out as <c language="csharp">public static readonly</c> fields rather than
    /// as enum members, so the only way to ask what a type offers is to reflect over it. Reading them
    /// through here rather than inline in each test keeps the null handling in one place: the two target
    /// frameworks disagree about whether <see cref="FieldInfo.GetValue"/> returns a nullable, so a
    /// null-forgiving operator is required on one and reported as unnecessary on the other.
    /// </remarks>
    internal static class StaticConstants
    {
        /// <summary>
        /// The name and value of every public static field the type declares, in declaration order.
        /// </summary>
        /// <typeparam name="T">The type whose constants to read.</typeparam>
        /// <returns>One pair per constant.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a field holds something other than a <typeparamref name="T"/>.</exception>
        public static (string Name, T Value)[] Of<T>() where T : class
        {
            return
            [
                .. typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Select(static field => (field.Name, ValueOf<T>(field))),
            ];
        }

        /// <summary>
        /// Reads one static field, insisting it holds what it is declared to.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="field">The field to read.</param>
        /// <returns>The value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the field is null or of another type.</exception>
        private static T ValueOf<T>(FieldInfo field) where T : class
        {
            return field.GetValue(null) as T ?? throw new InvalidOperationException($"'{field.Name}' does not hold a {typeof(T).Name}.");
        }
    }
}
