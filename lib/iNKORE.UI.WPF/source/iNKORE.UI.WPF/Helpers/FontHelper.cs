using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Helpers
{
    public static class FontHelper
    {
        public static FontStyle GetToggledValue(this FontStyle value, FontStyle? normal = null, FontStyle? italic = null)
        {
            if (!normal.HasValue) normal = FontStyles.Normal;
            if (!italic.HasValue) italic = FontStyles.Italic;

            return (value == normal) ? italic.Value : normal.Value;
        }

        public static FontStyle GetToggledValue(this FontStyle? value, FontStyle? normal = null, FontStyle? italic = null)
        {
            return GetToggledValue(value.GetValueOrDefault(FontStyles.Normal), normal, italic);
        }


        public static FontWeight GetToggledValue(this FontWeight value, FontWeight? normal = null, FontWeight? bold = null)
        {
            if (!normal.HasValue) normal = FontWeights.Normal;
            if (!bold.HasValue) bold = FontWeights.Bold;

            return (value == normal) ? bold.Value : normal.Value;
        }
        public static FontWeight GetToggledValue(this FontWeight? value, FontWeight? normal = null, FontWeight? bold = null)
        {
            return GetToggledValue(value.GetValueOrDefault(FontWeights.Normal), normal, bold);
        }

        public static TextDecorationCollection ToggleUnderline(this TextDecorationCollection? value, bool? useUnderline = null, TextDecorationCollection? underline = null)
        {
            if (underline == null) underline = TextDecorations.Underline;

            List<TextDecorationLocation> locs = new List<TextDecorationLocation>();
            foreach (var deco in underline)
            {
                if (!locs.Contains(deco.Location))
                    locs.Add(deco.Location);
            }


            if (!useUnderline.HasValue)
            {
                if (value != null)
                {
                    useUnderline = true;
                    foreach(var deco in value)
                    {
                        if (locs.Contains(deco.Location))
                        {
                            useUnderline = false;
                        }
                    }

                }
                else useUnderline = true;
            }

            if (value == null)
            {
                value = new TextDecorationCollection();
            }


            if (!useUnderline.Value)
            {
                foreach (var deco in value.ToArray())
                {
                    if (locs.Contains(deco.Location))
                        value.Remove(deco);
                    
                }
            }
            else
            {
                foreach (var deco in underline)
                {
                    value.Add(deco);
                }
            }


            return value;
        }

        public static TextDecorationCollection ToggleStrikethrough(this TextDecorationCollection? value, bool? useUnderline = null)
        {
            return ToggleUnderline(value, useUnderline, TextDecorations.Strikethrough);
        }

        public static TextDecorationCollection CopyCollection(this TextDecorationCollection c)
        {
            return new TextDecorationCollection(c.ToArray());
        }

        public static string SymbolThemeFontFamilyKey => GetSymbolThemeFontFamily();

        private static string GetSymbolThemeFontFamily()
        {
            if (CheckSysFontExisting("Segoe Fluent Icons"))
            {
                return "SegoeFluentIcons";
            }
            else if (CheckSysFontExisting("Segoe MDL2 Assets"))
            {
                return "SegoeMDL2Assets";
            }
            else if (CheckSysFontExisting("Segoe UI Symbol"))
            {
                return "SegoeUISymbol";
            }
            else
            {
                return "FluentSystemIcons";
            }
        }

        public static bool CheckSysFontExisting(string fontName = "文鼎細黑")
        {
            FontFamily font;
            try
            {
                font = new FontFamily(fontName);
                if (!font.FamilyNames.Any())
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

    }
}
