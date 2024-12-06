// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Controls;

namespace Wpf.Ui.Gallery.Models;

public record NavigationCard
{
    public string? Name { get; init; }

    public SymbolRegular Icon { get; init; }

    public string? Description { get; init; }

    public Type? PageType { get; init; }
}
