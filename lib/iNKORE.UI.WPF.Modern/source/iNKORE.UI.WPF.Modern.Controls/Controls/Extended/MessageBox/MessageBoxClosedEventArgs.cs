using System;
using System.Windows;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public class MessageBoxClosedEventArgs : EventArgs
    {
        internal MessageBoxClosedEventArgs(MessageBoxResult result)
        {
            Result = result;
        }

        public MessageBoxResult Result { get; }
    }
}
