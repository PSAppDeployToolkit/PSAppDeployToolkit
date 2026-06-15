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

using Fluence.Wpf.Theming;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Tests.Theming
{
    /// <summary>
    /// Deterministic <c>(ApplicationTheme) -> XAML string</c> serializer for the design-time
    /// color + brush snapshot. It builds the dictionary via
    /// <see cref="FluenceThemeEngine.BuildStandalone"/> (default accent <c>#0078D4</c>,
    /// machine-independent chrome) and emits canonical, ordinal-sorted XAML.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The on-disk generator (<c>RegenerateDesignTimeResources</c>) and the CI drift guard
    /// (<c>DesignTimeResources_AreCurrent</c>) share this one type, so the committed file and the
    /// in-memory comparison are produced identically.
    /// </para>
    /// <para>
    /// Emitted, each section ordinal-sorted by key: every <see cref="Color"/> token; every solid
    /// <see cref="SolidColorBrush"/> twin; the <see cref="LinearGradientBrush"/> elevation brushes;
    /// the <see cref="CornerRadius"/> layout tokens. Deliberately omitted: the runtime-only
    /// <c>AcrylicNoiseBrush</c> (<see cref="ImageBrush"/>), <c>FlyoutShadowEffect</c>
    /// (<see cref="System.Windows.Media.Effects.DropShadowEffect"/>), the focus-visual
    /// <see cref="Style"/> resources (all non-color, not needed for a static color preview), and
    /// the <c>SystemColor*</c> aliases (live <see cref="SystemColors"/> snapshots whose values
    /// track the OS theme and accent, so they would make the committed snapshot machine-dependent
    /// and break the drift guard on CI).
    /// </para>
    /// </remarks>
    internal static class DesignTimeResourceWriter
    {
        /// <summary>
        /// Returns the canonical design-time XAML string for <paramref name="theme"/>.
        /// Must run on the WPF STA thread with an <see cref="Application"/> in scope (pack URIs
        /// in <c>BaseColorTables</c> require it).
        /// </summary>
        /// <param name="theme">The theme to serialize.</param>
        /// <returns>The canonical design-time XAML string for the specified theme.</returns>
        internal static string Generate(ApplicationTheme theme)
        {
            ResourceDictionary dict = FluenceThemeEngine.BuildStandalone(theme);

            List<KeyValuePair<string, Color>> colors = [];
            List<KeyValuePair<string, SolidColorBrush>> brushes = [];
            List<KeyValuePair<string, LinearGradientBrush>> gradients = [];
            List<KeyValuePair<string, CornerRadius>> radii = [];

            foreach (object keyObject in dict.Keys)
            {
                if (keyObject is not string key) { continue; }
                switch (dict[key])
                {
                    case Color c:
                        colors.Add(new KeyValuePair<string, Color>(key, c));
                        break;
                    case CornerRadius r:
                        radii.Add(new KeyValuePair<string, CornerRadius>(key, r));
                        break;
                    case LinearGradientBrush g:
                        gradients.Add(new KeyValuePair<string, LinearGradientBrush>(key, g));
                        break;
                    case SolidColorBrush b when !IsExcludedBrushKey(key):
                        brushes.Add(new KeyValuePair<string, SolidColorBrush>(key, b));
                        break;
                    default:
                        // ImageBrush / DropShadowEffect / Style and the excluded SystemColor*
                        // aliases are intentionally not serialized.
                        break;
                }
            }

            colors.Sort(static (x, y) => string.CompareOrdinal(x.Key, y.Key));
            brushes.Sort(static (x, y) => string.CompareOrdinal(x.Key, y.Key));
            gradients.Sort(static (x, y) => string.CompareOrdinal(x.Key, y.Key));
            radii.Sort(static (x, y) => string.CompareOrdinal(x.Key, y.Key));

            StringBuilder sb = new();
            _ = sb.Append("<ResourceDictionary").Append('\n');
            _ = sb.Append("    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"").Append('\n');
            _ = sb.Append("    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">").Append('\n');
            _ = sb.Append('\n');
            _ = sb.Append("    <!--  AUTO-GENERATED by DesignTimeResourceWriter. Do not edit by hand.  -->").Append('\n');
            _ = sb.Append("    <!--  Complete ").Append(theme.ToString()).Append(" color + brush snapshot (default accent #0078D4) for the XAML designer.  -->").Append('\n');
            _ = sb.Append("    <!--  Regenerate with the RegenerateDesignTimeResources test after an intentional engine change.  -->").Append('\n');

            _ = sb.Append('\n').Append("    <!--  Colors  -->").Append('\n');
            foreach (KeyValuePair<string, Color> kv in colors)
            {
                _ = sb.Append("    <Color x:Key=\"").Append(kv.Key).Append("\">").Append(Hex(kv.Value)).Append("</Color>").Append('\n');
            }

            _ = sb.Append('\n').Append("    <!--  Solid color brushes  -->").Append('\n');
            foreach (KeyValuePair<string, SolidColorBrush> kv in brushes)
            {
                _ = sb.Append("    <SolidColorBrush x:Key=\"").Append(kv.Key).Append("\" Color=\"").Append(Hex(kv.Value.Color)).Append("\" />").Append('\n');
            }

            _ = sb.Append('\n').Append("    <!--  Elevation border brushes  -->").Append('\n');
            foreach (KeyValuePair<string, LinearGradientBrush> kv in gradients)
            {
                AppendGradient(sb, kv.Key, kv.Value);
            }

            _ = sb.Append('\n').Append("    <!--  Layout tokens  -->").Append('\n');
            foreach (KeyValuePair<string, CornerRadius> kv in radii)
            {
                _ = sb.Append("    <CornerRadius x:Key=\"").Append(kv.Key).Append("\">").Append(Radius(kv.Value)).Append("</CornerRadius>").Append('\n');
            }

            _ = sb.Append('\n').Append("</ResourceDictionary>").Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Writes the canonical XAML for <paramref name="theme"/> to its source path as UTF-8 with BOM.
        /// </summary>
        /// <param name="theme">The theme to write to disk.</param>
        internal static void WriteToDisk(ApplicationTheme theme)
        {
            string content = Generate(theme);
            File.WriteAllText(PathFor(theme), content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        /// <summary>
        /// Returns the committed source-tree path of the generated file for <paramref name="theme"/>.
        /// </summary>
        /// <param name="theme">The theme to get the path for.</param>
        /// <returns>The path of the generated XAML file for the specified theme.</returns>
        internal static string PathFor(ApplicationTheme theme)
        {
            return Path.Combine(RepoRoot(), "Fluence.Wpf", "Properties", "DesignTime." + theme.ToString() + ".xaml");
        }

        // Live-OS SystemColors aliases: their snapshot value depends on the host machine's OS theme
        // and accent, so they must not enter the committed (machine-independent) design-time file.
        private static bool IsExcludedBrushKey(string key)
        {
            return key.StartsWith("SystemColor", StringComparison.Ordinal);
        }

        private static void AppendGradient(StringBuilder sb, string key, LinearGradientBrush brush)
        {
            _ = sb.Append("    <LinearGradientBrush x:Key=\"").Append(key)
                  .Append("\" MappingMode=\"").Append(brush.MappingMode.ToString())
                  .Append("\" StartPoint=\"").Append(Pt(brush.StartPoint))
                  .Append("\" EndPoint=\"").Append(Pt(brush.EndPoint)).Append("\">").Append('\n');

            if (brush.RelativeTransform is ScaleTransform scale)
            {
                _ = sb.Append("        <LinearGradientBrush.RelativeTransform>").Append('\n');
                _ = sb.Append("            <ScaleTransform CenterX=\"").Append(Num(scale.CenterX))
                      .Append("\" CenterY=\"").Append(Num(scale.CenterY))
                      .Append("\" ScaleX=\"").Append(Num(scale.ScaleX))
                      .Append("\" ScaleY=\"").Append(Num(scale.ScaleY)).Append("\" />").Append('\n');
                _ = sb.Append("        </LinearGradientBrush.RelativeTransform>").Append('\n');
            }

            foreach (GradientStop stop in brush.GradientStops)
            {
                _ = sb.Append("        <GradientStop Color=\"").Append(Hex(stop.Color))
                      .Append("\" Offset=\"").Append(Num(stop.Offset)).Append("\" />").Append('\n');
            }

            _ = sb.Append("    </LinearGradientBrush>").Append('\n');
        }

        private static string Hex(Color c)
        {
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", c.A, c.R, c.G, c.B);
        }

        private static string Num(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Pt(Point p)
        {
            return Num(p.X) + "," + Num(p.Y);
        }

        private static string Radius(CornerRadius r)
        {
            // Compare formatted strings (not raw doubles) so the analyzer's floating-point-equality
            // rule does not fire; collapse a uniform radius to a single token.
            string tl = Num(r.TopLeft);
            string tr = Num(r.TopRight);
            string br = Num(r.BottomRight);
            string bl = Num(r.BottomLeft);
            bool uniform = string.Equals(tl, tr, StringComparison.Ordinal)
                && string.Equals(tr, br, StringComparison.Ordinal)
                && string.Equals(br, bl, StringComparison.Ordinal);
            return uniform ? tl : tl + "," + tr + "," + br + "," + bl;
        }

        private static string RepoRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Fluence.Wpf.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate Fluence.Wpf.sln ancestor directory from " + AppContext.BaseDirectory);
        }
    }
}
