function Initialize-FluenceApplication
{
    <#
    .SYNOPSIS
        Ensures the WPF Application exists and seeds the Fluence theme slots and accent.
    .DESCRIPTION
        Creates an Application with ShutdownMode OnExplicitShutdown when none exists, then applies a
        theme, a backdrop and an accent only when the caller asked for one or when nothing has been
        applied in this process yet. Shared by the dialog path, the progress pump and the window host.

        The "only when asked" rule is what makes Set-FluenceTheme, Set-FluenceBackdrop and
        Set-FluenceAccent stick: a dialog opened afterwards with no -Theme, -Backdrop or -Accent of
        its own leaves the applied state alone instead of resetting it to the module defaults. The
        first call in a process still seeds Auto and Mica with the system accent, so a script that
        never touches theming gets the documented defaults. A partial request is completed from
        ApplicationThemeManager's own CurrentTheme and CurrentBackdrop, because Apply takes both.

        Seeding is recorded in an AppDomain data slot rather than a module variable: on an MTA host
        the UI work runs in a second runspace with its own module instance, and the flag has to be
        the same one the caller's instance sees.
    .PARAMETER Theme
        The requested theme name (Auto, Light, Dark, or HighContrast). Empty means the caller did not
        ask for a theme.
    .PARAMETER Backdrop
        The requested backdrop name (Mica, Acrylic, Tabbed, None, or Auto). Empty means the caller
        did not ask for a backdrop.
    .PARAMETER Accent
        Optional custom accent color (System.Windows.Media.Color). Null means the caller did not ask
        for an accent.
    .NOTES
        Must run on a UI (STA) thread; call it through Invoke-OnFluenceUi. Does not show any window.
    #>
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [string]$Theme,

        [Parameter()]
        [string]$Backdrop,

        [Parameter()]
        [object]$Accent
    )

    $seeded = [bool][System.AppDomain]::CurrentDomain.GetData($script:ThemeSeededSlot)

    if ($null -eq [System.Windows.Application]::Current)
    {
        $app = [System.Windows.Application]::new()
        $app.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown
    }

    $themeAsked = -not [string]::IsNullOrWhiteSpace($Theme)
    $backdropAsked = -not [string]::IsNullOrWhiteSpace($Backdrop)

    if ($themeAsked -or $backdropAsked -or -not $seeded)
    {
        if ($themeAsked)
        {
            $themeValue = [Fluence.Wpf.ApplicationTheme]$Theme
        }
        elseif ($seeded)
        {
            $themeValue = [Fluence.Wpf.ApplicationThemeManager]::CurrentTheme
        }
        else
        {
            $themeValue = [Fluence.Wpf.ApplicationTheme]::Auto
        }

        if ($backdropAsked)
        {
            $backdropValue = ConvertTo-FluenceBackdropType -Backdrop $Backdrop
        }
        elseif ($seeded)
        {
            $backdropValue = [Fluence.Wpf.ApplicationThemeManager]::CurrentBackdrop
        }
        else
        {
            $backdropValue = ConvertTo-FluenceBackdropType -Backdrop 'Mica'
        }

        [Fluence.Wpf.ApplicationThemeManager]::Apply($themeValue, $backdropValue)
    }

    if ($null -ne $Accent)
    {
        [Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent([System.Windows.Media.Color]$Accent)
    }
    elseif (-not $seeded)
    {
        [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()
    }

    [System.AppDomain]::CurrentDomain.SetData($script:ThemeSeededSlot, $true)
}
