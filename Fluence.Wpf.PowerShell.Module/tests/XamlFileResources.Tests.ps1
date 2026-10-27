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
    Import-Module (Join-Path $PSScriptRoot '../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1') -Force
    $script:FixtureRoot = Join-Path $TestDrive 'XAML space Ω'
    $null = New-Item -ItemType Directory -Path (Join-Path $script:FixtureRoot 'Resources/More') -Force
    $script:PalettePath = Join-Path $script:FixtureRoot 'Resources/Palette.xaml'
    [System.IO.File]::WriteAllText($script:PalettePath, @'
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
  <ResourceDictionary.MergedDictionaries><ResourceDictionary Source="More/Colors.xaml" /></ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
'@)
    [System.IO.File]::WriteAllText((Join-Path $script:FixtureRoot 'Resources/More/Colors.xaml'), @'
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <SolidColorBrush x:Key="FileBrush" Color="#FF237A45" />
</ResourceDictionary>
'@)
    & (Get-Module Fluence.Wpf.PowerShell) {
        param($imagePath)
        Invoke-OnFluenceUi -Script {
            param($path)
            $bitmap = [System.Windows.Media.Imaging.BitmapSource]::Create(2, 1, 96, 96,
                [System.Windows.Media.PixelFormats]::Bgra32, $null, [byte[]]@(0, 0, 255, 255, 0, 255, 0, 255), 8)
            $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
            $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
            $stream = [System.IO.File]::Create($path)
            try { $encoder.Save($stream) } finally { $stream.Dispose() }
        } -ArgumentList @($imagePath)
    } (Join-Path $script:FixtureRoot 'image.png')

    function New-FileXaml
    {
        [CmdletBinding()]
        [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
            Justification = 'Test fixture helper writes only a XAML file in the Pester TestDrive.')]
        param([string]$Root = 'Window', [string]$Dictionary = 'Resources/Palette.xaml', [string]$Name = 'View.xaml')
        $body = '<Grid><Border x:Name="Probe" Background="{StaticResource FileBrush}" /><Image x:Name="ImageProbe"><Image.Source><BitmapImage UriSource="image.png" CacheOption="OnLoad" /></Image.Source></Image></Grid>'
        $xaml = '<{0} xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">' -f $Root
        $xaml += '<{0}.Resources><ResourceDictionary><ResourceDictionary.MergedDictionaries><ResourceDictionary Source="{1}" /></ResourceDictionary.MergedDictionaries></ResourceDictionary></{0}.Resources>' -f $Root, $Dictionary
        $xaml += $body + '</' + $Root + '>'
        $path = Join-Path $script:FixtureRoot $Name
        [System.IO.File]::WriteAllText($path, $xaml)
        return $path
    }

    function Invoke-FileXamlProbe
    {
        param([string]$Path, [string]$Mode = 'XamlFile', [string]$Xaml = '')
        & (Get-Module Fluence.Wpf.PowerShell) {
            param($filePath, $inputMode, $text)
            Invoke-OnFluenceUi -Script {
                param($path, $mode, $xaml)
                Initialize-FluenceApplication -Theme Light -Backdrop None
                $spec = @{ Mode = $mode; XamlPath = $path; Xaml = $xaml; ChromeBound = @{}; Data = @{}; CallerRunspaceId = [System.Management.Automation.Runspaces.Runspace]::DefaultRunspace.InstanceId }
                $existingWindows = @([System.Windows.Application]::Current.Windows)
                try
                {
                    $window = Set-FluenceWindowContent -Spec $spec -State @{ Error = $null }
                    $probe = $window.FindName('Probe')
                    $wrapped = ($null -eq $probe)
                    if ($wrapped) { $probe = $window.Content.FindName('Probe') }
                    $image = $window.FindName('ImageProbe')
                    if ($null -eq $image) { $image = $window.Content.FindName('ImageProbe') }
                    $facts = @{ Color = $probe.Background.Color.ToString(); Wrapped = $wrapped }
                    if ($null -ne $image)
                    {
                        $pixels = [byte[]]::new(8)
                        $image.Source.CopyPixels($pixels, 8, 0)
                        $facts.Width = $image.Source.PixelWidth
                        $facts.Height = $image.Source.PixelHeight
                        $facts.Pixels = $pixels
                    }
                    return $facts
                }
                finally
                {
                    # Failed XAML parsing can register a partially constructed Window before
                    # throwing. Close only the windows this probe created, including that case.
                    foreach ($created in @([System.Windows.Application]::Current.Windows))
                    {
                        if ($existingWindows -notcontains $created) { $created.Close() }
                    }
                }
            } -ArgumentList @($filePath, $inputMode, $text)
        } $Path $Mode $Xaml
    }

    function Assert-SourceUnlocked
    {
        param([string]$Path)
        $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        $stream.Dispose()
    }
}

Describe 'XAML file resource locations' {
    It 'resolves nested dictionaries and decodes a sibling image with a <Root> root' -ForEach @(
        @{ Root = 'Window'; Wrapped = $false }
        @{ Root = 'Grid'; Wrapped = $true }
    ) {
        $path = New-FileXaml -Root $Root
        Push-Location $TestDrive
        try { $facts = Invoke-FileXamlProbe -Path $path } finally { Pop-Location }
        $facts.Color | Should -BeExactly '#FF237A45'
        $facts.Wrapped | Should -Be $Wrapped
        $facts.Width | Should -Be 2
        $facts.Height | Should -Be 1
        $facts.Pixels[2] | Should -Be 255
        Assert-SourceUnlocked $path
    }

    It 'retains absolute dictionary locations' {
        $uri = [System.Uri]::new($script:PalettePath, [System.UriKind]::Absolute).AbsoluteUri
        $path = New-FileXaml -Dictionary $uri
        (Invoke-FileXamlProbe -Path $path).Color | Should -BeExactly '#FF237A45'
        Assert-SourceUnlocked $path
    }

    It 'reports a missing dictionary and releases the source file' {
        $path = New-FileXaml -Dictionary 'Missing.xaml'
        $failure = { Invoke-FileXamlProbe -Path $path } | Should -Throw -PassThru
        $failure.Exception.ToString() | Should -Match 'missing.xaml'
        Assert-SourceUnlocked $path
    }

    It 'preserves string-mode parsing and library pack resources' {
        $xaml = @'
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Window.Resources><ResourceDictionary><ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="pack://application:,,,/Fluence.Wpf;component/Themes/Typography/Typography.xaml" />
  </ResourceDictionary.MergedDictionaries><SolidColorBrush x:Key="FileBrush" Color="#FF237A45" /></ResourceDictionary></Window.Resources>
  <Border x:Name="Probe" Background="{StaticResource FileBrush}" TextElement.FontFamily="{StaticResource FluentFontFamily}" />
</Window>
'@
        (Invoke-FileXamlProbe -Mode XamlString -Xaml $xaml).Color | Should -BeExactly '#FF237A45'
        $path = Join-Path $script:FixtureRoot 'Pack.xaml'
        [System.IO.File]::WriteAllText($path, $xaml)
        (Invoke-FileXamlProbe -Path $path).Color | Should -BeExactly '#FF237A45'
        Assert-SourceUnlocked $path
    }
}
