using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Converters
{
    public class StringToFontFamilyConverter : AdvancedValueConverterBase<string, FontFamily>
    {
        public override FontFamily DoConvert(string from)
        {
            return new FontFamily(from);
        }

        public override string DoConvertBack(FontFamily to)
        {
            return to.Source;
        }
    }
}
