function Set-FluenceTheme
{
    <#
    .SYNOPSIS
        Applies a Fluent theme (and optionally a backdrop) to the process-wide WPF resources.
    .DESCRIPTION
        Runs the Fluence theme engine on the UI (STA) thread, rebuilding the computed color and
        brush dictionary for the requested theme. When -Backdrop is omitted the current backdrop
        is preserved. The accent intent is always preserved by the engine.
    .PARAMETER Theme
        Auto, Light, Dark, or HighContrast.
    .PARAMETER Backdrop
        Mica, Acrylic, Tabbed, None, or Auto. When omitted, the current backdrop is kept.
    .PARAMETER UpdateAccent
        Accepted and ignored. Earlier library versions took an updateAccent flag on Apply; the 0.9
        engine always preserves the accent intent, so the switch stays only for contract stability.
    .EXAMPLE
        Set-FluenceTheme -Theme Dark
    .EXAMPLE
        Set-FluenceTheme -Theme Light -Backdrop Acrylic
    .NOTES
        Uses the calling STA thread or a module-owned STA runspace. An existing host application
        is supported only when the command already runs on its dispatcher thread. Omitting -Backdrop keeps the
        current backdrop, and the accent intent is preserved.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Mutates transient in-process WPF theme resources only; makes no persistent or destructive system change, so ShouldProcess prompting is not appropriate.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateSet('Auto', 'Light', 'Dark', 'HighContrast')]
        [string]$Theme,

        [Parameter()]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop,

        [Parameter()]
        [switch]$UpdateAccent
    )

    $backdropName = $null
    if ($PSBoundParameters.ContainsKey('Backdrop'))
    {
        $backdropName = $Backdrop
    }

    $null = Invoke-OnFluenceUi -Script {
        param($themeName, $backdropName)

        if ($null -eq [System.Windows.Application]::Current)
        {
            $app = [System.Windows.Application]::new()
            $app.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown
        }

        $theme = [Fluence.Wpf.ApplicationTheme]$themeName
        if ([string]::IsNullOrWhiteSpace($backdropName))
        {
            $backdrop = [Fluence.Wpf.ApplicationThemeManager]::CurrentBackdrop
        }
        else
        {
            $backdrop = ConvertTo-FluenceBackdropType -Backdrop $backdropName
        }

        [Fluence.Wpf.ApplicationThemeManager]::Apply($theme, $backdrop)

        # Record the apply so a later dialog without -Theme or -Backdrop leaves this state alone
        # instead of re-seeding the module defaults; see Initialize-FluenceApplication.
        [System.AppDomain]::CurrentDomain.SetData($script:ThemeSeededSlot, $true)
    } -ArgumentList @($Theme, $backdropName)
}
