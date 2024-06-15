Function Unblock-AppExecution {
    <#
.SYNOPSIS

Unblocks the execution of applications performed by the Block-AppExecution function

.DESCRIPTION

This function is called by the Close-ADTSession function or when the script itself is called with the parameters -CleanupBlockedApps

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Unblock-AppExecution

.NOTES

It is used when the -BlockExecution parameter is specified with the Show-ADTInstallationWelcome function to undo the actions performed by Block-AppExecution.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        $adtEnv = Get-ADTEnvironment
        $adtSession = Get-ADTSession
        Write-ADTDebugHeader
    }
    Process {
        ## Bypass if no Admin rights
        If (!$adtEnv.IsAdmin) {
            Write-ADTLogEntry -Message "Bypassing Function [$($MyInvocation.MyCommand.Name)], because [User: $($adtEnv.ProcessNTAccount)] is not admin."
            Return
        }

        ## Remove Debugger values to unblock processes
        [PSObject[]]$unblockProcesses = $null
        [PSObject[]]$unblockProcesses += (Get-ChildItem -LiteralPath $adtEnv.regKeyAppExecution -Recurse -ErrorAction 'Ignore' | ForEach-Object { Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction 'Ignore' })
        ForEach ($unblockProcess in ($unblockProcesses | Where-Object { $_.Debugger -like '*AppDeployToolkit_BlockAppExecutionMessage*' })) {
            Write-ADTLogEntry -Message "Removing the Image File Execution Options registry key to unblock execution of [$($unblockProcess.PSChildName)]."
            $unblockProcess | Remove-ItemProperty -Name 'Debugger' -ErrorAction 'Ignore'
        }

        ## If block execution variable is $true, set it to $false
        $adtSession.Internal.BlockExecution = $false

        ## Remove the scheduled task if it exists
        [String]$schTaskBlockedAppsName = $adtSession.GetPropertyValue('installName') + '_BlockedApps'
        Try {
            If (Get-SchedulerTask -ContinueOnError $true | Select-Object -Property 'TaskName' | Where-Object { $_.TaskName -eq "\$schTaskBlockedAppsName" }) {
                Write-ADTLogEntry -Message "Deleting Scheduled Task [$schTaskBlockedAppsName]."
                Start-ADTProcess -Path $adtEnv.exeSchTasks -Parameters "/Delete /TN $schTaskBlockedAppsName /F"
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Error retrieving/deleting Scheduled Task.`n$(Resolve-ADTError)" -Severity 3
        }

        ## Remove BlockAppExecution Schedule Task XML file
        [String]$xmlSchTaskFilePath = "$adtSession.GetPropertyValue('dirAppDeployTemp')\SchTaskUnBlockApps.xml"
        If (Test-Path -LiteralPath $xmlSchTaskFilePath) {
            $null = Remove-Item -LiteralPath $xmlSchTaskFilePath -Force -ErrorAction 'Ignore'
        }

        ## Remove BlockAppExection Temporary directory
        [String]$blockExecutionTempPath = Join-Path -Path $adtSession.GetPropertyValue('dirAppDeployTemp') -ChildPath 'BlockExecution'
        If (Test-Path -LiteralPath $blockExecutionTempPath -PathType 'Container') {
            Remove-Folder -Path $blockExecutionTempPath
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
