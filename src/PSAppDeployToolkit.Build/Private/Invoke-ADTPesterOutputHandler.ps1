#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTPesterOutputHandler
#
#-----------------------------------------------------------------------------

filter Invoke-ADTPesterOutputHandler
{
    # Capture all InformationRecord objects output from Pester so we can re-route them.
    # We also do some sanitising here as we want verbose Pester output except for CodeCov.
    if ($_ -is [System.Management.Automation.InformationRecord])
    {
        # Skip over the messages we don't want.
        $rawLine = $_.MessageData.Message -replace '\x1B\[[0-9;]*m'
        if (!$rawLine.Length -or ($rawLine -cmatch '^(Pester v\d+|Running tests\.|\s\d+|Missed commands:|\r\nFile\s+Class\s+Function\s+Line\s+Command)'))
        {
            return
        }

        # Process the message to remove empty lines.
        Write-ADTBuildLogEntry -Message ($_.MessageData.Message.Split("`n", [System.StringSplitOptions]::RemoveEmptyEntries) -replace '\r' | & {
                process
                {
                    # Skip over any empty lines.
                    if ([System.String]::IsNullOrWhiteSpace($_ -replace '\x1B\[[0-9;]*m'))
                    {
                        return
                    }

                    # Strip any ending commas.
                    if (($_ -replace '\x1B\[[0-9;]*m').Trim().EndsWith(','))
                    {
                        return $_.Replace(',', [System.Management.Automation.Language.NullString]::Value)
                    }

                    # Otherwise, return the line as-is.
                    return $_
                }
            })
    }
    else
    {
        return $_
    }
}
