// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Wpf.Ui.UnitTests.Extensions;

public class SymbolExtensionsTests
{
    [Fact]
    public void GivenAllRegularSymbols_Swap_ReturnsValidFilledSymbol()
    {
        foreach (SymbolRegular regularSymbol in Enum.GetValues(typeof(SymbolRegular)))
        {
            _ = regularSymbol.Swap();
        }
    }

    [Fact]
    public void GivenAllFilledSymbols_Swap_ReturnsValidRegularSymbol()
    {
        foreach (SymbolFilled filledSymbol in Enum.GetValues(typeof(SymbolFilled)))
        {
            _ = filledSymbol.Swap();
        }
    }

    [Fact]
    public void GivenAllRegularSymbols_GetString_ReturnsValidString()
    {
        foreach (SymbolRegular regularSymbol in Enum.GetValues(typeof(SymbolRegular)))
        {
            if (regularSymbol == SymbolRegular.Empty)
            {
                continue;
            }

            var receivedString = regularSymbol.GetString();

            Assert.NotEqual(string.Empty, receivedString);
        }
    }

    [Fact]
    public void GivenAllFilledSymbols_GetString_ReturnsValidString()
    {
        foreach (SymbolFilled filledSymbol in Enum.GetValues(typeof(SymbolFilled)))
        {
            if (filledSymbol == SymbolFilled.Empty)
            {
                continue;
            }

            var receivedString = filledSymbol.GetString();

            Assert.NotEqual(string.Empty, receivedString);
        }
    }
}
