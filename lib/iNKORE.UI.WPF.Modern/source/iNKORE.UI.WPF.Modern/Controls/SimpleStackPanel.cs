using iNKORE.UI.WPF.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleStackPanelBase = iNKORE.UI.WPF.Controls.SimpleStackPanel;

namespace iNKORE.UI.WPF.Modern.Controls
{
    // I was going to include this, but i found out that I can't reference to the new SImpleStackPanel, so I have to exclude it from the project.
    [Obsolete("We moved this control, please use iNKORE.UI.WPF.Controls.SimpleStackPanel from iNKORE.UI.WPF instead.")]
    public class SimpleStackPanel : SimpleStackPanelBase
    {

    }
}
