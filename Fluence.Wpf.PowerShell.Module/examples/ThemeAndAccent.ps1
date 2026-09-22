# ThemeAndAccent.ps1 - Switch Light/Dark/Auto themes and cycle custom accent colors live, and watch
# the window icon follow the theme. The module handles STA, assembly loading, the Application, and the
# message loop; runtime theming goes through Set-FluenceTheme and Set-FluenceAccent, -WatchSystemTheme
# follows the OS setting while open, and the brand Light/Dark vector icons (merged into application
# resources by the library) are rasterized once and swapped to match the resolved theme.
# Run: powershell.exe -File ThemeAndAccent.ps1   OR   pwsh -File ThemeAndAccent.ps1

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
    Title="Fluence.Wpf - Theme and Accent"
    Width="560"
    Height="380"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <StackPanel Margin="24" VerticalAlignment="Center">
        <TextBlock Text="Theme" fluence:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
        <StackPanel Orientation="Horizontal" Margin="0,8,0,16">
            <fluence:Button x:Name="LightBtn" Content="Light" Margin="0,0,8,0" />
            <fluence:Button x:Name="DarkBtn" Content="Dark" Margin="0,0,8,0" />
            <fluence:Button x:Name="AutoBtn" Content="Auto (follow Windows)" />
        </StackPanel>
        <TextBlock Text="Accent" fluence:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
        <StackPanel Orientation="Horizontal" Margin="0,8,0,16">
            <fluence:Button x:Name="AccentBtn" Content="Cycle custom accent" Appearance="Accent" Margin="0,0,8,0" />
            <fluence:Button x:Name="SystemAccentBtn" Content="Use system accent" />
        </StackPanel>
        <fluence:InfoBar x:Name="StatusBar" IsOpen="True" IsClosable="False" Severity="Informational" Title="Tip" Message="Switch the theme (or change Windows while Auto is on) - the title-bar and taskbar icon follow it." />
    </StackPanel>
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
