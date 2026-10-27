<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Opt-in render tests for the Show-FluenceWindow host path. Every It is tagged UI and skips unless
# FLUENCE_PS_UI=1, mirroring the library's opt-in Screenshots pattern and the dialog render tests.
#
# Each window is driven and closed by a DispatcherTimer created ON the UI thread, inside the
# -Content or -Initialize block (which is the only code that runs on the UI thread while ShowDialog
# pumps its modal loop). The blocks are fully self-contained: their only inputs are $Window and the
# -Data hashtable, and they reference only those, .NET types, and module commands. That keeps them
# valid on every host shape, including the module-owned STA runspace an MTA host uses, where the
# block is recreated from text and cannot see caller scope.

BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force
}

Describe 'Show-FluenceWindow render' -Tag UI {

    It 'returns the stashed result from a -Content block and runs the block on the UI thread' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $data = @{}

        $content = {
            param($Window, $Data)

            $stack = [System.Windows.Controls.StackPanel]::new()
            $stack.Margin = [System.Windows.Thickness]::new(24)
            $stack.VerticalAlignment = [System.Windows.VerticalAlignment]::Center

            $okButton = [Fluence.Wpf.Controls.Button]::new()
            $okButton.Content = 'OK'
            $okButton.Appearance = [Fluence.Wpf.ControlAppearance]::Accent
            $okButton.add_Click({
                Close-FluenceWindow -Window $Window -Result 'clicked'
            }.GetNewClosure())
            $null = $stack.Children.Add($okButton)

            $label = [System.Windows.Controls.TextBlock]::new()
            $label.Text = 'Content block window'
            $label.Margin = [System.Windows.Thickness]::new(0, 12, 0, 0)
            $null = $stack.Children.Add($label)

            $Window.Content = $stack
            $Data.Shown = $true

            # Auto-drive: after the window is up, raise the button's Click so the result is stashed.
            $timer = [System.Windows.Threading.DispatcherTimer]::new()
            $timer.Interval = [timespan]::FromMilliseconds(800)
            $timer.add_Tick({
                $timer.Stop()
                $okButton.RaiseEvent(
                    [System.Windows.RoutedEventArgs]::new([System.Windows.Controls.Primitives.ButtonBase]::ClickEvent))
            }.GetNewClosure())
            $timer.Start()
        }

        $result = Show-FluenceWindow -Content $content -Theme Light -Backdrop None -Data $data

        $result | Should -Be 'clicked'
        $data.Shown | Should -BeTrue
    }

    It 'parses a FluenceWindow from a -Xaml string and finds a named control in -Initialize' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $data = @{}

        $xaml = @'
<fluence:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fluence="http://schemas.fluencewpf.com"
    Title="From XAML String"
    Width="480"
    Height="260"
    SystemBackdropType="None">
    <StackPanel Margin="24" VerticalAlignment="Center">
        <fluence:Button x:Name="Ok" Content="OK" />
    </StackPanel>
</fluence:FluenceWindow>
'@

        $initialize = {
            param($Window, $Data)

            $Data.Found = ($null -ne $Window.FindName('Ok'))

            $timer = [System.Windows.Threading.DispatcherTimer]::new()
            $timer.Interval = [timespan]::FromMilliseconds(600)
            $timer.add_Tick({
                $timer.Stop()
                if ($Window.IsVisible)
                {
                    $Window.Close()
                }
            }.GetNewClosure())
            $timer.Start()
        }

        $null = Show-FluenceWindow -Xaml $xaml -Initialize $initialize -Theme Light -Data $data

        $data.Found | Should -BeTrue
    }

    It 'loads a FluenceWindow from a -XamlPath file and finds a named control in -Initialize' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $data = @{}

        $xaml = @'
<fluence:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fluence="http://schemas.fluencewpf.com"
    Title="From XAML File"
    Width="480"
    Height="260"
    SystemBackdropType="None">
    <fluence:FluenceWindow.Resources>
        <ResourceDictionary><ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Palette.xaml" />
        </ResourceDictionary.MergedDictionaries></ResourceDictionary>
    </fluence:FluenceWindow.Resources>
    <StackPanel Margin="24" VerticalAlignment="Center">
        <fluence:Button x:Name="Ok" Content="OK" Background="{StaticResource FileBrush}" />
    </StackPanel>
</fluence:FluenceWindow>
'@

        $tempXaml = Join-Path $TestDrive 'Window resource Ω.xaml'
        $palette = Join-Path $TestDrive 'Palette.xaml'
        [System.IO.File]::WriteAllText($palette, '<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"><SolidColorBrush x:Key="FileBrush" Color="#FF237A45" /></ResourceDictionary>')
        [System.IO.File]::WriteAllText($tempXaml, $xaml, [System.Text.UTF8Encoding]::new($true))

        try
        {
            $initialize = {
                param($Window, $Data)

                $Data.Found = ($null -ne $Window.FindName('Ok'))
                $Data.Color = $Window.FindName('Ok').Background.Color.ToString()

                $timer = [System.Windows.Threading.DispatcherTimer]::new()
                $timer.Interval = [timespan]::FromMilliseconds(600)
                $timer.add_Tick({
                    $timer.Stop()
                    if ($Window.IsVisible)
                    {
                        $Window.Close()
                    }
                }.GetNewClosure())
                $timer.Start()
            }

            $null = Show-FluenceWindow -XamlPath $tempXaml -Initialize $initialize -Theme Light -Data $data

            $data.Found | Should -BeTrue
            $data.Color | Should -BeExactly '#FF237A45'
        }
        finally
        {
            if (Test-Path -LiteralPath $tempXaml)
            {
                Remove-Item -LiteralPath $tempXaml -Force
            }
            if (Test-Path -LiteralPath $palette)
            {
                Remove-Item -LiteralPath $palette -Force
            }
        }
    }
}
