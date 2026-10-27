/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

namespace FluenceStarterApp.ViewModels;

public sealed class ShellViewModel : ObservableObject
{
    private readonly HomeViewModel _homePage = new();
    private readonly ActivityViewModel _activityPage = new();
    private readonly SettingsViewModel _settingsPage = new();
    private readonly Stack<string> _routeHistory = new();
    private ObservableObject _currentPage;
    private string _currentRoute;
    private bool _isPaneOpen = true;

    public ShellViewModel()
    {
        _currentPage = _homePage;
        _currentRoute = "home";
        _settingsPage.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.UseCompactNavigation))
            {
                IsPaneOpen = !_settingsPage.UseCompactNavigation;
            }
        };
        TogglePaneCommand = new RelayCommand(() => IsPaneOpen = !IsPaneOpen);
        GoBackCommand = new RelayCommand(GoBack, () => _routeHistory.Count > 0);
        NavigateCommand = new RelayCommand<string>(route =>
        {
            if (route is not null)
            {
                Navigate(route);
            }
        });
    }

    public ObservableObject CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public IRelayCommand<string> NavigateCommand { get; }

    public IRelayCommand TogglePaneCommand { get; }

    public IRelayCommand GoBackCommand { get; }

    public string CurrentRoute
    {
        get => _currentRoute;
        private set => SetProperty(ref _currentRoute, value);
    }

    public bool IsPaneOpen
    {
        get => _isPaneOpen;
        set
        {
            if (SetProperty(ref _isPaneOpen, value))
            {
                _settingsPage.UseCompactNavigation = !value;
            }
        }
    }

    private void Navigate(string route)
    {
        ObservableObject? destination = route switch
        {
            "home" => _homePage,
            "activity" => _activityPage,
            "settings" => _settingsPage,
            _ => null
        };

        if (destination is null || string.Equals(_currentRoute, route, StringComparison.Ordinal))
        {
            return;
        }

        _routeHistory.Push(_currentRoute);
        CurrentRoute = route;
        CurrentPage = destination;
        GoBackCommand.NotifyCanExecuteChanged();
    }

    private void GoBack()
    {
        if (_routeHistory.Count == 0)
        {
            return;
        }

        string route = _routeHistory.Pop();
        CurrentRoute = route;
        CurrentPage = route switch
        {
            "home" => _homePage,
            "activity" => _activityPage,
            "settings" => _settingsPage,
            _ => throw new InvalidOperationException("The navigation history contains an unknown route.")
        };
        GoBackCommand.NotifyCanExecuteChanged();
    }
}
