// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Data;

namespace Wpf.Ui.Demo.Mvvm.Helpers;

internal class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string enumString)
        {
            throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
        }

        if (!Enum.IsDefined(typeof(Wpf.Ui.Appearance.ApplicationTheme), value))
        {
            throw new ArgumentException("ExceptionEnumToBooleanConverterValueMustBeAnEnum");
        }

        var enumValue = Enum.Parse(typeof(Wpf.Ui.Appearance.ApplicationTheme), enumString);

        return enumValue.Equals(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string enumString)
        {
            throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
        }

        return Enum.Parse(typeof(Wpf.Ui.Appearance.ApplicationTheme), enumString);
    }
}
