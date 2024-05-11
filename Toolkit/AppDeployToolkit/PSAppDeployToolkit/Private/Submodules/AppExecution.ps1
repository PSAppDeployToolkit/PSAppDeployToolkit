#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Block-AppExecution {
    <#
.SYNOPSIS

Block the execution of an application(s)

.DESCRIPTION

This function is called when you pass the -BlockExecution parameter to the Stop-RunningApplications function. It does the following:

1.  Makes a copy of this script in a temporary directory on the local machine.
2.  Checks for an existing scheduled task from previous failed installation attempt where apps were blocked and if found, calls the Unblock-AppExecution function to restore the original IFEO registry keys.
        This is to prevent the function from overriding the backup of the original IFEO options.
3.  Creates a scheduled task to restore the IFEO registry key values in case the script is terminated uncleanly by calling the local temporary copy of this script with the parameter -CleanupBlockedApps.
4.  Modifies the "Image File Execution Options" registry key for the specified process(s) to call this script with the parameter -ShowBlockedAppDialog.
5.  When the script is called with those parameters, it will display a custom message to the user to indicate that execution of the application has been blocked while the installation is in progress.
        The text of this message can be customized in the XML configuration file.

.PARAMETER ProcessName

Name of the process or processes separated by commas

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Block-AppExecution -ProcessName ('winword','excel')

.NOTES

This is an internal script function and should typically not be called directly.

