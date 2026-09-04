using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace PSADT.Interop.Tests.TestHelpers
{
    /// <summary>
    /// Asserts the invariants shared by the typed constant families, whose members are declared one per
    /// line from a Windows SDK symbol.
    /// </summary>
    internal static class ConstantFamily
    {
        /// <summary>
        /// Asserts that every declared constant names its own field and holds a value no sibling holds.
        /// </summary>
        /// <remarks>
        /// Both halves matter and neither is a tautology. The name is supplied by the compiler through
        /// CallerMemberName, so a member given an explicit name that disagrees with its field would pass
        /// unnoticed. The values come from SDK symbols, so comparing one against the symbol it was built
        /// from could not fail; picking the wrong symbol can, and that shows up as two members sharing a
        /// value.
        /// </remarks>
        /// <typeparam name="TSelf">The constant family to check.</typeparam>
        /// <param name="expectedCount">The number of constants the family is expected to declare.</param>
        internal static void AssertMembersAreNamedAndDistinct<TSelf>(int expectedCount) where TSelf : TypedConstant<TSelf>
        {
            List<KeyValuePair<string, TSelf>> members = [];
            foreach (FieldInfo field in typeof(TSelf).GetFields(BindingFlags.NonPublic | BindingFlags.Static).Where(static f => f.FieldType == typeof(TSelf)))
            {
                object? value = field.GetValue(null);
                Assert.NotNull(value);
                members.Add(new(field.Name, (TSelf)value));
            }

            Assert.Equal(expectedCount, members.Count);
            foreach (KeyValuePair<string, TSelf> member in members)
            {
                Assert.Equal(member.Key, member.Value.ToString(), StringComparer.Ordinal);
            }
            Assert.Equal(members.Count, members.Select(static m => m.Value.ToIntPtr()).Distinct().Count());
        }
    }
}
