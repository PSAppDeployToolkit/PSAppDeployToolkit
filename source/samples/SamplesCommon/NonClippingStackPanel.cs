using iNKORE.UI.WPF.Modern.Controls;
using System.Windows;
using System.Windows.Media;
using iNKORE.UI.WPF.Controls;

namespace SamplesCommon
{
    public class NonClippingStackPanel : SimpleStackPanel
    {
        protected override Geometry GetLayoutClip(Size layoutSlotSize)
        {
            return null;
        }
    }
}
