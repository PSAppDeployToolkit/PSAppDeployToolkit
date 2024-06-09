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

It is used when the -BlockExecution parameter is specified with the Show-ADTInstallationWelcome function to block applications.

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
        $adtEnv = Get-ADTEnvironment
        $adtConfig = Get-ADTConfig
        $adtModule = Get-ADTModuleInfo
        $adtSession = Get-ADTSession
        Write-ADTDebugHeader

        ## Remove illegal characters from the scheduled task arguments string
        [char[]]$invalidScheduledTaskChars = '$', '!', '''', '"', '(', ')', ';', '\', '`', '*', '?', '{', '}', '[', ']', '<', '>', '|', '&', '%', '#', '~', '@', ' '
        [string]$SchInstallName = $adtSession.GetPropertyValue('installName')
        ForEach ($invalidChar in $invalidScheduledTaskChars) {
            [string]$SchInstallName = $SchInstallName -replace [regex]::Escape($invalidChar),''
        }
        [string]$blockExecutionTempPath = Join-Path -Path $adtSession.GetPropertyValue('dirAppDeployTemp') -ChildPath 'BlockExecution'
        [string]$schTaskUnblockAppsCommand = "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command Import-Module -Name '$blockExecutionTempPath'; Import-ADTModuleState; Unblock-AppExecution"
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
            <Command>$($adtEnv.envPSProcessPath)</Command>
            <Arguments>$schTaskUnblockAppsCommand</Arguments>
        </Exec>
    </Actions>
</Task>
"@
    }
    Process {
        ## Bypass if no Admin rights
        If (!$adtEnv.IsAdmin) {
            Write-ADTLogEntry -Message "Bypassing Function [$($MyInvocation.MyCommand.Name)], because [User: $($adtEnv.ProcessNTAccount)] is not admin."
            Return
        }

        [String]$schTaskBlockedAppsName = $adtSession.GetPropertyValue('installName') + '_BlockedApps'

        ## Delete this file if it exists as it can cause failures (it is a bug from an older version of the toolkit)
        If (Test-Path -LiteralPath "$($adtConfig.Toolkit.TempPath)\$($adtModule.Name)" -PathType 'Leaf' -ErrorAction 'Ignore') {
            $null = Remove-Item -LiteralPath "$($adtConfig.Toolkit.TempPath)\$($adtModule.Name)" -Force -ErrorAction 'Ignore'
        }

        If (Test-Path -LiteralPath $blockExecutionTempPath -PathType 'Container') {
            Remove-Folder -Path $blockExecutionTempPath
        }

        Try {
            $null = New-Item -Path $blockExecutionTempPath -ItemType 'Directory' -ErrorAction 'Stop'
        }
        Catch {
            Write-ADTLogEntry -Message "Unable to create [$blockExecutionTempPath]. Possible attempt to gain elevated rights."
        }

        Copy-Item -LiteralPath (Get-ADTModuleInfo).ModuleBase -Destination $blockExecutionTempPath -Exclude 'thumbs.db' -Force -Recurse
        Export-Clixml -LiteralPath "$blockExecutionTempPath\$($adtModule.Name).xml" -Depth ([System.Int32]::MaxValue)

        ## Build the debugger block value script
        [String[]]$debuggerBlockScript = "strCommand = `"$($adtEnv.envPSProcessPath) -ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command Import-Module -Name '$($adtModule.ModuleBase)'; Import-ADTModuleState; Show-ADTBlockedAppDialog`""
        $debuggerBlockScript += 'set oWShell = CreateObject("WScript.Shell")'
        $debuggerBlockScript += 'oWShell.Run strCommand, 0, false'
        $debuggerBlockScript | Out-File -FilePath "$blockExecutionTempPath\AppDeployToolkit_BlockAppExecutionMessage.vbs" -Force -Encoding 'Default' -ErrorAction 'Ignore'
        [String]$debuggerBlockValue = "$env:WinDir\System32\wscript.exe `"$blockExecutionTempPath\AppDeployToolkit_BlockAppExecutionMessage.vbs`""

        ## Set contents to be readable for all users (BUILTIN\USERS)
        Try {
            $Users = ConvertTo-ADTNTAccountOrSID -SID 'S-1-5-32-545'
            Set-ItemPermission -Path $blockExecutionTempPath -User $Users -Permission 'Read' -Inheritance ('ObjectInherit', 'ContainerInherit')
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to set read permissions on path [$blockExecutionTempPath]. The function might not be able to work correctly." -Severity 2
        }

        ## Create a scheduled task to run on startup to call this script and clean up blocked applications in case the installation is interrupted, e.g. user shuts down during installation"
        Write-ADTLogEntry -Message 'Creating scheduled task to cleanup blocked applications in case the installation is interrupted.'
        If (Get-SchedulerTask -ContinueOnError $true | Select-Object -Property 'TaskName' | Where-Object { $_.TaskName -eq "\$schTaskBlockedAppsName" }) {
            Write-ADTLogEntry -Message "Scheduled task [$schTaskBlockedAppsName] already exists."
        }
        Else {
            ## Export the scheduled task XML to file
            Try {
                ## Specify the filename to export the XML to
                ## XML does not need to be user readable to stays in protected TEMP folder
                [String]$xmlSchTaskFilePath = "$($adtSession.GetPropertyValue('dirAppDeployTemp'))\SchTaskUnBlockApps.xml"
                [String]$xmlUnblockAppsSchTask | Out-File -FilePath $xmlSchTaskFilePath -Force -ErrorAction 'Stop'
            }
            Catch {
                Write-ADTLogEntry -Message "Failed to export the scheduled task XML file [$xmlSchTaskFilePath]. `r`n$(Resolve-Error)" -Severity 3
                Return
            }

            ## Import the Scheduled Task XML file to create the Scheduled Task
            [PSObject]$schTaskResult = Execute-Process -Path $adtEnv.exeSchTasks -Parameters "/create /f /tn $schTaskBlockedAppsName /xml `"$xmlSchTaskFilePath`"" -WindowStyle 'Hidden' -CreateNoWindow -PassThru -ExitOnProcessFailure $false
            If ($schTaskResult.ExitCode -ne 0) {
                Write-ADTLogEntry -Message "Failed to create the scheduled task [$schTaskBlockedAppsName] by importing the scheduled task XML file [$xmlSchTaskFilePath]." -Severity 3
                Return
            }
        }

        [String[]]$blockProcessName = $processName
        ## Append .exe to match registry keys
        [String[]]$blockProcessName = $blockProcessName | ForEach-Object { $_ + '.exe' } -ErrorAction 'Ignore'

        ## Enumerate each process and set the debugger value to block application execution
        ForEach ($blockProcess in $blockProcessName) {
            Write-ADTLogEntry -Message "Setting the Image File Execution Option registry key to block execution of [$blockProcess]."
            Set-RegistryKey -Key (Join-Path -Path $adtEnv.regKeyAppExecution -ChildPath $blockProcess) -Name 'Debugger' -Value $debuggerBlockValue -ContinueOnError $true
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
