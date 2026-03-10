#-----------------------------------------------------------------------------
#
# MARK: Export-ADTScriptBlockToFile
#
#-----------------------------------------------------------------------------

function Private:Export-ADTScriptBlockToFile
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [PSAppDeployToolkit.Foundation.ValidateNotNullOrWhiteSpace()]
        [System.Management.Automation.ScriptBlock]$ScriptBlock,

        [Parameter(Mandatory = $true)]
        [PSAppDeployToolkit.Foundation.ValidateNotNullOrWhiteSpace()]
        [System.String]$LiteralPath,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Text.Encoding]$Encoding = [System.Text.UTF8Encoding]::new($true, $true),

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Force
    )

    # Throw if the file exists.
    if ((Test-Path -LiteralPath $LiteralPath -PathType Container) -and !$Force)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("The specified file path [$LiteralPath] already exists.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'LiteralPathAlreadyExists'
            TargetObject = $LiteralPath
            RecommendedAction = "Validate your input or use [-Force] and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }

    # Parse the script block to fix indentation, then return as a string.
    $stringReader = [System.IO.StringReader]::new($ScriptBlock.ToString())
    $lines = [System.Collections.Generic.List[System.String]]::new()
    $substringIndex = -1
    try
    {
        # Read all lines to the end.
        while ($null -ne ($line = $stringReader.ReadLine()))
        {
            # Skip over any leading lines that are just whitespace.
            if ($substringIndex -eq -1)
            {
                if ([System.String]::IsNullOrWhiteSpace($line))
                {
                    continue
                }
                for ($i = 0; $i -lt $line.Length; $i++)
                {
                    if (![System.Char]::IsWhiteSpace($line[$i]))
                    {
                        $substringIndex = $i
                        break
                    }
                }
            }

            # Add this line to the collector, trimming the right amount off the start.
            if (![System.String]::IsNullOrWhiteSpace($line))
            {
                $lines.Add($line.Substring($substringIndex))
            }
            else
            {
                $lines.Add($line)
            }
        }
    }
    finally
    {
        $stringReader = $stringReader.Dispose()
    }

    # Remove any empty lines at the end.
    while ($true)
    {
        if ($lines.Count -eq 0)
        {
            $naerParams = @{
                Exception = [System.FormatException]::new("The process ScriptBlock resulted in an empty result.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                ErrorId = 'ScriptBlockStringDataInvalid'
                TargetObject = $lines
                RecommendedAction = "Review the scriptblock provided and try again."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
        if (![System.String]::IsNullOrWhiteSpace($lines[-1]))
        {
            break
        }
        $lines.RemoveAt($lines.Count - 1)
    }

    # Export the array to the specified path.
    [System.IO.File]::WriteAllLines($LiteralPath, $lines, $Encoding)
}
