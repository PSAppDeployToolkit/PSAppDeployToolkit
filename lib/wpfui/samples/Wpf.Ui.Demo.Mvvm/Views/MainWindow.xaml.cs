// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace Wpf.Ui.Demo.Mvvm.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : INavigationWindow
{
    public ViewModels.MainWindowViewModel ViewModel { get; }

    public MainWindow(ViewModels.MainWindowViewModel viewModel, INavigationService navigationService)
    {
        ViewModel = viewModel;
        DataContext = this;

        Appearance.SystemThemeWatcher.Watch(this);

        InitializeComponent();

        navigationService.SetNavigationControl(RootNavigation);
    }

    public INavigationView GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) =>
        RootNavigation.SetPageProviderService(navigationViewPageProvider);

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();

    /// <summary>
    /// Raises the closed event.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Make sure that closing this window will begin the process of closing the application.
        Application.Current.Shutdown();
    }

    INavigationView INavigationWindow.GetNavigation()
    {
        throw new NotImplementedException();
    }

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }
}
