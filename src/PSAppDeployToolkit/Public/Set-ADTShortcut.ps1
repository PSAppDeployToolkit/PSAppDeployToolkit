#-----------------------------------------------------------------------------
#
# MARK: Set-ADTShortcut
#
#-----------------------------------------------------------------------------

function Set-ADTShortcut
{
    <#
    .SYNOPSIS
        Modifies a .lnk or .url type shortcut.

    .DESCRIPTION
        The `Set-ADTShortcut` function modifies a .lnk or .url shortcut file, with the configured options. Only specify the parameters that you want to change.

    .PARAMETER LiteralPath
        Path to the shortcut to be changed.

    .PARAMETER InputObject
        An existing IShortcutLinkInfo object to modify.

    .PARAMETER TargetPath
        Sets target path or URL that the shortcut launches.

    .PARAMETER Arguments
        Sets the arguments used against the target path.

    .PARAMETER IconLocation
        Sets location of the icon used for the shortcut.

    .PARAMETER IconIndex
        Sets the index of the icon. Executables, DLLs, ICO files with multiple icons need the icon index to be specified. This parameter is an Integer. The first index is 0.

    .PARAMETER Description
        Sets the description of the shortcut as can be seen in the shortcut's properties.

    .PARAMETER WorkingDirectory
        Sets working directory to be used for the target path.

    .PARAMETER WindowStyle
        Sets the shortcut's window style to be minimised, maximised, etc.

    .PARAMETER RunAsAdmin
        Specifies that the command executed by the shortcut should be done so with elevated permissions. Setting this option will prompt the user to elevate when the shortcut is executed.

    .PARAMETER Hotkey
        Sets the hotkey to launch the shortcut, e.g. "CTRL+SHIFT+F".

    .PARAMETER Clear
        Clears one or more shortcut properties. Only clears if the corresponding parameter isn't specified.

    .PARAMETER Force
        Forces creation of the shortcut if one doesn't already exist at the `-LiteralPath` provided.

    .PARAMETER PassThru
        Returns an IShortcutLinkInfo snapshot of the shortcut.

    .INPUTS
        PSADT.ShortcutManagement.IShortcutLinkInfo

        You can pipe a IShortcutLinkInfo object into this function to specify the shortcut to modify.

        When piping a IShortcutLinkInfo object into this function, the only properties modified are the ones explicitly specified via this function's parameters.

    .OUTPUTS
        None

        By default, this function does not return any output.

    .OUTPUTS
        PSADT.ShortcutManagement.IShortcutLinkInfo

        When the `-PassThru` parameter is provided, this function returns a IShortcutLinkInfo object representing the modified shortcut.

    .EXAMPLE
        Set-ADTShortcut -LiteralPath "$envCommonDesktop\Application.lnk" -TargetPath "$envProgramFiles\Application\application.exe"

        Creates a shortcut on the All Users desktop named 'Application', targeted to '$envProgramFiles\Application\application.exe'.

    .EXAMPLE
        Get-ADTShortcut -LiteralPath "$envCommonDesktop\Application.lnk" | Set-ADTShortcut -WindowStyle Maximized

        Modifies the shortcut on the All Users desktop named 'Application' to launch maximized.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function supports the `-WhatIf` and `-Confirm` parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTShortcut
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    [OutputType([PSADT.ShortcutManagement.IShortcutLinkInfo])]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipelineByPropertyName = $true, Position = 0, ParameterSetName = 'LiteralPath')]
        [PSAppDeployToolkit.Attributes.ValidateExtension('.lnk', '.url')]
        [Alias('Path')]
        [System.String]$LiteralPath,

        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ParameterSetName = 'InputObject')]
        [ValidateNotNull()]
        [PSADT.ShortcutManagement.IShortcutLinkInfo]$InputObject,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$TargetPath,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$Arguments,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$IconLocation,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.UInt32]]$IconIndex,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$Description,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$WorkingDirectory,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [PSADT.ShortcutManagement.ShortcutWindowStyle]$WindowStyle,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$RunAsAdmin,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$Hotkey,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Arguments', 'IconLocation', 'IconIndex', 'Description', 'WorkingDirectory', 'WindowStyle', 'Hotkey')]
        [System.String[]]$Clear,

        [Parameter(Mandatory = $false, ParameterSetName = 'LiteralPath')]
        [System.Management.Automation.SwitchParameter]$Force,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        if ((($PSCmdlet.ParameterSetName -eq 'LiteralPath') -and ($PSBoundParameters.Count -eq 1)) -or $PSBoundParameters.Count -eq 0)
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("At least one change must be specified.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                ErrorId = 'FunctionCalledWithInsufficientParameters'
                TargetObject = $PSBoundParameters
                RecommendedAction = "Please review the provided input and try again."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
        $exists = $true
        if ($PSCmdlet.ParameterSetName -eq 'LiteralPath')
        {
            try
            {
                $LiteralPath = Resolve-ADTFileSystemPath -LiteralPath $LiteralPath -File
            }
            catch
            {
                if ($_.Exception -is [System.IO.FileNotFoundException])
                {
                    $LiteralPath = $_.TargetObject.ResolvedPath
                    $exists = $false
                    if (!$Force)
                    {
                        $PSCmdlet.ThrowTerminatingError($_)
                        return
                    }
                }
                else
                {
                    $PSCmdlet.ThrowTerminatingError($_)
                    return
                }
            }
            if (!$exists -and [System.String]::IsNullOrWhiteSpace($TargetPath))
            {
                $naerParams = @{
                    Exception = [System.InvalidOperationException]::new("The [-TargetPath] parameter must be specified when forcibly creating a new shortcut.")
                    Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                    ErrorId = 'NoTargetPathForNonPreExistingShortcut'
                    TargetObject = $PSBoundParameters
                    RecommendedAction = "Please review the provided input and try again."
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
        }
    }

    process
    {
        try
        {
            try
            {
                if ($PSCmdlet.ParameterSetName -eq 'InputObject')
                {
                    $LiteralPath = $InputObject.FilePath
                }

                # Return early if we shouldn't process.
                if (!$PSCmdlet.ShouldProcess($LiteralPath, 'Modify shortcut'))
                {
                    return
                }

                # Handle .url/.lnk files separately as required.
                if ([System.IO.Path]::GetExtension($LiteralPath) -eq '.url')
                {
                    # Set up the IDisposable shortcut object.
                    $shortcut = if (!$exists)
                    {
                        Write-ADTLogEntry -Message "Creating shortcut [$LiteralPath]."
                        if (!$PSCmdlet.ShouldProcess($LiteralPath, 'Create shortcut file'))
                        {
                            return
                        }
                        [PSADT.ShortcutManagement.InternetShortcutFile]::Create($TargetPath)
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Changing shortcut [$LiteralPath]."
                        [PSADT.ShortcutManagement.InternetShortcutFile]::Load($LiteralPath, [PSADT.Interop.STGM]::STGM_READWRITE)
                    }

                    # Process all valid parameters.
                    try
                    {
                        # TargetPath.
                        if ($PSBoundParameters.ContainsKey('TargetPath') -and $exists)
                        {
                            $shortcut.Url = $TargetPath
                        }
                        elseif ($Clear -contains 'TargetPath')
                        {
                            $shortcut.Url = [System.Management.Automation.Language.NullString]::Value
                        }

                        # IconLocation.
                        if ($PSBoundParameters.ContainsKey('IconLocation'))
                        {
                            $shortcut.IconFile = $IconLocation
                            if (!$PSBoundParameters.ContainsKey('IconIndex'))
                            {
                                $shortcut.IconIndex = 0
                            }
                        }
                        elseif ($Clear -contains 'IconLocation')
                        {
                            $shortcut.IconFile = [System.Management.Automation.Language.NullString]::Value
                            $shortcut.IconIndex = $null
                        }

                        # IconIndex.
                        if ($PSBoundParameters.ContainsKey('IconIndex'))
                        {
                            $shortcut.IconIndex = $IconIndex
                        }
                        elseif ($Clear -contains 'IconIndex')
                        {
                            $shortcut.IconIndex = $null
                        }

                        # Description.
                        if ($PSBoundParameters.ContainsKey('Description'))
                        {
                            $shortcut.Description = $Description
                        }
                        elseif ($Clear -contains 'Description')
                        {
                            $shortcut.Description = [System.Management.Automation.Language.NullString]::Value
                        }

                        # WorkingDirectory.
                        if ($PSBoundParameters.ContainsKey('WorkingDirectory'))
                        {
                            $shortcut.WorkingDirectory = $WorkingDirectory
                        }
                        elseif ($Clear -contains 'WorkingDirectory')
                        {
                            $shortcut.WorkingDirectory = [System.Management.Automation.Language.NullString]::Value
                        }

                        # WindowStyle.
                        if ($PSBoundParameters.ContainsKey('WindowStyle'))
                        {
                            $shortcut.ShowCommand = $WindowStyle
                        }
                        elseif ($Clear -contains 'WindowStyle')
                        {
                            $shortcut.ShowCommand = $null
                        }

                        # Hotkey.
                        if ($PSBoundParameters.ContainsKey('Hotkey'))
                        {
                            $shortcut.Hotkey = $Hotkey
                        }
                        elseif ($Clear -contains 'Hotkey')
                        {
                            $shortcut.Hotkey = $null
                        }

                        # Finalise shortcut.
                        if (!$exists)
                        {
                            $shortcut.Save($LiteralPath)
                        }
                        else
                        {
                            $shortcut.Save()
                        }
                        if ($PassThru)
                        {
                            return $shortcut.GetInfoSnapshot()
                        }
                    }
                    finally
                    {
                        $shortcut.Dispose()
                    }
                }
                else
                {
                    # Set up the IDisposable shortcut object.
                    $shortcut = if (!$exists)
                    {
                        Write-ADTLogEntry -Message "Creating shortcut [$LiteralPath]."
                        if (!$PSCmdlet.ShouldProcess($LiteralPath, 'Create shortcut file'))
                        {
                            return
                        }
                        [PSADT.ShortcutManagement.ShellLinkFile]::Create($TargetPath)
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Changing shortcut [$LiteralPath]."
                        [PSADT.ShortcutManagement.ShellLinkFile]::Load($LiteralPath, [PSADT.Interop.STGM]::STGM_READWRITE)
                    }

                    # Process all valid parameters.
                    try
                    {
                        # TargetPath.
                        if ($PSBoundParameters.ContainsKey('TargetPath') -and $exists)
                        {
                            $shortcut.TargetPath = $TargetPath
                        }
                        elseif ($Clear -contains 'TargetPath')
                        {
                            $shortcut.TargetPath = [System.Management.Automation.Language.NullString]::Value
                        }

                        # Arguments.
                        if ($PSBoundParameters.ContainsKey('Arguments'))
                        {
                            $shortcut.Arguments = $Arguments
                        }
                        elseif ($Clear -contains 'Arguments')
                        {
                            $shortcut.Arguments = [System.Management.Automation.Language.NullString]::Value
                        }

                        # IconLocation.
                        if ($PSBoundParameters.ContainsKey('IconLocation'))
                        {
                            $shortcut.IconLocation = $IconLocation
                            if (!$PSBoundParameters.ContainsKey('IconIndex'))
                            {
                                $shortcut.IconIndex = 0
                            }
                        }
                        elseif ($Clear -contains 'IconLocation')
                        {
                            $shortcut.IconLocation = $null
                            $shortcut.IconIndex = $null
                        }

                        # IconIndex.
                        if ($PSBoundParameters.ContainsKey('IconIndex'))
                        {
                            $shortcut.IconIndex = $IconIndex
                        }
                        elseif ($Clear -contains 'IconIndex')
                        {
                            $shortcut.IconIndex = $null
                        }

                        # Description.
                        if ($PSBoundParameters.ContainsKey('Description'))
                        {
                            $shortcut.Description = $Description
                        }
                        elseif ($Clear -contains 'Description')
                        {
                            $shortcut.Description = [System.Management.Automation.Language.NullString]::Value
                        }

                        # WorkingDirectory.
                        if ($PSBoundParameters.ContainsKey('WorkingDirectory'))
                        {
                            $shortcut.WorkingDirectory = $WorkingDirectory
                        }
                        elseif ($Clear -contains 'WorkingDirectory')
                        {
                            $shortcut.WorkingDirectory = [System.Management.Automation.Language.NullString]::Value
                        }

                        # WindowStyle.
                        if ($PSBoundParameters.ContainsKey('WindowStyle'))
                        {
                            $shortcut.WindowStyle = $WindowStyle
                        }
                        elseif ($Clear -contains 'WindowStyle')
                        {
                            $shortcut.WindowStyle = [PSADT.ShortcutManagement.ShortcutWindowStyle]::Normal
                        }

                        # RunAsAdmin.
                        if ($PSBoundParameters.ContainsKey('RunAsAdmin'))
                        {
                            $shortcut.RunAsAdmin = $RunAsAdmin
                        }

                        # Hotkey.
                        if ($PSBoundParameters.ContainsKey('Hotkey'))
                        {
                            $shortcut.Hotkey = $Hotkey
                        }
                        elseif ($Clear -contains 'Hotkey')
                        {
                            $shortcut.Hotkey = $null
                        }

                        # Finalise shortcut.
                        if (!$exists)
                        {
                            $shortcut.Save($LiteralPath)
                        }
                        else
                        {
                            $shortcut.Save()
                        }
                        if ($PassThru)
                        {
                            return $shortcut.GetInfoSnapshot()
                        }
                    }
                    finally
                    {
                        $shortcut.Dispose()
                    }
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to $(if (!$exists) {"create"} else {"change"}) the shortcut [$LiteralPath]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
