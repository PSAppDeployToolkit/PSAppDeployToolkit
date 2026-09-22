function Get-FluenceSeverityIcon
{
    <#
    .SYNOPSIS
        Maps a dialog -Icon value to its FontIcon glyph and themed brush key.
    .DESCRIPTION
        Returns @{ Glyph; BrushKey } for Info/Success/Warning/Error/Question, or $null when no icon
        should be shown (None or empty). The four real severities take their glyph and brush key from
        the library helper [Fluence.Wpf.Controls.InfoBar] so they never drift from the InfoBar template;
        Question (which has no InfoBarSeverity) uses the Segoe Fluent Help glyph with the neutral brush.
    .PARAMETER Icon
        The dialog -Icon value: None, Info, Success, Warning, Error, Question, or an empty string.
    .OUTPUTS
        System.Collections.Hashtable (or $null)
    .NOTES
        Does not require a host application, but the Fluence.Wpf assembly must be loaded (the module
        loads it on import) so the severity-glyph helper resolves.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Icon
    )

    if ([string]::IsNullOrWhiteSpace($Icon) -or $Icon -eq 'None')
    {
        return $null
    }

    $informational = [Fluence.Wpf.InfoBarSeverity]::Informational
    if ($Icon -eq 'Question')
    {
        # Question is a dialog affordance with no InfoBarSeverity counterpart: the Segoe Fluent Help
        # glyph with the same neutral brush as Informational.
        return @{
            Glyph    = [string][char]0xE897
            BrushKey = [Fluence.Wpf.Controls.InfoBar]::GetSeverityBrushKey($informational)
        }
    }

    $severity = switch ($Icon)
    {
        'Success' { [Fluence.Wpf.InfoBarSeverity]::Success }
        'Warning' { [Fluence.Wpf.InfoBarSeverity]::Warning }
        'Error'   { [Fluence.Wpf.InfoBarSeverity]::Error }
        default   { $informational }
    }

    return @{
        Glyph    = [Fluence.Wpf.Controls.InfoBar]::GetSeverityGlyph($severity)
        BrushKey = [Fluence.Wpf.Controls.InfoBar]::GetSeverityBrushKey($severity)
    }
}
