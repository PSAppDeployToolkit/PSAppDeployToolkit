#-----------------------------------------------------------------------------
#
# MARK: Unblock-ADTAppExecutionInternal
#
#-----------------------------------------------------------------------------

function Private:Unblock-ADTAppExecutionInternal
{
    [CmdletBinding(DefaultParameterSetName = 'None')]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Tasks')]
        [ValidateNotNullOrEmpty()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [Microsoft.Management.Infrastructure.CimInstance[]]$Tasks,

        [Parameter(Mandatory = $true, ParameterSetName = 'TaskName')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$TaskName
    )

    # Remove Debugger values to unblock processes.
    Get-ItemProperty -Path "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\*\MyFilter", "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\*" -Name Debugger, FilterFullPath -Verbose:$false -ErrorAction Ignore | & {
        process
        {
            # Debugger is absent on keys carrying only FilterFullPath, and under StrictMode reading it then
            # throws, which at the startup task's ErrorActionPreference of Continue skips this return. Asked
            # for by name, as a list of names is searched case-sensitively while the registry's are not.
            if (!$_.PSObject.Properties['Debugger'] -or ($_.Debugger -notlike '*PSAppDeployToolkit*'))
            {
                return
            }

            if ($_.PSObject.Properties['FilterFullPath'])
            {
                Write-Verbose -Message "Removing the Image File Execution Options registry key to unblock execution of [$($_.FilterFullPath)]."
                Remove-ItemProperty -LiteralPath $_.PSParentPath -Name UseFilter -Verbose:$false
                Remove-ItemProperty -LiteralPath $_.PSPath -Name Debugger -Verbose:$false
                Remove-Item -LiteralPath "$($_.PSParentPath)\MyFilter" -Verbose:$false
                if (!(Get-ChildItem -LiteralPath $_.PSParentPath -Verbose:$false) -and !(Get-ItemProperty -LiteralPath $_.PSParentPath -Verbose:$false))
                {
                    Remove-Item -LiteralPath $_.PSParentPath -Verbose:$false
                }
            }
            else
            {
                Write-Verbose -Message "Removing the Image File Execution Options registry key to unblock execution of [$($_.PSChildName)]."
                Remove-ItemProperty -LiteralPath $_.PSPath -Name Debugger -Verbose:$false
                if (!(Get-ChildItem -LiteralPath $_.PSPath -Verbose:$false) -and !(Get-ItemProperty -LiteralPath $_.PSPath -Verbose:$false))
                {
                    Remove-Item -LiteralPath $_.PSPath -Verbose:$false
                }
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
            Write-Verbose -Message "Deleting Scheduled Tasks ['$([System.String]::Join("', '", $Tasks.TaskName))']."
            $Tasks | Unregister-ScheduledTask -Confirm:$false -Verbose:$false
            break
        }
    }
}
