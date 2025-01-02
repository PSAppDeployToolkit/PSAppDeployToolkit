﻿#-----------------------------------------------------------------------------
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

    .PARAMETER Path
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
        Windows style of the application. Options: Normal, Maximized, Minimized. Default is: Normal.

    .PARAMETER RunAsAdmin
        Set shortcut to run program as administrator. This option will prompt user to elevate when executing shortcut.

    .PARAMETER Hotkey
        Create a Hotkey to launch the shortcut, e.g. "CTRL+SHIFT+F".

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        New-ADTShortcut -Path "$envCommonStartMenuPrograms\My Shortcut.lnk" -TargetPath "$envWinDir\notepad.exe" -IconLocation "$envWinDir\notepad.exe" -Description 'Notepad' -WorkingDirectory '%HOMEDRIVE%\%HOMEPATH%'

        Creates a new shortcut for Notepad with the specified parameters.

    .NOTES
        An active ADT session is NOT required to use this function.

        Url shortcuts only support TargetPath, IconLocation and IconIndex. Other parameters are ignored.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateScript({
                if (![System.IO.Path]::GetExtension($_).ToLower().Equals('.lnk') -and ![System.IO.Path]::GetExtension($_).ToLower().Equals('.url'))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified path does not have the correct extension.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$Path,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$TargetPath,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Arguments,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$IconLocation,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$IconIndex,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Description,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WorkingDirectory,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Normal', 'Maximized', 'Minimized')]
        [System.String]$WindowStyle,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$RunAsAdmin,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Hotkey
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        # Make sure .NET's current directory is synced with PowerShell's.
        try
        {
            try
            {
                [System.IO.Directory]::SetCurrentDirectory((Get-Location -PSProvider FileSystem).ProviderPath)
                $FullPath = [System.IO.Path]::GetFullPath($Path)
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Specified path [$Path] is not valid."
            return
        }

        try
        {
            try
            {
                # Make sure directory is present before continuing.
                if (!($PathDirectory = [System.IO.Path]::GetDirectoryName($FullPath)))
                {
                    # The path is root or no filename supplied.
                    if (![System.IO.Path]::GetFileNameWithoutExtension($FullPath))
                    {
                        # No filename supplied.
                        $naerParams = @{
                            Exception = [System.ArgumentException]::new("Specified path [$FullPath] is a directory and not a file.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                            ErrorId = 'ShortcutPathInvalid'
                            TargetObject = $FullPath
                            RecommendedAction = "Please confirm the provided value and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                }
                elseif (!(Test-Path -LiteralPath $PathDirectory -PathType Container))
                {
                    try
                    {
                        Write-ADTLogEntry -Message "Creating shortcut directory [$PathDirectory]."
                        $null = New-Item -LiteralPath $PathDirectory -ItemType Directory -Force
                    }
                    catch
                    {
                        Write-ADTLogEntry -Message "Failed to create shortcut directory [$PathDirectory].`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity 3
                        throw
                    }
                }

                # Remove any pre-existing shortcut first.
                if (Test-Path -LiteralPath $FullPath -PathType Leaf)
                {
                    Write-ADTLogEntry -Message "The shortcut [$FullPath] already exists. Deleting the file..."
                    Remove-ADTFile -LiteralPath $FullPath
                }

                # Build out the shortcut.
                Write-ADTLogEntry -Message "Creating shortcut [$FullPath]."
                if ($Path -match '\.url$')
                {
                    [String[]]$URLFile = '[InternetShortcut]', "URL=$TargetPath"
                    if ($null -ne $IconIndex)
                    {
                        $URLFile += "IconIndex=$IconIndex"
                    }
                    if ($IconLocation)
                    {
                        $URLFile += "IconFile=$IconLocation"
                    }
                    [System.IO.File]::WriteAllLines($FullPath, $URLFile, [System.Text.UTF8Encoding]::new($false))
                }
                else
                {
                    $shortcut = [System.Activator]::CreateInstance([System.Type]::GetTypeFromProgID('WScript.Shell')).CreateShortcut($FullPath)
                    $shortcut.TargetPath = $TargetPath
                    if ($Arguments)
                    {
                        $shortcut.Arguments = $Arguments
                    }
                    if ($Description)
                    {
                        $shortcut.Description = $Description
                    }
                    if ($WorkingDirectory)
                    {
                        $shortcut.WorkingDirectory = $WorkingDirectory
                    }
                    if ($Hotkey)
                    {
                        $shortcut.Hotkey = $Hotkey
                    }
                    if ($IconLocation)
                    {
                        $shortcut.IconLocation = $IconLocation + ",$IconIndex"
                    }
                    $shortcut.WindowStyle = switch ($WindowStyle)
                    {
                        Normal { 1; break }
                        Maximized { 3; break }
                        Minimized { 7; break }
                        default { 1; break }
                    }

                    # Save the changes.
                    $shortcut.Save()

                    # Set shortcut to run program as administrator.
                    if ($RunAsAdmin)
                    {
                        Write-ADTLogEntry -Message 'Setting shortcut to run program as administrator.'
                        $fileBytes = [System.IO.FIle]::ReadAllBytes($FullPath)
                        $fileBytes[21] = $filebytes[21] -bor 32
                        [System.IO.FIle]::WriteAllBytes($FullPath, $fileBytes)
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to create shortcut [$Path]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
