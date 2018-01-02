<#
.SYNOPSIS
	This script is a template that allows you to extend the toolkit with your own custom functions.
    # LICENSE #
    PowerShell App Deployment Toolkit - Provides a set of functions to perform common application deployment tasks on Windows. 
    Copyright (C) 2017 - Sean Lillis, Dan Cunningham, Muhammad Mashwani, Aman Motazedian.
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or any later version. This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. 
    You should have received a copy of the GNU Lesser General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.
.DESCRIPTION
	The script is automatically dot-sourced by the AppDeployToolkitMain.ps1 script.
.NOTES
    Toolkit Exit Code Ranges:
    60000 - 68999: Reserved for built-in exit codes in Deploy-Application.ps1, Deploy-Application.exe, and AppDeployToolkitMain.ps1
    69000 - 69999: Recommended for user customized exit codes in Deploy-Application.ps1
    70000 - 79999: Recommended for user customized exit codes in AppDeployToolkitExtensions.ps1
.LINK 
	http://psappdeploytoolkit.com
#>
[CmdletBinding()]
Param (
)

##*===============================================
##* VARIABLE DECLARATION
##*===============================================

# Variables: Script
[string]$appDeployToolkitExtName = 'PSAppDeployToolkitExt'
[string]$appDeployExtScriptFriendlyName = 'App Deploy Toolkit Extensions'
[version]$appDeployExtScriptVersion = [version]'1.5.0'
[string]$appDeployExtScriptDate = '02/12/2017'
[hashtable]$appDeployExtScriptParameters = $PSBoundParameters

##*===============================================
##* FUNCTION LISTINGS
##*===============================================


#region Function Trigger-AppEvalCycle
Function Trigger-AppEvalCycle {
    <#
    .SYNOPSIS
    Schedule a SCCM 2012 Application Evaluation Cycle task to be triggered in the specified time.
    .DESCRIPTION
    This function is called when the user selects to defer the installation. It does the following:
    1. Removes the scheduled task configuration XML, if it already exists on the machine.
    2. Creates a temporary directory on the local machine, if the folder doesn’t exists.
    3. Creates an scheduled task configuration XML file on the temporary directory.
    4. Checks if a scheduled task with that name already exists on the machine, if it exists then delete it.
    5. Create a new scheduled task based on the XML file created on step 3.
    6. Removes the scheduled task configuration XML.
    7. Once the specified time is reached a scheduled task runs a SCCM 2012 Application Evaluation Cycle will start and it will trigger the installation/uninstallation to start if the machine is still part of the install/uninstall collection.
    .PARAMETER Time
    Specify the time, in hours, to run the scheduled task.
    .EXAMPLE
    Trigger-AppEvalCycle -Time 24
    .NOTES
    This is an internal script function and should typically not be called directly.
    It is used to ensure that when the users defers the installation a new installation attempt will be made in the specified time if the machine is still part of the install/uninstall collection.
    Version 1.0 – Jeppe Olsen.
    #>
    [CmdletBinding()]
    Param (
    [Parameter(Mandatory=$true)]
    [ValidateNotNullorEmpty()]
    [int]$Time = 2
    )
    
    Begin {
    ## Get the name of this function and write header
    [string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
    Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    
    ## Specify the scheduled task configuration in XML format
    [string]$taskRunDateTime = (((Get-Date).AddMinutes($Time)).ToUniversalTime()).ToString(“yyyy-MM-ddTHH:mm:ss.fffffffZ”)
    
    #specify the task scheduler executable
    [string] $execSchTasks = "$envWinDir\System32\schtasks.exe"
    
    #Specify the task name
    [string]$taskName = $installName + ‘_AppEvalCycle’
    
    }
    Process {
    ## Bypass if in NonInteractive mode
    If ($deployModeNonInteractive) {
    Write-Log -Message “Bypassing Function [${CmdletName}] [Mode: $deployMode].” -Source ${CmdletName}
    Return
    }
    [string]$xmlTask =  @"
    <?xml version="1.0" encoding="UTF-16"?>
    <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
      <RegistrationInfo/>
      <Triggers>
        <TimeTrigger id="1">
          <StartBoundary>$taskRunDateTime</StartBoundary>
          <Enabled>true</Enabled>
        </TimeTrigger>
      </Triggers>
      <Principals>
        <Principal id="Author">
          <UserId>S-1-5-18</UserId>
          <RunLevel>HighestAvailable</RunLevel>
        </Principal>
      </Principals>
      <Settings>
        <MultipleInstancesPolicy>StopExisting</MultipleInstancesPolicy>
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
        <ExecutionTimeLimit>PT72H</ExecutionTimeLimit>
        <Priority>7</Priority>
      </Settings>
      <Actions Context="Author">
        <Exec id="StartPowerShellJob">
          <Command>cmd</Command>
          <Arguments>/c WMIC /namespace:\\root\ccm path sms_client CALL TriggerSchedule '{00000000-0000-0000-0000-000000000121}' /NOINTERACTIVE</Arguments>
        </Exec>
        <Exec>
          <Command>schtasks</Command>
          <Arguments>/delete /tn $taskName /f</Arguments>
        </Exec>
      </Actions>
    </Task>
"@
    
    #Export the xml to file
    try{
        [string] $schXmlFile = "$dirAppDeployTemp\$taskName"
        if(-not (Test-Path $dirAppDeployTemp)){New-Item $dirAppDeployTemp -ItemType Directory -Force}
        [string] $xmlTask | Out-File -FilePath $schXmlFile -Force -ErrorAction Stop
    }
    catch{
        Write-Log -Message "Failed to export the scheduled task XML file [$schXmlFile].   `n$(Resolve-Error)" -Severity 3 -Source ${CmdLetName}
        Return
    }
    # Create scheduled task
    Write-Log -Message "Creating scheduled task to run Application Deployment Evaluation Cycle at $taskRunDateTime"
    [psobject] $taskResult = Execute-Process -Path $execSchTasks -Parameters "/create /f /tn $taskName /xml `"$schXmlFile`"" -WindowStyle Hidden -CreateNoWindow -PassThru
    If($taskResult.ExitCode -ne 0){
        Write-log -Message "Failed to create the scheduled task, with exit code : $($taskResult.ExitCode)" -Severity 3 -Source ${CmdletName}
        Return
    }
    }
    End {
    Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
    }
#endregion
##*===============================================
##* END FUNCTION LISTINGS
##*===============================================

##*===============================================
##* SCRIPT BODY
##*===============================================

If ($scriptParentPath) {
	Write-Log -Message "Script [$($MyInvocation.MyCommand.Definition)] dot-source invoked by [$(((Get-Variable -Name MyInvocation).Value).ScriptName)]" -Source $appDeployToolkitExtName
}
Else {
	Write-Log -Message "Script [$($MyInvocation.MyCommand.Definition)] invoked directly" -Source $appDeployToolkitExtName
}

##*===============================================
##* END SCRIPT BODY
##*===============================================