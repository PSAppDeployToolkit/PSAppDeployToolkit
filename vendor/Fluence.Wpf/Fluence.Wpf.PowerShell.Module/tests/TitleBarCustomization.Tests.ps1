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

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'Title bar customization' {
    It 'exposes -TitleBarText and -TitleBarIcon on <Command>' -ForEach @(
        @{ Command = 'Show-FluenceDialog' }
        @{ Command = 'Show-FluenceMessage' }
        @{ Command = 'Get-FluenceInput' }
        @{ Command = 'Show-FluenceListSelection' }
        @{ Command = 'Show-FluenceRestartPrompt' }
        @{ Command = 'Show-FluenceProgress' }
        @{ Command = 'Show-FluenceWindow' }
    ) {
        $parameters = (Get-Command $Command).Parameters
        $parameters['Title'].Aliases | Should -Contain 'TitleBarText'
        $parameters.ContainsKey('TitleBarIcon') | Should -BeTrue
    }

    It 'rejects an unsupported icon scheme on <Command> before opening a window' -ForEach @(
        @{ Command = 'Show-FluenceDialog'; Arguments = @{ Message = 'x' } }
        @{ Command = 'Show-FluenceMessage'; Arguments = @{ Message = 'x' } }
        @{ Command = 'Get-FluenceInput'; Arguments = @{ Message = 'x' } }
        @{ Command = 'Show-FluenceListSelection'; Arguments = @{ Items = @('x') } }
        @{ Command = 'Show-FluenceRestartPrompt'; Arguments = @{} }
        @{ Command = 'Show-FluenceProgress'; Arguments = @{ Message = 'x' } }
        @{ Command = 'Show-FluenceWindow'; Arguments = @{} }
    ) {
        $Arguments['TitleBarIcon'] = 'https://example.com/icon.png'
        { & $Command @Arguments } | Should -Throw "*-TitleBarIcon*unsupported scheme 'https'*"
    }

    It 'rejects a missing local icon file before opening a dialog' {
        { Show-FluenceDialog -TitleBarIcon (Join-Path $TestDrive 'missing.ico') } |
            Should -Throw '*-TitleBarIcon file not found*'
    }

    It 'sets title text and icon on dialog, progress, and a Window XAML root' -Tag UI -Skip:($env:FLUENCE_PS_UI -ne '1') {
        Add-Type -AssemblyName System.Drawing
        $iconPath = Join-Path $TestDrive 'titlebar.png'
        $bitmap = [System.Drawing.Bitmap]::new(16, 16)
        try
        {
            $bitmap.Save($iconPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally
        {
            $bitmap.Dispose()
        }

        $iconUri = (& (Get-Module Fluence.Wpf.PowerShell) {
            param($path)
            Resolve-FluenceImageSource -Image $path -ParameterName 'TitleBarIcon'
        } $iconPath)

        $facts = & (Get-Module Fluence.Wpf.PowerShell) {
            param($uri)
            Invoke-OnFluenceUi -Script {
                param($source)
                Initialize-FluenceApplication -Theme Light -Backdrop None

                $dialogSpec = @{
                    Title = 'Dialog title'; TitleBarIcon = $source; Prompts = @(); Message = @('Body')
                    Buttons = @(New-FluenceButton -Text OK -IsDefault); MinWidth = 360
                }
                $dialogState = @{ Result = @{}; Window = $null }
                $dialog = New-FluenceDialogWindow -Spec $dialogSpec -State $dialogState

                $progressSpec = @{
                    Title = 'Progress title'; TitleBarIcon = $source; Width = 450
                    Position = 'Center'; Topmost = $false
                }
                $progressState = @{ Message = 'Working'; Detail = ''; Indeterminate = $true }
                $progress = New-FluenceProgressWindow -Spec $progressSpec -State $progressState

                $xaml = '<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Title="XAML title" Width="300" Height="200" />'
                $windowSpec = @{
                    Mode = 'XamlString'; Xaml = $xaml; ChromeBound = @{ Title = 'Override title' }
                    TitleBarIcon = $source; Owner = $null; Initialize = $null
                    InitializeText = $null; CallerRunspaceId = [System.Management.Automation.Runspaces.Runspace]::DefaultRunspace.InstanceId
                }
                $xamlWindow = Set-FluenceWindowContent -Spec $windowSpec -State @{ Error = $null }

                $result = @{
                    DialogTitle = $dialog.Title; DialogIcon = ($null -ne $dialog.Icon)
                    ProgressTitle = $progress.Window.Title; ProgressIcon = ($null -ne $progress.Window.Icon)
                    XamlTitle = $xamlWindow.Title; XamlIcon = ($null -ne $xamlWindow.Icon)
                }
                $dialog.Close()
                $progress.Window.Close()
                $xamlWindow.Close()
                return $result
            } -ArgumentList @($uri)
        } $iconUri

        if ($facts -is [System.Collections.IList]) { $facts = $facts[$facts.Count - 1] }
        $facts.DialogTitle | Should -Be 'Dialog title'
        $facts.DialogIcon | Should -BeTrue
        $facts.ProgressTitle | Should -Be 'Progress title'
        $facts.ProgressIcon | Should -BeTrue
        $facts.XamlTitle | Should -Be 'Override title'
        $facts.XamlIcon | Should -BeTrue
    }

    It 'applies -TitleBarText and -TitleBarIcon through the public XAML window command' -Tag UI -Skip:($env:FLUENCE_PS_UI -ne '1') {
        Add-Type -AssemblyName System.Drawing
        $iconPath = Join-Path $TestDrive 'public-titlebar.png'
        $bitmap = [System.Drawing.Bitmap]::new(16, 16)
        try
        {
            $bitmap.Save($iconPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally
        {
            $bitmap.Dispose()
        }

        $data = @{}
        $xaml = '<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Title="XAML title" Width="300" Height="200" />'
        $initialize = {
            param($Window, $Data)
            $Data.Title = $Window.Title
            $Data.HasIcon = ($null -ne $Window.Icon)
            $timer = [System.Windows.Threading.DispatcherTimer]::new()
            $timer.Interval = [timespan]::FromMilliseconds(100)
            $timer.add_Tick({
                $timer.Stop()
                $Window.Close()
            }.GetNewClosure())
            $timer.Start()
        }
        $null = Show-FluenceWindow -Xaml $xaml -Initialize $initialize -TitleBarText 'Public title' -TitleBarIcon $iconPath -Data $data
        $data.Title | Should -Be 'Public title'
        $data.HasIcon | Should -BeTrue
    }

    It 'loads a <Kind> icon source on a public XAML window' -Tag UI -Skip:($env:FLUENCE_PS_UI -ne '1') -ForEach @(
        @{ Kind = 'file URI' }
        @{ Kind = 'pack URI' }
    ) {
        if ($Kind -eq 'file URI')
        {
            Add-Type -AssemblyName System.Drawing
            $iconPath = Join-Path $TestDrive 'titlebar.ico'
            $stream = [System.IO.File]::Create($iconPath)
            try
            {
                [System.Drawing.SystemIcons]::Application.Save($stream)
            }
            finally
            {
                $stream.Dispose()
            }
            $iconSource = ([System.Uri]$iconPath).AbsoluteUri
        }
        else
        {
            $iconSource = 'pack://application:,,,/Fluence.Wpf;component/Themes/Icons/Fluence_Icon_NoBackground_256.png'
        }

        $data = @{}
        $xaml = '<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Width="300" Height="200" />'
        $initialize = {
            param($Window, $Data)
            $Data.HasIcon = ($null -ne $Window.Icon)
            $Data.IconWidth = $Window.Icon.Width
            $timer = [System.Windows.Threading.DispatcherTimer]::new()
            $timer.Interval = [timespan]::FromMilliseconds(100)
            $timer.add_Tick({
                $timer.Stop()
                $Window.Close()
            }.GetNewClosure())
            $timer.Start()
        }
        $null = Show-FluenceWindow -Xaml $xaml -Initialize $initialize -TitleBarIcon $iconSource -Data $data
        $data.HasIcon | Should -BeTrue
        $data.IconWidth | Should -BeGreaterThan 0
    }

    It 'preserves a XAML root icon when -TitleBarIcon is unbound' -Tag UI -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $data = @{}
        $xaml = @'
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        Title="XAML title"
        Icon="pack://application:,,,/Fluence.Wpf;component/Themes/Icons/Fluence_Icon_NoBackground_256.png"
        Width="300" Height="200" />
'@
        $initialize = {
            param($Window, $Data)
            $Data.HasIcon = ($null -ne $Window.Icon)
            $Data.IconWidth = $Window.Icon.Width
            $timer = [System.Windows.Threading.DispatcherTimer]::new()
            $timer.Interval = [timespan]::FromMilliseconds(100)
            $timer.add_Tick({
                $timer.Stop()
                $Window.Close()
            }.GetNewClosure())
            $timer.Start()
        }
        $null = Show-FluenceWindow -Xaml $xaml -Initialize $initialize -Data $data
        $data.HasIcon | Should -BeTrue
        $data.IconWidth | Should -BeGreaterThan 0
    }
}
