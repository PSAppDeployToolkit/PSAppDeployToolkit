#-----------------------------------------------------------------------------
#
# MARK: Convert-ADTRegistryKeyToHashtable
#
#-----------------------------------------------------------------------------

function Private:Convert-ADTRegistryKeyToHashtable
{
    begin
    {
        # Open collector to store all converted keys.
        $data = @{}
    }

    process
    {
        # Process potential subkeys first.
        $subdata = $_ | Get-ChildItem | & $MyInvocation.MyCommand

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
                        if (($_.Name -notmatch '^PS((Parent)?Path|ChildName|Provider)$') -and ![System.String]::IsNullOrWhiteSpace((Out-String -InputObject $_.Value)))
                        {
                            # Handle bools as string values.
                            if ($_.Value -match '^(True|False)$')
                            {
                                $subdata.Add($_.Name, [System.Boolean]::Parse($_.Value))
                            }
                            elseif ($_.Value -match '^-?\d+$')
                            {
                                $subdata.Add($_.Name, [System.Int32]::Parse($_.Value))
                            }
                            elseif ($_.Value -match '^0[xX][0-9a-fA-F]+$')
                            {
                                $subdata.Add($_.Name, [System.Int32]::Parse($_.Value.Replace('0x', $null), [System.Globalization.NumberStyles]::HexNumber))
                            }
                            else
                            {
                                $subdata.Add($_.Name, $_.Value)
                            }
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
