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


# 04-ThemeAndAccent.ps1 - Switch Light/Dark/Auto themes and cycle custom accent colors live, and watch
# the window icon follow the theme. The module handles STA, assembly loading, the Application, and the
# message loop; runtime theming goes through Set-FluenceTheme and Set-FluenceAccent, -WatchSystemTheme
# follows the OS setting while open, and the brand Light/Dark vector icons (merged into application
# resources by the library) are rasterized once and swapped to match the resolved theme.
# Run: powershell.exe -File 04-ThemeAndAccent.ps1   OR   pwsh -File 04-ThemeAndAccent.ps1

# $Data is read inside the add_Click / theme-change closures (via .GetNewClosure()), which the analyzer
# cannot trace statically; it is the sanctioned cross-handler state channel on an MTA UI runspace, not
# a defect.
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Data',
    Justification = '$Data is read inside the GetNewClosure handlers, which the analyzer cannot trace.')]
param()

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

$xaml = @'
<fluence:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fluence="http://schemas.fluencewpf.com"
    Title="Fluence.Wpf - Theme and accent"
    Width="560"
    Height="440"
    MinWidth="480"
    MinHeight="360"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <Grid Margin="24">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>
        <StackPanel Grid.Row="0" Margin="0,0,0,16">
            <TextBlock Text="Theme and accent" fluence:TextBlockExtensions.Typography="Title" Foreground="{DynamicResource TextFillColorPrimaryBrush}" TextWrapping="Wrap" />
            <TextBlock Text="Choose a theme and accent color to see the window update with a brief fade." Margin="0,8,0,0" fluence:TextBlockExtensions.Typography="Body" Foreground="{DynamicResource TextFillColorSecondaryBrush}" TextWrapping="Wrap" />
        </StackPanel>
        <fluence:SmoothScrollViewer Grid.Row="1" HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto">
            <StackPanel>
                <TextBlock Text="Theme" Margin="0,0,0,12" fluence:TextBlockExtensions.Typography="BodyStrong" Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
                <WrapPanel Margin="0,0,0,8">
                    <fluence:Button x:Name="LightBtn" Content="Light" Margin="0,0,8,8" />
                    <fluence:Button x:Name="DarkBtn" Content="Dark" Margin="0,0,8,8" />
                    <fluence:Button x:Name="AutoBtn" Content="Use system setting" Margin="0,0,0,8" />
                </WrapPanel>
                <TextBlock Text="Accent" Margin="0,0,0,12" fluence:TextBlockExtensions.Typography="BodyStrong" Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
                <WrapPanel Margin="0,0,0,8">
                    <fluence:Button x:Name="AccentBtn" Content="Cycle custom accent" Appearance="Accent" Margin="0,0,8,8" />
                    <fluence:Button x:Name="SystemAccentBtn" Content="Use system accent" Margin="0,0,0,8" />
                </WrapPanel>
                <fluence:InfoBar x:Name="StatusBar" IsOpen="True" IsClosable="False" Severity="Informational" Title="Tip" Message="The window and its icon follow your theme. Choose Use system setting to follow Windows." />
            </StackPanel>
        </fluence:SmoothScrollViewer>
    </Grid>
</fluence:FluenceWindow>
'@

Show-FluenceWindow -Xaml $xaml -WatchSystemTheme -Data @{ AccentIndex = 0 } -Initialize {
    param($Window, $Data)

    # A small palette to cycle through; blue, green, red, purple.
    $accents = @(
        [System.Windows.Media.Color]::FromRgb(0x00, 0x78, 0xD4),
        [System.Windows.Media.Color]::FromRgb(0x10, 0x89, 0x3E),
        [System.Windows.Media.Color]::FromRgb(0xC4, 0x2B, 0x1C),
        [System.Windows.Media.Color]::FromRgb(0x74, 0x37, 0xC9)
    )

    # The brand Light/Dark icons are resolution-independent DrawingImages merged into application
    # resources by the library. Window.Icon drives the Win32 taskbar/alt-tab HICON, which does not
    # render a vector DrawingImage, so rasterize each variant once to a frozen bitmap (the same
    # approach FluenceWindow uses for its own default icon).
    $rasterize = {
        param($DrawingImage)
        if ($null -eq $DrawingImage) { return $null }
        $px = 256
        $visual = [System.Windows.Media.DrawingVisual]::new()
        $context = $visual.RenderOpen()
        $context.DrawImage($DrawingImage, [System.Windows.Rect]::new(0, 0, $px, $px))
        $context.Close()
        $bitmap = [System.Windows.Media.Imaging.RenderTargetBitmap]::new(
            $px, $px, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
        $bitmap.Render($visual)
        if ($bitmap.CanFreeze) { $bitmap.Freeze() }
        return $bitmap
    }

    $resources = [System.Windows.Application]::Current
    $lightIcon = & $rasterize ($resources.TryFindResource('FluenceIconLightDrawingImage'))
    $darkIcon = & $rasterize ($resources.TryFindResource('FluenceIconDarkDrawingImage'))

    # Pick the icon variant whose plate matches the resolved theme (dark icon for a dark theme, light
    # icon for a light theme). ResolvedTheme reflects what is actually showing, so a forced Light/Dark
    # wins over the OS setting and an Auto window still tracks Windows.
    $applyIcon = {
        $dark = [Fluence.Wpf.ApplicationThemeManager]::ResolvedTheme -eq [Fluence.Wpf.ApplicationTheme]::Dark
        $Window.Icon = if ($dark) { $darkIcon } else { $lightIcon }
    }.GetNewClosure()

    & $applyIcon
    [Fluence.Wpf.ApplicationThemeManager]::add_Changed($applyIcon)
    $Window.add_Closed({ [Fluence.Wpf.ApplicationThemeManager]::remove_Changed($applyIcon) }.GetNewClosure())

    $Window.FindName('LightBtn').add_Click({ Set-FluenceTheme -Theme Light }.GetNewClosure())
    $Window.FindName('DarkBtn').add_Click({ Set-FluenceTheme -Theme Dark }.GetNewClosure())
    $Window.FindName('AutoBtn').add_Click({ Set-FluenceTheme -Theme Auto }.GetNewClosure())

    $Window.FindName('AccentBtn').add_Click({
            Set-FluenceAccent -Color $accents[$Data.AccentIndex % $accents.Count]
            $Data.AccentIndex++
        }.GetNewClosure())
    $Window.FindName('SystemAccentBtn').add_Click({ Set-FluenceAccent -System }.GetNewClosure())
}
