using System;
using PSADT.ShortcutManagement;
using Xunit;

namespace PSADT.Tests.ShortcutManagement
{
    /// <summary>
    /// Tests the hotkey value shell links and internet shortcuts store, and the string form they store
    /// it through.
    /// </summary>
    /// <remarks>
    /// The string form is not cosmetic. <c>ShellLinkFile.Hotkey</c> and
    /// <c>InternetShortcutFile.Hotkey</c> both read by calling <see cref="ShortcutHotkey.ToString"/> and
    /// write by calling <see cref="ShortcutHotkey.Parse"/>, so reading a shortcut and writing it back
    /// unchanged goes through both. Anything the pair cannot represent losslessly is silently altered on
    /// the way through, which is why the round-trip theory below covers the whole modifier space and
    /// every name the formatter can emit rather than a sample of them.
    /// </remarks>
    public sealed class ShortcutHotkeyTests
    {
        /// <summary>
        /// Verifies that the packed value splits into the key code in the low byte and the modifiers in
        /// the high byte, which is the layout the shell stores.
        /// </summary>
        /// <param name="value">The packed value to decompose.</param>
        /// <param name="keyCode">The expected key code.</param>
        /// <param name="control">Whether the control modifier is expected.</param>
        /// <param name="shift">Whether the shift modifier is expected.</param>
        /// <param name="alt">Whether the alt modifier is expected.</param>
        /// <param name="extended">Whether the extended flag is expected.</param>
        [Theory]
        [InlineData(0x0041, (byte)0x41, false, false, false, false)]
        [InlineData(0x0141, (byte)0x41, false, true, false, false)]
        [InlineData(0x0241, (byte)0x41, true, false, false, false)]
        [InlineData(0x0441, (byte)0x41, false, false, true, false)]
        [InlineData(0x0841, (byte)0x41, false, false, false, true)]
        [InlineData(0x0341, (byte)0x41, true, true, false, false)]
        [InlineData(0x0741, (byte)0x41, true, true, true, false)]
        [InlineData(0x0F41, (byte)0x41, true, true, true, true)]
        [InlineData(0x0270, (byte)0x70, true, false, false, false)]
        [InlineData(0x00FF, (byte)0xFF, false, false, false, false)]
        public void FromValue_SplitsTheKeyCodeFromTheModifiers(int value, byte keyCode, bool control, bool shift, bool alt, bool extended)
        {
            // Act
            ShortcutHotkey hotkey = ShortcutHotkey.FromValue((ushort)value);

            // Assert
            Assert.Equal(keyCode, hotkey.KeyCode);
            Assert.Equal(control, hotkey.Control);
            Assert.Equal(shift, hotkey.Shift);
            Assert.Equal(alt, hotkey.Alt);
            Assert.Equal(extended, hotkey.Extended);
        }

        /// <summary>
        /// Verifies that the packed value is rebuilt from the parts, so the property is the exact
        /// inverse of the decomposition above.
        /// </summary>
        /// <param name="value">The packed value the parts should rebuild.</param>
        /// <param name="keyCode">The key code to supply.</param>
        /// <param name="control">Whether to set the control modifier.</param>
        /// <param name="shift">Whether to set the shift modifier.</param>
        /// <param name="alt">Whether to set the alt modifier.</param>
        /// <param name="extended">Whether to set the extended flag.</param>
        [Theory]
        [InlineData(0x0041, (byte)0x41, false, false, false, false)]
        [InlineData(0x0141, (byte)0x41, false, true, false, false)]
        [InlineData(0x0241, (byte)0x41, true, false, false, false)]
        [InlineData(0x0441, (byte)0x41, false, false, true, false)]
        [InlineData(0x0841, (byte)0x41, false, false, false, true)]
        [InlineData(0x0F41, (byte)0x41, true, true, true, true)]
        [InlineData(0x00FF, (byte)0xFF, false, false, false, false)]
        public void Value_PacksTheModifiersIntoTheHighByte(int value, byte keyCode, bool control, bool shift, bool alt, bool extended)
        {
            // Arrange
            ShortcutHotkey hotkey = ShortcutHotkey.FromValue((ushort)((keyCode & 0xFF) | (((control ? 0x02 : 0) | (shift ? 0x01 : 0) | (alt ? 0x04 : 0) | (extended ? 0x08 : 0)) << 8)));

            // Act & Assert
            Assert.Equal((ushort)value, hotkey.Value);
            Assert.Equal((ushort)value, hotkey.ToUInt16());
            Assert.Equal((ushort)value, (ushort)hotkey);
        }

