#-----------------------------------------------------------------------------
#
# MARK: Write-ADTBuildLogEntry
#
#-----------------------------------------------------------------------------

function Write-ADTBuildLogEntry
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Message,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.ConsoleColor]$ForegroundColor
    )

    # Prepend a timestamp onto the message and write it out.
    $dateTime = if ($env:GITHUB_ACTIONS -eq 'true')
    {
        [System.DateTime]::Now.ToUniversalTime().ToString('O')
    }
    else
    {
        [System.DateTime]::Now.ToString('O')
    }
    $null = $PSBoundParameters.Remove('Message')
    if ([System.Console]::IsOutputRedirected -and ($env:GITHUB_ACTIONS -ne 'true'))
    {
        $Message -replace '^', "[$dateTime] " -replace '\x1B\[[0-9;]*m' | Write-Host @PSBoundParameters
    }
    else
    {
        $Message -replace '^', "[$dateTime] " | Write-Host @PSBoundParameters
    }
}
