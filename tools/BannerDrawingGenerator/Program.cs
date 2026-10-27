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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace Fluence.Wpf.BannerDrawingGenerator
{
    internal static partial class Program
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
        private static readonly XNamespace Xlink = "http://www.w3.org/1999/xlink";
        private static readonly XNamespace Wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        [STAThread]
        private static async Task Main(string[] args)
        {
            if (args.Length is not 1)
            {
                throw new ArgumentException("Pass the absolute Fluence.WPF repository root.", nameof(args));
            }

            string rootDirectory = Path.GetFullPath(args[0]);
            string assetsDirectory = Path.Combine(rootDirectory, "assets");
            string outputDirectory = Path.Combine(rootDirectory, "Fluence.Wpf.Demo", "Resources");
            XDocument dictionary = new(new XElement(Wpf + "ResourceDictionary",
                new XAttribute(XNamespace.Xmlns + "x", Xaml)));
            byte[]? sharedMark = null;
            byte[]? sharedTagline = null;
            Dictionary<string, byte[]> wordmarks = new(StringComparer.Ordinal);

            foreach (string theme in new[] { "Light", "Dark" })
            {
                string sourcePath = Path.Combine(assetsDirectory, $"Fluence_Banner_{theme}.svg");
                byte[] sourceBytes = ReadBytes(sourcePath);
                XElement svg = XDocument.Load(sourcePath).Root ?? throw new InvalidDataException();
                (DrawingImage drawing, Dictionary<string, byte[]> images) = Build(svg);
                byte[] mark = images["_Image1"];
                byte[] tagline = images["_Image3"];
                if (sharedMark?.SequenceEqual(mark) is false)
                {
                    throw new InvalidDataException("The two banner SVGs embed different mark images.");
                }

                if (sharedTagline?.SequenceEqual(tagline) is false)
                {
                    throw new InvalidDataException("The two banner SVGs embed different tagline images.");
                }

                sharedMark = mark;
                sharedTagline = tagline;
                XElement resource = XElement.Parse(XamlWriter.Save(drawing));
                resource.SetAttributeValue(Xaml + "Key", $"HomeBanner{theme}DrawingImage");
                XElement[] bitmaps = [.. resource.Descendants(Wpf + "BitmapImage")];
                if (bitmaps.Length is not 3)
                {
                    throw new InvalidDataException("Each banner must use exactly three embedded images.");
                }

                string[] fileNames =
                [
                    "Fluence_Banner_EmbeddedMark.png",
                    $"Fluence_Banner_EmbeddedWordmark_{theme}.png",
                    "Fluence_Banner_EmbeddedTagline.png",
                ];
                for (int index = 0; index < bitmaps.Length; index++)
                {
                    XElement bitmap = bitmaps[index];
                    bitmap.Attribute("BaseUri")?.Remove();
                    bitmap.Attribute("CacheOption")?.Remove();
                    bitmap.SetAttributeValue("UriSource",
                        $"pack://application:,,,/Fluence.Wpf.Demo;component/Resources/{fileNames[index]}");
                }

                dictionary.Root!.Add(new XComment($" Generated from Fluence_Banner_{theme}.svg SHA-256 {Convert.ToHexString(SHA256.HashData(sourceBytes)).ToLowerInvariant()} "));
                dictionary.Root.Add(resource);
                wordmarks.Add(fileNames[1], images["_Image2"]);
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{theme}: source SHA-256 {Convert.ToHexString(SHA256.HashData(sourceBytes)).ToLowerInvariant()}, bounds {drawing.Drawing.Bounds}"));
            }

            string xml = ("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + dictionary + "\n")
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n');
            byte[] markup = [.. new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble(), .. Encoding.UTF8.GetBytes(xml)];
            foreach ((string fileName, byte[] wordmark) in wordmarks)
            {
                await File.WriteAllBytesAsync(Path.Combine(outputDirectory, fileName), wordmark, CancellationToken.None).ConfigureAwait(false);
            }

            await File.WriteAllBytesAsync(Path.Combine(outputDirectory, "Fluence_Banner_EmbeddedMark.png"), sharedMark ?? throw new InvalidDataException(), CancellationToken.None).ConfigureAwait(false);
            await File.WriteAllBytesAsync(Path.Combine(outputDirectory, "Fluence_Banner_EmbeddedTagline.png"), sharedTagline ?? throw new InvalidDataException(), CancellationToken.None).ConfigureAwait(false);
            await File.WriteAllBytesAsync(Path.Combine(outputDirectory, "BannerDrawings.xaml"), markup, CancellationToken.None).ConfigureAwait(false);
        }

        private static byte[] ReadBytes(string path)
        {
            using BinaryReader reader = new(File.OpenRead(path));
            return reader.ReadBytes(checked((int)reader.BaseStream.Length));
        }

        private static (DrawingImage Drawing, Dictionary<string, byte[]> Images) Build(XElement svg)
        {
            string[] expectedIds = ["_Image1", "_Image2", "_Image3"];
            XElement[] definitions = [.. svg.Descendants().Where(static x => string.Equals(x.Name.LocalName, "image", StringComparison.Ordinal))];
            string[] uses = [.. svg.Descendants().Where(static x => string.Equals(x.Name.LocalName, "use", StringComparison.Ordinal))
                .Select(static x => (string?)x.Attribute(Xlink + "href") ?? "")];
            if (definitions.Length is not 3 || !uses.SequenceEqual(expectedIds.Select(static id => "#" + id), StringComparer.Ordinal))
            {
                throw new InvalidDataException("The banner must define and use exactly _Image1, _Image2, and _Image3 in that order.");
            }

            Dictionary<string, BitmapImage> bitmaps = new(StringComparer.Ordinal);
            Dictionary<string, byte[]> images = new(StringComparer.Ordinal);
            foreach (XElement definition in definitions)
            {
                string id = (string?)definition.Attribute("id") ?? throw new InvalidDataException("An embedded image has no id.");
                if (!expectedIds.Contains(id, StringComparer.Ordinal) || images.ContainsKey(id))
                {
                    throw new InvalidDataException($"Unsupported or duplicate banner image id: {id}.");
                }

                string href = (string?)definition.Attribute(Xlink + "href") ?? throw new InvalidDataException($"Image {id} has no PNG data.");
                const string dataPrefix = "data:image/png;base64,";
                if (!href.StartsWith(dataPrefix, StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Image {id} is not an embedded PNG.");
                }

                byte[] png = Convert.FromBase64String(href[dataPrefix.Length..]);
                using MemoryStream stream = new(png);
                BitmapImage bitmap = new();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                images.Add(id, png);
                bitmaps.Add(id, bitmap);
            }

            DrawingGroup root = new();
            root.Children.Add(new GeometryDrawing(Brushes.Transparent, pen: null, new RectangleGeometry(new Rect(0, 0, 4800, 2521))));
            foreach (XElement child in svg.Elements())
            {
                if (!string.Equals(child.Name.LocalName, "defs", StringComparison.Ordinal))
                {
                    Add(child, root, inherited: null, bitmaps);
                }
            }
            DrawingImage drawing = new(root);
            return drawing.Drawing.Bounds != new Rect(0, 0, 4800, 2521)
                ? throw new InvalidDataException("The drawing must preserve the full SVG viewBox.")
                : ((DrawingImage Drawing, Dictionary<string, byte[]> Images))(drawing, images);
        }

        private static void Add(XElement node, DrawingGroup parent, Dictionary<string, string>? inherited, IReadOnlyDictionary<string, BitmapImage> bitmaps)
        {
            string type = node.Name.LocalName;
            Dictionary<string, string> style = inherited is null ? new(global::System.StringComparer.Ordinal) : new(inherited, global::System.StringComparer.Ordinal);
            foreach (string piece in ((string?)node.Attribute("style") ?? "").Split(';'))
            {
                string[] pair = piece.Split(':', 2);
                if (pair.Length is 2)
                {
                    style[pair[0].Trim()] = pair[1].Trim();
                }
            }
            if (string.Equals(type, "g", StringComparison.Ordinal))
            {
                DrawingGroup group = new();
                string? transform = (string?)node.Attribute("transform");
                if (transform is not null)
                {
                    double[] values = ParseNumbers(transform);
                    if (values.Length is not 6)
                    {
                        throw new InvalidDataException(transform);
                    }

                    group.Transform = new MatrixTransform(new Matrix(values[0], values[1], values[2], values[3], values[4], values[5]));
                }
                parent.Children.Add(group);
                foreach (XElement child in node.Elements())
                {
                    Add(child, group, style, bitmaps);
                }
            }
            else if (string.Equals(type, "use", StringComparison.Ordinal))
            {
                string href = (string?)node.Attribute(Xlink + "href") ?? throw new InvalidDataException("A banner image use has no href.");
                if (!href.StartsWith('#') || !bitmaps.TryGetValue(href[1..], out BitmapImage? bitmap))
                {
                    throw new InvalidDataException($"Unsupported banner image use: {href}.");
                }

                parent.Children.Add(new ImageDrawing(bitmap, new Rect(Number(node, "x"), Number(node, "y"), Number(node, "width"), Number(node, "height"))));
            }
            else if (string.Equals(type, "path", StringComparison.Ordinal))
            {
                Geometry geometry = Geometry.Parse((string?)node.Attribute("d") ?? throw new InvalidDataException());
                parent.Children.Add(new GeometryDrawing(GetBrush(style), pen: null, geometry));
            }
            else if (string.Equals(type, "text", StringComparison.Ordinal))
            {
                AddText(node, parent, style);
            }
            else if (string.Equals(type, "rect", StringComparison.Ordinal))
            {
                if (!style.TryGetValue("fill", out string? fill) || !string.Equals(fill, "none", StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Only the supplied transparent viewBox rectangle is supported.");
                }
            }
            else
            {
                throw new InvalidDataException(type);
            }
        }

        private static void AddText(XElement node, DrawingGroup parent, Dictionary<string, string> style)
        {
            double x = Number(node, "x");
            double y = Number(node, "y");
            RenderText(node.Nodes().OfType<XText>().FirstOrDefault()?.Value ?? "", ref x, y, parent, style);
            foreach (XElement tspan in node.Elements())
            {
                if (!string.Equals(tspan.Name.LocalName, "tspan", StringComparison.Ordinal) || tspan.Attribute("style") is not null)
                {
                    throw new InvalidDataException("Only unstyled positioned tspan runs are supported.");
                }

                string value = tspan.Value;
                double[] xs = ParseNumbers((string?)tspan.Attribute("x") ?? "");
                double[] ys = ParseNumbers((string?)tspan.Attribute("y") ?? "");
                for (int i = 0; i < value.Length; i++)
                {
                    if (i < xs.Length)
                    {
                        x = xs[i];
                    }

                    if (i < ys.Length)
                    {
                        y = ys[i];
                    }

                    RenderText(value.Substring(i, 1), ref x, y, parent, style);
                }
                XText? tail = tspan.NextNode as XText;
                if (tail is not null && !string.IsNullOrWhiteSpace(tail.Value))
                {
                    RenderText(tail.Value, ref x, y, parent, style);
                }
            }
        }

        private static void RenderText(string value, ref double x, double y, DrawingGroup parent, Dictionary<string, string> style)
        {
            if (value.Length is 0)
            {
                return;
            }

            string family = style["font-family"].Split(',')[^1].Trim().Trim('\'');
            int weight = style.TryGetValue("font-weight", out string? rawWeight) ? int.Parse(rawWeight, Culture) : 400;
            double size = ParseNumbers(style["font-size"])[0];
            Typeface typeface = new(new FontFamily(family), FontStyles.Normal, FontWeight.FromOpenTypeWeight(weight), FontStretches.Normal);
            if (!typeface.TryGetGlyphTypeface(out GlyphTypeface? glyphs) || glyphs is null)
            {
                throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture, $"Required banner font is unavailable: {family} at weight {weight}."));
            }

            FormattedText text = new(value, Culture, FlowDirection.LeftToRight, typeface, size, Brushes.Black, 1);
            Geometry geometry = text.BuildGeometry(new Point(x, y - text.Baseline));
            parent.Children.Add(new GeometryDrawing(GetBrush(style), pen: null, geometry));
            x += text.WidthIncludingTrailingWhitespace;
        }

        private static Brush GetBrush(Dictionary<string, string> style)
        {
            string raw = style.TryGetValue("fill", out string? fill) ? fill : "rgb(0,0,0)";
            if (string.Equals(raw, "none", StringComparison.Ordinal))
            {
                return Brushes.Transparent;
            }

            double[] channels = ParseNumbers(raw);
            return new SolidColorBrush(Color.FromRgb((byte)channels[0], (byte)channels[1], (byte)channels[2]));
        }

        private static double Number(XElement node, string name)
        {
            return ParseNumbers((string?)node.Attribute(name) ?? throw new InvalidDataException(name))[0];
        }

        private static double[] ParseNumbers(string value)
        {
            return [.. NumberPattern.Matches(value).Select(static match => double.Parse(match.Value, Culture))];
        }

        [GeneratedRegex(@"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][-+]?\d+)?")]
        private static partial Regex NumberPattern { get; }
    }
}
