using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace PSADT.Interop.Tests.TestHelpers
{
    /// <summary>
    /// Reads and asserts over the assembly's enumerations. Shared by the per-enumeration test classes,
    /// which all need the same two things: the declared members including any that share a value, and the
    /// sequence invariants those members are checked against.
    /// </summary>
    internal static class EnumMembers
    {
        /// <summary>
        /// Reads an enumeration's members as name and value pairs. Reflection over the fields is used
        /// rather than Enum.GetValues because the latter collapses members that share a value, which is
        /// precisely what some of these tests are looking for.
        /// </summary>
        /// <param name="type">The enumeration to read.</param>
        /// <returns>The declared members, in declaration order.</returns>
        internal static KeyValuePair<string, long>[] Get(Type type)
        {
            return [.. type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(static field => new KeyValuePair<string, long>(
                    field.Name,
                    Convert.ToInt64(field.GetRawConstantValue(), CultureInfo.InvariantCulture)))];
        }

        /// <summary>
        /// Enumerates the assembly's own non-flag enumerations, which are the ones a duplicate value
        /// would indicate a mistake in.
        /// </summary>
        /// <returns>The enumerations to sweep.</returns>
        internal static IEnumerable<Type> Ordinary()
        {
            return typeof(FIRMWARE_TABLE_ID).Assembly.GetTypes()
                .Where(static t => t.IsEnum
                    && string.Equals(t.Namespace, "PSADT.Interop", StringComparison.Ordinal)
                    && !Attribute.IsDefined(t, typeof(FlagsAttribute)));
        }

        /// <summary>
        /// Asserts that an enumeration's values, sorted, are exactly those expected.
        /// </summary>
        /// <param name="members">The members read from the enumeration.</param>
        /// <param name="expected">The values expected, in ascending order.</param>
        internal static void AssertValuesAre(KeyValuePair<string, long>[] members, long[] expected)
        {
            long[] actual = [.. members.Select(static m => m.Value)];
            Array.Sort(actual);
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Asserts that an enumeration's values run from zero with no gaps or repeats, and that its
        /// trailing maximum member names the last index.
        /// </summary>
        /// <param name="type">The enumeration to check.</param>
        /// <param name="expectedCount">The number of members expected.</param>
        /// <param name="maxMemberName">The name of the trailing maximum member.</param>
        internal static void AssertContiguousFromZero(Type type, int expectedCount, string maxMemberName)
        {
            KeyValuePair<string, long>[] members = Get(type);
            Assert.Equal(expectedCount, members.Length);
            AssertValuesAre(members, [.. Enumerable.Range(0, expectedCount).Select(static i => (long)i)]);
            Assert.Equal(expectedCount - 1, members.Single(m => string.Equals(m.Key, maxMemberName, StringComparison.Ordinal)).Value);
        }
    }
}
