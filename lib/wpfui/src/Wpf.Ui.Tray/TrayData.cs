// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Tray;

/// <summary>
/// Singleton containing persistent information about icons in the tray menu for application session.
/// </summary>
internal static class TrayData
{
    /// <summary>
    /// Gets or sets the collection of registered tray icons.
    /// </summary>
    public static List<INotifyIcon> NotifyIcons { get; set; } = new();
}
