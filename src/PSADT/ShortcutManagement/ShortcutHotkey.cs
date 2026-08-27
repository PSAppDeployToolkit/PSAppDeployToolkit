/*
 * Copyright (C) 2026 Devicie Pty Ltd. All rights reserved.
 *
 * This file is part of PSAppDeployToolkit.
 *
 * PSAppDeployToolkit is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation, either version 3
 * of the License, or (at your option) any later version.
 *
 * PSAppDeployToolkit is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *
 * See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with PSAppDeployToolkit. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Globalization;
using System.Text;
using PSADT.Interop;

namespace PSADT.ShortcutManagement
{
    /// <summary>
    /// Represents a keyboard hotkey combination for a Windows shortcut file.
    /// </summary>
    /// <remarks>
    /// The hotkey is stored as a 16-bit value where the low byte contains the virtual key code
    /// and the high byte contains modifier flags (Shift=0x01, Ctrl=0x02, Alt=0x04, Extended=0x08).
    /// </remarks>
    public readonly record struct ShortcutHotkey
    {
        /// <summary>
        /// Gets the virtual key code for the hotkey.
        /// </summary>
        public byte KeyCode { get; }

        /// <summary>
        /// Gets a value indicating whether the Control modifier is required.
        /// </summary>
        public bool Control { get; }

        /// <summary>
        /// Gets a value indicating whether the Shift modifier is required.
        /// </summary>
        public bool Shift { get; }

        /// <summary>
        /// Gets a value indicating whether the Alt modifier is required.
        /// </summary>
        public bool Alt { get; }

        /// <summary>
        /// Gets a value indicating whether this is an extended key.
        /// </summary>
        public bool Extended { get; }

        /// <summary>
        /// Gets the raw 16-bit hotkey value.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0099:Use Explicit enum value instead of 0", Justification = "There is no zero value for the enums in question.")]
        public ushort Value
        {
            get
            {
                HOTKEYF modifiers = 0;
                if (Shift)
                {
                    modifiers |= HOTKEYF.HOTKEYF_SHIFT;
                }
                if (Control)
                {
                    modifiers |= HOTKEYF.HOTKEYF_CONTROL;
                }
                if (Alt)
                {
                    modifiers |= HOTKEYF.HOTKEYF_ALT;
                }
                if (Extended)
                {
                    modifiers |= HOTKEYF.HOTKEYF_EXT;
                }
                return (ushort)(KeyCode | ((uint)modifiers << 8));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShortcutHotkey"/> struct.
        /// </summary>
        /// <param name="keyCode">The virtual key code.</param>
        /// <param name="control">Whether the Control modifier is required.</param>
        /// <param name="shift">Whether the Shift modifier is required.</param>
        /// <param name="alt">Whether the Alt modifier is required.</param>
        /// <param name="extended">Whether this is an extended key.</param>
        internal ShortcutHotkey(byte keyCode, bool control = false, bool shift = false, bool alt = false, bool extended = false)
        {
            KeyCode = keyCode;
            Control = control;
            Shift = shift;
            Alt = alt;
            Extended = extended;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShortcutHotkey"/> struct using a character key.
        /// </summary>
        /// <param name="key">The character key (A-Z, 0-9).</param>
        /// <param name="control">Whether the Control modifier is required.</param>
        /// <param name="shift">Whether the Shift modifier is required.</param>
        /// <param name="alt">Whether the Alt modifier is required.</param>
        /// <param name="extended">Whether this is an extended key.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the key is not A-Z or 0-9.</exception>
        internal ShortcutHotkey(char key, bool control = false, bool shift = false, bool alt = false, bool extended = false)
        {
            char upperKey = char.ToUpperInvariant(key);
            KeyCode = upperKey switch
            {
                >= 'A' and <= 'Z' => (byte)upperKey,
                >= '0' and <= '9' => (byte)upperKey,
                _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Key must be A-Z or 0-9."),
            };
            Control = control;
            Shift = shift;
            Alt = alt;
            Extended = extended;
        }

        /// <summary>
        /// Parses a hotkey string in the format used by WScript.Shell (e.g., "ALT+CTRL+F", "Ctrl+Shift+Q").
        /// </summary>
        /// <param name="hotkeyString">The hotkey string to parse.</param>
        /// <returns>A new <see cref="ShortcutHotkey"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="hotkeyString"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the hotkey string format is invalid.</exception>
        public static ShortcutHotkey Parse(string hotkeyString)
        {
            // Strip modifier prefixes from the front rather than splitting on the separator. The plus
            // sign is a key in its own right, both as the OEM plus and as the one on the numeric keypad,
            // and splitting on it discards exactly that key.
            ArgumentNullException.ThrowIfNull(hotkeyString);
            ReadOnlySpan<char> remaining = hotkeyString.AsSpan().Trim();
            bool control = false, shift = false, alt = false, extended = false;
            while (TryStripModifier(ref remaining, ref control, ref shift, ref alt, ref extended))
            {
                // Each successful strip consumes one modifier and its separator.
            }

            // Whatever survives naming the key. An empty remainder means the string was modifiers only.
            remaining = remaining.Trim();
            return remaining.IsEmpty
                ? throw new ArgumentException($"No valid key found in hotkey string: '{hotkeyString}'", nameof(hotkeyString))
                : new(ParseKeyCode(remaining.ToString()), control, shift, alt, extended);
        }

        /// <summary>
        /// Removes one leading modifier and its separator from the given span, if one is present.
        /// </summary>
        /// <remarks>
        /// A separator only separates when what precedes it names a modifier. That is what keeps a
        /// trailing key of "+" or "Num+" intact: the text before its separator is either empty or a key
        /// name, neither of which is a modifier, so stripping stops and the remainder is taken whole.
        /// </remarks>
        /// <param name="remaining">The text still to be parsed, advanced past the modifier on success.</param>
        /// <param name="control">Set to <see langword="true"/> if the modifier was control.</param>
        /// <param name="shift">Set to <see langword="true"/> if the modifier was shift.</param>
        /// <param name="alt">Set to <see langword="true"/> if the modifier was alt.</param>
        /// <param name="extended">Set to <see langword="true"/> if the modifier was the extended flag.</param>
        /// <returns><see langword="true"/> if a modifier was consumed; otherwise, <see langword="false"/>.</returns>
        private static bool TryStripModifier(ref ReadOnlySpan<char> remaining, ref bool control, ref bool shift, ref bool alt, ref bool extended)
        {
            int separator = remaining.IndexOf('+');
            if (separator < 0)
            {
                return false;
            }
            ReadOnlySpan<char> candidate = remaining[..separator].Trim();
            if (candidate.Equals("Ctrl".AsSpan(), StringComparison.OrdinalIgnoreCase) || candidate.Equals("Control".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                control = true;
            }
            else if (candidate.Equals("Shift".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                shift = true;
            }
            else if (candidate.Equals("Alt".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                alt = true;
            }
            else if (candidate.Equals("Ext".AsSpan(), StringComparison.OrdinalIgnoreCase) || candidate.Equals("Extended".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                extended = true;
            }
            else
            {
                return false;
            }
            remaining = remaining[(separator + 1)..];
            return true;
        }

        /// <summary>
        /// Parses a key name into a virtual key code.
        /// </summary>
        /// <param name="keyName">The name of the key to parse.</param>
        /// <returns>The virtual key code corresponding to the key name.</returns>
        /// <exception cref="ArgumentException">Thrown when the key name is not recognized.</exception>
        private static byte ParseKeyCode(string keyName)
        {
            // Single character (A-Z, 0-9), or one of the OEM punctuation keys GetKeyName spells out as
            // the character itself.
            string upper = keyName.ToUpperInvariant();
            if (upper.Length is 1)
            {
                char c = upper[0];
                return c switch
                {
                    >= 'A' and <= 'Z' => (byte)c,
                    >= '0' and <= '9' => (byte)c,
                    '+' => 0xBB,
                    ',' => 0xBC,
                    '-' => 0xBD,
                    '.' => 0xBE,
                    _ => throw new ArgumentException("Unknown key.", nameof(keyName)),
                };
            }

            // Function keys.
            if (upper.Length >= 2 && upper.Length <= 3 && upper[0] == 'F' && int.TryParse(upper.AsSpan(1), CultureInfo.InvariantCulture, out int fNum) && fNum >= 1 && fNum <= 24)
            {
                return (byte)(0x70 + fNum - 1);
            }

            // Numeric keypad keys, which GetKeyName prefixes with "Num".
            if (upper.Length is 4 && upper.StartsWith("NUM", StringComparison.Ordinal))
            {
                char c = upper[3];
                return c switch
                {
                    >= '0' and <= '9' => (byte)(0x60 + (c - '0')),
                    '*' => 0x6A,
                    '+' => 0x6B,
                    '-' => 0x6D,
                    '.' => 0x6E,
                    '/' => 0x6F,
                    _ => throw new ArgumentException("Unknown key.", nameof(keyName)),
                };
            }

            // The hexadecimal form GetKeyName falls back to for any code it has no name for.
            if (upper.Length is 3 or 4 && upper.StartsWith("0X", StringComparison.Ordinal))
            {
                return byte.TryParse(upper[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte parsed)
                    ? parsed
                    : throw new ArgumentException("Unknown key.", nameof(keyName));
            }

            // Special keys.
            return upper switch
            {
                "SPACE" => 0x20,
                "ENTER" or "RETURN" => 0x0D,
                "TAB" => 0x09,
                "ESC" or "ESCAPE" => 0x1B,
                "BACKSPACE" or "BACK" => 0x08,
                "DELETE" or "DEL" => 0x2E,
                "INSERT" or "INS" => 0x2D,
                "HOME" => 0x24,
                "END" => 0x23,
                "PAGEUP" or "PGUP" => 0x21,
                "PAGEDOWN" or "PGDN" => 0x22,
                "UP" => 0x26,
                "DOWN" => 0x28,
                "LEFT" => 0x25,
                "RIGHT" => 0x27,
                _ => throw new ArgumentException("Unknown key.", nameof(keyName)),
            };
        }

        /// <summary>
        /// Creates a <see cref="ShortcutHotkey"/> from a raw 16-bit hotkey value.
        /// </summary>
        /// <param name="value">The raw hotkey value.</param>
        /// <returns>A new <see cref="ShortcutHotkey"/> instance.</returns>
        public static ShortcutHotkey FromValue(ushort value)
        {
            byte keyCode = (byte)(value & 0xFF);
            HOTKEYF modifiers = (HOTKEYF)((value >> 8) & 0xFF);
            return new(
                keyCode: keyCode,
                control: modifiers.HasFlag(HOTKEYF.HOTKEYF_CONTROL),
                shift: modifiers.HasFlag(HOTKEYF.HOTKEYF_SHIFT),
                alt: modifiers.HasFlag(HOTKEYF.HOTKEYF_ALT),
                extended: modifiers.HasFlag(HOTKEYF.HOTKEYF_EXT)
            );
        }

        /// <summary>
        /// Creates a <see cref="ShortcutHotkey"/> from a raw 16-bit hotkey value.
        /// </summary>
        /// <param name="value">The raw hotkey value.</param>
        /// <returns>A new <see cref="ShortcutHotkey"/> instance.</returns>
        public static ShortcutHotkey FromUInt16(ushort value)
        {
            return FromValue(value);
        }

        /// <summary>
        /// Converts a <see cref="ShortcutHotkey"/> to its raw 16-bit value.
        /// </summary>
        /// <param name="hotkey">The hotkey to convert.</param>
        public static implicit operator ushort(ShortcutHotkey hotkey)
        {
            return hotkey.Value;
        }

        /// <summary>
        /// Converts this <see cref="ShortcutHotkey"/> to a <see cref="ushort"/> value.
        /// </summary>
        /// <returns>The raw 16-bit hotkey value.</returns>
        public ushort ToUInt16()
        {
            return Value;
        }

        /// <summary>
        /// Converts a raw 16-bit value to a <see cref="ShortcutHotkey"/>.
        /// </summary>
        /// <param name="value">The raw value to convert.</param>
        public static explicit operator ShortcutHotkey(ushort value)
        {
            return FromValue(value);
        }

        /// <summary>
        /// Returns a string that represents the key combination, including any active modifier keys and the associated
        /// key name.
        /// </summary>
        /// <remarks>The returned string includes the names of any modifier keys that are set, in the
        /// order: Control, Shift, Alt. The key name is determined by the value of the KeyCode property. This format is
        /// suitable for display in user interfaces or configuration dialogs where keyboard shortcuts are
        /// shown.</remarks>
        /// <returns>A string describing the key combination, formatted with modifier keys (such as "Ctrl", "Shift", or "Alt")
        /// followed by the key name.</returns>
        public override string ToString()
        {
            StringBuilder sb = new();
            if (Control)
            {
                _ = sb.Append("Ctrl+");
            }
            if (Shift)
            {
                _ = sb.Append("Shift+");
            }
            if (Alt)
            {
                _ = sb.Append("Alt+");
            }
            if (Extended)
            {
                // Emitted so the string form carries the whole modifier byte. Without it a shortcut read
                // through this and written back loses the flag the shell stored.
                _ = sb.Append("Ext+");
            }
            _ = sb.Append(GetKeyName(KeyCode));
            return sb.ToString();
        }

        /// <summary>
        /// Gets a human-readable name for a virtual key code.
        /// </summary>
        /// <param name="keyCode">The virtual key code to get the name for.</param>
        /// <returns>A string representing the name of the key corresponding to the given virtual key code. If the key code is not recognized, it returns a hexadecimal representation of the key code.</returns>
        private static string GetKeyName(byte keyCode)
        {
            return keyCode switch
            {
                // Handle alphanumeric keys (A-Z are 0x41-0x5A, 0-9 are 0x30-0x39).
                >= 0x41 and <= 0x5A => ((char)keyCode).ToString(),
                >= 0x30 and <= 0x39 => ((char)keyCode).ToString(),

                // Handle function keys (F1=0x70 to F24=0x87).
                >= 0x70 and <= 0x87 => $"F{(1 + (keyCode - 0x70)).ToString(CultureInfo.InvariantCulture)}",

                // Handle numpad keys (Num0=0x60 to Num9=0x69).
                >= 0x60 and <= 0x69 => $"Num{(keyCode - 0x60).ToString(CultureInfo.InvariantCulture)}",

                // Handle common special keys.
                0x20 => "Space",
                0x0D => "Enter",
                0x09 => "Tab",
                0x1B => "Esc",
                0x08 => "Backspace",
                0x2E => "Delete",
                0x2D => "Insert",
                0x24 => "Home",
                0x23 => "End",
                0x21 => "PageUp",
                0x22 => "PageDown",
                0x26 => "Up",
                0x28 => "Down",
                0x25 => "Left",
                0x27 => "Right",
                0x6A => "Num*",
                0x6B => "Num+",
                0x6D => "Num-",
                0x6F => "Num/",
                0x6E => "Num.",
                0xBB => "+",
                0xBD => "-",
                0xBC => ",",
                0xBE => ".",
                _ => $"0x{keyCode:X2}",
            };
        }
    }
}
