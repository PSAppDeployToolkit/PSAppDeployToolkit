// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Wpf.Ui;

/// <summary>
/// Represents a contract with the service that creates <see cref="ContentDialog"/>.
/// </summary>
/// <example>
/// <code lang="xml">
/// &lt;ContentPresenter x:Name="RootContentDialogPresenter" Grid.Row="0" /&gt;
/// </code>
/// <code lang="csharp">
/// IContentDialogService contentDialogService = new ContentDialogService();
/// contentDialogService.SetContentPresenter(RootContentDialogPresenter);
///
/// await _contentDialogService.ShowAsync(
///     new ContentDialog(){
///         Title = "The cake?",
///         Content = "IS A LIE!",
///         PrimaryButtonText = "Save",
///         SecondaryButtonText = "Don't Save",
///         CloseButtonText = "Cancel"
///     }
/// );
/// </code>
/// </example>
public class ContentDialogService : IContentDialogService
{
    private ContentPresenter? _dialogHost;

    [Obsolete("Use SetDialogHost instead.")]
    public void SetContentPresenter(ContentPresenter contentPresenter)
    {
        SetDialogHost(contentPresenter);
    }

    [Obsolete("Use GetDialogHost instead.")]
    public ContentPresenter? GetContentPresenter()
    {
        return GetDialogHost();
    }

    /// <inheritdoc/>
    public void SetDialogHost(ContentPresenter contentPresenter)
    {
        _dialogHost = contentPresenter;
    }

    /// <inheritdoc/>
    public ContentPresenter? GetDialogHost()
    {
        return _dialogHost;
    }

    /// <inheritdoc/>
    public Task<ContentDialogResult> ShowAsync(ContentDialog dialog, CancellationToken cancellationToken)
    {
        if (_dialogHost == null)
        {
            throw new InvalidOperationException("The DialogHost was never set.");
        }

        if (dialog.DialogHost != null && _dialogHost != dialog.DialogHost)
        {
            throw new InvalidOperationException(
                "The DialogHost is not the same as the one that was previously set."
            );
        }

        dialog.DialogHost = _dialogHost;

        return dialog.ShowAsync(cancellationToken);
    }
}
