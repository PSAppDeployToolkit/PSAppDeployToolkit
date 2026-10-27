/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Fluence.WindowProbe
{
    internal sealed class ProbeOptions
    {
        internal string Mode { get; private set; } = "";
        internal string Theme { get; set; } = "Light";
        internal string RenderMode { get; private set; } = "default";
        internal string Size { get; private set; } = "fixed";
        internal string Backdrop { get; set; } = "N/A";
        internal string Output { get; private set; } = "";
        internal bool Topmost { get; private set; }
        internal bool ExplicitTargetClear { get; private set; } = true;
        internal bool FullGlass { get; private set; } = true;
        internal bool DwmAttribute { get; private set; } = true;
        internal bool ForceTargetOpaque { get; private set; }
        internal int AutoCloseMs { get; private set; }
        internal int? ProbeBackdropSetHResult { get; set; }
        internal string ProbeBackdropSetStatus { get; set; } = "not issued";
        internal int? ProbeDarkModeSetHResult { get; set; }
        internal int? ProbeDarkModePrimaryHResult { get; set; }
        internal int? ProbeDarkModeFallbackHResult { get; set; }
        internal string ProbeDarkModeSetStatus { get; set; } = "not issued";
        internal int? ProbeCornerSetHResult { get; set; }
        internal string ProbeCornerSetStatus { get; set; } = "not issued";

        internal static string Usage => "Usage: Fluence.WindowProbe --mode plain|chrome-dwm|fluence|wpf-fluent --output ABSOLUTE-PATH [--theme Light|Dark] [--render-mode default|software-only] [--size fixed|content] [--topmost on|off] [--backdrop None|Mica|Acrylic|Tabbed (chrome-dwm/fluence only)] [--explicit-target-clear on|off] [--full-glass on|off] [--dwm-attribute on|off] [--force-target-opaque on|off] (chrome-dwm only) [--auto-close-ms N]";

        internal static ProbeOptions Parse(string[] args)
        {
            Dictionary<string, string> values = new(StringComparer.Ordinal);
            for (int index = 0; index < args.Length; index += 2)
            {
                if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal) || values.ContainsKey(args[index]))
                {
                    throw new ArgumentException("Expected unique --name value pairs.", nameof(args));
                }

                values.Add(args[index], args[index + 1]);
            }

            ProbeOptions options = new();
            foreach (KeyValuePair<string, string> item in values)
            {
                switch (item.Key)
                {
                    case "--mode": options.Mode = Pick(item.Value, "plain", "chrome-dwm", "fluence", "wpf-fluent"); break;
                    case "--theme": options.Theme = Pick(item.Value, "Light", "Dark"); break;
                    case "--render-mode": options.RenderMode = Pick(item.Value, "default", "software-only"); break;
                    case "--size": options.Size = Pick(item.Value, "fixed", "content"); break;
                    case "--backdrop": options.Backdrop = Pick(item.Value, "None", "Mica", "Acrylic", "Tabbed"); break;
                    case "--output": options.Output = item.Value; break;
                    case "--topmost": options.Topmost = OnOff(item.Value); break;
                    case "--explicit-target-clear": options.ExplicitTargetClear = OnOff(item.Value); break;
                    case "--full-glass": options.FullGlass = OnOff(item.Value); break;
                    case "--dwm-attribute": options.DwmAttribute = OnOff(item.Value); break;
                    case "--force-target-opaque": options.ForceTargetOpaque = OnOff(item.Value); break;
                    case "--auto-close-ms":
                        if (!int.TryParse(item.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int milliseconds) || milliseconds < 0 || milliseconds > 600000)
                        {
                            throw new ArgumentException("--auto-close-ms must be between 0 and 600000.", nameof(args));
                        }

                        options.AutoCloseMs = milliseconds;
                        break;
                    default: throw new ArgumentException("Unknown option: " + item.Key, nameof(args));
                }
            }

            string? root = Path.GetPathRoot(options.Output);
            if (options.Mode.Length is 0 || options.Output.Length is 0 || !Path.IsPathRooted(options.Output) || root is null || root.Length < 3)
            {
                throw new ArgumentException("--mode and an absolute --output path are required.", nameof(args));
            }

            bool materialMode = string.Equals(options.Mode, "chrome-dwm", StringComparison.Ordinal) || string.Equals(options.Mode, "fluence", StringComparison.Ordinal);
            if (!materialMode && values.ContainsKey("--backdrop"))
            {
                throw new ArgumentException("--backdrop requires chrome-dwm or fluence.", nameof(args));
            }

            if (materialMode && !values.ContainsKey("--backdrop"))
            {
                options.Backdrop = "Mica";
            }

            if (!string.Equals(options.Mode, "chrome-dwm", StringComparison.Ordinal) && (values.ContainsKey("--explicit-target-clear") || values.ContainsKey("--full-glass") || values.ContainsKey("--dwm-attribute") || values.ContainsKey("--force-target-opaque")))
            {
                throw new ArgumentException("Chrome diagnostic switches require chrome-dwm.", nameof(args));
            }

#if !NET10_0_OR_GREATER
            if (string.Equals(options.Mode, "wpf-fluent", StringComparison.Ordinal))
            {
                throw new ArgumentException("wpf-fluent requires the .NET 10 target.", nameof(args));
            }
#endif
            return options;
        }

        private static string Pick(string value, params string[] allowed)
        {
            foreach (string candidate in allowed)
            {
                if (string.Equals(value, candidate, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            throw new ArgumentException("Invalid value: " + value, nameof(value));
        }

        private static bool OnOff(string value)
        {
            return value switch
            {
                "on" => true,
                "off" => false,
                _ => throw new ArgumentException("Expected on or off: " + value, nameof(value)),
            };
        }
    }
}
