using System.Linq;
using PSADT.Interop.Tests.TestHelpers;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the process and thread attribute numbers, seven of whose twenty-eight values are hand-typed
    /// because CsWin32 does not surface them.
    /// </summary>
    public sealed class PROC_THREAD_ATTRIBUTE_NUMTests
    {
        /// <summary>
        /// Verifies that the numbers are the documented sequence, including the gap at twenty and
        /// twenty-one which the Windows headers leave unassigned. The hand-typed values sit between
        /// compiler-checked neighbours; a wrong one that happens not to collide with anything would
        /// otherwise pass unnoticed.
        /// </summary>
        [Fact]
        public void Values_AreTheDocumentedSequenceWithItsGap()
        {
            // Arrange
            long[] expected = [.. Enumerable.Range(0, 20).Select(static i => (long)i), .. Enumerable.Range(22, 8).Select(static i => (long)i)];

            // Assert
            EnumMembers.AssertValuesAre(EnumMembers.Get(typeof(PROC_THREAD_ATTRIBUTE_NUM)), expected);
        }
    }
}
