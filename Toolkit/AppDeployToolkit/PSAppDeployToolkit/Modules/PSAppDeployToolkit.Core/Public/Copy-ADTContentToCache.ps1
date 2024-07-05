function Copy-ADTContentToCache
{
    <#

    .SYNOPSIS
    Copies the toolkit content to a cache folder on the local machine and sets the $dirFiles and $supportFiles directory to the cache path.

    .DESCRIPTION
    Copies the toolkit content to a cache folder on the local machine and sets the $dirFiles and $supportFiles directory to the cache path.

    .PARAMETER Path
    The path to the software cache folder.

    .EXAMPLE
    Copy-ADTContentToCache -Path 'C:\Windows\Temp\PSAppDeployToolkit'

    .NOTES
    This function is provided as a template to copy the toolkit content to a cache folder on the local machine and set the $dirFiles directory to the cache path.

    This can be used in the absence of an Endpoint Management solution that provides a managed cache for source files, e.g. Intune is lacking this functionality whereas ConfigMgr includes this functionality.

    Since this cache folder is effectively unmanaged, it is important to cleanup the cache in the uninstall section for the current version and potentially also in the pre-installation section for previous versions.

    This can be done using [Remove-ADTFile -Path "(Get-ADTConfig).Toolkit.CachePath\$installName" -Recurse -ContinueOnError $true]

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path = "$((Get-ADTConfig).Toolkit.CachePath)\$((Get-ADTSession).GetPropertyValue('installName'))"
    )

    begin {
        $adtSession = Get-ADTSession
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process {
        # Create the cache folder if it does not exist.
        if (![System.IO.Directory]::Exists($Path)) 
        {
            try
            {
                Write-ADTLogEntry -Message "Creating cache folder [$Path]."
                [System.Void](New-Item -Path $Path -ItemType Directory)
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -Prefix "Failed to create cache folder [$Path]."
                return
            }
        }
        else
        {
            Write-ADTLogEntry -Message "Cache folder [$Path] already exists."
        }

        # Copy the toolkit content to the cache folder.
        try
        {
            Write-ADTLogEntry -Message "Copying toolkit content to cache folder [$Path]."
            Copy-File -Path (Join-Path $adtSession.GetPropertyValue('scriptParentPath') '*') -Destination $Path -Recurse
            $adtSession.SetPropertyValue('DirFiles', "$Path\Files")
            $adtSession.SetPropertyValue('DirSupportFiles', "$Path\SupportFiles")
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -Prefix "Failed to copy toolkit content to cache folder [$Path]."
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
