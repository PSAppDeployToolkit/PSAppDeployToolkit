function Set-ADTShortcut
{
    <#

    .SYNOPSIS
    Modifies a .lnk or .url type shortcut

    .DESCRIPTION
    Modifies a shortcut - .lnk or .url file, with configurable options.

    Only specify the parameters that you want to change.

    .PARAMETER Path
    Path to the shortcut to be changed

    .PARAMETER PathHash
    Hashtable of parameters to be changed

    .PARAMETER TargetPath
    Changes target path or URL that the shortcut launches

    .PARAMETER Arguments
    Changes Arguments to be passed to the target path

    .PARAMETER IconLocation
    Changes location of the icon used for the shortcut

    .PARAMETER IconIndex
    Change the index of the icon. Executables, DLLs, ICO files with multiple icons need the icon index to be specified. This parameter is an Integer. The first index is 0.

    .PARAMETER Description
    Changes description of the shortcut

    .PARAMETER WorkingDirectory
    Changes Working Directory to be used for the target path

    .PARAMETER WindowStyle
    Changes the Windows style of the application. Options: Normal, Maximized, Minimized, DontChange. Default is: DontChange.

    .PARAMETER RunAsAdmin
    Set shortcut to run program as administrator. This option will prompt user to elevate when executing shortcut. If not specified, the flag will not be changed.

    .PARAMETER Hotkey
    Changes the Hotkey to launch the shortcut, e.g. "CTRL+SHIFT+F"

    .INPUTS
    PSOjbect. Path to the shortcut to be changed or a hashtable of parameters to be changed.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    Set-ADTShortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk" -TargetPath "$envWinDir\System32\notepad.exe" -IconLocation "$envWinDir\System32\notepad.exe" -IconIndex 0 -Description 'Notepad' -WorkingDirectory "$envHomeDrive\$envHomePath"

    .NOTES
    Url shortcuts only support TargetPath, IconLocation and IconIndex. Other parameters are ignored.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true, Position = 0)]
        [ValidateScript({
            if (![System.IO.File]::Exists($_) -or (![System.IO.Path]::GetExtension($Path).ToLower().Equals('.lnk') -and ![System.IO.Path]::GetExtension($Path).ToLower().Equals('.url')))
            {
                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist or does not have the correct extension.'))
            }
            return !!$_
        })]
        [System.String]$Path,

        [Parameter(Mandatory = $false)]
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
        [System.String]$IconIndex,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Description,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WorkingDirectory,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Normal', 'Maximized', 'Minimized', 'DontChange')]
        [System.String]$WindowStyle = 'DontChange',

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
        Write-ADTLogEntry -Message "Changing shortcut [$Path]."
        try
        {
            try
            {
                # Make sure .NET's current directory is synced with PowerShell's.
                [System.IO.Directory]::SetCurrentDirectory((Get-Location -PSProvider FileSystem).ProviderPath)
                if ($extension -eq '.url')
                {
                    $URLFile = [System.IO.File]::ReadAllLines($Path).ForEach({
                        switch ($_)
                        {
                            {$_.StartsWith('URL=') -and $TargetPath} {"URL=$TargetPath"}
                            {$_.StartsWith('IconIndex=') -and ($null -ne $IconIndex)} {"IconIndex=$IconIndex"}
                            {$_.StartsWith('IconFile=') -and $IconLocation} {"IconFile=$IconLocation"}
                            default {$_}
                        }
                    })
                    [System.IO.File]::WriteAllLines($Path, $URLFile, [System.Text.UTF8Encoding]::new($false))
                }
                else
                {
                    # Open shortcut and set initial properties.
                    $shortcut = [System.Activator]::CreateInstance([System.Type]::GetTypeFromProgID('WScript.Shell')).CreateShortcut($Path)
                    if ($TargetPath)
                    {
                        $shortcut.TargetPath = $TargetPath
                    }
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

                    # Set the WindowStyle based on input.
                    $windowStyleInt = switch ($WindowStyle)
                    {
                        Normal {1}
                        Maximized {3}
                        Minimized {7}
                    }
                    If ($null -ne $windowStyleInt)
                    {
                        $shortcut.WindowStyle = $WindowStyleInt
                    }

                    # Handle icon, starting with retrieval previous value and split the path from the index.
                    $TempIconLocation, $TempIconIndex = $shortcut.IconLocation.Split(',')
                    $IconLocation = if ($IconLocation)
                    {
                        # New icon path was specified. Check whether new icon index was also specified.
                        if ($null -ne $IconIndex)
                        {
                            # Create new icon path from new icon path and new icon index.
                            $IconLocation + ",$IconIndex"
                        }
                        else
                        {
                            # No new icon index was specified as a parameter. We will keep the old one.
                            $IconLocation + ",$TempIconIndex"
                        }
                    }
                    elseif ($null -ne $IconIndex)
                    {
                        # New icon index was specified, but not the icon location. Append it to the icon path from the shortcut.
                        $IconLocation = $TempIconLocation + ",$IconIndex"
                    }
                    if ($IconLocation)
                    {
                        $shortcut.IconLocation = $IconLocation
                    }

                    # Save the changes.
                    $shortcut.Save()

                    # Set shortcut to run program as administrator.
                    if ($PSBoundParameters.ContainsKey('RunAsAdmin'))
                    {
                        $fileBytes = [System.IO.FIle]::ReadAllBytes($Path)
                        $fileBytes[21] = if ($PSBoundParameters.RunAsAdmin)
                        {
                            Write-ADTLogEntry -Message 'Setting shortcut to run program as administrator.'
                            $fileBytes[21] -bor 32
                        }
                        else
                        {
                            Write-ADTLogEntry -Message 'Setting shortcut to not run program as administrator.'
                            $fileBytes[21] -band (-bnot 32)
                        }
                        [System.IO.FIle]::WriteAllBytes($Path, $fileBytes)
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -Prefix "Failed to change the shortcut [$Path]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