        /// <summary>
        /// Verifies that every packed value survives a trip out to the parts and back, across the whole
        /// modifier space and the whole key code range.
        /// </summary>
        [Fact]
        public void FromValue_RoundTripsEveryPackedValue()
        {
            for (int modifiers = 0; modifiers <= 0x0F; modifiers++)
            {
                for (int keyCode = 0; keyCode <= 0xFF; keyCode++)
                {
                    ushort value = (ushort)(keyCode | (modifiers << 8));
                    Assert.Equal(value, ShortcutHotkey.FromValue(value).Value);
                }
            }
        }

        /// <summary>
        /// Verifies that the alternative spellings of the conversion members agree with each other, so
        /// callers in PowerShell and in C# get the same result.
        /// </summary>
        [Fact]
        public void ConversionMembers_AgreeWithEachOther()
        {
            // Arrange
            const ushort value = 0x0341;

            // Act
            ShortcutHotkey fromValue = ShortcutHotkey.FromValue(value);
            ShortcutHotkey fromUInt16 = ShortcutHotkey.FromUInt16(value);
            ShortcutHotkey fromCast = (ShortcutHotkey)value;

            // Assert
            Assert.Equal(fromValue, fromUInt16);
            Assert.Equal(fromValue, fromCast);
            Assert.Equal(fromValue.Value, fromValue.ToUInt16());
        }

        /// <summary>
        /// Verifies that the modifier tokens are recognised in both spellings and in any case, and that
        /// they combine.
        /// </summary>
        /// <param name="input">The hotkey string to parse.</param>
        /// <param name="expected">The packed value it should produce.</param>
        [Theory]
        [InlineData("A", 0x0041)]
        [InlineData("Ctrl+A", 0x0241)]
        [InlineData("Control+A", 0x0241)]
        [InlineData("CTRL+A", 0x0241)]
        [InlineData("ctrl+a", 0x0241)]
        [InlineData("Shift+A", 0x0141)]
        [InlineData("SHIFT+A", 0x0141)]
        [InlineData("Alt+A", 0x0441)]
        [InlineData("ALT+A", 0x0441)]
        [InlineData("Ctrl+Shift+A", 0x0341)]
        [InlineData("Ctrl+Alt+A", 0x0641)]
        [InlineData("Shift+Alt+A", 0x0541)]
        [InlineData("Ctrl+Shift+Alt+A", 0x0741)]
        [InlineData("Alt+Shift+Ctrl+A", 0x0741)]
        [InlineData(" Ctrl + Shift + A ", 0x0341)]
        public void Parse_RecognisesModifiers(string input, int expected)
        {
            Assert.Equal((ushort)expected, ShortcutHotkey.Parse(input).Value);
        }

        /// <summary>
        /// Verifies that the extended flag survives parsing, since the formatter emits it and the shell
        /// stores it in the same byte as the other modifiers.
        /// </summary>
        /// <param name="input">The hotkey string to parse.</param>
        /// <param name="expected">The packed value it should produce.</param>
        [Theory]
        [InlineData("Ext+A", 0x0841)]
        [InlineData("Extended+A", 0x0841)]
        [InlineData("Ctrl+Ext+A", 0x0A41)]
        [InlineData("Ctrl+Shift+Alt+Ext+A", 0x0F41)]
        public void Parse_RecognisesTheExtendedFlag(string input, int expected)
        {
            Assert.Equal((ushort)expected, ShortcutHotkey.Parse(input).Value);
        }

