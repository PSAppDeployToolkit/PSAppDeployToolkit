#-----------------------------------------------------------------------------
#
# MARK: Confirm-ADTStringTablesValid
#
#-----------------------------------------------------------------------------

function Confirm-ADTStringTablesValid
{
    # Internal worker function to ensure keys match 1:1, and no value matches.
    function Confirm-ADTStringTableValid
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
        $refKeys = [System.String[]]$Reference.Keys; $cmpKeys = [System.String[]]$Comparison.Keys
        if ($missing = $refKeys | & { process { if (!$cmpKeys.Contains($_)) { return $_ } } })
        {
            throw "The following hashtable keys are missing: ['$([System.String]::Join("', '", $missing))']."
        }
        if ($extras = $cmpKeys | & { process { if (!$refKeys.Contains($_)) { return $_ } } })
        {
            throw "The following hashtable keys are extras: ['$([System.String]::Join("', '", $extras))']."
        }

        # Throw if there's any identical values that aren't null or empty.
        $identical = $refKeys | & {
            process
            {
                # Skip when each entry is null or whitespace.
                $refVal = $Reference[$_]; $refIsNull = [System.String]::IsNullOrWhiteSpace((Out-String -InputObject $refVal))
                $cmpVal = $Comparison[$_]; $cmpIsNull = [System.String]::IsNullOrWhiteSpace((Out-String -InputObject $cmpVal))
                if ($refIsNull -and $cmpIsNull)
                {
                    return
                }

                # Perform value checks, bypassing PowerShell's equality nonsense with type coercion, etc.
                if (!$refIsNull -and $refVal.Equals($cmpVal))
                {
                    return $_
                }
                if (!$cmpIsNull -and $cmpVal.Equals($refVal))
                {
                    return $_
                }
                if ([System.Object]::Equals($refVal, $cmpVal))
                {
                    return $_
                }
            }
        }
        if ($identical)
        {
            throw "The following hashtable key values are identical: ['$([System.String]::Join("', '", $identical))']."
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

    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Verify the formatting of all PowerShell script files within the repository.
        Write-ADTBuildLogEntry -Message "Confirming string translation files have the same keys as English, this may take awhile."
        $reference = Import-LocalizedData -BaseDirectory ([System.Management.Automation.WildcardPattern]::Escape($Script:ModuleConstants.Paths.ModuleStrings)) -FileName strings.psd1
        foreach ($stringFile in (Get-ChildItem -LiteralPath $Script:ModuleConstants.Paths.ModuleStrings -Directory | Get-ChildItem -File))
        {
            Write-ADTBuildLogEntry -Message "Testing file [$($stringFile.FullName)]..."
            Confirm-ADTStringTableValid -Reference $reference -Comparison (Import-LocalizedData -BaseDirectory ([System.Management.Automation.WildcardPattern]::Escape($stringFile.Directory.FullName)) -FileName $stringFile.Name)
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}
