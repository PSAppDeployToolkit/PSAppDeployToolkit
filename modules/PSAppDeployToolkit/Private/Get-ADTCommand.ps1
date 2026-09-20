#-----------------------------------------------------------------------------
#
# MARK: Get-ADTCommand
#
#-----------------------------------------------------------------------------

function Private:Get-ADTCommand
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$Name
    )

    [System.Management.Automation.CommandInfo]$command = $null
    if (!$Script:CommandTable.TryGetValue($Name, [ref]$command))
    {
        $naerParams = @{
            Exception = [System.Management.Automation.CommandNotFoundException]::new("The term '$($Name)' is not recognized as the name of a cmdlet, function, script file, or operable program. Check the spelling of the name, or if a path was included, verify that the path is correct and try again.")
            Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
            ErrorId = 'CommandNotFoundException'
            TargetObject = $Name
            RecommendedAction = "Please report this error to the PSAppDeployToolkit team for further review."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    return $command
}
