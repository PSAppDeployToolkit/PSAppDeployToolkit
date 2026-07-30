#-----------------------------------------------------------------------------
#
# MARK: Copy-ADTContentToCache
#
#-----------------------------------------------------------------------------

function Copy-ADTContentToCache
{
    <#
    .SYNOPSIS
        Copies the toolkit content to a cache folder on the local machine and sets the `$adtSession.DirFiles` and `$adtSession.DirSupportFiles` directories to the cache path.

    .DESCRIPTION
        The `Copy-ADTContentToCache` function copies the toolkit content to a cache folder on the local machine and sets the `$adtSession.DirFiles` and `$adtSession.DirSupportFiles` directories to the cache path.

        This function is useful in environments where an Endpoint Management solution does not provide a managed cache for source files, such as Intune.

        It is important to clean up the cache in the uninstall section for the current version and potentially also in the pre-installation section for previous versions.

    .PARAMETER LiteralPath
        The path to the software cache folder. Folder should be application specific since it is erased and recreated on each run to prevent mixing with stale or maliciously planted content.

        Defaults to the cache folder defined by `(Get-ADTConfig).Toolkit.CachePath` with a subfolder named after the current session's InstallName.

    .PARAMETER Content
        Specifies one or more content categories to copy. Copies all by default.

        Valid values for this parameter are:
        - `Files`: Copies only the Files folder and remaps the DirFiles session property.
        - `SupportFiles`: Copies only the SupportFiles folder and remaps the DirSupportFiles session property.
        - `Toolkit`: Copies all other content except the Files and SupportFiles folders.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Copy-ADTContentToCache -LiteralPath "$envWinDir\Temp\PSAppDeployToolkit\$(adtSession.InstallName)"

        This example copies the toolkit content to the specified cache folder.

    .EXAMPLE
        Copy-ADTContentToCache -Content Toolkit

        This example copies the toolkit content to the default cache folder, excluding the Files and SupportFiles folders and leaving DirFiles and DirSupportFiles pointing at the original location.

    .EXAMPLE
        Copy-ADTContentToCache -Content Files,SupportFiles

        This example copies only the Files and SupportFiles folders to the default cache folder, excluding all other content.

    .NOTES
        An active ADT session is required to use this function.

        This can be used in the absence of an Endpoint Management solution that provides a managed cache for source files, e.g. Intune is lacking this functionality whereas ConfigMgr includes this functionality.

        Since this cache folder is effectively unmanaged, it is important to cleanup the cache in the uninstall section for the current version and potentially also in the pre-installation section for previous versions.

        This can be done using `Remove-ADTFile -LiteralPath "(Get-ADTConfig).Toolkit.CachePath\$($adtSession.InstallName)" -Recurse -ErrorAction Ignore`.

        For security, the destination cache folder is erased and recreated on each run so that stale or maliciously planted content cannot be picked up. Additionally, when running with admin rights, the machine-wide cache folder defined by `(Get-ADTConfig).Toolkit.CachePath` has its ownership reclaimed and its permissions reset to inherit from its parent, mitigating the risk of a standard user pre-creating the folder to gain write access.

        This function supports the `-WhatIf` and `-Confirm` parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Copy-ADTContentToCache

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/src/PSAppDeployToolkit/Public/Copy-ADTContentToCache.ps1
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [Alias('Path', 'PSPath')]
        [System.String]$LiteralPath = "$((Get-ADTConfig).Toolkit.CachePath)\$((Get-ADTSession).InstallName)",

        [Parameter(Mandatory = $false)]
        [ValidateSet('Files', 'SupportFiles', 'Toolkit')]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String[]]$Content = @('Files', 'SupportFiles', 'Toolkit')
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        try
        {
            $adtSession = Get-ADTSession
            $scriptDir = Get-ADTSessionCacheScriptDirectory
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
        $folderNames = @('Files', 'SupportFiles')
    }

    process
    {
        # Add a redundant $scriptDir assertion to shut CodeQL up.
        if (($null -eq $scriptDir) -or !(Test-Path -LiteralPath $scriptDir -PathType Container))
        {
            $naerParams = @{
                Exception = [System.IO.DirectoryNotFoundException]::new("The active deployment session does not have a valid ScriptDirectory established.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                ErrorId = 'DeploymentSessionScriptDirectoryDoesNotExist'
                TargetObject = $scriptDir
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }

        # Guard against using the root cache folder directly. The destination is erased on each run, so allowing the root
        # would wipe every other package's cache. A per-deployment subfolder (e.g. named after the InstallName) is required.
        $cachePath = (Get-ADTConfig).Toolkit.CachePath
        if ([System.IO.Path]::GetFullPath($LiteralPath).TrimEnd('\') -eq [System.IO.Path]::GetFullPath($cachePath).TrimEnd('\'))
        {
            $naerParams = @{
                Exception = [System.ArgumentException]::new("The cache path [$LiteralPath] cannot be the root cache folder [$cachePath]. Specify a subfolder, such as one named after the deployment's InstallName.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                ErrorId = 'CachePathIsRootDirectory'
                TargetObject = $LiteralPath
                RecommendedAction = "Specify a cache subfolder rather than the root cache path."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }

        # Check if source and destination are the same (already running from cache). If so, there's nothing to do and we
        # must never erase the destination as it would destroy the source content.
        if ([System.IO.Path]::GetFullPath($scriptDir).TrimEnd('\') -eq [System.IO.Path]::GetFullPath($LiteralPath).TrimEnd('\'))
        {
            Write-ADTLogEntry -Message "Source and destination are the same path [$LiteralPath]. Skipping copy operation."
            return
        }

        # When running with admin rights, if the cache folder is a parent of LiteralPath, set the owner to Administrators group and reset any applied permissions
        if ((Get-ADTEnvironmentTable).IsAdmin -and (Test-Path -LiteralPath $cachePath -PathType Container) -and [System.IO.Path]::GetFullPath($LiteralPath).StartsWith("$cachePath\", [System.StringComparison]::OrdinalIgnoreCase))
        {
            try
            {
                try
                {
                    Write-ADTLogEntry -Message "Securing root cache folder [$cachePath]."
                    if ($PSCmdlet.ShouldProcess($cachePath, 'Secure root cache folder'))
                    {
                        # Using the SID instead of BUILTIN\Administrators to overcome localization issues.
                        Set-ADTItemPermission -LiteralPath $cachePath -Owner '*S-1-5-32-544' -EnableInheritance -RemoveExplicitRules -InformationAction SilentlyContinue
                    }
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to secure cache folder [$cachePath]."
                return
            }
        }

        # Erase any existing destination cache folder before copying to prevent mixing with stale or maliciously planted content
        if (Test-Path -LiteralPath $LiteralPath -PathType Container)
        {
            Write-ADTLogEntry -Message "Erasing existing cache folder [$LiteralPath]."
            if ($PSCmdlet.ShouldProcess($LiteralPath, 'Erase existing cache folder'))
            {
                try
                {
                    try
                    {
                        Remove-Item -LiteralPath $LiteralPath -Recurse -Force
                    }
                    catch
                    {
                        Write-Error -ErrorRecord $_
                    }
                }
                catch
                {
                    Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to erase existing cache folder [$LiteralPath]."
                    return
                }
            }
        }

        # Create the cache folder.
        Write-ADTLogEntry -Message "Creating cache folder [$LiteralPath]."
        if ($PSCmdlet.ShouldProcess($LiteralPath, 'Create cache folder'))
        {
            try
            {
                try
                {
                    $null = New-Item -Path $LiteralPath -ItemType Directory
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to create cache folder [$LiteralPath]."
                return
            }
        }

        # Copy the toolkit content to the cache folder.
        Write-ADTLogEntry -Message "Copying toolkit content to cache folder [$LiteralPath]."
        if (!$PSCmdlet.ShouldProcess($LiteralPath, "Copy content from [$scriptDir]"))
        {
            return
        }
        try
        {
            try
            {
                if (($Content | Select-ADTUniqueObject | Measure-Object).Count -eq $MyInvocation.MyCommand.Parameters.Content.Attributes.Where({ $_ -is [System.Management.Automation.ValidateSetAttribute] }).ValidValues.Count)
                {
                    # Fast path: copy everything in a single operation.
                    Copy-ADTFile -Path (Join-Path -Path $scriptDir -ChildPath *) -Destination $LiteralPath -Recurse
                }
                else
                {
                    # Selective copy: enumerate top-level items and copy based on -Exclude.
                    if ('Toolkit' -in $Content)
                    {
                        Get-ChildItem -LiteralPath $scriptDir -Force | & { process { if ($_.Name -notin $folderNames) { Copy-ADTFile -LiteralPath $_.FullName -Destination $LiteralPath -Recurse } } }
                    }
                    $filesSourcePath = Join-Path -Path $scriptDir -ChildPath 'Files'
                    if (('Files' -in $Content) -and (Test-Path -LiteralPath $filesSourcePath -PathType Container))
                    {
                        Copy-ADTFile -LiteralPath $filesSourcePath -Destination $LiteralPath -Recurse
                    }
                    $supportFilesSourcePath = Join-Path -Path $scriptDir -ChildPath 'SupportFiles'
                    if (('SupportFiles' -in $Content) -and (Test-Path -LiteralPath $supportFilesSourcePath -PathType Container))
                    {
                        Copy-ADTFile -LiteralPath $supportFilesSourcePath -Destination $LiteralPath -Recurse
                    }
                }

                # Remap session properties for categories that were copied.
                $filesDestPath = Join-Path -Path $LiteralPath -ChildPath 'Files'
                if (('Files' -in $Content) -and (Test-Path -LiteralPath $filesDestPath -PathType Container))
                {
                    $adtSession.DirFiles = $filesDestPath
                }
                $supportFilesDestPath = Join-Path -Path $LiteralPath -ChildPath 'SupportFiles'
                if (('SupportFiles' -in $Content) -and (Test-Path -LiteralPath $supportFilesDestPath -PathType Container))
                {
                    $adtSession.DirSupportFiles = $supportFilesDestPath
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to copy toolkit content to cache folder [$LiteralPath]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
