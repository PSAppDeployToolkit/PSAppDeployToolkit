#-----------------------------------------------------------------------------
#
# MARK: Copy-ADTContentToCache
#
#-----------------------------------------------------------------------------

function Copy-ADTContentToCache
{
    <#
    .SYNOPSIS
        Copies the toolkit content to a cache folder on the local machine and sets the $adtSession.DirFiles and $adtSession.DirSupportFiles directory to the cache path.

    .DESCRIPTION
        Copies the toolkit content to a cache folder on the local machine and sets the $adtSession.DirFiles and $adtSession.DirSupportFiles directory to the cache path.

        This function is useful in environments where an Endpoint Management solution does not provide a managed cache for source files, such as Intune.

        It is important to clean up the cache in the uninstall section for the current version and potentially also in the pre-installation section for previous versions.

    .PARAMETER LiteralPath
        The path to the software cache folder.

    .PARAMETER Exclude
        Specifies one or more content categories to exclude from the cache copy. Acceptable values are 'Files', 'SupportFiles', and 'Other'.

        - Files: Excludes the Files folder and does not remap the DirFiles session property.
        - SupportFiles: Excludes the SupportFiles folder and does not remap the DirSupportFiles session property.
        - Other: Excludes all content other than the Files and SupportFiles folders.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Copy-ADTContentToCache -LiteralPath "$envWinDir\Temp\PSAppDeployToolkit"

        This example copies the toolkit content to the specified cache folder.

    .EXAMPLE
        Copy-ADTContentToCache -Exclude Files,SupportFiles

        This example copies the toolkit content to the default cache folder, excluding the Files and SupportFiles folders and leaving DirFiles and DirSupportFiles pointing at the original location.

    .EXAMPLE
        Copy-ADTContentToCache -Exclude Other

        This example copies only the Files and SupportFiles folders to the default cache folder, excluding all other content.

    .NOTES
        An active ADT session is required to use this function.

        This can be used in the absence of an Endpoint Management solution that provides a managed cache for source files, e.g. Intune is lacking this functionality whereas ConfigMgr includes this functionality.

        Since this cache folder is effectively unmanaged, it is important to cleanup the cache in the uninstall section for the current version and potentially also in the pre-installation section for previous versions.

        This can be done using `Remove-ADTFile -LiteralPath "(Get-ADTConfig).Toolkit.CachePath\$($adtSession.InstallName)" -Recurse -ErrorAction Ignore`.

        This function supports the -WhatIf and -Confirm parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Copy-ADTContentToCache
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [Alias('Path', 'PSPath')]
        [System.String]$LiteralPath = "$((Get-ADTConfig).Toolkit.CachePath)\$((Get-ADTSession).InstallName)",

        [Parameter(Mandatory = $false)]
        [ValidateSet('Files', 'SupportFiles', 'Other')]
        [System.String[]]$Exclude
    )

    begin
    {
        if ('Files' -in $Exclude -and 'SupportFiles' -in $Exclude -and 'Other' -in $Exclude)
        {
            $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Exclude -ProvidedValue $Exclude -ExceptionMessage 'Cannot specify all possible values for -Exclude parameter as there would be nothing to copy.'))
        }

        try
        {
            $adtSession = Get-ADTSession
            $scriptDir = Get-ADTSessionCacheScriptDirectory
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }

        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        $folderNames = @('Files', 'SupportFiles')
    }

    process
    {
        # Create the cache folder if it does not exist.
        if (!(Test-Path -LiteralPath $LiteralPath -PathType Container))
        {
            Write-ADTLogEntry -Message "Creating cache folder [$LiteralPath]."
            if (!$PSCmdlet.ShouldProcess($LiteralPath, 'Create cache folder'))
            {
                return
            }
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
        else
        {
            Write-ADTLogEntry -Message "Cache folder [$LiteralPath] already exists."
        }

        # Copy the toolkit content to the cache folder.
        Write-ADTLogEntry -Message "Copying toolkit content to cache folder [$LiteralPath]."
        if (!$PSCmdlet.ShouldProcess($LiteralPath, "Copy toolkit content from [$scriptDir]"))
        {
            return
        }
        try
        {
            try
            {
                # Check if source and destination are the same (already running from cache)
                if ((Resolve-Path $scriptDir).Path -eq (Resolve-Path $LiteralPath).Path)
                {
                    Write-ADTLogEntry -Message "Source and destination are the same path [$LiteralPath]. Skipping copy operation."
                }
                elseif (!$PSBoundParameters.ContainsKey('Exclude'))
                {
                    # Fast path: copy everything in a single operation.
                    Copy-ADTFile -Path (Join-Path -Path $scriptDir -ChildPath *) -Destination $LiteralPath -Recurse
                }
                else
                {
                    # Selective copy: enumerate top-level items and copy based on -Exclude.
                    if ('Other' -notin $Exclude)
                    {
                        Get-ChildItem -LiteralPath $scriptDir -Force | & { process { if ($_.Name -notin $folderNames) { Copy-ADTFile -LiteralPath $_.FullName -Destination $LiteralPath -Recurse } } }
                    }
                    $filesSourcePath = Join-Path -Path $scriptDir -ChildPath 'Files'
                    if (('Files' -notin $Exclude) -and (Test-Path -LiteralPath $filesSourcePath -PathType Container))
                    {
                        Copy-ADTFile -LiteralPath $filesSourcePath -Destination $LiteralPath -Recurse
                    }
                    $supportFilesSourcePath = Join-Path -Path $scriptDir -ChildPath SupportFiles
                    if (('SupportFiles' -notin $Exclude) -and (Test-Path -LiteralPath $supportFilesSourcePath -PathType Container))
                    {
                        Copy-ADTFile -LiteralPath $supportFilesSourcePath -Destination $LiteralPath -Recurse
                    }
                }

                # Remap session properties for categories that were copied.
                $filesDestPath = Join-Path -Path $LiteralPath -ChildPath 'Files'
                if (('Files' -notin $Exclude) -and (Test-Path -LiteralPath $filesDestPath -PathType Container))
                {
                    $adtSession.DirFiles = $filesDestPath
                }
                $supportFilesDestPath = Join-Path -Path $LiteralPath -ChildPath SupportFiles
                if (('SupportFiles' -notin $Exclude) -and (Test-Path -LiteralPath $supportFilesDestPath -PathType Container))
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
