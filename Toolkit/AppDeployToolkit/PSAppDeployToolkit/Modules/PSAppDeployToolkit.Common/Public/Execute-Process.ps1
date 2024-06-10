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

Execute-Process -Path 'setup.exe' -Parameters "-s -f2`"$((Get-ADTConfig).Toolkit.LogPath)\$installName.log`""

Launch InstallShield "setup.exe" from the ".\Files" sub-directory and force log files to the logging folder.

.EXAMPLE

Execute-Process -Path 'setup.exe' -Parameters "/s /v`"ALLUSERS=1 /qn /L* \`"$((Get-ADTConfig).Toolkit.LogPath)\$installName.log`"`""

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
        [Int32]$MsiExecWaitTime = (Get-ADTConfig).MSI.MutexWaitTime,
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
        $adtSession = Get-ADTSession
        Write-ADTDebugHeader
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
                [String]$PathFolders = (Get-ADTSession).GetPropertyValue('dirFiles')
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
                [Boolean]$MsiExecAvailable = Test-ADTIsMutexAvailable -MutexName 'Global\_MSIExecute' -MutexWaitTime (New-TimeSpan -Seconds $MsiExecWaitTime)
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
                ElseIf ($adtSession.GetPropertyValue('AppRebootCodes').Contains($returnCode)) {
                    Write-ADTLogEntry -Message "Execution completed successfully with exit code [$returnCode]. A reboot is required." -Severity 2
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
                ElseIf ($adtSession.GetPropertyValue('AppExitCodes').Contains($returnCode)) {
                    Write-ADTLogEntry -Message "Execution completed successfully with exit code [$returnCode]." -Severity 0
                }
                Else {
                    [String]$MsiExitCodeMessage = ''
                    If ($Path -match 'msiexec') {
                        [String]$MsiExitCodeMessage = Get-ADTMsiExitCodeMessage -MsiExitCode $returnCode
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
                Write-ADTLogEntry -Message "Function failed, setting exit code to [$returnCode].`n$(Resolve-ADTError)" -Severity 3
                If (-not $ContinueOnError) {
                    Throw "Function failed, setting exit code to [$returnCode]. $($_.Exception.Message)"
                }
            }
            Else {
                Write-ADTLogEntry -Message "Execution completed with exit code [$returnCode]. Function failed.`n$(Resolve-ADTError)" -Severity 3
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
        Write-ADTDebugFooter
    }
}
