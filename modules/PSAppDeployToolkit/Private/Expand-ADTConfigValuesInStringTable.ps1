#-----------------------------------------------------------------------------
#
# MARK: Expand-ADTConfigValuesInStringTable
#
#-----------------------------------------------------------------------------

function Private:Expand-ADTConfigValuesInStringTable
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Config', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Hashtable,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Config
    )

    # Internal filter to substitute the config's values into the table's strings.
    filter Update-ADTStringTableValues
    {
        # Go recursive if we have received a hashtable, otherwise substitute the value.
        foreach ($section in $($_.GetEnumerator()))
        {
            if ($section.Value -is [System.String])
            {
                $_.($section.Key) = $substitutions.Replace($section.Value,
                    {
                        return $args[0].Groups[1].Value.Split('\', [System.StringSplitOptions]::RemoveEmptyEntries) | & {
                            begin
                            {
                                $result = $Config
                            }
                            process
                            {
                                $result = $result.$_
                            }
                            end
                            {
                                return $result
                            }
                        }
                    })
            }
            elseif ($section.Value -is [System.Collections.Hashtable])
            {
                $section.Value | & $MyInvocation.MyCommand
            }
        }
    }

    # Substitute using the one regex for the whole table. Compiling it costs far more than it saves here.
    $substitutions = [System.Text.RegularExpressions.Regex]::new('\{([^\d{}]+)\}')
    $Hashtable | Update-ADTStringTableValues
}
