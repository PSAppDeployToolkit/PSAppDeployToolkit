// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.ViewModels.Pages.DialogsAndFlyouts;

public partial class FlyoutViewModel : ViewModel
{
    [ObservableProperty]
    private bool _isFlyoutOpen = false;

    [RelayCommand]
    private void OnButtonClick(object sender)
    {
        if (!IsFlyoutOpen)
        {
            IsFlyoutOpen = true;
        }
    }
}
