using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Controls
{
    /// <summary>
    /// Defines the visibility for time-parts that are visible for the <see cref="DatePicker"/>. 
    /// </summary>
    [Flags]
    public enum TimePartVisibility
    {
        Hour = 1 << 1,
        Minute = 1 << 2,
        Second = 1 << 3,
        HourMinute = Hour | Minute,
        All = HourMinute | Second
    }

    public enum TimePickerFormat
    {
        Long,
        Short
    }
}
