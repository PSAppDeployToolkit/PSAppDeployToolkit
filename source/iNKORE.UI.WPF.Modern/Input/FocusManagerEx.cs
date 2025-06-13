using System.Windows;
using System.Windows.Input;

namespace iNKORE.UI.WPF.Modern.Input
{
    internal static class FocusManagerEx
    {
        public static UIElement FindNextFocusableElement(FocusNavigationDirection focusNavigationDirection)
        {
            if (Keyboard.FocusedElement is UIElement focusedElement)
            {
                return focusedElement.PredictFocus(focusNavigationDirection) as UIElement;
            }

            return null;
        }
    }
}
