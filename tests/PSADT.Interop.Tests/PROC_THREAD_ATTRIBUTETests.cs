using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the process and thread attributes, each of which is composed by hand from an attribute number
    /// and a set of flags. Pairing a name with the wrong number yields a value that still compiles and
    /// still looks plausible, so the names are the only oracle for which number belongs where.
    /// </summary>
    public sealed class PROC_THREAD_ATTRIBUTETests
    {
        /// <summary>
        /// Verifies that every attribute carries the number of the same name, that the pairing is one to
        /// one, and that every attribute sets the input flag. An attribute defined as a bare number would
        /// be rejected by UpdateProcThreadAttribute at runtime and by nothing at build time.
        /// </summary>
        [Fact]
        public void EveryAttribute_CarriesTheNumberOfTheSameName()
        {
            // Arrange
            Dictionary<string, uint> numbers = typeof(PROC_THREAD_ATTRIBUTE_NUM).GetFields(BindingFlags.Public | BindingFlags.Static)
                .ToDictionary(static f => WithoutSeparators(f.Name, "ProcThreadAttribute"), static f => Convert.ToUInt32(f.GetRawConstantValue(), CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase);
            FieldInfo[] attributes = typeof(PROC_THREAD_ATTRIBUTE).GetFields(BindingFlags.Public | BindingFlags.Static);

            // Assert
            Assert.Equal(numbers.Count, attributes.Length);
            foreach (FieldInfo attribute in attributes)
            {
                string name = WithoutSeparators(attribute.Name, "PROC_THREAD_ATTRIBUTE_");
                Assert.True(numbers.TryGetValue(name, out uint number), $"{attribute.Name} has no PROC_THREAD_ATTRIBUTE_NUM member of the same name.");

                uint value = Convert.ToUInt32(attribute.GetRawConstantValue(), CultureInfo.InvariantCulture);
                Assert.Equal(number, value & Windows.Win32.PInvoke.PROC_THREAD_ATTRIBUTE_NUMBER);
                Assert.Equal(Windows.Win32.PInvoke.PROC_THREAD_ATTRIBUTE_INPUT, value & Windows.Win32.PInvoke.PROC_THREAD_ATTRIBUTE_INPUT);
            }
        }

        /// <summary>
        /// Strips a known prefix and any underscores from a member name, so the two naming conventions used
        /// by the attributes and their numbers can be compared to each other.
        /// </summary>
        /// <param name="name">The member name to reduce.</param>
        /// <param name="prefix">The prefix to remove.</param>
        /// <returns>The name without its prefix or separators.</returns>
        private static string WithoutSeparators(string name, string prefix)
        {
            return name[prefix.Length..].Replace("_", string.Empty, StringComparison.Ordinal);
        }
    }
}
