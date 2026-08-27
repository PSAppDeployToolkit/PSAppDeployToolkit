using System;
using System.Collections.Generic;
using System.Globalization;
using System.Buffers.Binary;
using System.Linq;
using System.Reflection;
using System.Text;
using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the integrity of the hand-maintained constant families rather than their behaviour. Several
    /// of these enumerations carry hundreds of values typed in by hand from Windows headers, where a
    /// transposed digit or a duplicated line produces something that still compiles and still looks
    /// plausible.
    /// </summary>
    /// <remarks>
    /// Every assertion here has an oracle independent of the source. Nothing compares a member against
    /// the same CsWin32 symbol the source aliases, because that could not fail. What is checked instead
    /// is self-consistency: a FourCC that must spell its own name, a sequence that must have no gaps, a
    /// privilege name the operating system must recognise, and the absence of accidental duplicates.
    /// </remarks>
    public sealed class EnumIntegrityTests
    {
        /// <summary>
        /// Verifies that every ACPI table identifier spells its own member name. The values are FourCC
        /// codes stored little-endian, so the name is a complete oracle for the number and a transposed
        /// hex digit cannot survive.
        /// </summary>
        [Fact]
        public void FirmwareTableId_AcpiMembersDecodeToTheirOwnName()
        {
            // Arrange
            const string prefix = "ACPI_";
            KeyValuePair<string, long>[] members = [.. GetMembers(typeof(FIRMWARE_TABLE_ID)).Where(static m => m.Key.StartsWith("ACPI_", StringComparison.Ordinal))];

            // Assert
            Assert.Equal(23, members.Length);
            foreach (KeyValuePair<string, long> member in members)
            {
                byte[] bytes = new byte[4];
                BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)member.Value);
                Assert.Equal(member.Key[prefix.Length..], Encoding.ASCII.GetString(bytes));
            }
        }

        /// <summary>
        /// Verifies that the SMBIOS member is zero. It is the one member that is not a FourCC: the RSMB
        /// provider takes a table identifier of zero, so the value is provider-relative rather than a
        /// packed signature, and the test above deliberately exempts it.
        /// </summary>
        [Fact]
        public void FirmwareTableId_SmbiosIsTheProviderRelativeZero()
        {
            // Assert
            Assert.Equal(0u, (uint)FIRMWARE_TABLE_ID.SMBIOS);
        }

        /// <summary>
        /// Verifies that the system information classes form an unbroken run from zero, and that the
        /// trailing Max member equals the count of the members before it. Almost all 255 values are typed
        /// in by hand, so a skipped or repeated number is the likeliest defect and this catches it
        /// wherever it lands.
        /// </summary>
        [Fact]
        public void SystemInformationClass_IsAContiguousSequenceFromZero()
        {
            // Assert
            AssertContiguousFromZero(typeof(SYSTEM_INFORMATION_CLASS), 255, "MaxSystemInfoClass");
        }

        /// <summary>
        /// Verifies the same invariant for the object information classes, which mix aliased and
        /// hand-typed values in the same run.
        /// </summary>
        [Fact]
        public void ObjectInformationClass_IsAContiguousSequenceFromZero()
        {
            // Assert
            AssertContiguousFromZero(typeof(OBJECT_INFORMATION_CLASS), 9, "MaxObjectInfoClass");
        }

        /// <summary>
        /// Verifies that the privilege identifiers form an unbroken run from zero, since they are indexed
        /// positionally by name and a gap would silently shift everything after it.
        /// </summary>
        [Fact]
        public void SePrivilege_IsAContiguousSequenceFromZero()
        {
            // Arrange
            KeyValuePair<string, long>[] members = GetMembers(typeof(SE_PRIVILEGE));

            // Assert
            Assert.Equal(36, members.Length);
            AssertValuesAre(members, [.. Enumerable.Range(0, members.Length).Select(static i => (long)i)]);
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
        public void SePrivilege_EveryNameIsRecognisedByTheOperatingSystem()
        {
            // Arrange
            List<string> unrecognised = [];

            // Act
            foreach (KeyValuePair<string, long> member in GetMembers(typeof(SE_PRIVILEGE)))
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

        /// <summary>
        /// Verifies that the thread creation flags are the documented single bits, including the gap at
        /// 0x08 which Windows reserves. A hand-typed list of bit values is exactly where a doubled or
        /// skipped shift hides, and the gap makes a naive "each value is twice the last" check useless.
        /// </summary>
        [Fact]
        public void ThreadCreateFlags_AreTheDocumentedSingleBits()
        {
            // Assert
            AssertValuesAre(GetMembers(typeof(THREAD_CREATE_FLAGS)), [0x01L, 0x02L, 0x04L, 0x10L, 0x20L, 0x40L]);
        }

        /// <summary>
        /// Verifies that the extended creation flags are consecutive single bits from the lowest.
        /// </summary>
        [Fact]
        public void ExtendedProcessCreationFlags_AreConsecutiveSingleBits()
        {
            // Assert
            AssertValuesAre(GetMembers(typeof(EXTENDED_PROCESS_CREATION_FLAG)), [0x01L, 0x02L, 0x04L]);
        }

        /// <summary>
        /// Verifies that the only enumerations sharing a value are the ones Windows itself aliases.
        /// </summary>
        /// <remarks>
        /// This is the assertion that covers the alias families, whose members are defined in terms of
        /// CsWin32 symbols. Comparing such a member against the symbol it aliases cannot fail, but
        /// aliasing the wrong symbol can, and it shows up as two names holding one value. The Windows
        /// headers alias liberally, so the expected set below records every pair that is genuinely a
        /// header alias; anything new failing here is a mistake in this repository, not in Windows.
        /// Flag enumerations are excluded, where combined values are normal.
        /// </remarks>
        [Fact]
        public void OrdinaryEnums_ShareValuesOnlyWhereWindowsItselfDoes()
        {
            // Arrange
            string[] expected =
            [
                "IMAGE_FILE_MACHINE: IMAGE_FILE_MACHINE_ALPHA64, IMAGE_FILE_MACHINE_AXP64 = 644",
                "INSTALLSTATE: INSTALLSTATE_ADVERTISED, INSTALLSTATE_REMOVED = 1",
                "SHIL_SIZE: SHIL_JUMBO, SHIL_LAST = 4",
                "SHOW_WINDOW_CMD: SW_FORCEMINIMIZE, SW_MAX = 11",
                "SHOW_WINDOW_CMD: SW_MAXIMIZE, SW_SHOWMAXIMIZED = 3",
                "SHOW_WINDOW_CMD: SW_NORMAL, SW_SHOWNORMAL = 1",
                "WINDOW_MESSAGE: WM_DDE_EXECUTE, WM_DDE_LAST = 1000",
                "WINDOW_MESSAGE: WM_DDE_FIRST, WM_DDE_INITIATE = 992",
                "WINDOW_MESSAGE: WM_IME_COMPOSITION, WM_IME_KEYLAST = 271",
                "WINDOW_MESSAGE: WM_KEYDOWN, WM_KEYFIRST = 256",
                "WINDOW_MESSAGE: WM_MOUSEFIRST, WM_MOUSEMOVE = 512",
                "WINDOW_MESSAGE: WM_SETTINGCHANGE, WM_WININICHANGE = 26",
                "WM_NCHITTEST: HTGROWBOX, HTSIZE = 4",
                "WM_NCHITTEST: HTMAXBUTTON, HTZOOM = 9",
                "WM_NCHITTEST: HTMINBUTTON, HTREDUCE = 8",
            ];
            List<string> actual = [];

            // Act
            foreach (Type type in OrdinaryEnums())
            {
                foreach (IGrouping<long, KeyValuePair<string, long>> group in GetMembers(type).GroupBy(static m => m.Value).Where(static g => g.Skip(1).Any()))
                {
                    string[] names = [.. group.Select(static m => m.Key)];
                    Array.Sort(names, StringComparer.Ordinal);
                    actual.Add($"{type.Name}: {string.Join(", ", names)} = {group.Key.ToString(CultureInfo.InvariantCulture)}");
                }
            }

            // Assert
            string[] sorted = [.. actual];
            Array.Sort(sorted, StringComparer.Ordinal);
            Array.Sort(expected, StringComparer.Ordinal);
            Assert.Equal(expected, sorted);
        }

        /// <summary>
        /// Verifies that the sweep above actually looked at the alias families, so it cannot pass by
        /// examining nothing.
        /// </summary>
        [Fact]
        public void OrdinaryEnums_SweepCoversTheAliasFamilies()
        {
            // Act
            string[] swept = [.. OrdinaryEnums().Select(static t => t.Name)];

            // Assert
            Assert.Contains(nameof(WINDOW_MESSAGE), swept, StringComparer.Ordinal);
            Assert.Contains(nameof(SHOW_WINDOW_CMD), swept, StringComparer.Ordinal);
            Assert.Contains(nameof(SYSTEM_INFORMATION_CLASS), swept, StringComparer.Ordinal);
            Assert.True(swept.Length >= 20, $"expected the sweep to cover at least twenty enumerations, saw {swept.Length}");
        }

        /// <summary>
        /// Verifies that the object attribute flags are the thirteen consecutive bits from the lowest, and
        /// that the validity mask is exactly the flags Windows itself defines.
        /// </summary>
        /// <remarks>
        /// Three of the thirteen are hand-typed from the unofficial headers because CsWin32 does not surface
        /// them. Together the two assertions pin all three: distinct single bits covering an unbroken run
        /// from bit zero leaves each hand-typed flag only one value it can hold, and the mask then confirms
        /// which three they are, since it deliberately excludes the flags that are not valid from user mode.
        /// </remarks>
        [Fact]
        public void ObjectAttributes_AreConsecutiveBitsAndTheMaskExcludesTheUnofficialOnes()
        {
            // Arrange
            KeyValuePair<string, long>[] members = [.. GetMembers(typeof(OBJECT_ATTRIBUTES)).Where(static m => !string.Equals(m.Key, nameof(OBJECT_ATTRIBUTES.OBJ_VALID_ATTRIBUTES), StringComparison.Ordinal))];
            long[] unofficial =
            [
                (long)OBJECT_ATTRIBUTES.OBJ_PROTECT_CLOSE,
                (long)OBJECT_ATTRIBUTES.OBJ_AUDIT_OBJECT_CLOSE,
                (long)OBJECT_ATTRIBUTES.OBJ_NO_RIGHTS_UPGRADE,
            ];

            // Assert: thirteen distinct single bits, occupying an unbroken run from the lowest
            Assert.Equal(13, members.Length);
            foreach (KeyValuePair<string, long> member in members)
            {
                Assert.True(member.Value is not 0 && (member.Value & (member.Value - 1)) is 0, $"{member.Key} is not a single bit");
            }
            Assert.Equal(members.Length, members.Select(static m => m.Value).Distinct().Count());
            Assert.Equal((1L << members.Length) - 1, members.Aggregate(0L, static (bits, m) => bits | m.Value));

            // Assert: the mask is everything except the three the unofficial headers add
            long documented = members.Where(m => !unofficial.Contains(m.Value)).Aggregate(0L, static (mask, m) => mask | m.Value);
            Assert.Equal((long)OBJECT_ATTRIBUTES.OBJ_VALID_ATTRIBUTES, documented);
        }

        /// <summary>
        /// Verifies that the one hand-typed summary-information identifier sits immediately after its
        /// compiler-checked neighbour, which is where the property-set specification puts it. CsWin32 does
        /// not surface this one, so the neighbour is the only oracle available for it.
        /// </summary>
        [Fact]
        public void MsiPropertyId_TheHandTypedIdentifierFollowsItsNeighbour()
        {
            // Assert
            Assert.Equal((uint)MSI_PROPERTY_ID.PID_APPNAME + 1, (uint)MSI_PROPERTY_ID.PID_SECURITY);
        }

        /// <summary>
        /// Enumerates the assembly's own non-flag enumerations, which are the ones a duplicate value
        /// would indicate a mistake in.
        /// </summary>
        /// <returns>The enumerations to sweep.</returns>
        private static IEnumerable<Type> OrdinaryEnums()
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
        private static void AssertValuesAre(KeyValuePair<string, long>[] members, long[] expected)
        {
            long[] actual = [.. members.Select(static m => m.Value)];
            Array.Sort(actual);
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Reads an enumeration's members as name and value pairs. Reflection over the fields is used
        /// rather than Enum.GetValues because the latter collapses members that share a value, which is
        /// precisely what some of these tests are looking for.
        /// </summary>
        /// <param name="type">The enumeration to read.</param>
        /// <returns>The declared members, in declaration order.</returns>
        private static KeyValuePair<string, long>[] GetMembers(Type type)
        {
            return [.. type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(static field => new KeyValuePair<string, long>(
                    field.Name,
                    Convert.ToInt64(field.GetRawConstantValue(), CultureInfo.InvariantCulture)))];
        }

        /// <summary>
        /// Asserts that an enumeration's values run from zero with no gaps or repeats, and that its
        /// trailing maximum member names the last index.
        /// </summary>
        /// <param name="type">The enumeration to check.</param>
        /// <param name="expectedCount">The number of members expected.</param>
        /// <param name="maxMemberName">The name of the trailing maximum member.</param>
        private static void AssertContiguousFromZero(Type type, int expectedCount, string maxMemberName)
        {
            KeyValuePair<string, long>[] members = GetMembers(type);
            Assert.Equal(expectedCount, members.Length);
            AssertValuesAre(members, [.. Enumerable.Range(0, expectedCount).Select(static i => (long)i)]);
            Assert.Equal(expectedCount - 1, members.Single(m => string.Equals(m.Key, maxMemberName, StringComparison.Ordinal)).Value);
        }
    }
}
