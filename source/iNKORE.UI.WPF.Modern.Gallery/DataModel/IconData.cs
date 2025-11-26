// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Windows.Media;
using iNKORE.UI.WPF.Modern.Common.IconKeys;

namespace iNKORE.UI.WPF.Modern.Gallery.DataModel
{
    public class IconData
    {
        public string Name { get; set; }
        // Which icon set this icon came from (e.g. "SegoeFluentIcons", "FluentSystemIcons.Regular")
        public string Set { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        // The actual font to use for rendering this glyph (important for Fluent System Icons)
        public FontFamily FontFamily { get; set; }

        public string Code { get; protected set; }


        private string p_glyph;
        public string Glyph
        {
            get => this.p_glyph;
            set
            {
                this.p_glyph = value;
                this.Code = ToCode(this.p_glyph);
            }
        }

        public string CodeGlyph => string.IsNullOrWhiteSpace(Code) ? string.Empty : "\\u" + Code;
        public string TextGlyph => string.IsNullOrWhiteSpace(Code) ? string.Empty : "&#x" + Code + ";";

        // WPF doesn't have Symbol enum like WinUI
        public string SymbolName => null;


        public static string ToCode(string glyph)
        {
            var codepoint = FontIconData.ToUtf32(glyph);
            return $"{codepoint:X}";
        }
    }
}
