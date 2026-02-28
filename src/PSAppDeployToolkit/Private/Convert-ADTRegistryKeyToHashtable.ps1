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
        $subdata = $_ | Get-ChildItem | & $MyInvocation.get_MyCommand()

        # Open a new subdata hashtable if we had no subkeys.
        if ($null -eq $subdata)
        {
            $subdata = @{}
        }

        # Process this item and store its values.
        $_ | Get-ItemProperty | & {
            process
            {
                $_.PSObject.get_Properties() | & {
                    process
                    {
                        if (($_.get_Name() -notmatch '^PS((Parent)?Path|ChildName|Provider)$') -and ![System.String]::IsNullOrWhiteSpace((Out-String -InputObject $_.get_Value())))
                        {
                            # Handle bools as string values.
                            if ($_.get_Value() -match '^(True|False)$')
                            {
                                $subdata.Add($_.get_Name(), [System.Boolean]::Parse($_.get_Value()))
                            }
                            elseif ($_.get_Value() -match '^-?\d+$')
                            {
                                $subdata.Add($_.get_Name(), [System.Int32]::Parse($_.get_Value()))
                            }
                            elseif ($_.get_Value() -match '^0[xX][0-9a-fA-F]+$')
                            {
                                $subdata.Add($_.get_Name(), [System.Int32]::Parse($_.get_Value().Replace('0x', [System.Management.Automation.Language.NullString]::get_Value()), [System.Globalization.NumberStyles]::HexNumber))
                            }
                            else
                            {
                                $subdata.Add($_.get_Name(), $_.get_Value())
                            }
                        }
                    }
                }
            }
        }

        # Add the subdata to the sections if it's got a count.
        if ($subdata.get_Count())
        {
            $data.Add($_.PSPath -replace '^.+\\', $subdata)
        }
    }

    end
    {
        # If there's something in the collector, return it.
        if ($data.get_Count())
        {
            return $data
        }
    }
}
