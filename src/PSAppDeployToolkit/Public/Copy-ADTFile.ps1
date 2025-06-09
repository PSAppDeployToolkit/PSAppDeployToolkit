#-----------------------------------------------------------------------------
#
# MARK: Copy-ADTFile
#
#-----------------------------------------------------------------------------

function Copy-ADTFile
{
    <#
    .SYNOPSIS
        Copies files and directories from a source to a destination.

    .DESCRIPTION
        Copies files and directories from a source to a destination. This function supports recursive copying, overwriting existing files, and returning the copied items.

    .PARAMETER Path
        Path of the file to copy. Multiple paths can be specified.

    .PARAMETER Destination
        Destination Path of the file to copy.

    .PARAMETER Recurse
        Copy files in subdirectories.

    .PARAMETER Flatten
        Flattens the files into the root destination directory.

    .PARAMETER ContinueFileCopyOnError
        Continue copying files if an error is encountered. This will continue the deployment script and will warn about files that failed to be copied.

    .PARAMETER FileCopyMode
        Select from 'Native' or 'Robocopy'. Default is configured in config.psd1. Note that Robocopy supports * in file names, but not folders, in source paths.

    .PARAMETER RobocopyParams
        Override the default Robocopy parameters.

    .PARAMETER RobocopyAdditionalParams
        Append to the default Robocopy parameters.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Copy-ADTFile -Path 'C:\Path\file.txt' -Destination 'D:\Destination\file.txt'

        Copies the file 'file.txt' from 'C:\Path' to 'D:\Destination'.

    .EXAMPLE
        Copy-ADTFile -Path 'C:\Path\Folder' -Destination 'D:\Destination\Folder' -Recurse

        Recursively copies the folder 'Folder' from 'C:\Path' to 'D:\Destination'.

    .EXAMPLE
        Copy-ADTFile -Path 'C:\Path\file.txt' -Destination 'D:\Destination\file.txt'

        Copies the file 'file.txt' from 'C:\Path' to 'D:\Destination', overwriting the destination file if it exists.

    .EXAMPLE
        Copy-ADTFile -Path "$($adtSession.DirFiles)\*" -Destination C:\some\random\file\path

        Copies all files within the active session's Files folder to 'C:\some\random\file\path', overwriting the destination file if it exists.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Copy-ADTFile
    #>

    [CmdletBinding(SupportsShouldProcess = $false)]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Destination,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Recurse,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Flatten,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ContinueFileCopyOnError,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Native', 'Robocopy')]
        [System.String]$FileCopyMode,

        [Parameter(Mandatory = $false)]
        [System.String]$RobocopyParams = '/NJH /NJS /NS /NC /NP /NDL /FP /IA:RASHCNETO /IS /IT /IM /XX /MT:4 /R:1 /W:1',

        [Parameter(Mandatory = $false)]
        [System.String]$RobocopyAdditionalParams

    )

    begin
    {
        # If a FileCopyMode hasn't been specified, potentially initialize the module so we can get it from the config.
        if (!$PSBoundParameters.ContainsKey('FileCopyMode'))
        {
            $null = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
            $FileCopyMode = (Get-ADTConfig).Toolkit.FileCopyMode
        }

        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

        # Verify that Robocopy can be used if selected
        if ($FileCopyMode -eq 'Robocopy')
        {
            # Check if Robocopy is on the system.
            if (Test-Path -LiteralPath "$([System.Environment]::SystemDirectory)\Robocopy.exe" -PathType Leaf)
            {
                # Disable Robocopy if $Path has a folder containing a * wildcard.
                if ($Path -match '\*.*\\')
                {
                    Write-ADTLogEntry -Message "Asterisk wildcard specified in folder portion of path variable. Falling back to native PowerShell method." -Severity 2
                    $FileCopyMode = 'Native'
                }
                # Don't just check for an extension here, also check for base name without extension to allow copying to a directory such as .config.
                elseif ([System.IO.Path]::HasExtension($Destination) -and [System.IO.Path]::GetFileNameWithoutExtension($Destination) -and !(Test-Path -LiteralPath $Destination -PathType Container))
                {
                    Write-ADTLogEntry -Message "Destination path appears to be a file. Falling back to native PowerShell method." -Severity 2
                    $FileCopyMode = 'Native'
                }
                else
                {
                    $robocopyCommand = "$([System.Environment]::SystemDirectory)\Robocopy.exe"

                    if ($Recurse -and !$Flatten)
                    {
                        # Add /E to Robocopy parameters if it is not already included.
                        if ($RobocopyParams -notmatch '/E(\s+|$)' -and $RobocopyAdditionalParams -notmatch '/E(\s+|$)')
                        {
                            $RobocopyParams = $RobocopyParams + " /E"
                        }
                    }
                    else
                    {
                        # Ensure that /E is not included in the Robocopy parameters as it will copy recursive folders.
                        $RobocopyParams = $RobocopyParams -replace '/E(\s+|$)'
                        $RobocopyAdditionalParams = $RobocopyAdditionalParams -replace '/E(\s+|$)'
                    }

                    # Older versions of Robocopy do not support /IM, remove if unsupported.
                    if ((& $robocopyCommand /?) -notmatch '/IM\s')
                    {
                        $RobocopyParams = $RobocopyParams -replace '/IM(\s+|$)'
                        $RobocopyAdditionalParams = $RobocopyAdditionalParams -replace '/IM(\s+|$)'
                    }
                }
            }
            else
            {
                Write-ADTLogEntry -Message "Robocopy is not available on this system. Falling back to native PowerShell method." -Severity 2
                $FileCopyMode = 'Native'
            }
        }
    }

    process
    {
        if ($FileCopyMode -eq 'Robocopy')
        {
            foreach ($srcPath in $Path)
            {
                try
                {
                    if (!(Get-ChildItem -Path $srcPath -Recurse -Force))
                    {
                        if (!$ContinueFileCopyOnError)
                        {
                            Write-ADTLogEntry -Message "Source path [$srcPath] not found." -Severity 2
                            $naerParams = @{
                                Exception = [System.IO.FileNotFoundException]::new("Source path [$srcPath] not found.")
                                Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                                ErrorId = 'FileNotFoundError'
                                TargetObject = $srcPath
                                RecommendedAction = 'Please verify that the path is accessible and try again.'
                            }
                            Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
                        }
                        Write-ADTLogEntry -Message "Source path [$srcPath] not found. Will continue due to ContinueFileCopyOnError = `$true." -Severity 2
                        continue
                    }

                    # Pre-create destination folder if it does not exist; Robocopy will auto-create non-existent destination folders, but pre-creating ensures we can use Resolve-Path.
                    if (!(Test-Path -LiteralPath $Destination -PathType Container))
                    {
                        Write-ADTLogEntry -Message "Destination assumed to be a folder which does not exist, creating destination folder [$Destination]."
                        $null = New-Item -Path $Destination -Type Directory -Force
                    }

                    # If source exists as a folder, append the last subfolder to the destination, so that Robocopy produces similar results to native PowerShell.
                    if (Test-Path -Path $srcPath -PathType Container)
                    {
                        # Trim ending backslash from paths which can cause problems with Robocopy.
                        # Resolve paths in case relative paths beggining with .\, ..\, or \ are used.
                        # Strip Microsoft.PowerShell.Core\FileSystem:: from the beginning of the resulting string, since Resolve-Path adds this to UNC paths.
                        $robocopySource = (Get-Item -Path $srcPath.TrimEnd('\') -Force).FullName -replace '^Microsoft\.PowerShell\.Core\\FileSystem::'
                        $robocopyDestination = Join-Path ((Get-Item -LiteralPath $Destination -Force).FullName -replace '^Microsoft\.PowerShell\.Core\\FileSystem::') (Split-Path -Path $srcPath -Leaf)
                        $robocopyFile = '*'
                    }
                    else
                    {
                        # Else assume source is a file and split args to the format <SourceFolder> <DestinationFolder> <FileName>.
                        # Trim ending backslash from paths which can cause problems with Robocopy.
                        # Resolve paths in case relative paths beggining with .\, ..\, or \ are used.
                        # Strip Microsoft.PowerShell.Core\FileSystem:: from the beginning of the resulting string, since Resolve-Path adds this to UNC paths.
                        $ParentPath = Split-Path -Path $srcPath -Parent
                        $robocopySource = if ([System.String]::IsNullOrWhiteSpace($ParentPath))
                        {
                            $ExecutionContext.SessionState.Path.CurrentLocation.Path
                        }
                        else
                        {
                           (Get-Item -LiteralPath $ParentPath -Force).FullName -replace '^Microsoft\.PowerShell\.Core\\FileSystem::'
                        }
                        $robocopyDestination = (Get-Item -LiteralPath $Destination.TrimEnd('\') -Force).FullName -replace '^Microsoft\.PowerShell\.Core\\FileSystem::'
                        $robocopyFile = (Split-Path -Path $srcPath -Leaf)
                    }

                    # Set up copy operation.
                    if ($Flatten)
                    {
                        # Copy all files from the root source folder.
                        $copyFileSplat = @{
                            Destination = $Destination  # Use the original destination path, not $robocopyDestination which could have had a subfolder appended to it.
                            Recurse = $false  # Disable recursion as this will create subfolders in the destination.
                            Flatten = $false  # Disable flattening to prevent infinite loops.
                            ContinueFileCopyOnError = $ContinueFileCopyOnError
                            FileCopyMode = $FileCopyMode
                            RobocopyParams = $RobocopyParams
                            RobocopyAdditionalParams = $RobocopyAdditionalParams
                        }
                        if ($PSBoundParameters.ContainsKey('ErrorAction'))
                        {
                            $copyFileSplat.ErrorAction = $PSBoundParameters.ErrorAction
                        }
                        Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination] root folder, flattened."
                        if (Get-ChildItem -Path (Join-Path $robocopySource $robocopyFile) -File -Force -ErrorAction Ignore)
                        {
                            Copy-ADTFile @copyFileSplat -Path (Join-Path $robocopySource $robocopyFile)
                        }

                        # Copy all files from subfolders, appending file name to subfolder path and repeat Copy-ADTFile.
                        Get-ChildItem -LiteralPath $robocopySource -Directory -Recurse -Force -ErrorAction Ignore | & {
                            process
                            {
                                if (Get-ChildItem -Path (Join-Path $_.FullName $robocopyFile) -File -Force -ErrorAction Ignore)
                                {
                                    Copy-ADTFile @copyFileSplat -Path (Join-Path $_.FullName $robocopyFile)
                                }
                            }
                        }

                        # Skip to next $srcPath in $Path since we have handed off all copy tasks to separate executions of the function.
                        continue
                    }
                    elseif ($Recurse)
                    {
                        Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination]."
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Copying file(s) in path [$srcPath] to destination [$Destination]."
                    }

                    # Create new directory if it doesn't exist.
                    if (!(Test-Path -LiteralPath $robocopyDestination -PathType Container))
                    {
                        $null = New-Item -Path $robocopyDestination -Type Directory -Force
                    }

                    # Backup destination folder attributes in case known Robocopy bug overwrites them.
                    $destFolderAttributes = [System.IO.File]::GetAttributes($robocopyDestination)

                    # Begin copy operation.
                    $robocopyArgs = "`"$robocopySource`" `"$robocopyDestination`" `"$robocopyFile`" $RobocopyParams $RobocopyAdditionalParams"
                    Write-ADTLogEntry -Message "Executing Robocopy command: $robocopyCommand $robocopyArgs"
                    $robocopyResult = Start-ADTProcess -FilePath $robocopyCommand -ArgumentList $robocopyArgs -CreateNoWindow -PassThru -SuccessExitCodes 0, 1, 2, 3, 4, 5, 6, 7, 8 -ErrorAction Ignore

                    # Trim the last line plus leading whitespace from each line of Robocopy output.
                    $robocopyOutput = if ($robocopyResult.StdOut) { $robocopyResult.StdOut.Trim() -Replace '\n\s+', "`n" }
                    Write-ADTLogEntry -Message "Robocopy output:`n$robocopyOutput"

                    # Restore folder attributes in case Robocopy overwrote them.
                    try
                    {
                        [System.IO.File]::SetAttributes($robocopyDestination, $destFolderAttributes)
                    }
                    catch
                    {
                        Write-ADTLogEntry -Message "Failed to apply attributes [$destFolderAttributes] destination folder [$robocopyDestination]: $($_.Exception.Message)" -Severity 2
                    }

                    # Process the resulting exit code.
                    switch ($robocopyResult.ExitCode)
                    {
                        0 { Write-ADTLogEntry -Message "Robocopy completed. No files were copied. No failure was encountered. No files were mismatched. The files already exist in the destination directory; therefore, the copy operation was skipped."; break }
                        1 { Write-ADTLogEntry -Message "Robocopy completed. All files were copied successfully."; break }
                        2 { Write-ADTLogEntry -Message "Robocopy completed. There are some additional files in the destination directory that aren't present in the source directory. No files were copied."; break }
                        3 { Write-ADTLogEntry -Message "Robocopy completed. Some files were copied. Additional files were present. No failure was encountered."; break }
                        4 { Write-ADTLogEntry -Message "Robocopy completed. Some Mismatched files or directories were detected. Examine the output log. Housekeeping might be required." -Severity 2; break }
                        5 { Write-ADTLogEntry -Message "Robocopy completed. Some files were copied. Some files were mismatched. No failure was encountered."; break }
                        6 { Write-ADTLogEntry -Message "Robocopy completed. Additional files and mismatched files exist. No files were copied and no failures were encountered meaning that the files already exist in the destination directory." -Severity 2; break }
                        7 { Write-ADTLogEntry -Message "Robocopy completed. Files were copied, a file mismatch was present, and additional files were present." -Severity 2; break }
                        8 { Write-ADTLogEntry -Message "Robocopy completed. Several files didn't copy." -Severity 2; break }
                        16
                        {
                            Write-ADTLogEntry -Message "Robocopy error [$($robocopyResult.ExitCode)]: Serious error. Robocopy did not copy any files. Either a usage error or an error due to insufficient access privileges on the source or destination directories." -Severity 3
                            if (!$ContinueFileCopyOnError)
                            {
                                $naerParams = @{
                                    Exception = [System.Management.Automation.ApplicationFailedException]::new("Robocopy error $($robocopyResult.ExitCode): Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $robocopyOutput")
                                    Category = [System.Management.Automation.ErrorCategory]::OperationStopped
                                    ErrorId = 'RobocopyError'
                                    TargetObject = $srcPath
                                    RecommendedAction = "Please verify that Path and Destination are accessible and try again."
                                }
                                Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
                            }
                            break
                        }
                        default
                        {
                            Write-ADTLogEntry -Message "Robocopy error [$($robocopyResult.ExitCode)]. Unknown Robocopy error." -Severity 3
                            if (!$ContinueFileCopyOnError)
                            {
                                $naerParams = @{
                                    Exception = [System.Management.Automation.ApplicationFailedException]::new("Robocopy error $($robocopyResult.ExitCode): Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $robocopyOutput")
                                    Category = [System.Management.Automation.ErrorCategory]::OperationStopped
                                    ErrorId = 'RobocopyError'
                                    TargetObject = $srcPath
                                    RecommendedAction = "Please verify that Path and Destination are accessible and try again."
                                }
                                Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
                            }
                            break
                        }
                    }
                }
                catch
                {
                    Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to copy file(s) in path [$srcPath] to destination [$Destination]."
                    if (!$ContinueFileCopyOnError)
                    {
                        Write-ADTLogEntry -Message 'ContinueFileCopyOnError not specified, exiting function.'
                        return
                    }
                }
            }
        }
        elseif ($FileCopyMode -eq 'Native')
        {
            foreach ($srcPath in $Path)
            {
                try
                {
                    try
                    {
                        # If destination has no extension, or if it has an extension only and no name (e.g. a .config folder) and the destination folder does not exist.
                        if ((![System.IO.Path]::HasExtension($Destination) -or ([System.IO.Path]::HasExtension($Destination) -and ![System.IO.Path]::GetFileNameWithoutExtension($Destination))) -and !(Test-Path -LiteralPath $Destination -PathType Container))
                        {
                            Write-ADTLogEntry -Message "Destination assumed to be a folder which does not exist, creating destination folder [$Destination]."
                            $null = New-Item -Path $Destination -Type Directory -Force
                        }

                        # If destination appears to be a file name but parent folder does not exist, create it.
                        if ([System.IO.Path]::HasExtension($Destination) -and [System.IO.Path]::GetFileNameWithoutExtension($Destination) -and !(Test-Path -LiteralPath ($destinationParent = Split-Path $Destination -Parent) -PathType Container))
                        {
                            Write-ADTLogEntry -Message "Destination assumed to be a file whose parent folder does not exist, creating destination folder [$destinationParent]."
                            $null = New-Item -Path $destinationParent -Type Directory -Force
                        }

                        # Set up parameters for Copy-Item operation.
                        $ciParams = @{
                            Destination = $Destination
                            Force = $true
                        }
                        if ($ContinueFileCopyOnError)
                        {
                            $ciParams.Add('ErrorAction', [System.Management.Automation.ActionPreference]::SilentlyContinue)
                            $ciParams.Add('ErrorVariable', 'FileCopyError')
                        }

                        # Perform copy operation.
                        $null = if ($Flatten)
                        {
                            Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination] root folder, flattened."
                            if ($srcPaths = Get-ChildItem -Path $srcPath -File -Recurse -Force -ErrorAction Ignore)
                            {
                                Copy-Item -LiteralPath $srcPaths.PSPath @ciParams
                            }
                        }
                        elseif ($Recurse)
                        {
                            Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination]."
                            Copy-Item -Path $srcPath -Recurse @ciParams
                        }
                        else
                        {
                            Write-ADTLogEntry -Message "Copying file in path [$srcPath] to destination [$Destination]."
                            Copy-Item -Path $srcPath @ciParams
                        }

                        # Measure success.
                        if ($ContinueFileCopyOnError -and $FileCopyError.Count)
                        {
                            Write-ADTLogEntry -Message "The following warnings were detected while copying file(s) in path [$srcPath] to destination [$Destination].`n`n$([System.String]::Join("`n", $FileCopyError.Exception.Message))" -Severity 2
                        }
                        else
                        {
                            Write-ADTLogEntry -Message 'File copy completed successfully.'
                        }
                    }
                    catch
                    {
                        Write-Error -ErrorRecord $_
                    }
                }
                catch
                {
                    Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to copy file(s) in path [$srcPath] to destination [$Destination]."
                    if (!$ContinueFileCopyOnError)
                    {
                        Write-ADTLogEntry -Message 'ContinueFileCopyOnError not specified, exiting function.'
                        return
                    }
                }
            }
        }
    }
    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
