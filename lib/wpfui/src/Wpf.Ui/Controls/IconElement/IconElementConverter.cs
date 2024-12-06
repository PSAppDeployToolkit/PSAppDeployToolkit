// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Extensions;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Tries to convert <see cref="SymbolRegular"/> and <seealso cref="SymbolFilled"/>  to <see cref="SymbolIcon"/>.
/// </summary>
public class IconElementConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        if (sourceType == typeof(SymbolRegular))
        {
            return true;
        }

        if (sourceType == typeof(SymbolFilled))
        {
            return true;
        }

        return false;
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => false;

    public override object? ConvertFrom(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value
    ) =>
        value switch
        {
            SymbolRegular symbolRegular => new SymbolIcon(symbolRegular),
            SymbolFilled symbolFilled => new SymbolIcon(symbolFilled.Swap(), filled: true),
            _ => null
        };

    public override object ConvertTo(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value,
        Type destinationType
    )
    {
        throw GetConvertFromException(value);
    }
}
