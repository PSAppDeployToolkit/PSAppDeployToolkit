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
        $_.GetEnumerator() | & {
            process
            {
                if (![System.String]::IsNullOrWhiteSpace((Out-String -InputObject $_.Value)))
                {
                    $data.Add("$($_.Key) = [$($_.Value.GetType().FullName)]'$($_.Value.ToString().Replace("'", "''"))'")
                }
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