        /// <summary>
        /// Verifies that the key names the formatter can emit all parse back to the code they came from.
        /// </summary>
        /// <param name="input">The key name to parse.</param>
        /// <param name="expected">The key code it names.</param>
        [Theory]
        // Alphanumerics, which are their own virtual key codes.
        [InlineData("A", (byte)0x41)]
        [InlineData("a", (byte)0x41)]
        [InlineData("Z", (byte)0x5A)]
        [InlineData("0", (byte)0x30)]
        [InlineData("9", (byte)0x39)]
        // Function keys, at both ends of the range.
        [InlineData("F1", (byte)0x70)]
        [InlineData("f1", (byte)0x70)]
        [InlineData("F12", (byte)0x7B)]
        [InlineData("F24", (byte)0x87)]
        // Named keys, including the abbreviations only the parser accepts.
        [InlineData("Space", (byte)0x20)]
        [InlineData("Enter", (byte)0x0D)]
        [InlineData("Return", (byte)0x0D)]
        [InlineData("Tab", (byte)0x09)]
        [InlineData("Esc", (byte)0x1B)]
        [InlineData("Escape", (byte)0x1B)]
        [InlineData("Backspace", (byte)0x08)]
        [InlineData("Back", (byte)0x08)]
        [InlineData("Delete", (byte)0x2E)]
        [InlineData("Del", (byte)0x2E)]
        [InlineData("Insert", (byte)0x2D)]
        [InlineData("Ins", (byte)0x2D)]
        [InlineData("Home", (byte)0x24)]
        [InlineData("End", (byte)0x23)]
        [InlineData("PageUp", (byte)0x21)]
        [InlineData("PgUp", (byte)0x21)]
        [InlineData("PageDown", (byte)0x22)]
        [InlineData("PgDn", (byte)0x22)]
        [InlineData("Up", (byte)0x26)]
        [InlineData("Down", (byte)0x28)]
        [InlineData("Left", (byte)0x25)]
        [InlineData("Right", (byte)0x27)]
        // Numpad keys, which the formatter emits and so the parser has to accept.
        [InlineData("Num0", (byte)0x60)]
        [InlineData("Num9", (byte)0x69)]
        [InlineData("Num*", (byte)0x6A)]
        [InlineData("Num+", (byte)0x6B)]
        [InlineData("Num-", (byte)0x6D)]
        [InlineData("Num.", (byte)0x6E)]
        [InlineData("Num/", (byte)0x6F)]
        // The four OEM punctuation keys the formatter names by their character.
        [InlineData("+", (byte)0xBB)]
        [InlineData("-", (byte)0xBD)]
        [InlineData(",", (byte)0xBC)]
        [InlineData(".", (byte)0xBE)]
        // The hexadecimal fallback the formatter emits for anything unnamed.
        [InlineData("0x01", (byte)0x01)]
        [InlineData("0xAD", (byte)0xAD)]
        [InlineData("0xff", (byte)0xFF)]
        public void Parse_RecognisesEveryKeyNameTheFormatterEmits(string input, byte expected)
        {
            Assert.Equal(expected, ShortcutHotkey.Parse(input).KeyCode);
        }

        /// <summary>
        /// Verifies that a key whose name is itself the separator parses, which the shell can store for
        /// the OEM plus key and for the numeric keypad plus.
        /// </summary>
        /// <param name="input">The hotkey string to parse.</param>
        /// <param name="expected">The packed value it should produce.</param>
        [Theory]
        [InlineData("Ctrl++", 0x02BB)]
        [InlineData("Ctrl+Num+", 0x026B)]
        [InlineData("Shift+Alt++", 0x05BB)]
        public void Parse_RecognisesAKeyNamedAfterTheSeparator(string input, int expected)
        {
            Assert.Equal((ushort)expected, ShortcutHotkey.Parse(input).Value);
        }

        /// <summary>
        /// Verifies that a hotkey string naming no key at all is rejected, rather than yielding a
        /// modifier-only value the shell cannot act on.
        /// </summary>
        /// <param name="input">The hotkey string to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Ctrl")]
        [InlineData("Ctrl+")]
        [InlineData("Ctrl+Shift")]
        public void Parse_RejectsAStringNamingNoKey(string input)
        {
            _ = Assert.Throws<ArgumentException>(() => ShortcutHotkey.Parse(input));
        }

        /// <summary>
        /// Verifies that an unrecognised key name is rejected rather than silently becoming some other
        /// key.
        /// </summary>
        /// <param name="input">The hotkey string to reject.</param>
        [Theory]
        [InlineData("Nonsense")]
        [InlineData("F0")]
        [InlineData("F25")]
        [InlineData("Ctrl+Nonsense")]
        [InlineData("0x100")]
        [InlineData("0xZZ")]
        public void Parse_RejectsAnUnknownKeyName(string input)
        {
            _ = Assert.Throws<ArgumentException>(() => ShortcutHotkey.Parse(input));
        }

        /// <summary>
        /// Verifies that a null hotkey string is rejected as a null argument rather than as a bad value.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Parse_RejectsNull()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => ShortcutHotkey.Parse(null!));
        }

