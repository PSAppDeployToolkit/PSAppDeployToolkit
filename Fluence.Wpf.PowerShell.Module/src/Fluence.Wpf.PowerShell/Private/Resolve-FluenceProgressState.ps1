function Resolve-FluenceProgressState
{
    <#
    .SYNOPSIS
        Merges a progress update into the current progress state and clamps the percentage.
    .DESCRIPTION
        Pure logic shared by Show-FluenceProgress and Update-FluenceProgress. Only the values the
        caller bound are changed; a bound -PercentComplete is clamped to 0..100 and switches the bar
        to determinate, and -Indeterminate switches it back. Returns the new state as an ordered
        hashtable with Message, Detail, PercentComplete and Indeterminate.
    .PARAMETER Current
        The state to start from (Message, Detail, PercentComplete, Indeterminate). Null means defaults.
    .PARAMETER Bound
        The caller's $PSBoundParameters, read for Message, Detail, PercentComplete and Indeterminate.
    .OUTPUTS
        System.Collections.Hashtable
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param
    (
        [Parameter()]
        [System.Collections.IDictionary]$Current,

        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$Bound
    )

    $state = @{
        Message         = ''
        Detail          = ''
        PercentComplete = 0
        Indeterminate   = $true
    }
    if ($null -ne $Current)
    {
        foreach ($key in @('Message', 'Detail', 'PercentComplete', 'Indeterminate'))
        {
            if ($Current.ContainsKey($key))
            {
                $state[$key] = $Current[$key]
            }
        }
    }

    if ($Bound.ContainsKey('Message'))
    {
        $state.Message = [string]$Bound['Message']
    }
    if ($Bound.ContainsKey('Detail'))
    {
        $state.Detail = [string]$Bound['Detail']
    }
    if ($Bound.ContainsKey('PercentComplete'))
    {
        $percent = [double]$Bound['PercentComplete']
        if ([double]::IsNaN($percent))
        {
            $percent = 0
        }
        $state.PercentComplete = [System.Math]::Min(100.0, [System.Math]::Max(0.0, $percent))
        $state.Indeterminate = $false
    }
    if ($Bound.ContainsKey('Indeterminate') -and [bool]$Bound['Indeterminate'])
    {
        $state.Indeterminate = $true
    }

    return $state
}
