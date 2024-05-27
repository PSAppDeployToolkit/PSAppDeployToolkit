#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Execute-Process {
    <#
.SYNOPSIS

Execute a process with optional arguments, working directory, window style.

.DESCRIPTION

Executes a process, e.g. a file included in the Files directory of the App Deploy Toolkit, or a file on the local machine.
Provides various options for handling the return codes (see Parameters).

.PARAMETER Path

Path to the file to be executed. If the file is located directly in the "Files" directory of the App Deploy Toolkit, only the file name needs to be specified.
Otherwise, the full path of the file must be specified. If the files is in a subdirectory of "Files", use the "$dirFiles" variable as shown in the example.

.PARAMETER Parameters

Arguments to be passed to the executable

.PARAMETER SecureParameters

Hides all parameters passed to the executable from the Toolkit log file

.PARAMETER WindowStyle

Style of the window of the process executed. Options: Normal, Hidden, Maximized, Minimized. Default: Normal.
Note: Not all processes honor WindowStyle. WindowStyle is a recommendation passed to the process. They can choose to ignore it.
Only works for native Windows GUI applications. If the WindowStyle is set to Hidden, UseShellExecute should be set to $true.

.PARAMETER CreateNoWindow

Specifies whether the process should be started with a new window to contain it. Only works for Console mode applications. UseShellExecute should be set to $false.
Default is false.

.PARAMETER WorkingDirectory

The working directory used for executing the process. Defaults to the directory of the file being executed.
Parameter UseShellExecute affects this parameter.

.PARAMETER NoWait

Immediately continue after executing the process.

.PARAMETER PassThru

If NoWait is not specified, returns an object with ExitCode, STDOut and STDErr output from the process. If NoWait is specified, returns an object with Id, Handle and ProcessName.

.PARAMETER WaitForMsiExec

Sometimes an EXE bootstrapper will launch an MSI install. In such cases, this variable will ensure that
this function waits for the msiexec engine to become available before starting the install.

.PARAMETER MsiExecWaitTime

Specify the length of time in seconds to wait for the msiexec engine to become available. Default: 600 seconds (10 minutes).

.PARAMETER IgnoreExitCodes

List the exit codes to ignore or * to ignore all exit codes.

.PARAMETER PriorityClass

Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime. Default: Normal

.PARAMETER ExitOnProcessFailure

Specifies whether the function should call Close-ADTSession when the process returns an exit code that is considered an error/failure. Default: $true

.PARAMETER UseShellExecute

Specifies whether to use the operating system shell to start the process. $true if the shell should be used when starting the process; $false if the process should be created directly from the executable file.

The word "Shell" in this context refers to a graphical shell (similar to the Windows shell) rather than command shells (for example, bash or sh) and lets users launch graphical applications or open documents.
It lets you open a file or a url and the Shell will figure out the program to open it with.
The WorkingDirectory property behaves differently depending on the value of the UseShellExecute property. When UseShellExecute is true, the WorkingDirectory property specifies the location of the executable.
When UseShellExecute is false, the WorkingDirectory property is not used to find the executable. Instead, it is used only by the process that is started and has meaning only within the context of the new process.
If you set UseShellExecute to $true, there will be no available output from the process.

Default: $false

.PARAMETER ContinueOnError

Continue if an error occured while trying to start the process. Default: $false.

.EXAMPLE

Execute-Process -Path 'uninstall_flash_player_64bit.exe' -Parameters '/uninstall' -WindowStyle 'Hidden'

If the file is in the "Files" directory of the App Deploy Toolkit, only the file name needs to be specified.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Execute-Process -Path "$dirFiles\Bin\setup.exe" -Parameters '/S' -WindowStyle 'Hidden'

.EXAMPLE

Execute-Process -Path 'setup.exe' -Parameters '/S' -IgnoreExitCodes '1,2'

.EXAMPLE

Execute-Process -Path 'setup.exe' -Parameters "-s -f2`"$($Script:ADT.Config.Toolkit.LogPath)\$installName.log`""

Launch InstallShield "setup.exe" from the ".\Files" sub-directory and force log files to the logging folder.

.EXAMPLE

Execute-Process -Path 'setup.exe' -Parameters "/s /v`"ALLUSERS=1 /qn /L* \`"$($Script:ADT.Config.Toolkit.LogPath)\$installName.log`"`""

