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
        Modifies a shortcut - .lnk or .url file, with configurable options. Only specify the parameters that you want to change.

    .PARAMETER LiteralPath
        Path to the shortcut to be changed.

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
        Sets the shortcut to require elevated permissions to run.

    .PARAMETER Hotkey
        Sets the hotkey to launch the shortcut, e.g. "CTRL+SHIFT+F".

    .PARAMETER Force
        Forces creation of the shortcut if it doesn't exist.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Set-ADTShortcut -LiteralPath "$envCommonDesktop\Application.lnk" -TargetPath "$envProgramFiles\Application\application.exe"

        Creates a shortcut on the All Users desktop named 'Application', targeted to '$envProgramFiles\Application\application.exe'.

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
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true, Position = 0)]
        [ValidateScript({
                if (![System.IO.Path]::GetExtension($_).ToLowerInvariant().Equals('.lnk') -and ![System.IO.Path]::GetExtension($_).ToLowerInvariant().Equals('.url'))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LiteralPath -ProvidedValue $_ -ExceptionMessage 'The specified path does not have the correct extension.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [Alias('Path')]
        [System.String]$LiteralPath,

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
        [System.Management.Automation.SwitchParameter]$Force
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        if ($PSBoundParameters.Count -eq 1)
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

    process
    {
        try
        {
            try
            {
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
                        if ($PSBoundParameters.ContainsKey('TargetPath') -and $exists)
                        {
                            $shortcut.Url = $TargetPath
                        }
                        if ($PSBoundParameters.ContainsKey('IconLocation'))
                        {
                            $shortcut.IconFile = $IconLocation
                            if (!$PSBoundParameters.ContainsKey('IconIndex'))
                            {
                                $shortcut.IconIndex = 0
                            }
                        }
                        if ($PSBoundParameters.ContainsKey('IconIndex'))
                        {
                            $shortcut.IconIndex = $IconIndex
                        }
                        if ($PSBoundParameters.ContainsKey('Description'))
                        {
                            $shortcut.Description = $Description
                        }
                        if ($PSBoundParameters.ContainsKey('WorkingDirectory'))
                        {
                            $shortcut.WorkingDirectory = $WorkingDirectory
                        }
                        if ($PSBoundParameters.ContainsKey('WindowStyle'))
                        {
                            $shortcut.ShowCommand = $WindowStyle
                        }
                        if ($PSBoundParameters.ContainsKey('Hotkey'))
                        {
                            $shortcut.Hotkey = $Hotkey
                        }
                        if (!$exists)
                        {
                            $shortcut.Save($LiteralPath)
                        }
                        else
                        {
                            $shortcut.Save()
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
                        if ($PSBoundParameters.ContainsKey('TargetPath') -and $exists)
                        {
                            $shortcut.TargetPath = $TargetPath
                        }
                        if ($PSBoundParameters.ContainsKey('Arguments'))
                        {
                            $shortcut.Arguments = $Arguments
                        }
                        if ($PSBoundParameters.ContainsKey('IconLocation'))
                        {
                            $shortcut.IconLocation = $IconLocation
                            if (!$PSBoundParameters.ContainsKey('IconIndex'))
                            {
                                $shortcut.IconIndex = 0
                            }
                        }
                        if ($PSBoundParameters.ContainsKey('IconIndex'))
                        {
                            $shortcut.IconIndex = $IconIndex
                        }
                        if ($PSBoundParameters.ContainsKey('Description'))
                        {
                            $shortcut.Description = $Description
                        }
                        if ($PSBoundParameters.ContainsKey('WorkingDirectory'))
                        {
                            $shortcut.WorkingDirectory = $WorkingDirectory
                        }
                        if ($PSBoundParameters.ContainsKey('WindowStyle'))
                        {
                            $shortcut.WindowStyle = $WindowStyle
                        }
                        if ($PSBoundParameters.ContainsKey('RunAsAdmin'))
                        {
                            $shortcut.RunAsAdmin = $RunAsAdmin
                        }
                        if ($PSBoundParameters.ContainsKey('Hotkey'))
                        {
                            $shortcut.Hotkey = $Hotkey
                        }
                        if (!$exists)
                        {
                            $shortcut.Save($LiteralPath)
                        }
                        else
                        {
                            $shortcut.Save()
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
