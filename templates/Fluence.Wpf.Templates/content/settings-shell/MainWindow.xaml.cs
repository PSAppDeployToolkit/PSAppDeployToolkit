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
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
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

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using Fluence.Wpf;
using Fluence.Wpf.Controls;
using FluenceStarterApp.Pages;

namespace FluenceStarterApp;

public partial class MainWindow : FluenceWindow
{
    private readonly Dictionary<string, UserControl> _pages;
    private readonly Dictionary<string, NavigationViewItem> _categoryItems;
    private readonly Stack<string> _categoryHistory = new();
    private readonly HistoryBackCommand _backCommand;
    private string _currentCategory = "general";

    public MainWindow()
    {
        InitializeComponent();
        _backCommand = new HistoryBackCommand(this);
        AppTitleBar.BackCommand = _backCommand;

        _pages = new Dictionary<string, UserControl>(StringComparer.Ordinal)
        {
            ["general"] = new GeneralPage(),
            ["appearance"] = new AppearancePage(),
            ["notifications"] = new NotificationsPage(),
            ["about"] = new AboutPage(),
        };

        _categoryItems = new Dictionary<string, NavigationViewItem>(StringComparer.Ordinal)
        {
            ["general"] = GeneralNavigationItem,
            ["appearance"] = AppearanceNavigationItem,
            ["notifications"] = NotificationsNavigationItem,
            ["about"] = AboutNavigationItem,
        };

        ShowCategory("general");
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Routed caption commands otherwise follow keyboard focus. Point the actual
        // title-bar buttons at this window, including before any page takes focus.
        SetCaptionCommandTarget("PART_MinimizeButton");
        SetCaptionCommandTarget("PART_MaximizeButton");
        SetCaptionCommandTarget("PART_RestoreButton");
        SetCaptionCommandTarget("PART_CloseButton");
    }

    private void CategoryNavigation_ItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.Tag is string tag
            && _pages.ContainsKey(tag)
            && !string.Equals(tag, _currentCategory, StringComparison.Ordinal))
        {
            _categoryHistory.Push(_currentCategory);
            ShowCategory(tag);
        }
    }

    private void AppTitleBar_PaneToggleRequested(object? sender, EventArgs e)
    {
        CategoryNavigation.IsPaneOpen = !CategoryNavigation.IsPaneOpen;
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((e.Key == Key.Left && Keyboard.Modifiers == ModifierKeys.Alt)
            || e.Key == Key.BrowserBack)
        {
            e.Handled = GoBack();
        }
    }

    private bool GoBack()
    {
        if (_categoryHistory.Count == 0)
        {
            return false;
        }

        ShowCategory(_categoryHistory.Pop());
        return true;
    }

    private void ShowCategory(string category)
    {
        _currentCategory = category;
        PageHost.Content = _pages[category];
        CategoryNavigation.SelectedItem = _categoryItems[category];
        _backCommand.RaiseCanExecuteChanged();
    }

    private void SetCaptionCommandTarget(string partName)
    {
        if (GetTemplateChild(partName) is System.Windows.Controls.Button button)
        {
            button.CommandTarget = this;
        }
    }

    private sealed class HistoryBackCommand : ICommand
    {
        private readonly MainWindow _owner;

        public HistoryBackCommand(MainWindow owner) => _owner = owner;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _owner._categoryHistory.Count > 0;

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                _ = _owner.GoBack();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
