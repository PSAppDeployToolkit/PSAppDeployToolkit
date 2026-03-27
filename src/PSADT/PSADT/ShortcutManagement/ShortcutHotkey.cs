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
using System.Linq;
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
        public ShortcutHotkey(byte keyCode, bool control = false, bool shift = false, bool alt = false, bool extended = false)
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
        public ShortcutHotkey(char key, bool control = false, bool shift = false, bool alt = false, bool extended = false)
        {
            char upperKey = char.ToUpperInvariant(key);
            KeyCode = upperKey switch
            {
                >= 'A' and <= 'Z' => (byte)upperKey,
                >= '0' and <= '9' => (byte)upperKey,
                _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Key must be A-Z or 0-9.")
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
            ArgumentNullException.ThrowIfNull(hotkeyString);
            string[] parts = hotkeyString.Split(['+'], StringSplitOptions.RemoveEmptyEntries);
            bool control = false, shift = false, alt = false; byte keyCode = 0;
            foreach (string part in parts.Select(static part => part.Trim()))
            {
                string upper = part.ToUpperInvariant();
                if (upper is "CTRL" or "CONTROL")
                {
                    control = true;
                }
                else if (upper is "SHIFT")
                {
                    shift = true;
                }
                else if (upper is "ALT")
                {
                    alt = true;
                }
                else
                {
                    // This should be the key.
                    keyCode = ParseKeyCode(part);
                }
            }
            return keyCode == 0
                ? throw new ArgumentException($"No valid key found in hotkey string: '{hotkeyString}'", nameof(hotkeyString))
                : new ShortcutHotkey(keyCode, control, shift, alt);
        }

        /// <summary>
        /// Parses a key name into a virtual key code.
        /// </summary>
        private static byte ParseKeyCode(string keyName)
        {
            // Single character (A-Z, 0-9).
            string upper = keyName.ToUpperInvariant();
            if (upper.Length == 1)
            {
                char c = upper[0];
                return c switch
                {
                    >= 'A' and <= 'Z' => (byte)c,
                    >= '0' and <= '9' => (byte)c,
                    _ => throw new ArgumentException($"Unknown key: '{keyName}'")
                };
            }

            // Function keys.
            if (upper.Length >= 2 && upper.Length <= 3 && upper[0] == 'F' && int.TryParse(upper.AsSpan(1).ToString(), out int fNum) && fNum >= 1 && fNum <= 24)
            {
                return (byte)(0x70 + fNum - 1);
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
                _ => throw new ArgumentException($"Unknown key: '{keyName}'")
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
                control: (modifiers & HOTKEYF.HOTKEYF_CONTROL) != 0,
                shift: (modifiers & HOTKEYF.HOTKEYF_SHIFT) != 0,
                alt: (modifiers & HOTKEYF.HOTKEYF_ALT) != 0,
                extended: (modifiers & HOTKEYF.HOTKEYF_EXT) != 0
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
            _ = sb.Append(GetKeyName(KeyCode));
            return sb.ToString();
        }

        /// <summary>
        /// Gets a human-readable name for a virtual key code.
        /// </summary>
        private static string GetKeyName(byte keyCode)
        {
            return keyCode switch
            {
                // Handle alphanumeric keys (A-Z are 0x41-0x5A, 0-9 are 0x30-0x39).
                >= 0x41 and <= 0x5A => ((char)keyCode).ToString(),
                >= 0x30 and <= 0x39 => ((char)keyCode).ToString(),

                // Handle function keys (F1=0x70 to F24=0x87).
                >= 0x70 and <= 0x87 => $"F{1 + (keyCode - 0x70)}",

                // Handle numpad keys (Num0=0x60 to Num9=0x69).
                >= 0x60 and <= 0x69 => $"Num{keyCode - 0x60}",

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
                _ => $"0x{keyCode:X2}"
            };
        }
    }
}
