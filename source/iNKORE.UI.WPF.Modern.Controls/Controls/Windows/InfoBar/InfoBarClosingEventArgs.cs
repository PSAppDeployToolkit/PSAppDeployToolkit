using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public class InfoBarClosingEventArgs : EventArgs
    {
        public InfoBarCloseReason Reason { get; internal set; }

        public bool Cancel{ get; set; }
    }
}
