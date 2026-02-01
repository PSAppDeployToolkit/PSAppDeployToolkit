#-----------------------------------------------------------------------------
#
# MARK: Confirm-ADTHashtableKeyEquality
#
#-----------------------------------------------------------------------------

function Confirm-ADTHashtableKeyEquality
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Reference,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Comparison
    )

    # Throw if there's any missing/extra keys.
    $refKeys = $($Reference.Keys); $cmpKeys = $($Comparison.Keys)
    if ($missing = $refKeys | & { process { if (!$cmpKeys.Contains($_)) { return $_ } } })
    {
        throw "The following hashtable keys are missing: ['$([System.String]::Join("', '", $missing))']."
    }
    if ($extras = $cmpKeys | & { process { if (!$refKeys.Contains($_)) { return $_ } } })
    {
        throw "The following hashtable keys are extras: ['$([System.String]::Join("', '", $extras))']."
    }

    # Test each key's value, recursively processing child hashtables.
    foreach ($key in $refKeys)
    {
        # Cache each hashtable's key value and whether it's a hashtable.
        $vRef = $Reference[$key]; $vCmp = $Comparison[$key]
        $vRefIsHash = $vRef -is [System.Collections.Hashtable]
        $vCmpIsHash = $vCmp -is [System.Collections.Hashtable]

        # If one is hashtable and the other isn’t, that’s a mismatch.
        if ($vRefIsHash -xor $vCmpIsHash)
        {
            throw "The key value [$key] is a hashtable on one side and not the other."
        }
        elseif ($vRefIsHash -and $vCmpIsHash)
        {
            & $MyInvocation.MyCommand -Reference $vRef -Comparison $vCmp
        }
    }
}
