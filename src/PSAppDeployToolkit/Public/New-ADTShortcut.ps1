#-----------------------------------------------------------------------------
#
# MARK: New-ADTShortcut
#
#-----------------------------------------------------------------------------

function New-ADTShortcut
{
    <#
    .SYNOPSIS
        Creates a new .lnk or .url type shortcut.

    .DESCRIPTION
        Creates a new shortcut .lnk or .url file, with configurable options. This function allows you to specify various parameters such as the target path, arguments, icon location, description, working directory, window style, run as administrator, and hotkey.

    .PARAMETER LiteralPath
        Path to save the shortcut.

    .PARAMETER TargetPath
        Target path or URL that the shortcut launches.

    .PARAMETER Arguments
        Arguments to be passed to the target path.

    .PARAMETER IconLocation
        Location of the icon used for the shortcut.

    .PARAMETER IconIndex
        The index of the icon. Executables, DLLs, ICO files with multiple icons need the icon index to be specified. This parameter is an Integer. The first index is 0.

    .PARAMETER Description
        Description of the shortcut.

    .PARAMETER WorkingDirectory
        Working Directory to be used for the target path.

    .PARAMETER WindowStyle
        Windows style of the application. Options: Normal, Maximized, Minimized.

    .PARAMETER RunAsAdmin
        Set shortcut to run program as administrator. This option will prompt user to elevate when executing shortcut.

    .PARAMETER Hotkey
        Create a Hotkey to launch the shortcut, e.g. "CTRL+SHIFT+F".

    .PARAMETER Force
        Forces deletion of a pre-existing shortcut.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        New-ADTShortcut -LiteralPath "$envCommonStartMenuPrograms\My Shortcut.lnk" -TargetPath "$envWinDir\notepad.exe" -IconLocation "$envWinDir\notepad.exe" -Description 'Notepad' -WorkingDirectory '%HOMEDRIVE%\%HOMEPATH%'

        Creates a new shortcut for Notepad with the specified parameters.

    .NOTES
        An active ADT session is NOT required to use this function.

        Url shortcuts only support TargetPath, IconLocation and IconIndex. Other parameters are ignored.

        This function supports the `-WhatIf` and `-Confirm` parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/New-ADTShortcut
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateScript({
                if (![System.IO.Path]::GetExtension($_).ToLowerInvariant().Equals('.lnk') -and ![System.IO.Path]::GetExtension($_).ToLowerInvariant().Equals('.url'))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LiteralPath -ProvidedValue $_ -ExceptionMessage 'The specified path does not have the correct extension.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [Alias('Path', 'PSPath')]
        [System.String]$LiteralPath,

        [Parameter(Mandatory = $true)]
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
        [PSADT.ShortcutManagement.ShortcutWindowStyle]$WindowStyle = [PSADT.ShortcutManagement.ShortcutWindowStyle]::Normal,

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
        try
        {
            $LiteralPath = Resolve-ADTFileSystemPath -LiteralPath $LiteralPath -File
            if (!$Force)
            {
                $naerParams = @{
                    Exception = [System.IO.IOException]::new("The specified shortcut at [$LiteralPath] already exists.")
                    Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                    ErrorId = 'ShortcutPathIsPreExisting'
                    TargetObject = $LiteralPath
                    RecommendedAction = "Please review the provided input and try again."
                }
                throw (New-ADTErrorRecord @naerParams)
            }
        }
        catch
        {
            if ($_.Exception -isnot [System.IO.FileNotFoundException])
            {
                $PSCmdlet.ThrowTerminatingError($_)
            }
            $LiteralPath = $_.TargetObject.ResolvedPath
        }
    }

    process
    {
        try
        {
            try
            {
                # Confirm the supplied input path isn't a directory.
                if ([System.IO.Directory]::Exists($LiteralPath))
                {
                    # No filename supplied.
                    $naerParams = @{
                        Exception = [System.ArgumentException]::new("Specified path [$LiteralPath] is a directory and not a file.")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                        ErrorId = 'ShortcutPathInvalid'
                        TargetObject = $LiteralPath
                        RecommendedAction = "Please confirm the provided value and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Make sure directory is present before continuing.
                $pathDirectory = [System.IO.Path]::GetDirectoryName($LiteralPath)
                $newDir = if (!(Test-Path -LiteralPath $pathDirectory -PathType Container))
                {
                    if (!$Force)
                    {
                        $naerParams = @{
                            Exception = [System.ArgumentException]::new("Specified path directory does not exist and [-Force] not specified.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                            ErrorId = 'ShortcutPathDirectoryDoesNotExist'
                            TargetObject = $LiteralPath
                            RecommendedAction = "Please confirm the provided value and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    Write-ADTLogEntry -Message "Creating shortcut directory [$pathDirectory]."
                    New-Item -Path $pathDirectory -ItemType Directory -Force
                }
                try
                {
                    # Remove any pre-existing shortcut first.
                    if (Test-Path -LiteralPath $LiteralPath -PathType Leaf)
                    {
                        if (!$Force)
                        {
                            $naerParams = @{
                                Exception = [System.ArgumentException]::new("Specified shortcut already exists and [-Force] not specified.")
                                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                                ErrorId = 'ShortcutAlreadyExists'
                                TargetObject = $LiteralPath
                                RecommendedAction = "Please confirm the provided value and try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }
                        Write-ADTLogEntry -Message "The shortcut [$LiteralPath] already exists. Deleting the file..."
                        Remove-Item -LiteralPath $LiteralPath -Force
                    }

                    # Build out the shortcut.
                    if (!$PSBoundParameters.ContainsKey('WindowStyle'))
                    {
                        $PSBoundParameters.Add('WindowStyle', $WindowStyle)
                    }
                    if (!$PSBoundParameters.ContainsKey('Force') -or !$PSBoundParameters.Force)
                    {
                        $PSBoundParameters.Force = $true
                    }
                    Set-ADTShortcut @PSBoundParameters
                }
                catch
                {
                    if ($newDir)
                    {
                        Remove-Item -LiteralPath $newDir.FullName -Force -Confirm:$false
                    }
                    throw
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to create shortcut [$LiteralPath]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
