using System;
using System.Windows;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public sealed class AutoSuggestBoxQuerySubmittedEventArgs : EventArgs
    {
        public AutoSuggestBoxQuerySubmittedEventArgs()
        {
        }

        public object ChosenSuggestion { get; internal set; }
        public string QueryText { get; internal set; } = string.Empty;
    }
}
