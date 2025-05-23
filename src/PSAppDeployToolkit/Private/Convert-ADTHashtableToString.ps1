#-----------------------------------------------------------------------------
#
# MARK: Convert-ADTHashtableToString
#
#-----------------------------------------------------------------------------

function Private:Convert-ADTHashtableToString
{
    begin
    {
        # Open collector to store all processed hashtable entries.
        $data = [System.Collections.Generic.List[System.String]]::new()
    }

    process
    {
        # Process the current hashtable in the pipe.
        foreach ($kvp in $_.GetEnumerator())
        {
            if ([System.String]::IsNullOrWhiteSpace((Out-String -InputObject $kvp.Value)))
            {
                continue
            }
            elseif ($kvp.Value -is [System.Collections.Hashtable])
            {
                $data.Add("$($kvp.Key) = [$($kvp.Value.GetType().FullName)]$($kvp.Value | & $MyInvocation.MyCommand)")
            }
            elseif ($kvp.Value -is [System.Management.Automation.SwitchParameter])
            {
                $data.Add("$($kvp.Key) = [System.Boolean]'$($kvp.Value.ToString().Replace("'", "''"))'")
            }
            else
            {
                $data.Add("$($kvp.Key) = [$($kvp.Value.GetType().FullName)]'$($kvp.Value.ToString().Replace("'", "''"))'")
            }
        }
    }

    end
    {
        # If there's something in the collector, return it.
        if ($data.Count)
        {
            return "@{ $([System.String]::Join('; ', $data)) }"
        }
    }
}
