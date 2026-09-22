function ConvertTo-FluenceResult
{
    <#
    .SYNOPSIS
        Converts a raw result hashtable into a typed Fluence.DialogResult PSCustomObject.
    .DESCRIPTION
        Copies every key from the input hashtable into an ordered hashtable, then sets
        PSTypeName = 'Fluence.DialogResult' last (so a caller key named 'PSTypeName' cannot
        override the type) and casts the result to [pscustomobject].
    .PARAMETER Result
        The hashtable of collected values from the dialog renderer.
    .OUTPUTS
        Fluence.DialogResult
    .NOTES
        Private helper. Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.DialogResult')]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Result
    )

    # Seed PSTypeName AFTER copying the caller's keys so a result key literally named 'PSTypeName'
    # cannot clobber the synthetic type marker (hashtable keys are case-insensitive); the
    # [pscustomobject] cast consumes PSTypeName as the type tag rather than as a property.
    $ordered = [ordered]@{}
    foreach ($key in $Result.Keys)
    {
        $ordered[$key] = $Result[$key]
    }
    $ordered['PSTypeName'] = 'Fluence.DialogResult'
    return [pscustomobject]$ordered
}
