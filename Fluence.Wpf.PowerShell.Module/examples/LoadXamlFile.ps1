# LoadXamlFile.ps1 - Load the window UI from MainWindow.xaml on disk instead of an inline string, then
# wire its named controls. The module handles STA, assembly loading, the Application, theming, and the
# message loop, so the whole script is one Show-FluenceWindow -XamlPath call.
# Run: powershell.exe -File LoadXamlFile.ps1   OR   pwsh -File LoadXamlFile.ps1

# $Data is read inside the add_Click closure (via .GetNewClosure()), which the analyzer cannot trace;
# $e is the conventional ($s, $e) event-args parameter on the SelectionChanged handler. Both intentional.
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Data',
    Justification = '$Data is read inside the GetNewClosure handler, which the analyzer cannot trace.')]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'e',
    Justification = 'Conventional ($s, $e) event-args signature on the SelectionChanged handler; $e is unread.')]
param()

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

Show-FluenceWindow -XamlPath (Join-Path $PSScriptRoot 'MainWindow.xaml') -WatchSystemTheme -Data @{ AccentIndex = 0 } -Initialize {
    param($Window, $Data)

    # The "Cycle accent" button steps through a small palette; blue, green, red, purple.
    $accents = @(
        [System.Windows.Media.Color]::FromRgb(0x00, 0x78, 0xD4),
        [System.Windows.Media.Color]::FromRgb(0x10, 0x89, 0x3E),
        [System.Windows.Media.Color]::FromRgb(0xC4, 0x2B, 0x1C),
        [System.Windows.Media.Color]::FromRgb(0x74, 0x37, 0xC9)
    )

    $themeCombo = $Window.FindName('ThemeComboBox')
    if ($null -ne $themeCombo)
    {
        $themeCombo.add_SelectionChanged({
                param($s, $e)
                # Each ComboBoxItem carries a Tag of Auto / Light / Dark / HighContrast.
                $tag = $s.SelectedItem.Tag
                if ([string]::IsNullOrWhiteSpace($tag))
                {
                    $tag = 'Auto'
                }
                Set-FluenceTheme -Theme $tag
            }.GetNewClosure())
    }

    $accentBtn = $Window.FindName('AccentButton')
    if ($null -ne $accentBtn)
    {
        $accentBtn.add_Click({
                Set-FluenceAccent -Color $accents[$Data.AccentIndex % $accents.Count]
                $Data.AccentIndex++
            }.GetNewClosure())
    }

    $sysAccentBtn = $Window.FindName('SystemAccentButton')
    if ($null -ne $sysAccentBtn)
    {
        $sysAccentBtn.add_Click({ Set-FluenceAccent -System }.GetNewClosure())
    }
}
