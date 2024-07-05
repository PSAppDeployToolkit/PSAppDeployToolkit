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
    param (
    )

    begin {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process {
        # Bypass if no admin rights.
        if (!(Test-ADTCallerIsAdmin))
        {
            Write-ADTLogEntry -Message "Bypassing Function [$($MyInvocation.MyCommand.Name)], because [User: $([System.Security.Principal.WindowsIdentity]::GetCurrent().Name)] is not admin."
            return
        }

        # Remove Debugger values to unblock processes.
        Get-ItemProperty -Path "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\*" -Name Debugger -ErrorAction Ignore | Where-Object {$_.Debugger.Contains('Show-ADTBlockedAppDialog')} | ForEach-Object {
            Write-ADTLogEntry -Message "Removing the Image File Execution Options registry key to unblock execution of [$($_.PSChildName)]."
            Remove-ItemProperty -LiteralPath $_.PSPath -Name Debugger
        }

        # Remove the scheduled task if it exists.
        if (Get-ScheduledTask -TaskName ($taskName = "PSAppDeployToolkit_*_BlockedApps") -ErrorAction Ignore)
        {
            Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
