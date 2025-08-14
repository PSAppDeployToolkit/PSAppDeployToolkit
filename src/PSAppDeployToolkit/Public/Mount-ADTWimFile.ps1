#-----------------------------------------------------------------------------
#
# MARK: Mount-ADTWimFile
#
#-----------------------------------------------------------------------------

function Mount-ADTWimFile
{
    <#
    .SYNOPSIS
        Mounts a WIM file to a specified directory.

    .DESCRIPTION
        Mounts a WIM file to a specified directory. The function supports mounting by image index or image name. It also provides options to forcefully remove existing directories and return the mounted image details.

    .PARAMETER ImagePath
        Path to the WIM file to be mounted.

    .PARAMETER Path
        Directory where the WIM file will be mounted. The directory either must not exist, or must be empty and not have a pre-existing WIM mounted.

    .PARAMETER Index
        Index of the image within the WIM file to be mounted.

    .PARAMETER Name
        Name of the image within the WIM file to be mounted.

    .PARAMETER Force
        Forces the removal of the existing directory if it is not empty.

    .PARAMETER PassThru
        If specified, the function will return the results from `Mount-WindowsImage`.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        Microsoft.Dism.Commands.ImageObject

        Returns the mounted image details if the PassThru parameter is specified.

    .EXAMPLE
        Mount-ADTWimFile -ImagePath 'C:\Images\install.wim' -Path 'C:\Mount' -Index 1

        Mounts the first image in the 'install.wim' file to the 'C:\Mount' directory, creating the directory if it does not exist.

    .EXAMPLE
        Mount-ADTWimFile -ImagePath 'C:\Images\install.wim' -Path 'C:\Mount' -Name 'Windows 10 Pro'

        Mounts the image named 'Windows 10 Pro' in the 'install.wim' file to the 'C:\Mount' directory, creating the directory if it does not exist.

    .EXAMPLE
        Mount-ADTWimFile -ImagePath 'C:\Images\install.wim' -Path 'C:\Mount' -Index 1 -Force

        Mounts the first image in the 'install.wim' file to the 'C:\Mount' directory, forcefully removing the existing directory if it is not empty.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Mount-ADTWimFile
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Index')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Name')]
        [ValidateScript({
                if ($null -eq $_)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName ImagePath -ProvidedValue $_ -ExceptionMessage 'The specified input is null.'))
                }
                if (!$_.Exists)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName ImagePath -ProvidedValue $_ -ExceptionMessage 'The specified image path cannot be found.'))
                }
                if ([System.Uri]::new($_).IsUnc)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName ImagePath -ProvidedValue $_ -ExceptionMessage 'The specified image path cannot be a network share.'))
                }
                return !!$_
            })]
        [System.IO.FileInfo]$ImagePath,

        [Parameter(Mandatory = $true, ParameterSetName = 'Index')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Name')]
        [ValidateScript({
                if ($null -eq $_)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified input is null.'))
                }
                if ([System.Uri]::new($_).IsUnc)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified mount path cannot be a network share.'))
                }
                if (Get-ADTMountedWimFile -Path $_)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified mount path has a pre-existing WIM mounted.'))
                }
                if (Get-ChildItem -LiteralPath $_ -ErrorAction Ignore)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified mount path is not empty.'))
                }
                return !!$_
            })]
        [System.IO.DirectoryInfo]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'Index')]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.UInt32]]$Index,

        [Parameter(Mandatory = $true, ParameterSetName = 'Name')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false, ParameterSetName = 'Index')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Name')]
        [System.Management.Automation.SwitchParameter]$Force,

        [Parameter(Mandatory = $false, ParameterSetName = 'Index')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Name')]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Attempt to get specified WIM image before initialising.
        $null = try
        {
            $PSBoundParameters.Remove('PassThru')
            $PSBoundParameters.Remove('Force')
            $PSBoundParameters.Remove('Path')
            Get-WindowsImage @PSBoundParameters
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Announce commencement.
        Write-ADTLogEntry -Message "Mounting WIM file [$ImagePath] to [$Path]."
        try
        {
            try
            {
                # Provide a warning if this WIM file is already mounted.
                if (($wimFile = Get-ADTMountedWimFile -ImagePath $ImagePath))
                {
                    Write-ADTLogEntry -Message "The WIM file [$ImagePath] is already mounted at [$($wimFile.Path)] and will be mounted again." -Severity 2
                }

                # If we're using the force, forcibly remove the existing directory.
                if (Test-Path -LiteralPath $Path -PathType Container)
                {
                    if (Get-ChildItem -LiteralPath $Path -ErrorAction Ignore)
                    {
                        if (!$Force)
                        {
                            $naerParams = @{
                                Exception = [System.IO.IOException]::new("The specified mount path is not empty.")
                                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                                ErrorId = 'NonEmptyMountPathError'
                                TargetObject = $Path
                                RecommendedAction = "Please specify a path where a new folder can be created, or a path to an existing empty folder."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }
                        Write-ADTLogEntry -Message "Removing pre-existing path [$Path] as [-Force] was provided."
                        Remove-Item -LiteralPath $Path -Force -Confirm:$false
                    }
                }

                # If the path doesn't exist, create it.
                if (!(Test-Path -LiteralPath $Path -PathType Container))
                {
                    Write-ADTLogEntry -Message "Creating path [$Path] as it does not exist."
                    $Path = [System.IO.Directory]::CreateDirectory($Path).FullName
                }

                # Mount the WIM file.
                $res = Mount-WindowsImage @PSBoundParameters -Path $Path -ReadOnly -CheckIntegrity
                Write-ADTLogEntry -Message "Successfully mounted WIM file [$ImagePath]."

                # Store the result within the user's ADTSession if there's an active one.
                if (Test-ADTSessionActive)
                {
                    (Get-ADTSession).AddMountedWimFile($ImagePath)
                }

                # Return the result if we're passing through.
                if ($PassThru)
                {
                    return $res
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage 'Error occurred while attemping to mount WIM file.'
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
