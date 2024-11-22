Function Copy-File {
    <#
.SYNOPSIS

Copy a file or group of files to a destination path.

.DESCRIPTION

Copy a file or group of files to a destination path.

.PARAMETER Path

Path of the file to copy. Multiple paths can be specified

.PARAMETER Destination

Destination Path of the file to copy.

.PARAMETER Recurse

Copy files in subdirectories.

.PARAMETER Flatten

Flattens the files into the root destination directory.

.PARAMETER ContinueOnError

Continue if an error is encountered. This will continue the deployment script, but will not continue copying files if an error is encountered. Default is: $true.

.PARAMETER ContinueFileCopyOnError

Continue copying files if an error is encountered. This will continue the deployment script and will warn about files that failed to be copied. Default is: $false.

.PARAMETER UseRobocopy

Use Robocopy to copy files rather than native PowerShell method. Robocopy overcomes the 260 character limit. Supports * in file names, but not folders, in source paths. Default is configured in the AppDeployToolkitConfig.xml file: $true

.PARAMETER RobocopyParams

Override the default Robocopy parameters. Default is: /NJH /NJS /NS /NC /NP /NDL /FP /IS /IT /IM /XX /MT:4 /R:1 /W:1

.PARAMETER RobocopyAdditionalParams

Append to the default Robocopy parameters. Default is: /NJH /NJS /NS /NC /NP /NDL /FP /IS /IT /IM /XX /MT:4 /R:1 /W:1

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Copy-File -Path "$dirSupportFiles\MyApp.ini" -Destination "$envWinDir\MyApp.ini"

.EXAMPLE

Copy-File -Path "$dirSupportFiles\*.*" -Destination "$envTemp\tempfiles"

Copy all of the files in a folder to a destination folder.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullorEmpty()]
        [String[]]$Path,
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullorEmpty()]
        [String]$Destination,
        [Parameter(Mandatory = $false)]
        [Switch]$Recurse = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$Flatten,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueFileCopyOnError = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$UseRobocopy = (Get-ADTConfig).Toolkit.UseRobocopy,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$RobocopyParams = '/NJH /NJS /NS /NC /NP /NDL /FP /IS /IT /IM /XX /MT:4 /R:1 /W:1',
        [String]$RobocopyAdditionalParams
        )

    Begin {
        Write-ADTDebugHeader

        # Check if Robocopy is on the system
        If ($UseRobocopy) {
            If (Test-Path -Path "$env:SystemRoot\System32\Robocopy.exe" -PathType Leaf) {
                $RobocopyCommand = "$env:SystemRoot\System32\Robocopy.exe"
            }
            Else {
                $UseRobocopy = $false
                Write-ADTLogEntry "Robocopy is not available on this system. Falling back to native PowerShell method." -Severity 2
            }
        }
        Else {
            $UseRobocopy = $false
        }
    }
    Process {
        Foreach ($srcPath in $Path) {
            $UseRobocopyThis = $UseRobocopy
            If ($UseRobocopyThis) {
                Try {
                    # Disable Robocopy if $Path has a folder containing a * wildcard
                    If ($srcPath -match '\*.*\\') {
                        $UseRobocopyThis = $false
                        Write-ADTLogEntry "Asterisk wildcard specified in folder portion of path variable. Falling back to native PowerShell method." -Severity 2
                    }
                    # Don't just check for an extension here, also check for base name without extension to allow copying to a directory such as .config
                    If ([IO.Path]::HasExtension($Destination) -and [IO.Path]::GetFileNameWithoutExtension($Destination) -and -not (Test-Path -LiteralPath $Destination -PathType Container)) {
                        $UseRobocopyThis = $false
                        Write-ADTLogEntry "Destination path appears to be a file. Falling back to native PowerShell method." -Severity 2

                    }
                    If ($UseRobocopyThis) {

                        # Pre-create destination folder if it does not exist; Robocopy will auto-create non-existent destination folders, but pre-creating ensures we can use Resolve-Path
                        If (-not (Test-Path -LiteralPath $Destination -PathType Container)) {
                            Write-ADTLogEntry -Message "Destination assumed to be a folder which does not exist, creating destination folder [$Destination]."
                            $null = New-Item -Path $Destination -Type 'Directory' -Force -ErrorAction 'Stop'
                        }
                        If (Test-Path -LiteralPath $srcPath -PathType Container) {
                            # If source exists as a folder, append the last subfolder to the destination, so that Robocopy produces similar results to native Powershell
                            # Trim ending backslash from paths which can cause problems with Robocopy
                            # Resolve paths in case relative paths beggining with .\, ..\, or \ are used
                            $RobocopySource = (Resolve-Path -LiteralPath $srcPath.TrimEnd('\')).Path
                            $RobocopyDestination = Join-Path (Resolve-Path -LiteralPath $Destination).Path (Split-Path -Path $srcPath -Leaf)
                            $RobocopyFile = '*'
                        }
                        Else {
                            # Else assume source is a file and split args to the format <SourceFolder> <DestinationFolder> <FileName>
                            # Trim ending backslash from paths which can cause problems with Robocopy
                            # Resolve paths in case relative paths beggining with .\, ..\, or \ are used
                            $RobocopySource = (Resolve-Path -LiteralPath (Split-Path -Path $srcPath -Parent)).Path
                            $RobocopyDestination = (Resolve-Path -LiteralPath $Destination.TrimEnd('\')).Path
                            $RobocopyFile = (Split-Path -Path $srcPath -Leaf)
                        }
                        If ($Flatten) {
                            Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination] root folder, flattened."
                            [Hashtable]$CopyFileSplat = @{
                                Path                     = (Join-Path $RobocopySource $RobocopyFile) # This will ensure that the source dir will have \* appended if it was a folder (which prevents creation of a folder at the destination), or keeps the original file name if it was a file
                                Destination              = $Destination # Use the original destination path, not $RobocopyDestination which could have had a subfolder appended to it
                                Recurse                  = $false # Disable recursion as this will create subfolders in the destination
                                Flatten                  = $false # Disable flattening to prevent infinite loops
                                ContinueOnError          = $ContinueOnError
                                ContinueFileCopyOnError  = $ContinueFileCopyOnError
                                UseRobocopy              = $UseRobocopy
                                RobocopyParams           = $RobocopyParams
                                RobocopyAdditionalParams = $RobocopyAdditionalParams
                            }
                            # Copy all files from the root source folder
                            Copy-File @CopyFileSplat
                            # Copy all files from subfolders
                            Get-ChildItem -Path $RobocopySource -Directory -Recurse -Force -ErrorAction 'Ignore' | ForEach-Object {
                                # Append file name to subfolder path and repeat Copy-File
                                $CopyFileSplat.Path = Join-Path $_.FullName $RobocopyFile
                                Copy-File @CopyFileSplat
                            }
                            # Skip to next $SrcPath in $Path since we have handed off all copy tasks to separate executions of the function
                            Continue
                        }
                        If ($Recurse) {
                            # Add /E to Robocopy parameters if it is not already included
                            if ($RobocopyParams -notmatch '/E(\s|$)' -and $RobocopyAdditionalParams -notmatch '/E(\s|$)') {
                                $RobocopyParams = $RobocopyParams + " /E"
                            }
                            Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination]."
                        }
                        Else {
                            # Ensure that /E is not included in the Robocopy parameters as it will copy recursive folders
                            $RobocopyParams = $RobocopyParams -replace '/E(\s|$)'
                            $RobocopyAdditionalParams = $RobocopyAdditionalParams -replace '/E(\s|$)'
                            Write-ADTLogEntry -Message "Copying file(s) in path [$srcPath] to destination [$Destination]."
                        }

                        $RobocopyArgs = "$RobocopyParams $RobocopyAdditionalParams `"$RobocopySource`" `"$RobocopyDestination`" `"$RobocopyFile`""
                        Write-ADTLogEntry -Message "Executing Robocopy command: $RobocopyCommand $RobocopyArgs"
                        $RobocopyResult = Start-ADTProcess -Path $RobocopyCommand -Parameters $RobocopyArgs -CreateNoWindow -NoExitOnProcessFailure -PassThru -IgnoreExitCodes 0,1,2,3,4,5,6,7,8 -ErrorAction Ignore
                        # Trim the leading whitespace from each line of Robocopy output, ignore the last empty line, and join the lines back together
                        $RobocopyOutput = ($RobocopyResult.StdOut.Split("`n").TrimStart() | Select-Object -SkipLast 1) -join "`n"
                        Write-ADTLogEntry -Message "Robocopy output:`n$RobocopyOutput"

                        Switch ($RobocopyResult.ExitCode) {
                            0 { Write-ADTLogEntry -Message "Robocopy completed. No files were copied. No failure was encountered. No files were mismatched. The files already exist in the destination directory; therefore, the copy operation was skipped." }
                            1 { Write-ADTLogEntry -Message "Robocopy completed. All files were copied successfully." }
                            2 { Write-ADTLogEntry -Message "Robocopy completed. There are some additional files in the destination directory that aren't present in the source directory. No files were copied." }
                            3 { Write-ADTLogEntry -Message "Robocopy completed. Some files were copied. Additional files were present. No failure was encountered." }
                            4 { Write-ADTLogEntry -Message "Robocopy completed. Some Mismatched files or directories were detected. Examine the output log. Housekeeping might be required." -Severity 2 }
                            5 { Write-ADTLogEntry -Message "Robocopy completed. Some files were copied. Some files were mismatched. No failure was encountered." }
                            6 { Write-ADTLogEntry -Message "Robocopy completed. Additional files and mismatched files exist. No files were copied and no failures were encountered meaning that the files already exist in the destination directory." -Severity 2 }
                            7 { Write-ADTLogEntry -Message "Robocopy completed. Files were copied, a file mismatch was present, and additional files were present." -Severity 2 }
                            8 { Write-ADTLogEntry -Message "Robocopy completed. Several files didn't copy." -Severity 2 }
                            16 {
                                Write-ADTLogEntry -Message "Serious error. Robocopy did not copy any files. Either a usage error or an error due to insufficient access privileges on the source or destination directories.." -Severity 3
                                If (-not $ContinueOnError) {
                                    Throw "Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $($_.Exception.Message)"
                                }
                            }
                            default {
                                Write-ADTLogEntry -Message "Failed to copy file(s) in path [$srcPath] to destination [$Destination].`n$(Resolve-ADTError)" -Severity 3
                                If (-not $ContinueOnError) {
                                    Throw "Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $($_.Exception.Message)"
                                }
                            }
                        }
                    }
                }
                Catch {
                    Write-ADTLogEntry -Message "Failed to copy file(s) in path [$srcPath] to destination [$Destination].`n$(Resolve-ADTError)" -Severity 3
                    If (-not $ContinueOnError) {
                        Throw "Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $($_.Exception.Message)"
                    }
                }
            }
            If ($UseRobocopyThis -eq $false) {
                Try {
                    # If destination has no extension, or if it has an extension only and no name (e.g. a .config folder) and the destination folder does not exist
                    If ((-not ([IO.Path]::HasExtension($Destination))) -or ([IO.Path]::HasExtension($Destination) -and -not [IO.Path]::GetFileNameWithoutExtension($Destination)) -and (-not (Test-Path -LiteralPath $Destination -PathType 'Container'))) {
                        Write-ADTLogEntry -Message "Destination assumed to be a folder which does not exist, creating destination folder [$Destination]."
                        $null = New-Item -Path $Destination -Type 'Directory' -Force -ErrorAction 'Stop'
                    }
                    # If destination appears to be a file name but parent folder does not exist, create it
                    $DestinationParent = Split-Path $Destination -Parent
                    If ([IO.Path]::HasExtension($Destination) -and [IO.Path]::GetFileNameWithoutExtension($Destination) -and -not (Test-Path -LiteralPath $DestinationParent -PathType 'Container')) {
                        Write-ADTLogEntry -Message "Destination assumed to be a file whose parent folder does not exist, creating destination folder [$DestinationParent]."
                        $null = New-Item -Path $DestinationParent -Type 'Directory' -Force -ErrorAction 'Stop'
                    }
                    If ($Flatten) {
                        Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination] root folder, flattened."
                        If ($ContinueFileCopyOnError) {
                            $null = Get-ChildItem -Path $srcPath -File -Recurse -Force -ErrorAction 'Ignore' | ForEach-Object {
                                Copy-Item -Path ($_.FullName) -Destination $Destination -Force -ErrorAction 'Ignore' -ErrorVariable 'FileCopyError'
                            }
                        }
                        Else {
                            $null = Get-ChildItem -Path $srcPath -File -Recurse -Force -ErrorAction 'Ignore' | ForEach-Object {
                                Copy-Item -Path ($_.FullName) -Destination $Destination -Force -ErrorAction 'Stop'
                            }
                        }
                    }
                    ElseIf ($Recurse) {
                        Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination]."
                        If ($ContinueFileCopyOnError) {
                            $null = Copy-Item -Path $srcPath -Destination $Destination -Force -Recurse -ErrorAction 'Ignore' -ErrorVariable 'FileCopyError'
                        }
                        Else {
                            $null = Copy-Item -Path $srcPath -Destination $Destination -Force -Recurse -ErrorAction 'Stop'
                        }
                    }
                    Else {
                        Write-ADTLogEntry -Message "Copying file in path [$srcPath] to destination [$Destination]."
                        If ($ContinueFileCopyOnError) {
                            $null = Copy-Item -Path $srcPath -Destination $Destination -Force -ErrorAction 'Ignore' -ErrorVariable 'FileCopyError'
                        }
                        Else {
                            $null = Copy-Item -Path $srcPath -Destination $Destination -Force -ErrorAction 'Stop'
                        }
                    }

                    If ($FileCopyError) {
                        Write-ADTLogEntry -Message "The following warnings were detected while copying file(s) in path [$srcPath] to destination [$Destination].`n$FileCopyError" -Severity 2
                    }
                    Else {
                        Write-ADTLogEntry -Message 'File copy completed successfully.'
                    }
                }
                Catch {
                    Write-ADTLogEntry -Message "Failed to copy file(s) in path [$srcPath] to destination [$Destination].`n$(Resolve-ADTError)" -Severity 3
                    If (-not $ContinueOnError) {
                        Throw "Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $($_.Exception.Message)"
                    }
                }
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
