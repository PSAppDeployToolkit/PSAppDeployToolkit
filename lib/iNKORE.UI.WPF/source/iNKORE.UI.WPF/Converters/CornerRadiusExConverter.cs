using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using iNKORE.UI.WPF.Common;

namespace iNKORE.UI.WPF.Converters
{
    public class CornerRadiusToCornerRadiusExConverter : AdvancedValueConverterBase<CornerRadius, CornerRadiusEx>
    {
        public override CornerRadiusEx DoConvert(CornerRadius value)
        {
            return new CornerRadiusEx(value);
        }

        public override CornerRadius DoConvertBack(CornerRadiusEx value)
        {
            return value.ToCornerRadius();
        }
    }
}
