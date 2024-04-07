Function Execute-ProcessAsUser {
	<#
.SYNOPSIS

Execute a process with a logged in user account, by using a scheduled task, to provide interaction with user in the SYSTEM context.

.DESCRIPTION

Execute a process with a logged in user account, by using a scheduled task, to provide interaction with user in the SYSTEM context.

.PARAMETER UserName

Logged in Username under which to run the process from. Default is: The active console user. If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user.

.PARAMETER Path

Path to the file being executed.

.PARAMETER TempPath

Path to the temporary directory used to store the script to be executed as user. If using a user writable directory, ensure you select -RunLevel 'LeastPrivilege'.

.PARAMETER Parameters

Arguments to be passed to the file being executed.

.PARAMETER SecureParameters

Hides all parameters passed to the executable from the Toolkit log file.

.PARAMETER RunLevel

Specifies the level of user rights that Task Scheduler uses to run the task. The acceptable values for this parameter are:

- HighestAvailable: Tasks run by using the highest available privileges (Admin privileges for Administrators). Default Value.

- LeastPrivilege: Tasks run by using the least-privileged user account (LUA) privileges.

.PARAMETER Wait

Wait for the process, launched by the scheduled task, to complete execution before accepting more input. Default is $false.

.PARAMETER PassThru

Returns the exit code from this function or the process launched by the scheduled task.

.PARAMETER WorkingDirectory

Set working directory for the process.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Int32.

Returns the exit code from this function or the process launched by the scheduled task.

.EXAMPLE

Execute-ProcessAsUser -UserName 'CONTOSO\User' -Path "$PSHOME\powershell.exe" -Parameters "-Command & { & `"C:\Test\Script.ps1`"; Exit `$LastExitCode }" -Wait

Execute process under a user account by specifying a username under which to execute it.

.EXAMPLE

Execute-ProcessAsUser -Path "$PSHOME\powershell.exe" -Parameters "-Command & { & `"C:\Test\Script.ps1`"; Exit `$LastExitCode }" -Wait

