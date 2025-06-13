using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using iNKORE.UI.WPF.Converters;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Common.Converters
{
    public class IconSourceToIconElementConverter : AdvancedValueConverterBase<IconSource, IconElement>
    {
        public override IconElement DoConvert(IconSource from)
        {
            return from.CreateIconElement();
        }

        public override IconSource DoConvertBack(IconElement to)
        {
            return to.CreateIconSource();
        }
    }
}
