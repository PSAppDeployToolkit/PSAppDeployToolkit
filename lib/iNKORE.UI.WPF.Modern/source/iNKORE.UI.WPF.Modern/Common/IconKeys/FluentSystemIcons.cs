using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Modern.Common.IconKeys
{
    public static partial class FluentSystemIcons
    {
        public static FontFamily FontFamilyRegular => FontDictionary.FluentSystemIcons;

        public static FontFamily FontFamilyFilled => FontDictionary.FluentSystemIconsFilled;

        public static FontIconData CreateIcon(string glyph, FluentSystemIconVariants variant)
        {
            switch (variant)
            {
                case FluentSystemIconVariants.Regular:
                    return new FontIconData(glyph, FontFamilyRegular);
                case FluentSystemIconVariants.Filled:
                    return new FontIconData(glyph, FontFamilyFilled);
            }

            return new FontIconData(glyph);
        }

        public static FontIconData CreateIcon(int chara, FluentSystemIconVariants variant)
        {
            return CreateIcon(FontIconData.ToGlyph(chara), variant);
        }
    }

    public enum FluentSystemIconVariants
    {
        Regular,
        Filled
    }
}
