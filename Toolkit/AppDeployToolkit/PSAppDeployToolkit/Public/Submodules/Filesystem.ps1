#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function New-Folder {
    <#
.SYNOPSIS

Create a new folder.

.DESCRIPTION

Create a new folder if it does not exist.

.PARAMETER Path

Path to the new folder to create.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

New-Folder -Path "$envWinDir\System32"

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            If (-not (Test-Path -LiteralPath $Path -PathType 'Container')) {
                Write-ADTLogEntry -Message "Creating folder [$Path]."
                $null = New-Item -Path $Path -ItemType 'Directory' -ErrorAction 'Stop' -Force
            }
            Else {
                Write-ADTLogEntry -Message "Folder [$Path] already exists."
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to create folder [$Path]. `r`n$(Resolve-Error)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to create folder [$Path]: $($_.Exception.Message)"
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

Function Remove-Folder {
    <#
.SYNOPSIS

Remove folder and files if they exist.

.DESCRIPTION

Remove folder and all files with or without recursion in a given path.

.PARAMETER Path

Path to the folder to remove.

.PARAMETER DisableRecursion

Disables recursion while deleting.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Remove-Folder -Path "$envWinDir\Downloaded Program Files"

Deletes all files and subfolders in the Windows\Downloads Program Files folder

.EXAMPLE

Remove-Folder -Path "$envTemp\MyAppCache" -DisableRecursion

Deletes all files in the Temp\MyAppCache folder but does not delete any subfolders.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [Switch]$DisableRecursion,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        If (Test-Path -LiteralPath $Path -PathType 'Container' -ErrorAction 'Ignore') {
            Try {
                If ($DisableRecursion) {
                    Write-ADTLogEntry -Message "Deleting folder [$path] without recursion..."
                    # Without recursion we have to go through the subfolder ourselves because Remove-Item asks for confirmation if we are trying to delete a non-empty folder without -Recurse
                    [Array]$ListOfChildItems = Get-ChildItem -LiteralPath $Path -Force
                    If ($ListOfChildItems) {
                        $SubfoldersSkipped = 0
                        ForEach ($item in $ListOfChildItems) {
                            # Check whether this item is a folder
                            If (Test-Path -LiteralPath $item.FullName -PathType Container) {
                                # Item is a folder. Check if its empty
                                # Get list of child items in the folder
                                [Array]$ItemChildItems = Get-ChildItem -LiteralPath $item.FullName -Force -ErrorAction 'Ignore' -ErrorVariable '+ErrorRemoveFolder'
                                If ($ItemChildItems.Count -eq 0) {
                                    # The folder is empty, delete it
                                    Remove-Item -LiteralPath $item.FullName -Force -ErrorAction 'Ignore' -ErrorVariable '+ErrorRemoveFolder'
                                }
                                Else {
                                    # Folder is not empty, skip it
                                    $SubfoldersSkipped++
                                    Continue
                                }
                            }
                            Else {
                                # Item is a file. Delete it
                                Remove-Item -LiteralPath $item.FullName -Force -ErrorAction 'Ignore' -ErrorVariable '+ErrorRemoveFolder'
                            }
                        }
                        If ($SubfoldersSkipped -gt 0) {
                            Throw "[$SubfoldersSkipped] subfolders are not empty!"
                        }
                    }
                    Else {
                        Remove-Item -LiteralPath $Path -Force -ErrorAction 'Ignore' -ErrorVariable '+ErrorRemoveFolder'
                    }
                }
                Else {
                    Write-ADTLogEntry -Message "Deleting folder [$path] recursively..."
                    Remove-Item -LiteralPath $Path -Force -Recurse -ErrorAction 'Ignore' -ErrorVariable '+ErrorRemoveFolder'
                }

                If ($ErrorRemoveFolder) {
                    Throw $ErrorRemoveFolder
                }
            }
            Catch {
                Write-ADTLogEntry -Message "Failed to delete folder(s) and file(s) from path [$path]. `r`n$(Resolve-Error)" -Severity 3
                If (-not $ContinueOnError) {
                    Throw "Failed to delete folder(s) and file(s) from path [$path]: $($_.Exception.Message)"
                }
            }
        }
        Else {
            Write-ADTLogEntry -Message "Folder [$Path] does not exist."
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
        [Boolean]$UseRobocopy = $Script:ADT.Config.Toolkit.UseRobocopy,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$RobocopyParams = '/NJH /NJS /NS /NC /NP /NDL /FP /IS /IT /IM /XX /MT:4 /R:1 /W:1',
        [String]$RobocopyAdditionalParams
        )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

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
                        $RobocopyResult = Execute-Process -Path $RobocopyCommand -Parameters $RobocopyArgs -CreateNoWindow -ContinueOnError $true -ExitOnProcessFailure $false -Passthru -IgnoreExitCodes '0,1,2,3,4,5,6,7,8'
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
                                Write-ADTLogEntry -Message "Failed to copy file(s) in path [$srcPath] to destination [$Destination]. `r`n$(Resolve-Error)" -Severity 3
                                If (-not $ContinueOnError) {
                                    Throw "Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $($_.Exception.Message)"
                                }
                            }
                        }
                    }
                }
                Catch {
                    Write-ADTLogEntry -Message "Failed to copy file(s) in path [$srcPath] to destination [$Destination]. `r`n$(Resolve-Error)" -Severity 3
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
                        Write-ADTLogEntry -Message "The following warnings were detected while copying file(s) in path [$srcPath] to destination [$Destination]. `r`n$FileCopyError" -Severity 2
                    }
                    Else {
                        Write-ADTLogEntry -Message 'File copy completed successfully.'
                    }
                }
                Catch {
                    Write-ADTLogEntry -Message "Failed to copy file(s) in path [$srcPath] to destination [$Destination]. `r`n$(Resolve-Error)" -Severity 3
                    If (-not $ContinueOnError) {
                        Throw "Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $($_.Exception.Message)"
                    }
                }
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

