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
        If Path is an array, continue copying files if an error is encountered.

    .PARAMETER FileCopyMode
        Select from 'Native' or 'Robocopy'. Default is configured in config.psd1. Note that Robocopy supports * in file names, but not folders, in source paths.

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
        Copy-ADTFile -Path 'C:\Path\file.txt' -Destination 'D:\Destination\file.txt' -Force

        Copies the file 'file.txt' from 'C:\Path' to 'D:\Destination', overwriting the destination file if it exists.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
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
        [System.Management.Automation.SwitchParameter]$Recurse = $false,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Flatten,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ContinueFileCopyOnError,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Native', 'Robocopy')]
        [System.String]$FileCopyMode = (Get-ADTConfig).Toolkit.FileCopyMode

    )

    dynamicparam
    {
        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        if ($FileCopyMode -eq 'Robocopy')
        {
            # Define the RobocopyParams parameter
            $paramDictionary.Add('RobocopyParams', [System.Management.Automation.RuntimeDefinedParameter]::new(
                    'RobocopyParams', [System.String], $(
                        [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false; HelpMessage = 'Override the default Robocopy parameters when FileCopyMode = Robocopy. Default value is: /NJH /NJS /NS /NC /NP /NDL /FP /IS /IT /IM /XX /MT:4 /R:1 /W:1' }
                        [System.Management.Automation.AllowEmptyStringAttribute]::new()
                    )
                ))

            # Define the RobocopyAdditionalParams parameter
            $paramDictionary.Add('RobocopyAdditionalParams', [System.Management.Automation.RuntimeDefinedParameter]::new(
                    'RobocopyAdditionalParams', [System.String], $(
                        [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false; HelpMessage = 'Append to the default Robocopy parameters when FileCopyMode = Robocopy.' }
                        [System.Management.Automation.AllowEmptyStringAttribute]::new()
                    )
                ))
        }

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

        # Check if Robocopy is on the system.
        if ($FileCopyMode -eq 'Robocopy')
        {
            if (& $Script:CommandTable.'Test-Path' -Path "$([System.Environment]::SystemDirectory)\Robocopy.exe" -PathType Leaf)
            {
                $robocopyCommand = "$([System.Environment]::SystemDirectory)\Robocopy.exe"
                $RobocopyParams = if ($PSBoundParameters.ContainsKey('RobocopyParams'))
                {
                    $PSBoundParameters.RobocopyParams
                }
                else
                {
                    '/NJH /NJS /NS /NC /NP /NDL /FP /IS /IT /IM /XX /MT:4 /R:1 /W:1'
                }
                $RobocopyAdditionalParams = if ($PSBoundParameters.ContainsKey('RobocopyAdditionalParams'))
                {
                    $PSBoundParameters.RobocopyAdditionalParams
                }
            }
            else
            {
                Write-ADTLogEntry -Message "Robocopy is not available on this system. Falling back to native PowerShell method." -Severity 2
                $FileCopyMode = 'Native'
            }

            # Disable Robocopy if $Path has a folder containing a * wildcard.
            if ($Path -match '\*.*\\')
            {
                Write-ADTLogEntry -Message "Asterisk wildcard specified in folder portion of path variable. Falling back to native PowerShell method." -Severity 2
                $FileCopyMode = 'Native'
            }
            # Don't just check for an extension here, also check for base name without extension to allow copying to a directory such as .config.
            elseif ([System.IO.Path]::HasExtension($Destination) -and [System.IO.Path]::GetFileNameWithoutExtension($Destination) -and !(& $Script:CommandTable.'Test-Path' -LiteralPath $Destination -PathType Container))
            {
                Write-ADTLogEntry -Message "Destination path appears to be a file. Falling back to native PowerShell method." -Severity 2
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
                    # Pre-create destination folder if it does not exist; Robocopy will auto-create non-existent destination folders, but pre-creating ensures we can use Resolve-Path
                    if (-not (& $Script:CommandTable.'Test-Path' -LiteralPath $Destination -PathType Container))
                    {
                        Write-ADTLogEntry -Message "Destination assumed to be a folder which does not exist, creating destination folder [$Destination]."
                        $null = & $Script:CommandTable.'New-Item' -Path $Destination -Type 'Directory' -Force
                    }
                    if (& $Script:CommandTable.'Test-Path' -LiteralPath $srcPath -PathType Container)
                    {
                        # If source exists as a folder, append the last subfolder to the destination, so that Robocopy produces similar results to native Powershell
                        # Trim ending backslash from paths which can cause problems with Robocopy
                        # Resolve paths in case relative paths beggining with .\, ..\, or \ are used
                        # Strip Microsoft.PowerShell.Core\FileSystem:: from the beginning of the resulting string, since Resolve-Path adds this to UNC paths
                        $robocopySource = (& $Script:CommandTable.'Resolve-Path' -LiteralPath $srcPath.TrimEnd('\')).Path -replace '^Microsoft\.PowerShell\.Core\\FileSystem::'
                        $robocopyDestination = & $Script:CommandTable.'Join-Path' ((& $Script:CommandTable.'Resolve-Path' -LiteralPath $Destination).Path -replace '^Microsoft\.PowerShell\.Core\\FileSystem::') (& $Script:CommandTable.'Split-Path' -Path $srcPath -Leaf)
                        $robocopyFile = '*'
                    }
                    else
                    {
                        # Else assume source is a file and split args to the format <SourceFolder> <DestinationFolder> <FileName>
                        # Trim ending backslash from paths which can cause problems with Robocopy
                        # Resolve paths in case relative paths beggining with .\, ..\, or \ are used
                        # Strip Microsoft.PowerShell.Core\FileSystem:: from the beginning of the resulting string, since Resolve-Path adds this to UNC paths
                        $ParentPath = & $Script:CommandTable.'Split-Path' -Path $srcPath -Parent
                        $robocopySource = if ([System.String]::IsNullOrWhiteSpace($ParentPath))
                        {
                            $PWD
                        }
                        else
                        {
                           (& $Script:CommandTable.'Resolve-Path' -LiteralPath $ParentPath).Path -replace '^Microsoft\.PowerShell\.Core\\FileSystem::'
                        }
                        $robocopyDestination = (& $Script:CommandTable.'Resolve-Path' -LiteralPath $Destination.TrimEnd('\')).Path -replace '^Microsoft\.PowerShell\.Core\\FileSystem::'
                        $robocopyFile = (& $Script:CommandTable.'Split-Path' -Path $srcPath -Leaf)
                    }
                    if ($Flatten)
                    {
                        Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination] root folder, flattened."
                        [Hashtable]$copyFileSplat = @{
                            Path                     = (& $Script:CommandTable.'Join-Path' $robocopySource $robocopyFile) # This will ensure that the source dir will have \* appended if it was a folder (which prevents creation of a folder at the destination), or keeps the original file name if it was a file
                            Destination              = $Destination # Use the original destination path, not $robocopyDestination which could have had a subfolder appended to it
                            Recurse                  = $false # Disable recursion as this will create subfolders in the destination
                            Flatten                  = $false # Disable flattening to prevent infinite loops
                            ContinueFileCopyOnError  = $ContinueFileCopyOnError
                            FileCopyMode             = $FileCopyMode
                            RobocopyParams           = $RobocopyParams
                            RobocopyAdditionalParams = $RobocopyAdditionalParams
                        }
                        if ($PSBoundParameters.ContainsKey('ErrorAction'))
                        {
                            $copyFileSplat.ErrorAction = $PSBoundParameters.ErrorAction
                        }
                        # Copy all files from the root source folder
                        Copy-ADTFile @copyFileSplat
                        # Copy all files from subfolders
                        & $Script:CommandTable.'Get-ChildItem' -Path $robocopySource -Directory -Recurse -Force -ErrorAction 'Ignore' | & $Script:CommandTable.'ForEach-Object' {
                            # Append file name to subfolder path and repeat Copy-ADTFile
                            $copyFileSplat.Path = & $Script:CommandTable.'Join-Path' $_.FullName $robocopyFile
                            Copy-ADTFile @copyFileSplat
                        }
                        # Skip to next $SrcPath in $Path since we have handed off all copy tasks to separate executions of the function
                        continue
                    }
                    if ($Recurse)
                    {
                        # Add /E to Robocopy parameters if it is not already included
                        if ($RobocopyParams -notmatch '/E(\s+|$)' -and $RobocopyAdditionalParams -notmatch '/E(\s+|$)')
                        {
                            $RobocopyParams = $RobocopyParams + " /E"
                        }
                        Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination]."
                    }
                    else
                    {
                        # Ensure that /E is not included in the Robocopy parameters as it will copy recursive folders
                        $RobocopyParams = $RobocopyParams -replace '/E(\s+|$)'
                        $RobocopyAdditionalParams = $RobocopyAdditionalParams -replace '/E(\s+|$)'
                        Write-ADTLogEntry -Message "Copying file(s) in path [$srcPath] to destination [$Destination]."
                    }

                    # Older versions of Robocopy do not support /IM, remove if unsupported
                    if (!((&Robocopy /?) -match '/IM\s'))
                    {
                        $RobocopyParams = $RobocopyParams -replace '/IM(\s+|$)'
                        $RobocopyAdditionalParams = $RobocopyAdditionalParams -replace '/IM(\s+|$)'
                    }

                    if (-not (& $Script:CommandTable.'Test-Path' -LiteralPath $robocopyDestination -PathType Container))
                    {
                        $null = & $Script:CommandTable.'New-Item' -Path $robocopyDestination -Type 'Directory' -Force
                    }

                    # Backup destination folder attributes in case known Robocopy bug overwrites them
                    $destFolderAttributes = [System.IO.File]::GetAttributes($robocopyDestination)

                    $robocopyArgs = "$RobocopyParams $RobocopyAdditionalParams `"$robocopySource`" `"$robocopyDestination`" `"$robocopyFile`""
                    Write-ADTLogEntry -Message "Executing Robocopy command: $robocopyCommand $robocopyArgs"
                    $robocopyResult = Start-ADTProcess -Path $robocopyCommand -Parameters $robocopyArgs -CreateNoWindow -NoExitOnProcessFailure -PassThru -SuccessCodes 0, 1, 2, 3, 4, 5, 6, 7, 8 -ErrorAction Ignore
                    # Trim the last line plus leading whitespace from each line of Robocopy output
                    $robocopyOutput = $robocopyResult.StdOut.Trim() -Replace '\n\s+', "`n"
                    Write-ADTLogEntry -Message "Robocopy output:`n$robocopyOutput"

                    # Restore folder attributes in case Robocopy overwrote them
                    try
                    {
                        [System.IO.File]::SetAttributes($robocopyDestination, $destFolderAttributes)
                    }
                    catch
                    {
                        Write-ADTLogEntry -Message "Failed to apply attributes $destFolderAttributes destination folder $robocopyDestination : $($_.Exception.Message)" -Severity 2
                    }

                    switch ($robocopyResult.ExitCode)
                    {
                        0 { Write-ADTLogEntry -Message "Robocopy completed. No files were copied. No failure was encountered. No files were mismatched. The files already exist in the destination directory; therefore, the copy operation was skipped." }
                        1 { Write-ADTLogEntry -Message "Robocopy completed. All files were copied successfully." }
                        2 { Write-ADTLogEntry -Message "Robocopy completed. There are some additional files in the destination directory that aren't present in the source directory. No files were copied." }
                        3 { Write-ADTLogEntry -Message "Robocopy completed. Some files were copied. Additional files were present. No failure was encountered." }
                        4 { Write-ADTLogEntry -Message "Robocopy completed. Some Mismatched files or directories were detected. Examine the output log. Housekeeping might be required." -Severity 2 }
                        5 { Write-ADTLogEntry -Message "Robocopy completed. Some files were copied. Some files were mismatched. No failure was encountered." }
                        6 { Write-ADTLogEntry -Message "Robocopy completed. Additional files and mismatched files exist. No files were copied and no failures were encountered meaning that the files already exist in the destination directory." -Severity 2 }
                        7 { Write-ADTLogEntry -Message "Robocopy completed. Files were copied, a file mismatch was present, and additional files were present." -Severity 2 }
                        8 { Write-ADTLogEntry -Message "Robocopy completed. Several files didn't copy." -Severity 2 }
                        16
                        {
                            Write-ADTLogEntry -Message "Robocopy error $($robocopyResult.ExitCode): Serious error. Robocopy did not copy any files. Either a usage error or an error due to insufficient access privileges on the source or destination directories.." -Severity 3
                            if (-not $ContinueFileCopyOnError)
                            {
                                $naerParams = @{
                                    Exception         = [System.Management.Automation.ApplicationFailedException]::new("Robocopy error $($robocopyResult.ExitCode): Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $robocopyOutput")
                                    Category          = [System.Management.Automation.ErrorCategory]::OperationStopped
                                    ErrorId           = 'RobocopyError'
                                    TargetObject      = $srcPath
                                    RecommendedAction = "Please verify that Path and Destination are accessible and try again."
                                }
                                & $Script:CommandTable.'Write-Error' -ErrorRecord (New-ADTErrorRecord @naerParams)
                            }
                        }
                        default
                        {
                            Write-ADTLogEntry -Message "Robocopy error $($robocopyResult.ExitCode). Unknown Robocopy error." -Severity 3
                            if (-not $ContinueFileCopyOnError)
                            {
                                $naerParams = @{
                                    Exception         = [System.Management.Automation.ApplicationFailedException]::new("Robocopy error $($robocopyResult.ExitCode): Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $robocopyOutput")
                                    Category          = [System.Management.Automation.ErrorCategory]::OperationStopped
                                    ErrorId           = 'RobocopyError'
                                    TargetObject      = $srcPath
                                    RecommendedAction = "Please verify that Path and Destination are accessible and try again."
                                }
                                & $Script:CommandTable.'Write-Error' -ErrorRecord (New-ADTErrorRecord @naerParams)
                            }
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
                        if ((![System.IO.Path]::HasExtension($Destination) -or ([System.IO.Path]::HasExtension($Destination) -and ![System.IO.Path]::GetFileNameWithoutExtension($Destination))) -and !(& $Script:CommandTable.'Test-Path' -LiteralPath $Destination -PathType Container))
                        {
                            Write-ADTLogEntry -Message "Destination assumed to be a folder which does not exist, creating destination folder [$Destination]."
                            $null = & $Script:CommandTable.'New-Item' -Path $Destination -Type Directory -Force
                        }

                        # If destination appears to be a file name but parent folder does not exist, create it.
                        if ([System.IO.Path]::HasExtension($Destination) -and [System.IO.Path]::GetFileNameWithoutExtension($Destination) -and !(& $Script:CommandTable.'Test-Path' -LiteralPath ($destinationParent = & $Script:CommandTable.'Split-Path' $Destination -Parent) -PathType Container))
                        {
                            Write-ADTLogEntry -Message "Destination assumed to be a file whose parent folder does not exist, creating destination folder [$destinationParent]."
                            $null = & $Script:CommandTable.'New-Item' -Path $destinationParent -Type Directory -Force
                        }

                        # Set up parameters for Copy-Item operation.
                        $ciParams = if ($ContinueFileCopyOnError)
                        {
                            @{
                                ErrorAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
                                ErrorVariable = 'FileCopyError'
                            }
                        }
                        else
                        {
                            @{}
                        }

                        # Perform copy operation.
                        if ($Flatten)
                        {
                            Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination] root folder, flattened."
                            $null = & $Script:CommandTable.'Get-ChildItem' -Path $srcPath -File -Recurse -Force -ErrorAction 'Ignore' | & {
                                process
                                {
                                    & $Script:CommandTable.'Copy-Item' -Path $_.FullName -Destination $Destination -Force @ciParams
                                }
                            }
                        }
                        elseif ($Recurse)
                        {
                            Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination]."
                            $null = & $Script:CommandTable.'Copy-Item' -Path $srcPath -Destination $Destination -Force -Recurse @ciParams
                        }
                        else
                        {
                            Write-ADTLogEntry -Message "Copying file in path [$srcPath] to destination [$Destination]."
                            $null = & $Script:CommandTable.'Copy-Item' -Path $srcPath -Destination $Destination -Force @ciParams
                        }
                        
                        # Measure success.
                        if ($ContinueFileCopyOnError -and (& $Script:CommandTable.'Test-Path' -LiteralPath Microsoft.PowerShell.Core\Variable::FileCopyError))
                        {
                            Write-ADTLogEntry -Message "The following warnings were detected while copying file(s) in path [$srcPath] to destination [$Destination].`n$FileCopyError" -Severity 2
                        }
                        else
                        {
                            Write-ADTLogEntry -Message 'File copy completed successfully.'
                        }
                    }
                    catch
                    {
                        & $Script:CommandTable.'Write-Error' -ErrorRecord $_
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
