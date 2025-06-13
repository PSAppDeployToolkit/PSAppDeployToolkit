using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Modern.Common.IconKeys
{
    public static class FontDictionary
    {
        public static ResourceDictionary Dictionary { get; private set; }
        static FontDictionary()
        {
            Dictionary = new ResourceDictionary()
            {
                Source = new Uri("/iNKORE.UI.WPF.Modern;component/Resources/Fonts/Fonts.xaml", UriKind.Relative)
            };
        }

        public static FontFamily GetFont(string fontName)
        {
            if (Dictionary.Contains(fontName) && Dictionary[fontName] is FontFamily family)
            {
                return family;
            }

            return new FontFamily(fontName);
        }

        public static FontFamily SegoeUISymbol => GetFont(ThemeKeys.SegoeUISymbolKey);
        public static FontFamily SegoeMDL2Assets => GetFont(ThemeKeys.SegoeMDL2AssetsKey);
        public static FontFamily SegoeFluentIcons => GetFont(ThemeKeys.SegoeFluentIconsKey);
        public static FontFamily FluentSystemIcons => GetFont(ThemeKeys.FluentSystemIconsKey);
        public static FontFamily FluentSystemIconsFilled => GetFont(ThemeKeys.FluentSystemIconsFilledKey);

    }

    public struct FontIconData
    {
        private FontFamily _fontFamily;
        public FontFamily FontFamily => _fontFamily;


        private string _glyph;
        public string Glyph => _glyph;

        public FontIconData(string glyph, FontFamily family = null)
        {
            _glyph = glyph;
            _fontFamily = family;
        }
    }
}
