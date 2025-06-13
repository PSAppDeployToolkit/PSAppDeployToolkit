using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public enum TeachingTipTailVisibility
    {
        Auto,
        Visible,
        Collapsed,
    };

    public enum TeachingTipCloseReason
    {
        CloseButton,
        LightDismiss,
        Programmatic,
    };

    public enum TeachingTipPlacementMode
    {
        Auto,
        Top,
        Bottom,
        Left,
        Right,
        TopRight,
        TopLeft,
        BottomRight,
        BottomLeft,
        LeftTop,
        LeftBottom,
        RightTop,
        RightBottom,
        Center
    };

    public enum TeachingTipHeroContentPlacementMode
    {
        Auto,
        Top,
        Bottom,
    };
}