        /// <summary>
        /// Verifies the names the formatter gives each range of key codes, since those names are what
        /// ends up in the shortcut file.
        /// </summary>
        /// <param name="keyCode">The key code to format.</param>
        /// <param name="expected">The name it should be given.</param>
        [Theory]
        [InlineData((byte)0x41, "A")]
        [InlineData((byte)0x5A, "Z")]
        [InlineData((byte)0x30, "0")]
        [InlineData((byte)0x39, "9")]
        [InlineData((byte)0x70, "F1")]
        [InlineData((byte)0x87, "F24")]
        [InlineData((byte)0x60, "Num0")]
        [InlineData((byte)0x69, "Num9")]
        [InlineData((byte)0x6A, "Num*")]
        [InlineData((byte)0x6B, "Num+")]
        [InlineData((byte)0x6D, "Num-")]
        [InlineData((byte)0x6E, "Num.")]
        [InlineData((byte)0x6F, "Num/")]
        [InlineData((byte)0x20, "Space")]
        [InlineData((byte)0x0D, "Enter")]
        [InlineData((byte)0x09, "Tab")]
        [InlineData((byte)0x1B, "Esc")]
        [InlineData((byte)0x08, "Backspace")]
        [InlineData((byte)0x2E, "Delete")]
        [InlineData((byte)0x2D, "Insert")]
        [InlineData((byte)0x24, "Home")]
        [InlineData((byte)0x23, "End")]
        [InlineData((byte)0x21, "PageUp")]
        [InlineData((byte)0x22, "PageDown")]
        [InlineData((byte)0x26, "Up")]
        [InlineData((byte)0x28, "Down")]
        [InlineData((byte)0x25, "Left")]
        [InlineData((byte)0x27, "Right")]
        [InlineData((byte)0xBB, "+")]
        [InlineData((byte)0xBD, "-")]
        [InlineData((byte)0xBC, ",")]
        [InlineData((byte)0xBE, ".")]
        [InlineData((byte)0x01, "0x01")]
        [InlineData((byte)0xAD, "0xAD")]
        public void ToString_NamesTheKeyCode(byte keyCode, string expected)
        {
            Assert.Equal(expected, ShortcutHotkey.FromValue(keyCode).ToString());
        }

        /// <summary>
        /// Verifies the order and spelling of the modifier prefixes, which is the half of the string
        /// form a human reads.
        /// </summary>
        /// <param name="value">The packed value to format.</param>
        /// <param name="expected">The string it should produce.</param>
        [Theory]
        [InlineData(0x0041, "A")]
        [InlineData(0x0241, "Ctrl+A")]
        [InlineData(0x0141, "Shift+A")]
        [InlineData(0x0441, "Alt+A")]
        [InlineData(0x0341, "Ctrl+Shift+A")]
        [InlineData(0x0741, "Ctrl+Shift+Alt+A")]
        [InlineData(0x0841, "Ext+A")]
        [InlineData(0x0F41, "Ctrl+Shift+Alt+Ext+A")]
        public void ToString_OrdersTheModifiersConsistently(int value, string expected)
        {
            Assert.Equal(expected, ShortcutHotkey.FromValue((ushort)value).ToString());
        }

        /// <summary>
        /// Verifies that formatting and parsing are inverse across every value the shell can store,
        /// which is the property the shortcut types rely on when they read a hotkey and write it back.
        /// </summary>
        /// <remarks>
        /// A key code of zero is excluded: the shortcut types treat a packed value of zero as "no
        /// hotkey" and never format one, and a modifier-only value is not a hotkey the shell can
        /// dispatch.
        /// </remarks>
        [Fact]
        public void ToString_RoundTripsThroughParseForEveryStorableValue()
        {
            for (int modifiers = 0; modifiers <= 0x0F; modifiers++)
            {
                for (int keyCode = 1; keyCode <= 0xFF; keyCode++)
                {
                    ushort value = (ushort)(keyCode | (modifiers << 8));
                    string formatted = ShortcutHotkey.FromValue(value).ToString();
                    Assert.Equal(value, ShortcutHotkey.Parse(formatted).Value);
                }
            }
        }

        /// <summary>
        /// Verifies that two hotkeys built from the same value are equal, since the type is a value used
        /// as a dictionary key and compared by the shortcut snapshots.
        /// </summary>
        [Fact]
        public void Equality_IsByValue()
        {
            // Arrange
            ShortcutHotkey left = ShortcutHotkey.FromValue(0x0341);
            ShortcutHotkey right = ShortcutHotkey.FromValue(0x0341);
            ShortcutHotkey different = ShortcutHotkey.FromValue(0x0342);

            // Assert
            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
            Assert.NotEqual(left, different);
        }
    }
}