Launch InstallShield "setup.exe" with embedded MSI and force log files to the logging folder.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [Alias('FilePath')]
        [ValidateNotNullorEmpty()]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [Alias('Arguments')]
        [ValidateNotNullorEmpty()]
        [String[]]$Parameters,
        [Parameter(Mandatory = $false)]
        [Switch]$SecureParameters = $false,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Normal', 'Hidden', 'Maximized', 'Minimized')]
        [Diagnostics.ProcessWindowStyle]$WindowStyle = 'Normal',
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$CreateNoWindow = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$WorkingDirectory,
        [Parameter(Mandatory = $false)]
        [Switch]$NoWait = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$PassThru = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$WaitForMsiExec = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$MsiExecWaitTime = $Script:ADT.Config.MSI.MutexWaitTime,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$IgnoreExitCodes,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Idle', 'Normal', 'High', 'AboveNormal', 'BelowNormal', 'RealTime')]
        [Diagnostics.ProcessPriorityClass]$PriorityClass = 'Normal',
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ExitOnProcessFailure = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$UseShellExecute = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            $private:returnCode = $null
            $stdOut = $stdErr = $null

            ## Validate and find the fully qualified path for the $Path variable.
            If (([IO.Path]::IsPathRooted($Path)) -and ([IO.Path]::HasExtension($Path))) {
                Write-ADTLogEntry -Message "[$Path] is a valid fully qualified path, continue."
                If (-not (Test-Path -LiteralPath $Path -PathType 'Leaf' -ErrorAction 'Stop')) {
                    Write-ADTLogEntry -Message "File [$Path] not found." -Severity 3
                    If (-not $ContinueOnError) {
                        Throw "File [$Path] not found."
                    }
                    Return
                }
            }
            Else {
                #  The first directory to search will be the 'Files' subdirectory of the script directory
                [String]$PathFolders = $Script:ADT.CurrentSession.GetPropertyValue('dirFiles')
                #  Add the current location of the console (Windows always searches this location first)
                [String]$PathFolders = $PathFolders + ';' + (Get-Location -PSProvider 'FileSystem').Path
                #  Add the new path locations to the PATH environment variable
                $env:PATH = $PathFolders + ';' + $env:PATH

                #  Get the fully qualified path for the file. Get-Command searches PATH environment variable to find this value.
                [String]$FullyQualifiedPath = Get-Command -Name $Path -CommandType 'Application' -TotalCount 1 -Syntax -ErrorAction 'Stop'

                #  Revert the PATH environment variable to it's original value
                $env:PATH = $env:PATH -replace [RegEx]::Escape($PathFolders + ';'), ''

                If ($FullyQualifiedPath) {
                    Write-ADTLogEntry -Message "[$Path] successfully resolved to fully qualified path [$FullyQualifiedPath]."
                    $Path = $FullyQualifiedPath
                }
                Else {
                    Write-ADTLogEntry -Message "[$Path] contains an invalid path or file name." -Severity 3
                    If (-not $ContinueOnError) {
                        Throw "[$Path] contains an invalid path or file name."
                    }
                    Return
                }
            }

            ## Set the Working directory (if not specified)
            If (-not $WorkingDirectory) {
                $WorkingDirectory = Split-Path -Path $Path -Parent -ErrorAction 'Stop'
            }

            ## If the WindowStyle parameter is set to 'Hidden', set the UseShellExecute parameter to '$true'.
            If ($WindowStyle -eq 'Hidden') {
                $UseShellExecute = $true
            }

            ## If MSI install, check to see if the MSI installer service is available or if another MSI install is already underway.
            ## Please note that a race condition is possible after this check where another process waiting for the MSI installer
            ##  to become available grabs the MSI Installer mutex before we do. Not too concerned about this possible race condition.
            If (($Path -match 'msiexec') -or ($WaitForMsiExec)) {
                [Timespan]$MsiExecWaitTimeSpan = New-TimeSpan -Seconds $MsiExecWaitTime
                [Boolean]$MsiExecAvailable = Test-IsMutexAvailable -MutexName 'Global\_MSIExecute' -MutexWaitTimeInMilliseconds $MsiExecWaitTimeSpan.TotalMilliseconds
                Start-Sleep -Seconds 1
                If (-not $MsiExecAvailable) {
                    #  Default MSI exit code for install already in progress
                    [Int32]$returnCode = 1618
                    Write-ADTLogEntry -Message 'Another MSI installation is already in progress and needs to be completed before proceeding with this installation.' -Severity 3
                    If (-not $ContinueOnError) {
                        Throw 'Another MSI installation is already in progress and needs to be completed before proceeding with this installation.'
                    }
                    Return
                }
            }

            Try {
                ## Disable Zone checking to prevent warnings when running executables
                $env:SEE_MASK_NOZONECHECKS = 1

                ## Using this variable allows capture of exceptions from .NET methods. Private scope only changes value for current function.
                $private:previousErrorActionPreference = $ErrorActionPreference
                $ErrorActionPreference = 'Stop'

                ## Define process
                $processStartInfo = New-Object -TypeName 'System.Diagnostics.ProcessStartInfo' -ErrorAction 'Stop'
                $processStartInfo.FileName = $Path
                $processStartInfo.WorkingDirectory = $WorkingDirectory
                $processStartInfo.UseShellExecute = $UseShellExecute
                $processStartInfo.ErrorDialog = $false
                $processStartInfo.RedirectStandardOutput = $true
                $processStartInfo.RedirectStandardError = $true
                $processStartInfo.CreateNoWindow = $CreateNoWindow
                If ($Parameters) {
                    $processStartInfo.Arguments = $Parameters
                }
                $processStartInfo.WindowStyle = $WindowStyle
                If ($processStartInfo.UseShellExecute -eq $true) {
                    Write-ADTLogEntry -Message 'UseShellExecute is set to true, standard output and error will not be available.'
                    $processStartInfo.RedirectStandardOutput = $false
                    $processStartInfo.RedirectStandardError = $false
                }
                $process = New-Object -TypeName 'System.Diagnostics.Process' -ErrorAction 'Stop'
                $process.StartInfo = $processStartInfo

                If ($processStartInfo.UseShellExecute -eq $false) {
                    ## Add event handler to capture process's standard output redirection
                    [ScriptBlock]$processEventHandler = { If (-not [String]::IsNullOrEmpty($EventArgs.Data)) {
                            $Event.MessageData.AppendLine($EventArgs.Data)
                        } }
                    $stdOutBuilder = New-Object -TypeName 'System.Text.StringBuilder' -ArgumentList ('')
                    $stdOutEvent = Register-ObjectEvent -InputObject $process -Action $processEventHandler -EventName 'OutputDataReceived' -MessageData $stdOutBuilder -ErrorAction 'Stop'
                    $stdErrBuilder = New-Object -TypeName 'System.Text.StringBuilder' -ArgumentList ('')
                    $stdErrEvent = Register-ObjectEvent -InputObject $process -Action $processEventHandler -EventName 'ErrorDataReceived' -MessageData $stdErrBuilder -ErrorAction 'Stop'
                }

                ## Start Process
                Write-ADTLogEntry -Message "Working Directory is [$WorkingDirectory]."
                If ($Parameters) {
                    If ($Parameters -match '-Command \&') {
                        Write-ADTLogEntry -Message "Executing [$Path [PowerShell ScriptBlock]]..."
                    }
                    Else {
                        If ($SecureParameters) {
                            Write-ADTLogEntry -Message "Executing [$Path (Parameters Hidden)]..."
                        }
                        Else {
                            Write-ADTLogEntry -Message "Executing [$Path $Parameters]..."
                        }
                    }
                }
                Else {
                    Write-ADTLogEntry -Message "Executing [$Path]..."
                }

                $null = $process.Start()
                ## Set priority
                If ($PriorityClass -ne 'Normal') {
                    Try {
                        If ($process.HasExited -eq $false) {
                            Write-ADTLogEntry -Message "Changing the priority class for the process to [$PriorityClass]"
                            $process.PriorityClass = $PriorityClass
                        }
                        Else {
                            Write-ADTLogEntry -Message "Cannot change the priority class for the process to [$PriorityClass], because the process has exited already." -Severity 2
                        }

                    }
                    Catch {
                        Write-ADTLogEntry -Message 'Failed to change the priority class for the process.' -Severity 2
                    }
                }
                ## NoWait specified, return process details. If it isn't specified, start reading standard Output and Error streams
                If ($NoWait) {
                    Write-ADTLogEntry -Message 'NoWait parameter specified. Continuing without waiting for exit code...'

                    If ($PassThru) {
                        If ($process.HasExited -eq $false) {
                            Write-ADTLogEntry -Message 'PassThru parameter specified, returning process details object.'
                            [PSObject]$ProcessDetails = New-Object -TypeName 'PSObject' -Property @{ Id = If ($process.Id) {
                                    $process.Id
                                }
                                Else {
                                    $null
                                } ; Handle                                                              = If ($process.Handle) {
                                    $process.Handle
                                }
                                Else {
                                    [IntPtr]::Zero
                                }; ProcessName                                                          = If ($process.ProcessName) {
                                    $process.ProcessName
                                }
                                Else {
                                    ''
                                }
                            }
                            Write-Output -InputObject ($ProcessDetails)
                        }
                        Else {
                            Write-ADTLogEntry -Message 'PassThru parameter specified, however the process has already exited.'
                        }
                    }
                }
                Else {
                    If ($processStartInfo.UseShellExecute -eq $false) {
                        $process.BeginOutputReadLine()
                        $process.BeginErrorReadLine()
                    }
                    ## Instructs the Process component to wait indefinitely for the associated process to exit.
                    $process.WaitForExit()

                    ## HasExited indicates that the associated process has terminated, either normally or abnormally. Wait until HasExited returns $true.
                    While (-not $process.HasExited) {
                        $process.Refresh(); Start-Sleep -Seconds 1
                    }

                    ## Get the exit code for the process
                    Try {
                        [Int32]$returnCode = $process.ExitCode
                    }
                    Catch [System.Management.Automation.PSInvalidCastException] {
                        #  Catch exit codes that are out of int32 range
                        [Int32]$returnCode = 60013
                    }

                    If ($processStartInfo.UseShellExecute -eq $false) {
                        ## Unregister standard output and error event to retrieve process output
                        If ($stdOutEvent) {
                            Unregister-Event -SourceIdentifier $stdOutEvent.Name -ErrorAction 'Stop'; $stdOutEvent = $null
                        }
                        If ($stdErrEvent) {
                            Unregister-Event -SourceIdentifier $stdErrEvent.Name -ErrorAction 'Stop'; $stdErrEvent = $null
                        }
                        $stdOut = $stdOutBuilder.ToString() -replace $null, ''
                        $stdErr = $stdErrBuilder.ToString() -replace $null, ''

                        If ($stdErr.Length -gt 0) {
                            Write-ADTLogEntry -Message "Standard error output from the process: $stdErr" -Severity 3
                        }
                    }
                }
            }
            Finally {
                If ($processStartInfo.UseShellExecute -eq $false) {
                    ## Make sure the standard output and error event is unregistered
                    If ($stdOutEvent) {
                        Unregister-Event -SourceIdentifier $stdOutEvent.Name -ErrorAction 'Ignore'; $stdOutEvent = $null
                    }
                    If ($stdErrEvent) {
                        Unregister-Event -SourceIdentifier $stdErrEvent.Name -ErrorAction 'Ignore'; $stdErrEvent = $null
                    }
                }
                ## Free resources associated with the process, this does not cause process to exit
                If ($process) {
                    $process.Dispose()
                }

                ## Re-enable Zone checking
                Remove-Item -LiteralPath 'env:SEE_MASK_NOZONECHECKS' -ErrorAction 'Ignore'

                If ($private:previousErrorActionPreference) {
                    $ErrorActionPreference = $private:previousErrorActionPreference
                }
            }

            If (-not $NoWait) {
                ## Check to see whether we should ignore exit codes
                $ignoreExitCodeMatch = $false
                If ($ignoreExitCodes) {
                    ## Check whether * was specified, which would tell us to ignore all exit codes
                    If ($ignoreExitCodes.Trim() -eq '*') {
                        $ignoreExitCodeMatch = $true
                    }
                    Else {
                        ## Split the processes on a comma
                        [Int32[]]$ignoreExitCodesArray = $ignoreExitCodes -split ','
                        ForEach ($ignoreCode in $ignoreExitCodesArray) {
                            If ($returnCode -eq $ignoreCode) {
                                $ignoreExitCodeMatch = $true
                            }
                        }
                    }
                }

                ## If the passthru switch is specified, return the exit code and any output from process
                If ($PassThru) {
                    Write-ADTLogEntry -Message 'PassThru parameter specified, returning execution results object.'
                    [PSObject]$ExecutionResults = New-Object -TypeName 'PSObject' -Property @{ ExitCode = $returnCode; StdOut = If ($stdOut) {
                            $stdOut
                        }
                        Else {
                            ''
                        }; StdErr = If ($stdErr) {
                            $stdErr
                        }
                        Else {
                            ''
                        }
                    }
                    Write-Output -InputObject ($ExecutionResults)
                }

                If ($ignoreExitCodeMatch) {
                    Write-ADTLogEntry -Message "Execution completed and the exit code [$returncode] is being ignored."
                }
                ElseIf (($returnCode -eq 3010) -or ($returnCode -eq 1641)) {
                    Write-ADTLogEntry -Message "Execution completed successfully with exit code [$returnCode]. A reboot is required." -Severity 2
                    $Script:ADT.CurrentSession.State.MsiRebootDetected = $true
                }
                ElseIf (($returnCode -eq 1605) -and ($Path -match 'msiexec')) {
                    Write-ADTLogEntry -Message "Execution failed with exit code [$returnCode] because the product is not currently installed." -Severity 3
                }
                ElseIf (($returnCode -eq -2145124329) -and ($Path -match 'wusa')) {
                    Write-ADTLogEntry -Message "Execution failed with exit code [$returnCode] because the Windows Update is not applicable to this system." -Severity 3
                }
                ElseIf (($returnCode -eq 17025) -and ($Path -match 'fullfile')) {
                    Write-ADTLogEntry -Message "Execution failed with exit code [$returnCode] because the Office Update is not applicable to this system." -Severity 3
                }
                ElseIf ($returnCode -eq 0) {
                    Write-ADTLogEntry -Message "Execution completed successfully with exit code [$returnCode]." -Severity 0
                }
                Else {
                    [String]$MsiExitCodeMessage = ''
                    If ($Path -match 'msiexec') {
                        [String]$MsiExitCodeMessage = Get-MsiExitCodeMessage -MsiExitCode $returnCode
                    }

                    If ($MsiExitCodeMessage) {
                        Write-ADTLogEntry -Message "Execution failed with exit code [$returnCode]: $MsiExitCodeMessage" -Severity 3
                    }
                    Else {
                        Write-ADTLogEntry -Message "Execution failed with exit code [$returnCode]." -Severity 3
                    }

                    If ($ExitOnProcessFailure) {
                        Close-ADTSession -ExitCode $returnCode
                    }
                }
            }
        }
        Catch {
            If ([String]::IsNullOrEmpty([String]$returnCode)) {
                [Int32]$returnCode = 60002
                Write-ADTLogEntry -Message "Function failed, setting exit code to [$returnCode]. `r`n$(Resolve-Error)" -Severity 3
                If (-not $ContinueOnError) {
                    Throw "Function failed, setting exit code to [$returnCode]. $($_.Exception.Message)"
                }
            }
            Else {
                Write-ADTLogEntry -Message "Execution completed with exit code [$returnCode]. Function failed. `r`n$(Resolve-Error)" -Severity 3
            }

            If ($PassThru) {
                [PSObject]$ExecutionResults = New-Object -TypeName 'PSObject' -Property @{ ExitCode = $returnCode; StdOut = If ($stdOut) {
                        $stdOut
                    }
                    Else {
                        ''
                    }; StdErr = If ($stdErr) {
                        $stdErr
                    }
                    Else {
                        ''
                    }
                }
                Write-Output -InputObject ($ExecutionResults)
            }

            If ($ExitOnProcessFailure) {
                Close-ADTSession -ExitCode $returnCode
            }
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

Execute-ProcessAsUser -UserName 'CONTOSO\User' -Path $envPSProcessPath -Parameters "-Command `"& { & 'C:\Test\Script.ps1'; Exit `$LastExitCode }`"" -Wait

Execute process under a user account by specifying a username under which to execute it.

.EXAMPLE

Execute-ProcessAsUser -Path $envPSProcessPath -Parameters "-Command `"& { & 'C:\Test\Script.ps1'; Exit `$LastExitCode }`"" -Wait

Execute process under a user account by using the default active logged in user that was detected when the toolkit was launched.

.EXAMPLE
Execute-ProcessAsUser -Path $envPSProcessPath -Parameters "-Command `"& { & 'C:\Test\Script.ps1'; Exit `$LastExitCode }`"" -RunLevel 'LeastPrivilege'

Execute process using 'LeastPrivilege' under a user account by using the default active logged in user that was detected when the toolkit was launched.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$UserName = $Script:ADT.Environment.RunAsActiveUser.NTAccount,
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

        If (-not [String]::IsNullOrEmpty($TempPath)) {
            $executeAsUserTempPath = $TempPath
            If (($TempPath -eq $Script:ADT.CurrentSession.LoggedOnUserTempPath) -and ($RunLevel -eq 'HighestPrivilege')) {
                Write-ADTLogEntry -Message "WARNING: Using [${CmdletName}] with a user writable directory using the 'HighestPrivilege' creates a security vulnerability. Please use -RunLevel 'LeastPrivilege' when using a user writable directoy." -Severity 'Warning'
            }
        }
        Else {
            [String]$executeAsUserTempPath = Join-Path -Path $Script:ADT.CurrentSession.GetPropertyValue('dirAppDeployTemp') -ChildPath 'ExecuteAsUser'
        }
    }
    Process {
        Try {
            ## Initialize exit code variable
            [Int32]$executeProcessAsUserExitCode = 0

            ## Confirm that the username field is not empty
            If (-not $UserName) {
                [Int32]$executeProcessAsUserExitCode = 60009
                Write-ADTLogEntry -Message "The function [${CmdletName}] has a -UserName parameter that has an empty default value because no logged in users were detected when the toolkit was launched." -Severity 3
                If (-not $ContinueOnError) {
                    Throw "The function [${CmdletName}] has a -UserName parameter that has an empty default value because no logged in users were detected when the toolkit was launched."
                }
                Return
            }

            ## Confirm if the toolkit is running with administrator privileges
            If (($RunLevel -eq 'HighestAvailable') -and (-not $Script:ADT.Environment.IsAdmin)) {
                [Int32]$executeProcessAsUserExitCode = 60003
                Write-ADTLogEntry -Message "The function [${CmdletName}] requires the toolkit to be running with Administrator privileges if the [-RunLevel] parameter is set to 'HighestAvailable'." -Severity 3
                If (-not $ContinueOnError) {
                    Throw "The function [${CmdletName}] requires the toolkit to be running with Administrator privileges if the [-RunLevel] parameter is set to 'HighestAvailable'."
                }
                Return
            }

            ## Check whether the specified Working Directory exists
            If ($WorkingDirectory -and (-not (Test-Path -LiteralPath $WorkingDirectory -PathType 'Container'))) {
                Write-ADTLogEntry -Message 'The specified working directory does not exist or is not a directory. The scheduled task might not work as expected.' -Severity 2
            }

            ##  Remove the temporary folder
            If (Test-Path -LiteralPath $executeAsUserTempPath -PathType 'Container') {
                Write-ADTLogEntry -Message "Previous [$executeAsUserTempPath] found. Attempting removal."
                Remove-Folder -Path $executeAsUserTempPath
            }
            #  Recreate the temporary folder
            Try {
                Write-ADTLogEntry -Message "Creating [$executeAsUserTempPath]."
                $null = New-Item -Path $executeAsUserTempPath -ItemType 'Directory' -ErrorAction 'Stop'
            }
            Catch {
                Write-ADTLogEntry -Message "Unable to create [$executeAsUserTempPath]. Possible attempt to gain elevated rights." -Severity 2
            }
            #  Copy RunHidden.vbs to temp path
            Try {
                Write-ADTLogEntry -Message "Copying [$appDeployRunHiddenVbsFile] to destination [$executeAsUserTempPath]."
                Copy-Item -LiteralPath $appDeployRunHiddenVbsFile -Destination $executeAsUserTempPath -Force -ErrorAction 'Stop'
            }
            Catch {
                Write-ADTLogEntry -Message "Unable to copy [$appDeployRunHiddenVbsFile] to destination [$executeAsUserTempPath]." -Severity 2
            }
            #  Set user permissions on RunHidden.vbs
            Try {
                Set-ItemPermission -Path "$($executeAsUserTempPath)\RunHidden.vbs" -User $UserName -Permission 'Read' -ErrorAction 'Stop'
            }
            Catch {
                Write-ADTLogEntry -Message "Failed to set read permissions on path [$($executeAsUserTempPath)\RunHidden.vbs]. The function might not be able to work correctly." -Severity 2
            }

            ## If powershell.exe or cmd.exe is being launched, then create a VBScript to launch the shell so that we can suppress the console window that flashes otherwise
            If (((Split-Path -Path $Path -Leaf) -ilike 'powershell*') -or ((Split-Path -Path $Path -Leaf) -ilike 'cmd*')) {
                If ($SecureParameters) {
                    Write-ADTLogEntry -Message "Preparing parameters for VBScript that will start [$Path (Parameters Hidden)] as the logged-on user [$userName] and suppress the console window..."
                }
                Else {
                    Write-ADTLogEntry -Message "Preparing parameters for VBScript that will start [$Path $Parameters] as the logged-on user [$userName] and suppress the console window..."
                }

                [String]$NewParameters = "/e:vbscript"
                If ($executeAsUserTempPath -match ' ') {
                    $NewParameters = "$($NewParameters) `"$($executeAsUserTempPath)\RunHidden.vbs`""
                }
                Else {
                    $NewParameters = "$($NewParameters) $($executeAsUserTempPath)\RunHidden.vbs"
                }
                If (($Path -notmatch "^[`'].*[`']$") -and ($Path -notmatch "^[`"].*[`"]$") -and $Path -match ' ') {
                    $NewParameters = "$($NewParameters) `"$($Path)`""
                }
                Else {
                    $NewParameters = "$NewParameters $Path"
                }

                $Parameters = "$NewParameters $Parameters"
                $Path = "$env:WinDir\System32\wscript.exe"
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
                [String]$schTaskName = "$($("$($Script:ADT.Environment.appDeployToolkitName)-ExecuteAsUser" -replace ' ', '').Trim('_') -replace '[_]+', '_')"
                #  Specify the filename to export the XML to
                [String]$previousXmlFileName = Get-ChildItem -Path "$($Script:ADT.CurrentSession.GetPropertyValue('dirAppDeployTemp'))\*" -Attributes '!Directory' -Include '*.xml' | Where-Object { $_.Name -match "^$($schTaskName)-\d{3}\.xml$" } | Sort-Object -Descending -Property 'LastWriteTime' | Select-Object -ExpandProperty 'Name' -First 1
                If (-not [String]::IsNullOrEmpty($previousXmlFileName)) {
                    [Int32]$xmlFileCount = [IO.Path]::GetFileNameWithoutExtension($previousXmlFileName) | ForEach-Object { $_.Substring($_.length - 3, 3) }
                    [String]$schTaskNameCount = '{0:d3}' -f $xmlFileCount++
                }
                $schTaskName = "$($schTaskName)-$($schTaskNameCount)"
                [String]$xmlSchTaskFilePath = "$($Script:ADT.CurrentSession.GetPropertyValue('dirAppDeployTemp'))\$($schTaskName).xml"

                #  Export the XML file
                [String]$xmlSchTask | Out-File -FilePath $xmlSchTaskFilePath -Force -ErrorAction 'Stop'
                Try {
                    Set-ItemPermission -Path $xmlSchTaskFilePath -User $UserName -Permission 'Read'
                }
                Catch {
                    Write-ADTLogEntry -Message "Failed to set read permissions on path [$xmlSchTaskFilePath]. The function might not be able to work correctly." -Severity 2
                }
            }
            Catch {
                [Int32]$executeProcessAsUserExitCode = 60007
                Write-ADTLogEntry -Message "Failed to export the scheduled task XML file [$xmlSchTaskFilePath]. `r`n$(Resolve-Error)" -Severity 3
                If (-not $ContinueOnError) {
                    Throw "Failed to export the scheduled task XML file [$xmlSchTaskFilePath]: $($_.Exception.Message)"
                }
                Return
            }

            ## Create Scheduled Task to run the process with a logged-on user account
            If ($Parameters) {
                If ($SecureParameters) {
                    Write-ADTLogEntry -Message "Creating scheduled task to execute [$Path (Parameters Hidden)] as the logged-on user [$userName]..."
                }
                Else {
                    Write-ADTLogEntry -Message "Creating scheduled task to execute [$Path $Parameters] as the logged-on user [$userName]..."
                }
            }
            Else {
                Write-ADTLogEntry -Message "Creating scheduled task to execute [$Path] as the logged-on user [$userName]..."
            }
            [PSObject]$schTaskResult = Execute-Process -Path $Script:ADT.Environment.exeSchTasks -Parameters "/create /f /tn $schTaskName /xml `"$xmlSchTaskFilePath`"" -WindowStyle 'Hidden' -CreateNoWindow -PassThru -ExitOnProcessFailure $false
            If ($schTaskResult.ExitCode -ne 0) {
                [Int32]$executeProcessAsUserExitCode = $schTaskResult.ExitCode
                Write-ADTLogEntry -Message "Failed to create the scheduled task by importing the scheduled task XML file [$xmlSchTaskFilePath]." -Severity 3
                If (-not $ContinueOnError) {
                    Throw "Failed to create the scheduled task by importing the scheduled task XML file [$xmlSchTaskFilePath]."
                }
                Return
            }

            ## Trigger the Scheduled Task
            If ($Parameters) {
                If ($SecureParameters) {
                    Write-ADTLogEntry -Message "Triggering execution of scheduled task with command [$Path] (Parameters Hidden) as the logged-on user [$userName]..."
                }
                Else {
                    Write-ADTLogEntry -Message "Triggering execution of scheduled task with command [$Path $Parameters] as the logged-on user [$userName]..."
                }
            }
            Else {
                Write-ADTLogEntry -Message "Triggering execution of scheduled task with command [$Path] as the logged-on user [$userName]..."
            }
            [PSObject]$schTaskResult = Execute-Process -Path $Script:ADT.Environment.exeSchTasks -Parameters "/run /i /tn $schTaskName" -WindowStyle 'Hidden' -CreateNoWindow -Passthru -ExitOnProcessFailure $false
            If ($schTaskResult.ExitCode -ne 0) {
                [Int32]$executeProcessAsUserExitCode = $schTaskResult.ExitCode
                Write-ADTLogEntry -Message "Failed to trigger scheduled task [$schTaskName]." -Severity 3
                #  Delete Scheduled Task
                Write-ADTLogEntry -Message 'Deleting the scheduled task which did not trigger.'
                Execute-Process -Path $Script:ADT.Environment.exeSchTasks -Parameters "/delete /tn $schTaskName /f" -WindowStyle 'Hidden' -CreateNoWindow -ExitOnProcessFailure $false
                If (-not $ContinueOnError) {
                    Throw "Failed to trigger scheduled task [$schTaskName]."
                }
                Return
            }

            ## Wait for the process launched by the scheduled task to complete execution
            If ($Wait) {
                Write-ADTLogEntry -Message "Waiting for the process launched by the scheduled task [$schTaskName] to complete execution (this may take some time)..."
                Start-Sleep -Seconds 1
                #If on Windows Vista or higer, Windows Task Scheduler 2.0 is supported. 'Schedule.Service' ComObject output is UI language independent
                If ($Script:ADT.Environment.envOSVersionMajor -gt 5) {
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
                    }
                    Catch {
                        Write-ADTLogEntry -Message "Failed to retrieve information from Task Scheduler. `r`n$(Resolve-Error)" -Severity 3
                    }
                    Finally {
                        Try {
                            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($ScheduleService)
                        }
                        Catch {
                        }
                    }
                }
                #Windows Task Scheduler 1.0
                Else {
                    While ((($exeSchTasksResult = & $Script:ADT.Environment.exeSchTasks /query /TN $schTaskName /V /FO CSV) | ConvertFrom-Csv | Select-Object -ExpandProperty 'Status' -First 1) -eq 'Running') {
                        Start-Sleep -Seconds 5
                    }
                    #  Get the exit code from the process launched by the scheduled task
                    [Int32]$executeProcessAsUserExitCode = ($exeSchTasksResultResult = & $($Script:ADT.Environment.exeSchTasks) /query /TN $schTaskName /V /FO CSV) | ConvertFrom-Csv | Select-Object -ExpandProperty 'Last Result' -First 1
                }
                Write-ADTLogEntry -Message "Exit code from process launched by scheduled task [$executeProcessAsUserExitCode]."
            }
            Else {
                Start-Sleep -Seconds 1
            }
        }
        Finally {
            ## Delete scheduled task
            Try {
                Write-ADTLogEntry -Message "Deleting scheduled task [$schTaskName]."
                Execute-Process -Path $Script:ADT.Environment.exeSchTasks -Parameters "/delete /tn $schTaskName /f" -WindowStyle 'Hidden' -CreateNoWindow -ErrorAction 'Stop'
            }
            Catch {
                Write-ADTLogEntry -Message "Failed to delete scheduled task [$schTaskName]. `r`n$(Resolve-Error)" -Severity 3
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
