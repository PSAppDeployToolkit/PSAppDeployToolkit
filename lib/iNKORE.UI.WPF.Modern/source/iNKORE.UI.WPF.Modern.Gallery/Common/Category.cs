using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Controls;
using System;

namespace iNKORE.UI.WPF.Modern.Gallery.Common
{

    public class CategoryBase { }

    public class Category : CategoryBase
    {
        public string Name { get; set; }
        public string Tooltip { get; set; }
        public FontIconData Icon { get; set; }
        //public Type TargetType { get; set; }
    }

    public class Separator : CategoryBase { }

    public class Header : CategoryBase
    {
        public string Name { get; set; }
    }
}
