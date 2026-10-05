using System;
using PSADT.Security;
using Windows.Win32.Foundation;
using Windows.Win32.Security.Authentication.Identity;
using Xunit;

namespace PSADT.Tests.Security
{
    /// <summary>
    /// Verifies that a split logon pair has one canonical form regardless of the order LSA enumerated it in.
    /// </summary>
    public sealed class ProcessTokenReferenceTests
    {
        /// <summary>
        /// Orders a split pair so that a later enumeration still compares equal to the reference it was taken from.
        /// </summary>
        [Fact]
        public void Constructor_OrdersSplitPairCanonically()
        {
            ProcessTokenLogon first = CreateLogon(new LUID { LowPart = 42 });
            ProcessTokenLogon second = CreateLogon(new LUID { LowPart = 43 });
            ProcessTokenReference forwards = new(first, second);
            ProcessTokenReference backwards = new(second, first);
            Assert.Equal(forwards, backwards);
            Assert.Same(first, forwards.Logon);
            Assert.Same(first, backwards.Logon);
            Assert.Same(second, forwards.Counterpart);
            Assert.Same(second, backwards.Counterpart);
        }

        /// <summary>
        /// Orders on the complete identifier rather than its low part alone.
        /// </summary>
        [Fact]
        public void Constructor_OrdersOnTheCompleteIdentifier()
        {
            ProcessTokenLogon low = CreateLogon(new LUID { LowPart = 99, HighPart = 0 });
            ProcessTokenLogon high = CreateLogon(new LUID { LowPart = 1, HighPart = 1 });
            Assert.Same(low, new ProcessTokenReference(high, low).Logon);
            Assert.Same(high, new ProcessTokenReference(low, high).Counterpart);
        }

        /// <summary>
        /// Reports membership for either half and for nothing else, leaving an unsplit logon without a counterpart.
        /// </summary>
        [Fact]
        public void Includes_MatchesEitherHalfOnly()
        {
            ProcessTokenLogon first = CreateLogon(new LUID { LowPart = 42 });
            ProcessTokenLogon second = CreateLogon(new LUID { LowPart = 43 });
            ProcessTokenReference pair = new(first, second);
            Assert.True(pair.Includes(first));
            Assert.True(pair.Includes(second));
            Assert.False(pair.Includes(CreateLogon(new LUID { LowPart = 44 })));
            ProcessTokenReference single = new(first);
            Assert.Null(single.Counterpart);
            Assert.True(single.Includes(first));
            Assert.False(single.Includes(second));
        }

        /// <summary>
        /// Matches on record value rather than instance, because every LSA read produces a separate object.
        /// </summary>
        [Fact]
        public void Includes_MatchesEquivalentRecordsFromSeparateReads()
        {
            ProcessTokenLogon first = CreateLogon(new LUID { LowPart = 42 });
            ProcessTokenLogon reread = CreateLogon(new LUID { LowPart = 42 });
            Assert.NotSame(first, reread);
            Assert.True(new ProcessTokenReference(first).Includes(reread));
        }

        /// <summary>
        /// Refuses a pair built from one logon session twice, which is not a split logon at all.
        /// </summary>
        /// <remarks>Holding the invariant here is what lets the suitability checks assume the two halves are different.</remarks>
        [Fact]
        public void Constructor_RefusesARepeatedLogonSession()
        {
            ProcessTokenLogon logon = CreateLogon(new LUID { LowPart = 42 });
            ProcessTokenLogon reread = CreateLogon(new LUID { LowPart = 42 });
            Assert.NotSame(logon, reread);
            _ = Assert.Throws<ArgumentException>(() => new ProcessTokenReference(logon, logon));
            _ = Assert.Throws<ArgumentException>(() => new ProcessTokenReference(logon, reread));
            Assert.NotNull(new ProcessTokenReference(logon, CreateLogon(new LUID { LowPart = 42, HighPart = 1 })));
        }

        /// <summary>
        /// Creates an original Winlogon record for deterministic tests.
        /// </summary>
        /// <param name="identifier">The logon identifier.</param>
        /// <returns>The logon record.</returns>
        private static ProcessTokenLogon CreateLogon(LUID identifier)
        {
            return new(in identifier, 5, new("S-1-5-21-1-2-3-1001"), SECURITY_LOGON_TYPE.Interactive, Interop.MSV_SUB_AUTHENTICATION_FILTER.LOGON_WINLOGON, 101);
        }
    }
}
