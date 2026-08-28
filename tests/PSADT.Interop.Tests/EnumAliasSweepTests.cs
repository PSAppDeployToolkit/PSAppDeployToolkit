using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PSADT.Interop.Tests.TestHelpers;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Sweeps every non-flag enumeration in the assembly at once, rather than testing any single one. The
    /// alias families define their members in terms of CsWin32 symbols, so comparing a member against the
    /// symbol it aliases cannot fail; aliasing the wrong symbol can, and it shows up as two names holding
    /// one value.
    /// </summary>
    public sealed class EnumAliasSweepTests
    {
        /// <summary>
        /// Verifies that the only enumerations sharing a value are the ones Windows itself aliases.
        /// </summary>
        /// <remarks>
        /// The Windows headers alias liberally, so the expected set below records every pair that is
        /// genuinely a header alias; anything new failing here is a mistake in this repository, not in
        /// Windows. Flag enumerations are excluded, where combined values are normal.
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
            foreach (Type type in EnumMembers.Ordinary())
            {
                foreach (IGrouping<long, KeyValuePair<string, long>> group in EnumMembers.Get(type).GroupBy(static m => m.Value).Where(static g => g.Skip(1).Any()))
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
            string[] swept = [.. EnumMembers.Ordinary().Select(static t => t.Name)];

            // Assert
            Assert.Contains(nameof(WINDOW_MESSAGE), swept, StringComparer.Ordinal);
            Assert.Contains(nameof(SHOW_WINDOW_CMD), swept, StringComparer.Ordinal);
            Assert.Contains(nameof(SYSTEM_INFORMATION_CLASS), swept, StringComparer.Ordinal);
            Assert.True(swept.Length >= 20, $"expected the sweep to cover at least twenty enumerations, saw {swept.Length}");
        }
    }
}
