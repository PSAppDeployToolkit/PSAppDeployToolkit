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

    begin {
        $adtEnv = Get-ADTEnvironment
        $adtSession = Get-ADTSession
        Initialize-ADTFunction -Cmdlet $PSCmdlet
    }

    process {
        # Bypass if no admin rights.
        if (!$adtEnv.IsAdmin)
        {
            Write-ADTLogEntry -Message "Bypassing Function [$($MyInvocation.MyCommand.Name)], because [User: $($adtEnv.ProcessNTAccount)] is not admin."
            return
        }

        # Remove Debugger values to unblock processes.
        Get-ItemProperty -Path "$($adtEnv.regKeyAppExecution)\*" -Name Debugger -ErrorAction Ignore | Where-Object {$_.Debugger.Contains('Show-ADTBlockedAppDialog')} | ForEach-Object {
            Write-ADTLogEntry -Message "Removing the Image File Execution Options registry key to unblock execution of [$($_.PSChildName)]."
            Remove-ItemProperty -LiteralPath $_.PSPath -Name Debugger
        }

        # Reset block execution state.
        $adtSession.BlockExecution = $false

        # Remove the scheduled task if it exists.
        try
        {
            Unregister-ScheduledTask -TaskName "$($adtSession.GetPropertyValue('installName'))_BlockedApps" -Confirm:$false
        }
        catch
        {
            Write-ADTLogEntry -Message "Error retrieving/deleting Scheduled Task.`n$(Resolve-ADTError)" -Severity 3
        }

        # Remove BlockAppExection temporary directory.
        if ([System.IO.Directory]::Exists(($tempPath = $adtSession.GetPropertyValue('dirAppDeployTemp'))))
        {
            Remove-ADTFolder -Path $tempPath
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
