using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the hand-written ShellExecuteEx record, whose only job is to lay its fields out the way the
    /// shell expects.
    /// </summary>
    public sealed class SHELLEXECUTEINFOWTests
    {
        /// <summary>
        /// Verifies that the record is the size the shell checks it against on this architecture. It is
        /// packed to one byte on x86 and to eight on x64, and sequential layout satisfies both, which is
        /// what allows one declaration to serve an AnyCPU assembly.
        /// </summary>
        [Fact]
        public void Size_MatchesTheNativeStructure()
        {
            Assert.Equal(IntPtr.Size is 8 ? 112 : 60, Unsafe.SizeOf<SHELLEXECUTEINFOW>());
        }
    }
}
