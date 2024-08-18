#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Unblock-ADTAppExecution
{
    <#

    .SYNOPSIS
    Unblocks the execution of applications performed by the Block-ADTAppExecution function

    .DESCRIPTION
    This function is called by the Close-ADTSession function or when the script itself is called with the parameters -CleanupBlockedApps

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    Unblock-ADTAppExecution

    .NOTES
    It is used when the -BlockExecution parameter is specified with the Show-ADTInstallationWelcome function to undo the actions performed by Block-ADTAppExecution.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Microsoft.Management.Infrastructure.CimInstance[]]$Tasks = (& $Script:CommandTable.'Get-ScheduledTask' -TaskName "$($MyInvocation.MyCommand.Module.Name)_*_BlockedApps" -ErrorAction Ignore)
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $uaaeiParams = @{}; if ($Tasks) { $uaaeiParams.Add('Tasks', $Tasks) }
    }

    process
    {
        # Bypass if no admin rights.
        if (!(Test-ADTCallerIsAdmin))
        {
            Write-ADTLogEntry -Message "Bypassing Function [$($MyInvocation.MyCommand.Name)], because [User: $([System.Security.Principal.WindowsIdentity]::GetCurrent().Name)] is not admin."
            return
        }

        # Clean up blocked apps using our backend worker.
        try
        {
            try
            {
                Unblock-ADTAppExecutionInternal @uaaeiParams -Verbose 4>&1 | Write-ADTLogEntry
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
