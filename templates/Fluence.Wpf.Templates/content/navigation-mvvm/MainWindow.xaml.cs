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

using System.Windows;
using Fluence.Wpf;
using Fluence.Wpf.Controls;
using FluenceStarterApp.ViewModels;

namespace FluenceStarterApp;

public partial class MainWindow : FluenceWindow
{
    private bool _handlingItemInvocation;

    public MainWindow()
    {
        InitializeComponent();
        ShellViewModel shell = new();
        DataContext = shell;
        shell.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShellViewModel.CurrentRoute))
            {
                SelectCurrentRoute(shell.CurrentRoute);
            }
        };
        SelectCurrentRoute(shell.CurrentRoute);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // The caption button sits in the window's chrome. Target the owning window even
        // before a page has keyboard focus, and after focus moves to a nested control.
        if (GetTemplateChild("PART_MinimizeButton") is System.Windows.Controls.Button minimizeButton)
        {
            minimizeButton.CommandTarget = this;
        }
    }

    private void ShellTitleBar_PaneToggleRequested(object? sender, EventArgs e)
    {
        ((ShellViewModel)DataContext).TogglePaneCommand.Execute(null);
    }

    private void AppNavigation_ItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (!_handlingItemInvocation && e.InvokedItemContainer.Tag is string route)
        {
            _handlingItemInvocation = true;
            try
            {
                ((ShellViewModel)DataContext).NavigateCommand.Execute(route);
            }
            finally
            {
                _handlingItemInvocation = false;
            }
        }
    }

    private void SelectCurrentRoute(string route)
    {
        if (_handlingItemInvocation)
        {
            return;
        }

        if (route == "settings")
        {
            _handlingItemInvocation = true;
            try
            {
                AppNavigation.SelectFooterMenuItem(SettingsNavigationItem);
            }
            finally
            {
                _handlingItemInvocation = false;
            }
            return;
        }

        AppNavigation.SelectedItem = route switch
        {
            "home" => HomeNavigationItem,
            "activity" => ActivityNavigationItem,
            _ => HomeNavigationItem
        };
    }
}
