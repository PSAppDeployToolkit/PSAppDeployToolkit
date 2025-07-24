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

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Copy-ADTContentToCache -LiteralPath "$envWinDir\Temp\PSAppDeployToolkit"

        This example copies the toolkit content to the specified cache folder.

    .NOTES
        An active ADT session is required to use this function.

        This can be used in the absence of an Endpoint Management solution that provides a managed cache for source files, e.g. Intune is lacking this functionality whereas ConfigMgr includes this functionality.

        Since this cache folder is effectively unmanaged, it is important to cleanup the cache in the uninstall section for the current version and potentially also in the pre-installation section for previous versions.

        This can be done using `Remove-ADTFile -LiteralPath "(Get-ADTConfig).Toolkit.CachePath\$($adtSession.InstallName)" -Recurse -ErrorAction Ignore`.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Copy-ADTContentToCache
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Alias('Path', 'PSPath')]
        [System.String]$LiteralPath = "$((Get-ADTConfig).Toolkit.CachePath)\$((Get-ADTSession).InstallName)"
    )

    begin
    {
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
    }

    process
    {
        # Create the cache folder if it does not exist.
        if (!(Test-Path -LiteralPath $LiteralPath -PathType Container))
        {
            Write-ADTLogEntry -Message "Creating cache folder [$LiteralPath]."
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
        try
        {
            try
            {
                Copy-ADTFile -Path (Join-Path -Path $scriptDir -ChildPath *) -Destination $LiteralPath -Recurse
                $adtSession.DirFiles = Join-Path -Path $LiteralPath -ChildPath Files
                $adtSession.DirSupportFiles = Join-Path -Path $LiteralPath -ChildPath SupportFiles
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
