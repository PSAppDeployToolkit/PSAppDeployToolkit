using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public partial class SettingsExpander
    {
        /// <summary>
        /// Fires when the SettingsExpander is opened
        /// </summary>
        public event EventHandler? Expanded;

        /// <summary>
        /// Fires when the expander is closed
        /// </summary>
        public event EventHandler? Collapsed;
    }
}
