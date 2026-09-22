function Get-FluenceTheme
{
    <#
    .SYNOPSIS
        Returns the current Fluence theme state for the process.
    .DESCRIPTION
        Reads the process-wide theme statics directly (current theme, resolved theme, current
        backdrop, and dark-mode flag) and returns them as an object. This is a cheap, side-effect
        free reader; it does not marshal onto a UI thread or create a WPF Application.
    .EXAMPLE
        Get-FluenceTheme
    .EXAMPLE
        (Get-FluenceTheme).IsAppInDarkMode
    .OUTPUTS
        Fluence.ThemeInfo
    .NOTES
        Reads current process theme state; does not require or create a host application.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.ThemeInfo')]
    param()

    return [pscustomobject]@{
        PSTypeName      = 'Fluence.ThemeInfo'
        CurrentTheme    = [Fluence.Wpf.ApplicationThemeManager]::CurrentTheme
        ResolvedTheme   = [Fluence.Wpf.ApplicationThemeManager]::ResolvedTheme
        CurrentBackdrop = [Fluence.Wpf.ApplicationThemeManager]::CurrentBackdrop
        IsAppInDarkMode = [Fluence.Wpf.ApplicationThemeManager]::IsAppInDarkMode
    }
}
