function Get-FluenceButtonPreset
{
    <#
    .SYNOPSIS
        Returns a normalized array of Fluence.Button objects for a named button-set preset.
    .DESCRIPTION
        Maps a preset name (OK, OKCancel, YesNo, YesNoCancel) to an array of Fluence.Button
        objects with the correct IsDefault and IsCancel flags, ready for Show-FluenceDialog.
    .PARAMETER Preset
        The named button-set. One of: OK, OKCancel, YesNo, YesNoCancel.
    .OUTPUTS
        Fluence.Button[]
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.Button')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateSet('OK', 'OKCancel', 'YesNo', 'YesNoCancel')]
        [string]$Preset
    )

    switch ($Preset)
    {
        'OK'
        {
            return @(
                New-FluenceButton 'OK' -IsDefault
            )
        }
        'OKCancel'
        {
            return @(
                New-FluenceButton 'OK' -IsDefault
                New-FluenceButton 'Cancel' -IsCancel
            )
        }
        'YesNo'
        {
            return @(
                New-FluenceButton 'Yes' -IsDefault
                New-FluenceButton 'No'
            )
        }
        'YesNoCancel'
        {
            return @(
                New-FluenceButton 'Yes' -IsDefault
                New-FluenceButton 'No'
                New-FluenceButton 'Cancel' -IsCancel
            )
        }
    }
}
