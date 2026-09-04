using System;
using System.Collections.Generic;
using System.Linq;
using PSADT.Interop.Tests.TestHelpers;
using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the privilege identifiers, which carry 36 hand-typed values and 36 hand-typed names. The
    /// names double as the strings passed to LookupPrivilegeValue, so they need an oracle of their own.
    /// </summary>
    public sealed class SE_PRIVILEGETests
    {
        /// <summary>
        /// Verifies that the identifiers form an unbroken run from zero, since they are indexed
        /// positionally by name and a gap would silently shift everything after it.
        /// </summary>
        [Fact]
        public void Values_AreAContiguousSequenceFromZero()
        {
            // Arrange
            KeyValuePair<string, long>[] members = EnumMembers.Get(typeof(SE_PRIVILEGE));

            // Assert
            Assert.Equal(36, members.Length);
            EnumMembers.AssertValuesAre(members, [.. Enumerable.Range(0, members.Length).Select(static i => (long)i)]);
        }

        /// <summary>
        /// Verifies that the operating system recognises every privilege name. The names double as the
        /// strings passed to LookupPrivilegeValue, so a typo in one is invisible to any value-based check
        /// and only shows up when the privilege is actually adjusted at runtime.
        /// </summary>
        /// <remarks>
        /// This queries the local security authority and modifies nothing. No privilege is required to
        /// resolve a name to its identifier.
        /// </remarks>
        [Fact]
        public void EveryName_IsRecognisedByTheOperatingSystem()
        {
            // Arrange
            List<string> unrecognised = [];

            // Act
            foreach (KeyValuePair<string, long> member in EnumMembers.Get(typeof(SE_PRIVILEGE)))
            {
                if (string.Equals(member.Key, nameof(SE_PRIVILEGE.SeUnsolicitedInputPrivilege), StringComparison.Ordinal))
                {
                    // SE_UNSOLICITED_INPUT_NAME appears in the Windows headers but the privilege was never
                    // implemented, so the authority has no entry for it and a lookup fails with
                    // ERROR_NO_SUCH_PRIVILEGE. The member mirrors the header correctly; it just cannot be
                    // resolved, which is worth knowing before calling LookupPrivilegeValue with it.
                    continue;
                }

                LUID luid = default;
                Exception? exception = Record.Exception(() => NativeMethods.LookupPrivilegeValue((SE_PRIVILEGE)member.Value, out luid));
                if (exception is not null)
                {
                    unrecognised.Add($"{member.Key}: {exception.GetType().Name}");
                }
                else if (luid is { LowPart: 0, HighPart: 0 })
                {
                    unrecognised.Add($"{member.Key}: resolved to an empty identifier");
                }
            }

            // Assert
            Assert.Empty(unrecognised);
        }
    }
}
