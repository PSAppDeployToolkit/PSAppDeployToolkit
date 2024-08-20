#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Invoke-ADTCommandWithRetries
{
    <#

    .SYNOPSIS
    Drop-in replacement for any cmdlet/function where a retry is desirable due to transient issues.

    .DESCRIPTION
    This function invokes the specified cmdlet/function, accepting all of its parameters but retries an operation for the configured value before throwing.

    .PARAMETER Command
    The name of the command to invoke.

    .PARAMETER Retries
    How many retries to perform before throwing.

    .PARAMETER SleepSeconds
    How many seconds to sleep between retries.

    .PARAMETER Parameters
    A 'ValueFromRemainingArguments' parameter to collect the parameters as would be passed to the provided Command.

    While values can be directly provided to this parameter, it's not designed to be explicitly called.

    .INPUTS
    None. Invoke-ADTCommandWithRetries accepts no input.

    .OUTPUTS
    System.Object. Invoke-ADTCommandWithRetries returns the output of the invoked command.

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Object]$Command,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$Retries = 3,

        [Parameter(Mandatory = $false)]
        [ValidateRange(1, 60)]
        [System.UInt32]$SleepSeconds = 5,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Object]$Parameters
    )

    # Attempt to get command from our lookup table.
    $commandObj = if ($Command -is [System.Management.Automation.CommandInfo])
    {
        $Command
    }
    elseif ($Script:CommandTable.Contains($Command))
    {
        $Script:CommandTable.$Command
    }
    else
    {
        try
        {
            & $Script:CommandTable.'Get-Command' -Name $Command
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    # Convert the passed parameters into a dictionary for splatting onto the command.
    $boundParams = Convert-ADTValuesFromRemainingArguments -RemainingArguments $Parameters
    $callerName = (& $Script:CommandTable.'Get-PSCallStack')[1].Command

    # Perform the request, and retry it as per the configured values.
    for ($i = 0; $i -lt $Retries; $i++)
    {
        try
        {
            & $commandObj @boundParams
            return
        }
        catch
        {
            Write-ADTLogEntry -Message "The invocation to '$($commandObj.Name)' failed with message: $($_.Exception.Message.TrimEnd('.')). Trying again in $SleepSeconds second$(if (!$SleepSeconds.Equals(1)) {'s'})." -Severity 2 -Source $callerName
            [System.Threading.Thread]::Sleep($SleepSeconds * 1000)
            $errorRecord = $_
        }
    }

    # If we're here, we failed too many times. Throw the captured ErrorRecord.
    throw $errorRecord
}