Execute process under a user account by using the default active logged in user that was detected when the toolkit was launched.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[String]$UserName = $RunAsActiveUser.NTAccount,
		[Parameter(Mandatory = $true)]
		[ValidateNotNullorEmpty()]
		[String]$Path,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[String]$TempPath,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[String]$Parameters = '',
		[Parameter(Mandatory = $false)]
		[Switch]$SecureParameters = $false,
		[Parameter(Mandatory = $false)]
		[ValidateSet('HighestAvailable', 'LeastPrivilege')]
		[String]$RunLevel = 'HighestAvailable',
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Switch]$Wait = $false,
		[Parameter(Mandatory = $false)]
		[Switch]$PassThru = $false,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[String]$WorkingDirectory,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Boolean]$ContinueOnError = $true
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

		If ((-not [String]::IsNullOrEmpty($TempPath))) {
			$executeAsUserTempPath = $TempPath
			If (($TempPath -eq $loggedOnUserTempPath) -and ($RunLevel -eq 'HighestPrivilege')) {
				Write-Log -Message "WARNING: Using [${CmdletName}] with a user writable directory using the 'HighestPrivilege' creates a security vulnerability. Please use -RunLevel 'LeastPrivilege' when using a user writable directoy." -Severity 'Warning'
			}
		} Else {
			[String]$executeAsUserTempPath = Join-Path -Path $dirAppDeployTemp -ChildPath 'ExecuteAsUser'
		}
	}
	Process {
		Try {
			## Initialize exit code variable
			[Int32]$executeProcessAsUserExitCode = 0

			## Confirm that the username field is not empty
			If (-not $UserName) {
				[Int32]$executeProcessAsUserExitCode = 60009
				Write-Log -Message "The function [${CmdletName}] has a -UserName parameter that has an empty default value because no logged in users were detected when the toolkit was launched." -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw "The function [${CmdletName}] has a -UserName parameter that has an empty default value because no logged in users were detected when the toolkit was launched."
				}
				Return
			}

			## Confirm if the toolkit is running with administrator privileges
			If (($RunLevel -eq 'HighestAvailable') -and (-not $IsAdmin)) {
				[Int32]$executeProcessAsUserExitCode = 60003
				Write-Log -Message "The function [${CmdletName}] requires the toolkit to be running with Administrator privileges if the [-RunLevel] parameter is set to 'HighestAvailable'." -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw "The function [${CmdletName}] requires the toolkit to be running with Administrator privileges if the [-RunLevel] parameter is set to 'HighestAvailable'."
				}
				Return
			}

			## Check whether the specified Working Directory exists
			If ($WorkingDirectory -and (-not (Test-Path -LiteralPath $WorkingDirectory -PathType 'Container'))) {
				Write-Log -Message 'The specified working directory does not exist or is not a directory. The scheduled task might not work as expected.' -Severity 2 -Source ${CmdletName}
			}

			##  Remove the temporary folder
			If (Test-Path -LiteralPath $executeAsUserTempPath -PathType 'Container') {
				Write-Log -Message "Previous [$executeAsUserTempPath] found. Attempting removal." -Source ${CmdletName}
				Remove-Folder -Path $executeAsUserTempPath
			}
			#  Recreate the temporary folder
			Try {
				Write-Log -Message "Creating [$executeAsUserTempPath]." -Source ${CmdletName}
				$null = New-Item -Path $executeAsUserTempPath -ItemType 'Directory' -ErrorAction 'Stop'
			} Catch {
				Write-Log -Message "Unable to create [$executeAsUserTempPath]. Possible attempt to gain elevated rights." -Source ${CmdletName} -Severity 2
			}
			#  Copy RunHidden.vbs to temp path
			Try {
				Write-Log -Message "Copying [$appDeployRunHiddenVbsFile] to destination [$executeAsUserTempPath]." -Source ${CmdletName}
				Copy-Item -LiteralPath $appDeployRunHiddenVbsFile -Destination $executeAsUserTempPath -Force -ErrorAction 'Stop'
			} Catch {
				Write-Log -Message "Unable to copy [$appDeployRunHiddenVbsFile] to destination [$executeAsUserTempPath]." -Source ${CmdletName} -Severity 2
			}
			#  Set user permissions on RunHidden.vbs
			Try {
				Set-ItemPermission -Path "$($executeAsUserTempPath)\RunHidden.vbs" -User $UserName -Permission 'Read' -ErrorAction 'Stop'
			} Catch {
				Write-Log -Message "Failed to set read permissions on path [$($executeAsUserTempPath)\RunHidden.vbs]. The function might not be able to work correctly." -Source ${CmdletName} -Severity 2
			}

			## If powershell.exe or cmd.exe is being launched, then create a VBScript to launch the shell so that we can suppress the console window that flashes otherwise
			If (((Split-Path -Path $Path -Leaf) -ilike 'powershell*') -or ((Split-Path -Path $Path -Leaf) -ilike 'cmd*')) {
				If ($SecureParameters) {
					Write-Log -Message "Preparing parameters for VBScript that will start [$Path (Parameters Hidden)] as the logged-on user [$userName] and suppress the console window..." -Source ${CmdletName}
				} Else {
					Write-Log -Message "Preparing parameters for VBScript that will start [$Path $Parameters] as the logged-on user [$userName] and suppress the console window..." -Source ${CmdletName}
				}

				[String]$NewParameters = '/e:vbscript'
				If ($executeAsUserTempPath -match ' ') {
					$NewParameters = "$($NewParameters) `"$($executeAsUserTempPath)\RunHidden.vbs`""
				} Else {
					$NewParameters = "$($NewParameters) $($executeAsUserTempPath)\RunHidden.vbs"
				}
				If (($Path -notmatch "^[`'].*[`']$") -and ($Path -notmatch "^[`"].*[`"]$") -and $Path -match ' ') {
					$NewParameters = "$($NewParameters) `"$($Path)`""
				} Else {
					$NewParameters = "$NewParameters $Path"
				}

				$Parameters = "$NewParameters $Parameters"
				$Path = "$envWinDir\System32\wscript.exe"
			}
			#  Replace invalid XML characters in parameters with their valid XML equivalent
			[String]$XmlEscapedPath = [System.Security.SecurityElement]::Escape($Path)
			[String]$XmlEscapedParameters = [System.Security.SecurityElement]::Escape($Parameters)
			#  Prepare working directory XML element
			[String]$WorkingDirectoryInsert = ''
			If ($WorkingDirectory) {
				[String]$XmlEscapedWorkingDirectory = [System.Security.SecurityElement]::Escape($WorkingDirectory)
				$WorkingDirectoryInsert = "`r`n   <WorkingDirectory>$XmlEscapedWorkingDirectory</WorkingDirectory>"
			}
			[String]$XmlEscapedUserName = [System.Security.SecurityElement]::Escape($UserName)

			## Specify the scheduled task configuration in XML format
			[String]$xmlSchTask = @"
<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
  <RegistrationInfo />
  <Triggers />
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
    <Exec>
      <Command>$XmlEscapedPath</Command>
      <Arguments>$XmlEscapedParameters</Arguments>$WorkingDirectoryInsert
    </Exec>
  </Actions>
  <Principals>
    <Principal id="Author">
      <UserId>$XmlEscapedUserName</UserId>
      <LogonType>InteractiveToken</LogonType>
      <RunLevel>$RunLevel</RunLevel>
    </Principal>
  </Principals>
</Task>
"@
			## Export the XML to file
			Try {
				## Build the scheduled task XML name
				[String]$schTaskNameCount = '001'
				[String]$schTaskName = "$($("$($appDeployToolkitName)-ExecuteAsUser" -replace ' ', '').Trim('_') -replace '[_]+', '_')"
				#  Specify the filename to export the XML to
				[String]$previousXmlFileName = Get-ChildItem -Path "$($dirAppDeployTemp)\*" -Attributes '!Directory' -Include '*.xml' | Where-Object { $_.Name -match "^$($schTaskName)-\d{3}\.xml$" } | Sort-Object -Descending -Property 'LastWriteTime' | Select-Object -ExpandProperty 'Name' -First 1
				If (-not [String]::IsNullOrEmpty($previousXmlFileName)) {
					[Int32]$xmlFileCount = [IO.Path]::GetFileNameWithoutExtension($previousXmlFileName) | ForEach-Object { $_.Substring($_.length - 3, 3) }
					[String]$schTaskNameCount = '{0:d3}' -f $xmlFileCount++
				}
				$schTaskName = "$($schTaskName)-$($schTaskNameCount)"
				[String]$xmlSchTaskFilePath = "$($dirAppDeployTemp)\$($schTaskName).xml"

				#  Export the XML file
				[String]$xmlSchTask | Out-File -FilePath $xmlSchTaskFilePath -Force -ErrorAction 'Stop'
				Set-ItemPermission -Path $xmlSchTaskFilePath -User $UserName -Permission 'Read'
			} Catch {
				[Int32]$executeProcessAsUserExitCode = 60007
				Write-Log -Message "Failed to export the scheduled task XML file [$xmlSchTaskFilePath]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw "Failed to export the scheduled task XML file [$xmlSchTaskFilePath]: $($_.Exception.Message)"
				}
				Return
			}

			## Create Scheduled Task to run the process with a logged-on user account
			If ($Parameters) {
				If ($SecureParameters) {
					Write-Log -Message "Creating scheduled task to execute [$Path (Parameters Hidden)] as the logged-on user [$userName]..." -Source ${CmdletName}
				} Else {
					Write-Log -Message "Creating scheduled task to execute [$Path $Parameters] as the logged-on user [$userName]..." -Source ${CmdletName}
				}
			} Else {
				Write-Log -Message "Creating scheduled task to execute [$Path] as the logged-on user [$userName]..." -Source ${CmdletName}
			}
			[PSObject]$schTaskResult = Execute-Process -Path $exeSchTasks -Parameters "/create /f /tn $schTaskName /xml `"$xmlSchTaskFilePath`"" -WindowStyle 'Hidden' -CreateNoWindow -PassThru -ExitOnProcessFailure $false
			If ($schTaskResult.ExitCode -ne 0) {
				[Int32]$executeProcessAsUserExitCode = $schTaskResult.ExitCode
				Write-Log -Message "Failed to create the scheduled task by importing the scheduled task XML file [$xmlSchTaskFilePath]." -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw "Failed to create the scheduled task by importing the scheduled task XML file [$xmlSchTaskFilePath]."
				}
				Return
			}

			## Trigger the Scheduled Task
			If ($Parameters) {
				If ($SecureParameters) {
					Write-Log -Message "Triggering execution of scheduled task with command [$Path] (Parameters Hidden) as the logged-on user [$userName]..." -Source ${CmdletName}
				} Else {
					Write-Log -Message "Triggering execution of scheduled task with command [$Path $Parameters] as the logged-on user [$userName]..." -Source ${CmdletName}
				}
			} Else {
				Write-Log -Message "Triggering execution of scheduled task with command [$Path] as the logged-on user [$userName]..." -Source ${CmdletName}
			}
			[PSObject]$schTaskResult = Execute-Process -Path $exeSchTasks -Parameters "/run /i /tn $schTaskName" -WindowStyle 'Hidden' -CreateNoWindow -Passthru -ExitOnProcessFailure $false
			If ($schTaskResult.ExitCode -ne 0) {
				[Int32]$executeProcessAsUserExitCode = $schTaskResult.ExitCode
				Write-Log -Message "Failed to trigger scheduled task [$schTaskName]." -Severity 3 -Source ${CmdletName}
				#  Delete Scheduled Task
				Write-Log -Message 'Deleting the scheduled task which did not trigger.' -Source ${CmdletName}
				Execute-Process -Path $exeSchTasks -Parameters "/delete /tn $schTaskName /f" -WindowStyle 'Hidden' -CreateNoWindow -ExitOnProcessFailure $false
				If (-not $ContinueOnError) {
					Throw "Failed to trigger scheduled task [$schTaskName]."
				}
				Return
			}

			## Wait for the process launched by the scheduled task to complete execution
			If ($Wait) {
				Write-Log -Message "Waiting for the process launched by the scheduled task [$schTaskName] to complete execution (this may take some time)..." -Source ${CmdletName}
				Start-Sleep -Seconds 1
				#If on Windows Vista or higer, Windows Task Scheduler 2.0 is supported. 'Schedule.Service' ComObject output is UI language independent
				If (([Version]$envOSVersion).Major -gt 5) {
					Try {
						[__ComObject]$ScheduleService = New-Object -ComObject 'Schedule.Service' -ErrorAction 'Stop'
						$ScheduleService.Connect()
						$RootFolder = $ScheduleService.GetFolder('\')
						$Task = $RootFolder.GetTask("$schTaskName")
						# Task State(Status) 4 = 'Running'
						While ($Task.State -eq 4) {
							Start-Sleep -Seconds 5
						}
						#  Get the exit code from the process launched by the scheduled task
						[Int32]$executeProcessAsUserExitCode = $Task.LastTaskResult
					} Catch {
						Write-Log -Message "Failed to retrieve information from Task Scheduler. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
					} Finally {
						Try {
							$null = [Runtime.Interopservices.Marshal]::ReleaseComObject($ScheduleService)
						} Catch {
						}
					}
				}
				#Windows Task Scheduler 1.0
				Else {
					While ((($exeSchTasksResult = & $exeSchTasks /query /TN $schTaskName /V /FO CSV) | ConvertFrom-Csv | Select-Object -ExpandProperty 'Status' -First 1) -eq 'Running') {
						Start-Sleep -Seconds 5
					}
					#  Get the exit code from the process launched by the scheduled task
					[Int32]$executeProcessAsUserExitCode = ($exeSchTasksResult = & $exeSchTasks /query /TN $schTaskName /V /FO CSV) | ConvertFrom-Csv | Select-Object -ExpandProperty 'Last Result' -First 1
				}
				Write-Log -Message "Exit code from process launched by scheduled task [$executeProcessAsUserExitCode]." -Source ${CmdletName}
			} Else {
				Start-Sleep -Seconds 1
			}
		} Finally {
			## Delete scheduled task
			Try {
				Write-Log -Message "Deleting scheduled task [$schTaskName]." -Source ${CmdletName}
				Execute-Process -Path $exeSchTasks -Parameters "/delete /tn $schTaskName /f" -WindowStyle 'Hidden' -CreateNoWindow -ErrorAction 'Stop'
			} Catch {
				Write-Log -Message "Failed to delete scheduled task [$schTaskName]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			}

			## Remove the XML scheduled task file
			If (Test-Path -LiteralPath $xmlSchTaskFilePath -PathType 'Leaf') {
				Remove-File -Path $xmlSchTaskFilePath
			}

			##  Remove the temporary folder
			If (Test-Path -LiteralPath $executeAsUserTempPath -PathType 'Container') {
				Remove-Folder -Path $executeAsUserTempPath
			}
		}
	}
	End {
		If ($PassThru) {
			Write-Output -InputObject ($executeProcessAsUserExitCode)
		}

		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