Function Remove-File {
    <#
.SYNOPSIS

Removes one or more items from a given path on the filesystem.

.DESCRIPTION

Removes one or more items from a given path on the filesystem.

.PARAMETER Path

Specifies the path on the filesystem to be resolved. The value of Path will accept wildcards. Will accept an array of values.

.PARAMETER LiteralPath

Specifies the path on the filesystem to be resolved. The value of LiteralPath is used exactly as it is typed; no characters are interpreted as wildcards. Will accept an array of values.

.PARAMETER Recurse

Deletes the files in the specified location(s) and in all child items of the location(s).

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Remove-File -Path 'C:\Windows\Downloaded Program Files\Temp.inf'

.EXAMPLE

Remove-File -LiteralPath 'C:\Windows\Downloaded Program Files' -Recurse

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [ValidateNotNullorEmpty()]
        [String[]]$Path,
        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullorEmpty()]
        [String[]]$LiteralPath,
        [Parameter(Mandatory = $false)]
        [Switch]$Recurse = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## Build hashtable of parameters/value pairs to be passed to Remove-Item cmdlet
        [Hashtable]$RemoveFileSplat = @{ 'Recurse' = $Recurse
                                          'Force'                                = $true
                                          'ErrorVariable'                        = '+ErrorRemoveItem'
        }
        If ($ContinueOnError) {
            $RemoveFileSplat.Add('ErrorAction', 'Ignore')
        }
        Else {
            $RemoveFileSplat.Add('ErrorAction', 'Stop')
        }

        ## Resolve the specified path, if the path does not exist, display a warning instead of an error
        If ($PSCmdlet.ParameterSetName -eq 'Path') {
            [String[]]$SpecifiedPath = $Path
        }
        Else {
            [String[]]$SpecifiedPath = $LiteralPath
        }
        ForEach ($Item in $SpecifiedPath) {
            Try {
                If ($PSCmdlet.ParameterSetName -eq 'Path') {
                    [String[]]$ResolvedPath += Resolve-Path -Path $Item -ErrorAction 'Stop' | Where-Object { $_.Path } | Select-Object -ExpandProperty 'Path' -ErrorAction 'Stop'
                }
                Else {
                    [String[]]$ResolvedPath += Resolve-Path -LiteralPath $Item -ErrorAction 'Stop' | Where-Object { $_.Path } | Select-Object -ExpandProperty 'Path' -ErrorAction 'Stop'
                }
            }
            Catch [System.Management.Automation.ItemNotFoundException] {
                Write-ADTLogEntry -Message "Unable to resolve file(s) for deletion in path [$Item] because path does not exist." -Severity 2
            }
            Catch {
                Write-ADTLogEntry -Message "Failed to resolve file(s) for deletion in path [$Item]. `r`n$(Resolve-Error)" -Severity 3
                If (-not $ContinueOnError) {
                    Throw "Failed to resolve file(s) for deletion in path [$Item]: $($_.Exception.Message)"
                }
            }
        }

        ## Delete specified path if it was successfully resolved
        If ($ResolvedPath) {
            ForEach ($Item in $ResolvedPath) {
                Try {
                    If (($Recurse) -and (Test-Path -LiteralPath $Item -PathType 'Container')) {
                        Write-ADTLogEntry -Message "Deleting file(s) recursively in path [$Item]..."
                    }
                    ElseIf ((-not $Recurse) -and (Test-Path -LiteralPath $Item -PathType 'Container')) {
                        Write-ADTLogEntry -Message "Skipping folder [$Item] because the Recurse switch was not specified."
                        Continue
                    }
                    Else {
                        Write-ADTLogEntry -Message "Deleting file in path [$Item]..."
                    }
                    $null = Remove-Item @RemoveFileSplat -LiteralPath $Item
                }
                Catch {
                    Write-ADTLogEntry -Message "Failed to delete file(s) in path [$Item]. `r`n$(Resolve-Error)" -Severity 3
                    If (-not $ContinueOnError) {
                        Throw "Failed to delete file(s) in path [$Item]: $($_.Exception.Message)"
                    }
                }
            }
        }

        If ($ErrorRemoveItem) {
            Write-ADTLogEntry -Message "The following error(s) took place while removing file(s) in path [$SpecifiedPath]. `r`n$(Resolve-Error -ErrorRecord $ErrorRemoveItem)" -Severity 2
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

function Copy-FileToUserProfiles {
    <#
.SYNOPSIS

Copy one or more items to a each user profile on the system.

.DESCRIPTION

Copy one or more items to a each user profile on the system.

.PARAMETER Path

The path of the file or folder to copy.

.PARAMETER Destination

The path of the destination folder to append to the root of the user profile.

.PARAMETER Recurse

Copy files in subdirectories.

.PARAMETER Flatten

Flattens the files into the root destination directory.

.PARAMETER ContinueOnError

Continue if an error is encountered. This will continue the deployment script, but will not continue copying files if an error is encountered. Default is: $true.

.PARAMETER ContinueFileCopyOnError

Continue copying files if an error is encountered. This will continue the deployment script and will warn about files that failed to be copied. Default is: $false.

.PARAMETER UseRobocopy

Use Robocopy to copy files rather than native PowerShell method. Robocopy overcomes the 260 character limit. Only applies if $Path is specified as a folder. Default is configured in the AppDeployToolkitConfig.xml file: $true

.PARAMETER RobocopyAdditionalParams

Additional parameters to pass to Robocopy. Default is: $null

.PARAMETER ExcludeNTAccount

Specify NT account names in Domain\Username format to exclude from the list of user profiles.

.PARAMETER ExcludeSystemProfiles

Exclude system profiles: SYSTEM, LOCAL SERVICE, NETWORK SERVICE. Default is: $true.

.PARAMETER ExcludeServiceProfiles

Exclude service profiles where NTAccount begins with NT SERVICE. Default is: $true.

.PARAMETER ExcludeDefaultUser

Exclude the Default User. Default is: $false.

.INPUTS

You can pipe in string values for $Path.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Copy-FileToUserProfiles -Path "$dirSupportFiles\config.txt" -Destination "AppData\Roaming\MyApp"

Copy a single file to C:\Users\<UserName>\AppData\Roaming\MyApp for each user.

.EXAMPLE

Copy-FileToUserProfiles -Path "$dirSupportFiles\config.txt","$dirSupportFiles\config2.txt" -Destination "AppData\Roaming\MyApp"

Copy two files to C:\Users\<UserName>\AppData\Roaming\MyApp for each user.

.EXAMPLE

Copy-FileToUserProfiles -Path "$dirFiles\MyApp" -Destination "AppData\Local" -Recurse

Copy an entire folder to C:\Users\<UserName>\AppData\Local for each user.

.EXAMPLE

Copy-FileToUserProfiles -Path "$dirFiles\.appConfigFolder" -Recurse

Copy an entire folder to C:\Users\<UserName> for each user.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 1, ValueFromPipeline = $true)]
        [String[]]$Path,
        [Parameter(Mandatory = $false, Position = 2)]
        [String]$Destination,
        [Parameter(Mandatory = $false)]
        [Switch]$Recurse = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$Flatten,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$UseRobocopy = $Script:ADT.Config.Toolkit.UseRobocopy,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$RobocopyAdditionalParams = $null,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String[]]$ExcludeNTAccount,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ExcludeSystemProfiles = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ExcludeServiceProfiles = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$ExcludeDefaultUser = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueFileCopyOnError = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        [Hashtable]$CopyFileSplat = @{
            Path = $Path
            Recurse = $Recurse
            Flatten = $Flatten
            ContinueOnError = $ContinueOnError
            ContinueFileCopyOnError = $ContinueFileCopyOnError
            UseRobocopy = $UseRobocopy
        }
        if ($RobocopyAdditionalParams) {
            $CopyFileSplat.RobocopyAdditionalParams = $RobocopyAdditionalParams
        }

        [Hashtable]$GetUserProfileSplat = @{
            ExcludeSystemProfiles = $ExcludeSystemProfiles
            ExcludeServiceProfiles = $ExcludeServiceProfiles
            ExcludeDefaultUser = $ExcludeDefaultUser
        }
        if ($ExcludeNTAccount) {
            $GetUserProfileSplat.ExcludeNTAccount = $ExcludeNTAccount
        }

        foreach ($UserProfilePath in (Get-UserProfiles @GetUserProfileSplat).ProfilePath) {
            $CopyFileSplat.Destination = Join-Path $UserProfilePath $Destination
            Write-ADTLogEntry -Message "Copying path [$Path] to $($CopyFileSplat.Destination):"
            Copy-File @CopyFileSplat
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

Function Remove-FileFromUserProfiles {
    <#
.SYNOPSIS

Removes one or more items from each user profile on the system.

.DESCRIPTION

Removes one or more items from each user profile on the system.

.PARAMETER Path

Specifies the path to append to the root of the user profile to be resolved. The value of Path will accept wildcards. Will accept an array of values.

.PARAMETER LiteralPath

Specifies the path to append to the root of the user profile to be resolved. The value of LiteralPath is used exactly as it is typed; no characters are interpreted as wildcards. Will accept an array of values.

.PARAMETER Recurse

Deletes the files in the specified location(s) and in all child items of the location(s).

.PARAMETER ExcludeNTAccount

Specify NT account names in Domain\Username format to exclude from the list of user profiles.

.PARAMETER ExcludeSystemProfiles

Exclude system profiles: SYSTEM, LOCAL SERVICE, NETWORK SERVICE. Default is: $true.

.PARAMETER ExcludeServiceProfiles

Exclude service profiles where NTAccount begins with NT SERVICE. Default is: $true.

.PARAMETER ExcludeDefaultUser

Exclude the Default User. Default is: $false.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Remove-FileFromUserProfiles -Path "AppData\Roaming\MyApp\config.txt"

.EXAMPLE

Remove-FileFromUserProfiles -Path "AppData\Local\MyApp" -Recurse

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'Path')]
        [ValidateNotNullorEmpty()]
        [String[]]$Path,
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullorEmpty()]
        [String[]]$LiteralPath,
        [Parameter(Mandatory = $false)]
        [Switch]$Recurse = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String[]]$ExcludeNTAccount,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ExcludeSystemProfiles = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ExcludeServiceProfiles = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$ExcludeDefaultUser = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        [Hashtable]$RemoveFileSplat = @{
            Recurse = $Recurse
            ContinueOnError = $ContinueOnError
        }

        [Hashtable]$GetUserProfileSplat = @{
            ExcludeSystemProfiles = $ExcludeSystemProfiles
            ExcludeServiceProfiles = $ExcludeServiceProfiles
            ExcludeDefaultUser = $ExcludeDefaultUser
        }
        if ($ExcludeNTAccount) {
            $GetUserProfileSplat.ExcludeNTAccount = $ExcludeNTAccount
        }

        ForEach ($UserProfilePath in (Get-UserProfiles @GetUserProfileSplat).ProfilePath) {
            If ($PSCmdlet.ParameterSetName -eq 'Path') {
                $RemoveFileSplat.Path = $Path | ForEach-Object { Join-Path $UserProfilePath $_ }
                Write-ADTLogEntry -Message "Removing path [$Path] from $UserProfilePath`:"
            }
            ElseIf ($PSCmdlet.ParameterSetName -eq 'LiteralPath') {
                $RemoveFileSplat.LiteralPath = $LiteralPath | ForEach-Object { Join-Path $UserProfilePath $_ }
                Write-ADTLogEntry -Message "Removing literal path [$LiteralPath] from $UserProfilePath`:"
            }
            Remove-File @RemoveFileSplat
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

Function Set-ItemPermission {
    <#
.SYNOPSIS

    Allow you to easily change permissions on files or folders

.DESCRIPTION

    Allow you to easily change permissions on files or folders for a given user or group.
    You can add, remove or replace permissions, set inheritance and propagation.

.PARAMETER Path

    Path to the folder or file you want to modify (ex: C:\Temp)

.PARAMETER User

    One or more user names (ex: BUILTIN\Users, DOMAIN\Admin) to give the permissions to. If you want to use SID, prefix it with an asterisk * (ex: *S-1-5-18)

.PARAMETER Permission

    Permission or list of permissions to be set/added/removed/replaced. To see all the possible permissions go to 'http://technet.microsoft.com/fr-fr/library/ff730951.aspx'.

    Permission DeleteSubdirectoriesAndFiles does not apply to files.

.PARAMETER PermissionType

    Sets Access Control Type of the permissions. Allowed options: Allow, Deny   Default: Allow

.PARAMETER Inheritance

    Sets permission inheritance. Does not apply to files. Multiple options can be specified. Allowed options: ObjectInherit, ContainerInherit, None  Default: None

    None - The permission entry is not inherited by child objects, ObjectInherit - The permission entry is inherited by child leaf objects. ContainerInherit - The permission entry is inherited by child container objects.

.PARAMETER Propagation

    Sets how to propagate inheritance. Does not apply to files. Allowed options: None, InheritOnly, NoPropagateInherit  Default: None

    None - Specifies that no inheritance flags are set. NoPropagateInherit - Specifies that the permission entry is not propagated to child objects. InheritOnly - Specifies that the permission entry is propagated only to child objects. This includes both container and leaf child objects.

.PARAMETER Method

    Specifies which method will be used to apply the permissions. Allowed options: Add, Set, Reset.

    Add - adds permissions rules but it does not remove previous permissions, Set - overwrites matching permission rules with new ones, Reset - removes matching permissions rules and then adds permission rules, Remove - Removes matching permission rules, RemoveSpecific - Removes specific permissions, RemoveAll - Removes all permission rules for specified user/s
    Default: Add

.PARAMETER EnableInheritance

    Enables inheritance on the files/folders.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

    Will grant FullControl permissions to 'John' and 'Users' on 'C:\Temp' and its files and folders children.

    PS C:\>Set-ItemPermission -Path 'C:\Temp' -User 'DOMAIN\John', 'BUILTIN\Utilisateurs' -Permission FullControl -Inheritance ObjectInherit,ContainerInherit

.EXAMPLE

    Will grant Read permissions to 'John' on 'C:\Temp\pic.png'

    PS C:\>Set-ItemPermission -Path 'C:\Temp\pic.png' -User 'DOMAIN\John' -Permission 'Read'

.EXAMPLE

    Will remove all permissions to 'John' on 'C:\Temp\Private'

    PS C:\>Set-ItemPermission -Path 'C:\Temp\Private' -User 'DOMAIN\John' -Permission 'None' -Method 'RemoveAll'

.NOTES

    Original Author: Julian DA CUNHA - dacunha.julian@gmail.com, used with permission

.LINK

    https://psappdeploytoolkit.com
#>

    [CmdletBinding()]
    Param (
        [Parameter( Mandatory = $true, Position = 0, HelpMessage = 'Path to the folder or file you want to modify (ex: C:\Temp)', ParameterSetName = 'DisableInheritance' )]
        [Parameter( Mandatory = $true, Position = 0, HelpMessage = 'Path to the folder or file you want to modify (ex: C:\Temp)', ParameterSetName = 'EnableInheritance' )]
        [ValidateNotNullOrEmpty()]
        [Alias('File', 'Folder')]
        [String]$Path,

        [Parameter( Mandatory = $true, Position = 1, HelpMessage = 'One or more user names (ex: BUILTIN\Users, DOMAIN\Admin). If you want to use SID, prefix it with an asterisk * (ex: *S-1-5-18)', ParameterSetName = 'DisableInheritance')]
        [Alias('Username', 'Users', 'SID', 'Usernames')]
        [String[]]$User,

        [Parameter( Mandatory = $true, Position = 2, HelpMessage = "Permission or list of permissions to be set/added/removed/replaced. To see all the possible permissions go to 'http://technet.microsoft.com/fr-fr/library/ff730951.aspx'", ParameterSetName = 'DisableInheritance')]
        [Alias('Acl', 'Grant', 'Permissions', 'Deny')]
        [ValidateSet('AppendData', 'ChangePermissions', 'CreateDirectories', 'CreateFiles', 'Delete', `
                'DeleteSubdirectoriesAndFiles', 'ExecuteFile', 'FullControl', 'ListDirectory', 'Modify', `
                'Read', 'ReadAndExecute', 'ReadAttributes', 'ReadData', 'ReadExtendedAttributes', 'ReadPermissions', `
                'Synchronize', 'TakeOwnership', 'Traverse', 'Write', 'WriteAttributes', 'WriteData', 'WriteExtendedAttributes', 'None')]
        [String[]]$Permission,

        [Parameter( Mandatory = $false, Position = 3, HelpMessage = 'Whether you want to set Allow or Deny permissions', ParameterSetName = 'DisableInheritance')]
        [Alias('AccessControlType')]
        [ValidateSet('Allow', 'Deny')]
        [String]$PermissionType = 'Allow',

        [Parameter( Mandatory = $false, Position = 4, HelpMessage = 'Sets how permissions are inherited', ParameterSetName = 'DisableInheritance')]
        [ValidateSet('ContainerInherit', 'None', 'ObjectInherit')]
        [String[]]$Inheritance = 'None',

        [Parameter( Mandatory = $false, Position = 5, HelpMessage = 'Sets how to propage inheritance flags', ParameterSetName = 'DisableInheritance')]
        [ValidateSet('None', 'InheritOnly', 'NoPropagateInherit')]
        [String]$Propagation = 'None',

        [Parameter( Mandatory = $false, Position = 6, HelpMessage = 'Specifies which method will be used to add/remove/replace permissions.', ParameterSetName = 'DisableInheritance')]
        [ValidateSet('Add', 'Set', 'Reset', 'Remove', 'RemoveSpecific', 'RemoveAll')]
        [Alias('ApplyMethod', 'ApplicationMethod')]
        [String]$Method = 'Add',

        [Parameter( Mandatory = $true, Position = 1, HelpMessage = 'Enables inheritance, which removes explicit permissions.', ParameterSetName = 'EnableInheritance')]
        [Switch]$EnableInheritance
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }

    Process {
        # Test elevated perms
        If (-not $Script:ADT.Environment.IsAdmin) {
            Write-ADTLogEntry -Message 'Unable to use the function [Set-ItemPermission] without elevated permissions.'
            Throw 'Unable to use the function [Set-ItemPermission] without elevated permissions.'
        }

        # Check path existence
        If (-not (Test-Path -Path $Path -ErrorAction 'Stop')) {
            Write-ADTLogEntry -Message "Specified path does not exist [$Path]."
            Throw "Specified path does not exist [$Path]."
        }

        If ($EnableInheritance) {
            # Get object acls
            $Acl = Get-Acl -Path $Path -ErrorAction Stop
            # Enable inherance
            $Acl.SetAccessRuleProtection($false, $true)
            Write-ADTLogEntry -Message "Enabling Inheritance on path [$Path]."
            $null = Set-Acl -Path $Path -AclObject $Acl -ErrorAction 'Stop'
            Return
        }
        # Permissions
        [System.Security.AccessControl.FileSystemRights]$FileSystemRights = New-Object -TypeName 'System.Security.AccessControl.FileSystemRights'
        If ($Permission -ne 'None') {
            ForEach ($Entry in $Permission) {
                $FileSystemRights = $FileSystemRights -bor [System.Security.AccessControl.FileSystemRights]$Entry
            }
        }

        # InheritanceFlags
        $InheritanceFlag = New-Object -TypeName 'System.Security.AccessControl.InheritanceFlags'
        ForEach ($IFlag in $Inheritance) {
            $InheritanceFlag = $InheritanceFlag -bor [System.Security.AccessControl.InheritanceFlags]$IFlag
        }

        # PropagationFlags
        $PropagationFlag = [System.Security.AccessControl.PropagationFlags]$Propagation

        # Access Control Type
        $Allow = [System.Security.AccessControl.AccessControlType]$PermissionType

        # Modify variables to remove file incompatible flags if this is a file
        If (Test-Path -Path $Path -ErrorAction 'Stop' -PathType 'Leaf') {
            $FileSystemRights = $FileSystemRights -band (-bnot [System.Security.AccessControl.FileSystemRights]::DeleteSubdirectoriesAndFiles)
            $InheritanceFlag = [System.Security.AccessControl.InheritanceFlags]::None
            $PropagationFlag = [System.Security.AccessControl.PropagationFlags]::None
        }

        # Get object acls
        $Acl = Get-Acl -Path $Path -ErrorAction Stop
        # Disable inherance, Preserve inherited permissions
        $Acl.SetAccessRuleProtection($true, $true)
        $null = Set-Acl -Path $Path -AclObject $Acl -ErrorAction 'Stop'
        # Get updated acls - without inheritance
        $Acl = $null
        $Acl = Get-Acl -Path $Path -ErrorAction Stop
        # Apply permissions on Users
        ForEach ($U in $User) {
            # Trim whitespace and skip if empty
            $U = $U.Trim()
            If ($U.Length -eq 0) {
                Continue
            }
            # Set Username
            If ($U.StartsWith('*')) {
                # This is a SID, remove the *
                $U = $U.remove(0, 1)
                Try {
                    # Translate the SID
                    $UsersAccountName = ConvertTo-NTAccountOrSID -SID $U
                }
                Catch {
                    Write-ADTLogEntry "Failed to translate SID [$U]. Skipping..." -Severity 2
                    Continue
                }

                $Username = New-Object -TypeName 'System.Security.Principal.NTAccount' -ArgumentList ($UsersAccountName)
            }
            Else {
                $Username = New-Object -TypeName 'System.Security.Principal.NTAccount' -ArgumentList ($U)
            }

            # Set/Add/Remove/Replace permissions and log the changes
            $Rule = New-Object -TypeName 'System.Security.AccessControl.FileSystemAccessRule' -ArgumentList ($Username, $FileSystemRights, $InheritanceFlag, $PropagationFlag, $Allow)
            Switch ($Method) {
                'Add' {
                    Write-ADTLogEntry -Message "Setting permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]."
                    $Acl.AddAccessRule($Rule)
                    Break
                }
                'Set' {
                    Write-ADTLogEntry -Message "Setting permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]."
                    $Acl.SetAccessRule($Rule)
                    Break
                }
                'Reset' {
                    Write-ADTLogEntry -Message "Setting permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]."
                    $Acl.ResetAccessRule($Rule)
                    Break
                }
                'Remove' {
                    Write-ADTLogEntry -Message "Removing permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]."
                    $Acl.RemoveAccessRule($Rule)
                    Break
                }
                'RemoveSpecific' {
                    Write-ADTLogEntry -Message "Removing permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]."
                    $Acl.RemoveAccessRuleSpecific($Rule)
                    Break
                }
                'RemoveAll' {
                    Write-ADTLogEntry -Message "Removing permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]."
                    $Acl.RemoveAccessRuleAll($Rule)
                    Break
                }
            }
        }
        # Use the prepared ACL
        $null = Set-Acl -Path $Path -AclObject $Acl -ErrorAction 'Stop'
    }

    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
