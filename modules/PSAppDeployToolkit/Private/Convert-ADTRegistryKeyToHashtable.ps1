#-----------------------------------------------------------------------------
#
# MARK: Convert-ADTRegistryKeyToHashtable
#
#-----------------------------------------------------------------------------

function Private:Convert-ADTRegistryKeyToHashtable
{
    begin
    {
        # Captured here because $MyInvocation.MyCommand inside the anonymous scriptblock below resolves to
        # that block rather than to this function, and invoking it is refused where code integrity is enforced.
        $thisCommand = $MyInvocation.MyCommand

        # Open collector to store all converted keys.
        $data = @{}
    }

    process
    {
        # Process potential subkeys first.
        $subdata = $_ | Get-ChildItem | & {
            end
            {
                if ($registryKeys = $($input) | & { process { if ($null -ne $_) { return $_ } } })
                {
                    try
                    {
                        $registryKeys | & $thisCommand
                    }
                    finally
                    {
                        $registryKeys.Dispose()
                    }
                }
            }
        }

        # Open a new subdata hashtable if we had no subkeys.
        if ($null -eq $subdata)
        {
            $subdata = @{}
        }

        # Process this item and store its values.
        $_ | Get-ItemProperty | & {
            process
            {
                $_.PSObject.Properties | & {
                    process
                    {
                        # Return early for the provider's bookkeeping properties.
                        if ($_.Name -match '^PS((Parent)?Path|ChildName|Provider)$')
                        {
                            return
                        }

                        # A value that renders as nothing is stored as null, the form the module uses for an unset value, and the reader decides what that means for the setting it lands on.
                        if (!(Out-ADTString -InputObject $_.Value))
                        {
                            $subdata.Add($_.Name, $null)
                            return
                        }

                        # Anything that isn't a string is stored as the provider gave it, for the caller to accept or refuse.
                        if ($_.Value -isnot [System.String])
                        {
                            $subdata.Add($_.Name, $_.Value)
                            return
                        }

                        # Type a string from its text, leaving it as-is when it won't fit.
                        $boolean = $false; $number = 0; if ([System.Boolean]::TryParse($_.Value, [ref]$boolean))
                        {
                            $subdata.Add($_.Name, $boolean)
                        }
                        elseif ([System.Int32]::TryParse($_.Value, [System.Globalization.NumberStyles]::Integer, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$number))
                        {
                            $subdata.Add($_.Name, $number)
                        }
                        elseif ($_.Value.StartsWith('0x', [System.StringComparison]::OrdinalIgnoreCase) -and [System.Int32]::TryParse($_.Value.Substring(2), [System.Globalization.NumberStyles]::HexNumber, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$number))
                        {
                            $subdata.Add($_.Name, $number)
                        }
                        else
                        {
                            $subdata.Add($_.Name, $_.Value)
                        }
                    }
                }
            }
        }

        # Add the subdata to the sections if it's got a count.
        if ($subdata.Count)
        {
            $data.Add($_.PSPath -replace '^.+\\', $subdata)
        }
    }

    end
    {
        # If there's something in the collector, return it.
        if ($data.Count)
        {
            return $data
        }
    }
}
