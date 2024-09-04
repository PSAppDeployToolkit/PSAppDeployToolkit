#-----------------------------------------------------------------------------
#
# MARK: Copy-File
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
        Continue copying files if an error is encountered. This will continue the deployment script and will warn about files that failed to be copied. Default is: $false.

    .PARAMETER UseRobocopy
        Use Robocopy to copy files rather than native PowerShell method. Supports * in file names, but not folders, in source paths. Default is configured in config.psd1.

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
    param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullorEmpty()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullorEmpty()]
        [System.String]$Destination,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Recurse = $false,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Flatten,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ContinueFileCopyOnError,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$UseRobocopy = (Get-ADTConfig).Toolkit.UseRobocopy,

        [Parameter(Mandatory = $false)]
        [System.String]$RobocopyParams = '/NJH /NJS /NS /NC /NP /NDL /FP /IS /IT /IM /XX /MT:4 /R:1 /W:1',

        [Parameter(Mandatory = $false)]
        [System.String]$RobocopyAdditionalParams
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

        # Check if Robocopy is on the system
        if ($UseRobocopy)
        {
            if (& $Script:CommandTable.'Test-Path' -Path "$env:SystemRoot\System32\Robocopy.exe" -PathType Leaf)
            {
                $RobocopyCommand = "$env:SystemRoot\System32\Robocopy.exe"
            }
            else
            {
                $UseRobocopy = $false
                Write-ADTLogEntry "Robocopy is not available on this system. Falling back to native PowerShell method." -Severity 2
            }
        }
        else
        {
            $UseRobocopy = $false
        }
    }
    process
    {
        foreach ($srcPath in $Path)
        {
            try
            {
                $FileCopyError = $null
                $UseRobocopyThis = $UseRobocopy
                if ($UseRobocopyThis)
                {
                    # Disable Robocopy if $Path has a folder containing a * wildcard
                    if ($srcPath -match '\*.*\\')
                    {
                        $UseRobocopyThis = $false
                        Write-ADTLogEntry "Asterisk wildcard specified in folder portion of path variable. Falling back to native PowerShell method." -Severity 2
                    }
                    # Don't just check for an extension here, also check for base name without extension to allow copying to a directory such as .config
                    if ([IO.Path]::HasExtension($Destination) -and [IO.Path]::GetFileNameWithoutExtension($Destination) -and -not (& $Script:CommandTable.'Test-Path' -LiteralPath $Destination -PathType Container))
                    {
                        $UseRobocopyThis = $false
                        Write-ADTLogEntry "Destination path appears to be a file. Falling back to native PowerShell method." -Severity 2
                    }
                    if ($UseRobocopyThis)
                    {

                        # Pre-create destination folder if it does not exist; Robocopy will auto-create non-existent destination folders, but pre-creating ensures we can use Resolve-Path
                        if (-not (& $Script:CommandTable.'Test-Path' -LiteralPath $Destination -PathType Container))
                        {
                            Write-ADTLogEntry -Message "Destination assumed to be a folder which does not exist, creating destination folder [$Destination]."
                            $null = & $Script:CommandTable.'New-Item' -Path $Destination -Type 'Directory' -Force -ErrorAction 'Stop'
                        }
                        if (& $Script:CommandTable.'Test-Path' -LiteralPath $srcPath -PathType Container)
                        {
                            # If source exists as a folder, append the last subfolder to the destination, so that Robocopy produces similar results to native Powershell
                            # Trim ending backslash from paths which can cause problems with Robocopy
                            # Resolve paths in case relative paths beggining with .\, ..\, or \ are used
                            # Strip Microsoft.PowerShell.Core\FileSystem:: from the beginning of the resulting string, since Resolve-Path adds this to UNC paths
                            $RobocopySource = (& $Script:CommandTable.'Resolve-Path' -LiteralPath $srcPath.TrimEnd('\')).Path -replace '^Microsoft\.PowerShell\.Core\\FileSystem::'
                            $RobocopyDestination = & $Script:CommandTable.'Join-Path' ((& $Script:CommandTable.'Resolve-Path' -LiteralPath $Destination).Path -replace '^Microsoft\.PowerShell\.Core\\FileSystem::') (& $Script:CommandTable.'Split-Path' -Path $srcPath -Leaf)
                            $RobocopyFile = '*'
                        }
                        else
                        {
                            # Else assume source is a file and split args to the format <SourceFolder> <DestinationFolder> <FileName>
                            # Trim ending backslash from paths which can cause problems with Robocopy
                            # Resolve paths in case relative paths beggining with .\, ..\, or \ are used
                            # Strip Microsoft.PowerShell.Core\FileSystem:: from the beginning of the resulting string, since Resolve-Path adds this to UNC paths
                            $ParentPath = & $Script:CommandTable.'Split-Path' -Path $srcPath -Parent
                            if ([string]::IsNullOrEmpty($ParentPath))
                            {
                                $RobocopySource = $PWD
                            }
                            else
                            {
                                $RobocopySource = (& $Script:CommandTable.'Resolve-Path' -LiteralPath $ParentPath -ErrorAction Stop).Path -replace '^Microsoft\.PowerShell\.Core\\FileSystem::'
                            }
                            $RobocopyDestination = (& $Script:CommandTable.'Resolve-Path' -LiteralPath $Destination.TrimEnd('\') -ErrorAction Stop).Path -replace '^Microsoft\.PowerShell\.Core\\FileSystem::'
                            $RobocopyFile = (& $Script:CommandTable.'Split-Path' -Path $srcPath -Leaf)
                        }
                        if ($Flatten)
                        {
                            Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination] root folder, flattened."
                            [Hashtable]$CopyFileSplat = @{
                                Path                     = (& $Script:CommandTable.'Join-Path' $RobocopySource $RobocopyFile) # This will ensure that the source dir will have \* appended if it was a folder (which prevents creation of a folder at the destination), or keeps the original file name if it was a file
                                Destination              = $Destination # Use the original destination path, not $RobocopyDestination which could have had a subfolder appended to it
                                Recurse                  = $false # Disable recursion as this will create subfolders in the destination
                                Flatten                  = $false # Disable flattening to prevent infinite loops
                                ContinueFileCopyOnError  = $ContinueFileCopyOnError
                                UseRobocopy              = $UseRobocopy
                                RobocopyParams           = $RobocopyParams
                                RobocopyAdditionalParams = $RobocopyAdditionalParams
                            }
                            if ($PSBoundParameters.ContainsKey('ErrorAction'))
                            {
                                $CopyFileSplat.ErrorAction = $PSBoundParameters.ErrorAction
                            }
                            # Copy all files from the root source folder
                            Copy-File @CopyFileSplat
                            # Copy all files from subfolders
                            & $Script:CommandTable.'Get-ChildItem' -Path $RobocopySource -Directory -Recurse -Force -ErrorAction 'Ignore' | & $Script:CommandTable.'ForEach-Object' {
                                # Append file name to subfolder path and repeat Copy-File
                                $CopyFileSplat.Path = & $Script:CommandTable.'Join-Path' $_.FullName $RobocopyFile
                                Copy-File @CopyFileSplat
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

                        if (-not (& $Script:CommandTable.'Test-Path' -LiteralPath $RobocopyDestination -PathType Container))
                        {
                            $null = & $Script:CommandTable.'New-Item' -Path $RobocopyDestination -Type 'Directory' -Force -ErrorAction 'Stop'
                        }

                        # Backup destination folder attributes in case known Robocopy bug overwrites them
                        $DestFolderAttributes = [System.IO.File]::GetAttributes($RobocopyDestination)

                        $RobocopyArgs = "$RobocopyParams $RobocopyAdditionalParams `"$RobocopySource`" `"$RobocopyDestination`" `"$RobocopyFile`""
                        Write-ADTLogEntry -Message "Executing Robocopy command: $RobocopyCommand $RobocopyArgs"
                        $RobocopyResult = Start-ADTProcess -Path $RobocopyCommand -Parameters $RobocopyArgs -CreateNoWindow -NoExitOnProcessFailure -PassThru -IgnoreExitCodes 0, 1, 2, 3, 4, 5, 6, 7, 8 -ErrorAction Ignore
                        # Trim the last line plus leading whitespace from each line of Robocopy output
                        $RobocopyOutput = $RobocopyResult.StdOut.Trim() -Replace '\n\s+', "`n"
                        Write-ADTLogEntry -Message "Robocopy output:`n$RobocopyOutput"

                        # Restore folder attributes in case Robocopy overwrote them
                        try
                        {
                            [System.IO.File]::SetAttributes($RobocopyDestination, $DestFolderAttributes)
                        }
                        catch
                        {
                            Write-ADTLogEntry -Message "Failed to apply attributes $DestFolderAttributes destination folder $RobocopyDestination : $($_.Exception.Message)" -Severity 2
                        }

                        switch ($RobocopyResult.ExitCode)
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
                                Write-ADTLogEntry -Message "Serious error. Robocopy did not copy any files. Either a usage error or an error due to insufficient access privileges on the source or destination directories.." -Severity 3
                                if (-not $ContinueFileCopyOnError)
                                {
                                    throw "Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $($_.Exception.Message)"
                                }
                            }
                            default
                            {
                                Write-ADTLogEntry -Message "Robocopy error $($RobocopyResult.ExitCode)." -Severity 3
                                if (-not $ContinueFileCopyOnError)
                                {
                                    throw "Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $($_.Exception.Message)"
                                }
                            }
                        }
                    }
                }
                if ($UseRobocopyThis -eq $false)
                {
                    # If destination has no extension, or if it has an extension only and no name (e.g. a .config folder) and the destination folder does not exist
                    if ((-not ([IO.Path]::HasExtension($Destination))) -or ([IO.Path]::HasExtension($Destination) -and -not [IO.Path]::GetFileNameWithoutExtension($Destination)) -and (-not (& $Script:CommandTable.'Test-Path' -LiteralPath $Destination -PathType 'Container')))
                    {
                        Write-ADTLogEntry -Message "Destination assumed to be a folder which does not exist, creating destination folder [$Destination]."
                        $null = & $Script:CommandTable.'New-Item' -Path $Destination -Type 'Directory' -Force -ErrorAction 'Stop'
                    }
                    # If destination appears to be a file name but parent folder does not exist, create it
                    $DestinationParent = & $Script:CommandTable.'Split-Path' $Destination -Parent
                    if ([IO.Path]::HasExtension($Destination) -and [IO.Path]::GetFileNameWithoutExtension($Destination) -and -not (& $Script:CommandTable.'Test-Path' -LiteralPath $DestinationParent -PathType 'Container'))
                    {
                        Write-ADTLogEntry -Message "Destination assumed to be a file whose parent folder does not exist, creating destination folder [$DestinationParent]."
                        $null = & $Script:CommandTable.'New-Item' -Path $DestinationParent -Type 'Directory' -Force -ErrorAction 'Stop'
                    }
                    if ($Flatten)
                    {
                        Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination] root folder, flattened."
                        if ($ContinueFileCopyOnError)
                        {
                            $null = & $Script:CommandTable.'Get-ChildItem' -Path $srcPath -File -Recurse -Force -ErrorAction 'Ignore' | & $Script:CommandTable.'ForEach-Object' {
                                & $Script:CommandTable.'Copy-Item' -Path ($_.FullName) -Destination $Destination -Force -ErrorAction 'SilentlyContinue' -ErrorVariable 'FileCopyError'
                            }
                        }
                        else
                        {
                            $null = & $Script:CommandTable.'Get-ChildItem' -Path $srcPath -File -Recurse -Force -ErrorAction 'Ignore' | & $Script:CommandTable.'ForEach-Object' {
                                & $Script:CommandTable.'Copy-Item' -Path ($_.FullName) -Destination $Destination -Force -ErrorAction 'Stop'
                            }
                        }
                    }
                    elseif ($Recurse)
                    {
                        Write-ADTLogEntry -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination]."
                        if ($ContinueFileCopyOnError)
                        {
                            $null = & $Script:CommandTable.'Copy-Item' -Path $srcPath -Destination $Destination -Force -Recurse -ErrorAction 'SilentlyContinue' -ErrorVariable 'FileCopyError'
                        }
                        else
                        {
                            $null = & $Script:CommandTable.'Copy-Item' -Path $srcPath -Destination $Destination -Force -Recurse -ErrorAction 'Stop'
                        }
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Copying file in path [$srcPath] to destination [$Destination]."
                        if ($ContinueFileCopyOnError)
                        {
                            $null = & $Script:CommandTable.'Copy-Item' -Path $srcPath -Destination $Destination -Force -ErrorAction 'SilentlyContinue' -ErrorVariable 'FileCopyError'
                        }
                        else
                        {
                            $null = & $Script:CommandTable.'Copy-Item' -Path $srcPath -Destination $Destination -Force -ErrorAction 'Stop'
                        }
                    }

                    if ($FileCopyError)
                    {
                        Write-ADTLogEntry -Message "The following warnings were detected while copying file(s) in path [$srcPath] to destination [$Destination].`n$FileCopyError" -Severity 2
                    }
                    else
                    {
                        Write-ADTLogEntry -Message 'File copy completed successfully.'
                    }
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $($_.Exception.Message)"
                if (-not $ContinueFileCopyOnError)
                {
                    return
                }
            }
        }
    }
    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
