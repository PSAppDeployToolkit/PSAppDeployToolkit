# HelloWindow.ps1 - A persistent Mica window whose button cycles the backdrop live and rotates a
# greeting. The module handles STA, assembly loading, the Application, theming, and the message loop,
# so the whole script is one Show-FluenceWindow call.
# Run: powershell.exe -File HelloWindow.ps1   OR   pwsh -File HelloWindow.ps1

# $Data is read inside the add_Click closure (via .GetNewClosure()), which the analyzer cannot trace
# statically; it is the sanctioned cross-click state channel on an MTA UI runspace, not a defect.
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Data',
    Justification = '$Data is read inside the GetNewClosure handler, which the analyzer cannot trace.')]
param()

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

$xaml = @'
<fluence:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fluence="http://schemas.fluencewpf.com"
    Title="Fluence.Wpf - PowerShell Hello World"
    Width="520"
    Height="340"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
        <TextBlock x:Name="HelloLabel" Text="Hello, World!" HorizontalAlignment="Center" fluence:TextBlockExtensions.Typography="Title" Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
        <TextBlock x:Name="BackdropLabel" Text="Backdrop: Mica" Margin="0,8,0,20" HorizontalAlignment="Center" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
        <fluence:Button x:Name="CycleButton" Content="Next backdrop + greeting" Appearance="Accent" HorizontalAlignment="Center" />
    </StackPanel>
</fluence:FluenceWindow>
'@

Show-FluenceWindow -Xaml $xaml -WatchSystemTheme -Data @{ Tick = 0 } -Initialize {
    param($Window, $Data)

    $backdrops = @('Mica', 'Acrylic', 'Tabbed', 'None')
    $greetings = @('Hello, World!', 'Hej, varlden!', 'Hola, mundo!', 'Bonjour, le monde!', 'Ola, mundo!', 'Ciao, mondo!')

    $helloLabel = $Window.FindName('HelloLabel')
    $backdropLabel = $Window.FindName('BackdropLabel')
    $cycleButton = $Window.FindName('CycleButton')

    $cycleButton.add_Click({
            $Data.Tick++
            $name = $backdrops[$Data.Tick % $backdrops.Count]
            Set-FluenceBackdrop -Backdrop $name -Window $Window
            $backdropLabel.Text = "Backdrop: $name"
            $helloLabel.Text = $greetings[$Data.Tick % $greetings.Count]
        }.GetNewClosure())
}