It is used when the -BlockExecution parameter is specified with the Show-InstallationWelcome function to block applications.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        ## Specify process names separated by commas
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String[]]$ProcessName
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        ## Remove illegal characters from the scheduled task arguments string
        [char[]]$invalidScheduledTaskChars = '$', '!', '''', '"', '(', ')', ';', '\', '`', '*', '?', '{', '}', '[', ']', '<', '>', '|', '&', '%', '#', '~', '@', ' '
        [string]$SchInstallName = $Script:ADT.CurrentSession.GetPropertyValue('installName')
        ForEach ($invalidChar in $invalidScheduledTaskChars) {
            [string]$SchInstallName = $SchInstallName -replace [regex]::Escape($invalidChar),''
        }
        [string]$blockExecutionTempPath = Join-Path -Path $Script:ADT.CurrentSession.GetPropertyValue('dirAppDeployTemp') -ChildPath 'BlockExecution'
        [string]$schTaskUnblockAppsCommand += "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -File `"$blockExecutionTempPath\$scriptFileName`" -CleanupBlockedApps -ReferredInstallName `"$SchInstallName`" -ReferredInstallTitle `"$($Script:ADT.CurrentSession.GetPropertyValue('installTitle'))`" -ReferredLogName `"$($Script:ADT.CurrentSession.GetPropertyValue('logName'))`" -AsyncToolkitLaunch"
        ## Specify the scheduled task configuration in XML format
        [string]$xmlUnblockAppsSchTask = @"
<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
    <RegistrationInfo></RegistrationInfo>
    <Triggers>
        <BootTrigger>
            <Enabled>true</Enabled>
        </BootTrigger>
    </Triggers>
    <Principals>
        <Principal id="Author">
            <UserId>S-1-5-18</UserId>
        </Principal>
    </Principals>
    <Settings>
        <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
        <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
        <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
        <AllowHardTerminate>true</AllowHardTerminate>
        <StartWhenAvailable>false</StartWhenAvailable>
        <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
        <IdleSettings>
            <StopOnIdleEnd>false</StopOnIdleEnd>
            <RestartOnIdle>false</RestartOnIdle>
        </IdleSettings>
        <AllowStartOnDemand>true</AllowStartOnDemand>
        <Enabled>true</Enabled>
        <Hidden>false</Hidden>
        <RunOnlyIfIdle>false</RunOnlyIfIdle>
        <WakeToRun>false</WakeToRun>
        <ExecutionTimeLimit>PT1H</ExecutionTimeLimit>
        <Priority>7</Priority>
    </Settings>
    <Actions Context="Author">
        <Exec>
            <Command>$($Script:ADT.Environment.envPSProcessPath)</Command>
            <Arguments>$schTaskUnblockAppsCommand</Arguments>
        </Exec>
    </Actions>
</Task>
"@
    }
    Process {
        ## Bypass if no Admin rights
        If (!$Script:ADT.Environment.IsAdmin) {
            Write-ADTLogEntry -Message "Bypassing Function [${CmdletName}], because [User: $($Script:ADT.Environment.ProcessNTAccount)] is not admin." -Source ${CmdletName}
            Return
        }

        [String]$schTaskBlockedAppsName = $Script:ADT.CurrentSession.GetPropertyValue('installName') + '_BlockedApps'

        ## Delete this file if it exists as it can cause failures (it is a bug from an older version of the toolkit)
        If (Test-Path -LiteralPath "$($Script:ADT.Config.Toolkit.TempPath)\$($Script:MyInvocation.MyCommand.ScriptBlock.Module.Name)" -PathType 'Leaf' -ErrorAction 'Ignore') {
            $null = Remove-Item -LiteralPath "$($Script:ADT.Config.Toolkit.TempPath)\$($Script:MyInvocation.MyCommand.ScriptBlock.Module.Name)" -Force -ErrorAction 'Ignore'
        }

        If (Test-Path -LiteralPath $blockExecutionTempPath -PathType 'Container') {
            Remove-Folder -Path $blockExecutionTempPath
        }

        Try {
            $null = New-Item -Path $blockExecutionTempPath -ItemType 'Directory' -ErrorAction 'Stop'
        }
        Catch {
            Write-ADTLogEntry -Message "Unable to create [$blockExecutionTempPath]. Possible attempt to gain elevated rights." -Source ${CmdletName}
        }

        Copy-Item -Path "$scriptRoot\*.*" -Destination $blockExecutionTempPath -Exclude 'thumbs.db' -Force -Recurse -ErrorAction 'Ignore'

        ## Build the debugger block value script
        [String[]]$debuggerBlockScript = "strCommand = `"$($Script:ADT.Environment.envPSProcessPath) -ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -File `" & chr(34) & `"$blockExecutionTempPath\$scriptFileName`" & chr(34) & `" -ShowBlockedAppDialog -AsyncToolkitLaunch -ReferredInstallTitle `" & chr(34) & `"$($Script:ADT.CurrentSession.GetPropertyValue('installTitle'))`" & chr(34)"
        $debuggerBlockScript += 'set oWShell = CreateObject("WScript.Shell")'
        $debuggerBlockScript += 'oWShell.Run strCommand, 0, false'
        $debuggerBlockScript | Out-File -FilePath "$blockExecutionTempPath\AppDeployToolkit_BlockAppExecutionMessage.vbs" -Force -Encoding 'Default' -ErrorAction 'Ignore'
        [String]$debuggerBlockValue = "$env:WinDir\System32\wscript.exe `"$blockExecutionTempPath\AppDeployToolkit_BlockAppExecutionMessage.vbs`""

        ## Set contents to be readable for all users (BUILTIN\USERS)
        Try {
            $Users = ConvertTo-NTAccountOrSID -SID 'S-1-5-32-545'
            Set-ItemPermission -Path $blockExecutionTempPath -User $Users -Permission 'Read' -Inheritance ('ObjectInherit', 'ContainerInherit')
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to set read permissions on path [$blockExecutionTempPath]. The function might not be able to work correctly." -Source ${CmdletName} -Severity 2
        }

        ## Create a scheduled task to run on startup to call this script and clean up blocked applications in case the installation is interrupted, e.g. user shuts down during installation"
        Write-ADTLogEntry -Message 'Creating scheduled task to cleanup blocked applications in case the installation is interrupted.' -Source ${CmdletName}
        If (Get-SchedulerTask -ContinueOnError $true | Select-Object -Property 'TaskName' | Where-Object { $_.TaskName -eq "\$schTaskBlockedAppsName" }) {
            Write-ADTLogEntry -Message "Scheduled task [$schTaskBlockedAppsName] already exists." -Source ${CmdletName}
        }
        Else {
            ## Export the scheduled task XML to file
            Try {
                ## Specify the filename to export the XML to
                ## XML does not need to be user readable to stays in protected TEMP folder
                [String]$xmlSchTaskFilePath = "$($Script:ADT.CurrentSession.GetPropertyValue('dirAppDeployTemp'))\SchTaskUnBlockApps.xml"
                [String]$xmlUnblockAppsSchTask | Out-File -FilePath $xmlSchTaskFilePath -Force -ErrorAction 'Stop'
            }
            Catch {
                Write-ADTLogEntry -Message "Failed to export the scheduled task XML file [$xmlSchTaskFilePath]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                Return
            }

            ## Import the Scheduled Task XML file to create the Scheduled Task
            [PSObject]$schTaskResult = Execute-Process -Path $Script:ADT.Environment.exeSchTasks -Parameters "/create /f /tn $schTaskBlockedAppsName /xml `"$xmlSchTaskFilePath`"" -WindowStyle 'Hidden' -CreateNoWindow -PassThru -ExitOnProcessFailure $false
            If ($schTaskResult.ExitCode -ne 0) {
                Write-ADTLogEntry -Message "Failed to create the scheduled task [$schTaskBlockedAppsName] by importing the scheduled task XML file [$xmlSchTaskFilePath]." -Severity 3 -Source ${CmdletName}
                Return
            }
        }

        [String[]]$blockProcessName = $processName
        ## Append .exe to match registry keys
        [String[]]$blockProcessName = $blockProcessName | ForEach-Object { $_ + '.exe' } -ErrorAction 'Ignore'

        ## Enumerate each process and set the debugger value to block application execution
        ForEach ($blockProcess in $blockProcessName) {
            Write-ADTLogEntry -Message "Setting the Image File Execution Option registry key to block execution of [$blockProcess]." -Source ${CmdletName}
            Set-RegistryKey -Key (Join-Path -Path $Script:ADT.Environment.regKeyAppExecution -ChildPath $blockProcess) -Name 'Debugger' -Value $debuggerBlockValue -ContinueOnError $true
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Unblock-AppExecution {
    <#
.SYNOPSIS

Unblocks the execution of applications performed by the Block-AppExecution function

.DESCRIPTION

This function is called by the Exit-Script function or when the script itself is called with the parameters -CleanupBlockedApps

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Unblock-AppExecution

.NOTES

This is an internal script function and should typically not be called directly.

It is used when the -BlockExecution parameter is specified with the Show-InstallationWelcome function to undo the actions performed by Block-AppExecution.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## Bypass if no Admin rights
        If (!$Script:ADT.Environment.IsAdmin) {
            Write-ADTLogEntry -Message "Bypassing Function [${CmdletName}], because [User: $($Script:ADT.Environment.ProcessNTAccount)] is not admin." -Source ${CmdletName}
            Return
        }

        ## Remove Debugger values to unblock processes
        [PSObject[]]$unblockProcesses = $null
        [PSObject[]]$unblockProcesses += (Get-ChildItem -LiteralPath $Script:ADT.Environment.regKeyAppExecution -Recurse -ErrorAction 'Ignore' | ForEach-Object { Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction 'Ignore' })
        ForEach ($unblockProcess in ($unblockProcesses | Where-Object { $_.Debugger -like '*AppDeployToolkit_BlockAppExecutionMessage*' })) {
            Write-ADTLogEntry -Message "Removing the Image File Execution Options registry key to unblock execution of [$($unblockProcess.PSChildName)]." -Source ${CmdletName}
            $unblockProcess | Remove-ItemProperty -Name 'Debugger' -ErrorAction 'Ignore'
        }

        ## If block execution variable is $true, set it to $false
        $Script:ADT.CurrentSession.State.BlockExecution = $false

        ## Remove the scheduled task if it exists
        [String]$schTaskBlockedAppsName = $Script:ADT.CurrentSession.GetPropertyValue('installName') + '_BlockedApps'
        Try {
            If (Get-SchedulerTask -ContinueOnError $true | Select-Object -Property 'TaskName' | Where-Object { $_.TaskName -eq "\$schTaskBlockedAppsName" }) {
                Write-ADTLogEntry -Message "Deleting Scheduled Task [$schTaskBlockedAppsName]." -Source ${CmdletName}
                Execute-Process -Path $Script:ADT.Environment.exeSchTasks -Parameters "/Delete /TN $schTaskBlockedAppsName /F"
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Error retrieving/deleting Scheduled Task.`r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }

        ## Remove BlockAppExecution Schedule Task XML file
        [String]$xmlSchTaskFilePath = "$Script:ADT.CurrentSession.GetPropertyValue('dirAppDeployTemp')\SchTaskUnBlockApps.xml"
        If (Test-Path -LiteralPath $xmlSchTaskFilePath) {
            $null = Remove-Item -LiteralPath $xmlSchTaskFilePath -Force -ErrorAction 'Ignore'
        }

        ## Remove BlockAppExection Temporary directory
        [String]$blockExecutionTempPath = Join-Path -Path $Script:ADT.CurrentSession.GetPropertyValue('dirAppDeployTemp') -ChildPath 'BlockExecution'
        If (Test-Path -LiteralPath $blockExecutionTempPath -PathType 'Container') {
            Remove-Folder -Path $blockExecutionTempPath
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
