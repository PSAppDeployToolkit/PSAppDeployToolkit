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

using System.Windows;
using System.Windows.Controls;
using FluenceStarterApp.Models;

namespace FluenceStarterApp.Pages;

public partial class GeneralPage : UserControl
{
    public GeneralPage()
    {
        InitializeComponent();

        DensityChoice.ItemsSource = new[]
        {
            new ChoiceOption("Comfortable"),
            new ChoiceOption("Compact"),
            new ChoiceOption("Spacious"),
        };
        DensityChoice.SelectedIndex = 0;
        UpdateStatus();
    }

    private void PreferenceChanged(object sender, RoutedEventArgs e)
    {
        UpdateStatus();
    }

    private void DensityChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateStatus();
    }

    private void ReviewChoices_Click(object sender, RoutedEventArgs e)
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (GeneralStatus is null)
        {
            return;
        }

        string startup = WorkspaceToggle.IsChecked == true
            ? "Workspace opens first"
            : "The welcome view opens first";
        string density = (DensityChoice.SelectedItem as ChoiceOption)?.Name ?? "Comfortable";
        GeneralStatus.Text = $"{startup}. Reading density: {density}.";
    }
}
