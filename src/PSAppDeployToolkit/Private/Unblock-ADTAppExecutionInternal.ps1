#-----------------------------------------------------------------------------
#
# MARK: Unblock-ADTAppExecutionInternal
#
#-----------------------------------------------------------------------------

function Private:Unblock-ADTAppExecutionInternal
{
    <#

    .SYNOPSIS
    Core logic used within Unblock-ADTAppExecution.

    .DESCRIPTION
    This function contains core logic used within Unblock-ADTAppExecution, separated out to facilitate calling via PowerShell without dependency on the toolkit.

    .NOTES
    This function deliberately does not use the module's CommandTable to ensure it can run without module dependency.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding(DefaultParameterSetName = 'None')]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Tasks')]
        [ValidateNotNullOrEmpty()]
        [Microsoft.Management.Infrastructure.CimInstance[]]$Tasks,

        [Parameter(Mandatory = $true, ParameterSetName = 'TaskName')]
        [ValidateNotNullOrEmpty()]
        [System.String]$TaskName
    )

    # Remove Debugger values to unblock processes.
    Get-ItemProperty -Path "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\*\MyFilter", "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\*" -Name Debugger, FilterFullPath -Verbose:$false -ErrorAction Ignore | & {
        process
        {
            if (!$_.Debugger.Contains('PSAppDeployToolkit'))
            {
                return
            }

            if ($_.PSObject.Properties.Name.Contains('FilterFullPath'))
            {
                Write-Verbose -Message "Removing the Image File Execution Options registry key to unblock execution of [$($_.FilterFullPath)]."
                Remove-ItemProperty -LiteralPath $_.PSParentPath -Name UseFilter -Verbose:$false
                Remove-ItemProperty -LiteralPath $_.PSPath -Name Debugger -Verbose:$false
                Remove-Item -LiteralPath "$($_.PSParentPath)\MyFilter" -Verbose:$false
            }
            else
            {
                Write-Verbose -Message "Removing the Image File Execution Options registry key to unblock execution of [$($_.PSChildName)]."
                Remove-ItemProperty -LiteralPath $_.PSPath -Name Debugger -Verbose:$false
            }
        }
    }

    # Remove the scheduled task if it exists.
    switch ($PSCmdlet.ParameterSetName)
    {
        TaskName
        {
            Write-Verbose -Message "Deleting Scheduled Task [$TaskName]."
            Get-ScheduledTask -TaskName $TaskName -Verbose:$false -ErrorAction Ignore | Unregister-ScheduledTask -Confirm:$false -Verbose:$false
            break
        }
        Tasks
        {
            Write-Verbose -Message "Deleting Scheduled Tasks ['$($Tasks.TaskName -join "', '")']."
            $Tasks | Unregister-ScheduledTask -Confirm:$false -Verbose:$false
            break
        }
    }
}
