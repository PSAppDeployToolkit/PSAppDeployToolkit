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
    $Message -replace '^', "[$dateTime] " | Write-Host @PSBoundParameters
}
