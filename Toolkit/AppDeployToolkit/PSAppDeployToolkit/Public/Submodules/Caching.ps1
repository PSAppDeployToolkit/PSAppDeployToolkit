#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

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

    This can be done using [Remove-ADTFile -Path "$Script:ADT.Config.Toolkit.CachePath\$installName" -Recurse -ContinueOnError $true]

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [ValidateNotNullOrEmpty()]
        [System.String]$Path = "$($Script:ADT.Config.Toolkit.CachePath)\$((Get-ADTSession).GetPropertyValue('installName'))"
    )

    begin {
        Write-ADTDebugHeader
    }

    process {
        try
        {
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
                    Write-ADTLogEntry -Message "Failed to create cache folder [$Path].`n$(Resolve-Error)" -Severity 3
                    throw
                }
            }
            else
            {
                Write-ADTLogEntry -Message "Cache folder [$Path] already exists."
            }

            # Copy the toolkit content to the cache folder.
            Write-ADTLogEntry -Message "Copying toolkit content to cache folder [$Path]."
            Copy-File -Path (Join-Path (Get-ADTSession).GetPropertyValue('scriptParentPath') '*') -Destination $Path -Recurse

            # Set the Files directory to the cache path.
            (Get-ADTSession).SetPropertyValue('DirFiles', "$Path\Files")
            (Get-ADTSession).SetPropertyValue('DirSupportFiles', "$Path\SupportFiles")
        }
        catch
        {
            Write-ADTLogEntry -Message "Failed to copy toolkit content to cache folder [$Path].`n$(Resolve-Error)" -Severity 3
            throw
        }
    }

    end {
        Write-ADTDebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Remove-ADTContentFromCache
{
    <#

    .SYNOPSIS
    Removes the toolkit content from the cache folder on the local machine and reverts the $dirFiles and $supportFiles directory

    .DESCRIPTION
    Removes the toolkit content from the cache folder on the local machine and reverts the $dirFiles and $supportFiles directory

    .PARAMETER Path
    The path to the software cache folder.

    .EXAMPLE
    Remove-ADTContentFromCache -Path 'C:\Windows\Temp\PSAppDeployToolkit'

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [ValidateNotNullOrEmpty()]
        [System.String]$Path = "$($Script:ADT.Config.Toolkit.CachePath)\$((Get-ADTSession).GetPropertyValue('installName'))"
    )

    begin {
        Write-ADTDebugHeader
    }

    process {
        if (![System.IO.Directory]::Exists($Path))
        {
            Write-ADTLogEntry -Message "Cache folder [$Path] does not exist."
            return
        }

        try
        {
            Write-ADTLogEntry -Message "Removing cache folder [$Path]."
            Remove-Item -Path $Path -Recurse
            (Get-ADTSession).SetPropertyValue('DirFiles', (Join-Path -Path (Get-ADTSession).GetPropertyValue('scriptParentPath') -ChildPath Files))
            (Get-ADTSession).SetPropertyValue('DirSupportFiles', (Join-Path -Path (Get-ADTSession).GetPropertyValue('scriptParentPath') -ChildPath SupportFiles))
        }
        catch
        {
            Write-ADTLogEntry -Message "Failed to remove cache folder [$Path].`n$(Resolve-Error)" -Severity 3
            throw
        }
    }

    end {
        Write-ADTDebugFooter
    }
}
