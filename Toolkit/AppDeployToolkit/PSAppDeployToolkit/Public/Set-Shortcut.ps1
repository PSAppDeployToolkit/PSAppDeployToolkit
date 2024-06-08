Function Set-Shortcut {
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

Set shortcut to run program as administrator. This option will prompt user to elevate when executing shortcut. If not specified or set to $null, the flag will not be changed.

.PARAMETER Hotkey

Changes the Hotkey to launch the shortcut, e.g. "CTRL+SHIFT+F"

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

PSOjbect

Path to the shortcut to be changed or a hashtable of parameters to be changed

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Set-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk" -TargetPath "$envWinDir\System32\notepad.exe" -IconLocation "$envWinDir\System32\notepad.exe" -IconIndex 0 -Description 'Notepad' -WorkingDirectory "$envHomeDrive\$envHomePath"

.NOTES

Url shortcuts only support TargetPath, IconLocation and IconIndex. Other parameters are ignored.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding(DefaultParameterSetName = 'Default')]
    Param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, Position = 0, ParameterSetName = 'Default')]
        [ValidateNotNullorEmpty()]
        [String]$Path,
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, Position = 0, ParameterSetName = 'Pipeline')]
        [ValidateNotNullorEmpty()]
        [Hashtable]$PathHash,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$TargetPath,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$Arguments,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$IconLocation,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$IconIndex,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$Description,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$WorkingDirectory,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Normal', 'Maximized', 'Minimized', 'DontChange')]
        [String]$WindowStyle = 'DontChange',
        [Parameter(Mandatory = $false)]
        [System.Nullable[Boolean]]$RunAsAdmin,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Hotkey,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        Write-ADTDebugHeader
    }
    Process {
        Try {
            If ($PsCmdlet.ParameterSetName -eq 'Pipeline') {
                $Path = $PathHash.Path
            }

            If (-not (Test-Path -LiteralPath $Path -PathType 'Leaf' -ErrorAction 'Stop')) {
                Write-ADTLogEntry -Message "Failed to find the file [$Path]." -Severity 3
                If (-not $ContinueOnError) {
                    Throw
                }
                Return
            }
            $extension = [IO.Path]::GetExtension($Path).ToLower()
            If ((-not $extension) -or (($extension -ne '.lnk') -and ($extension -ne '.url'))) {
                Write-ADTLogEntry -Message "Specified file [$Path] is not a valid shortcut." -Severity 3
                If (-not $ContinueOnError) {
                    Throw
                }
                Return
            }
            # Make sure Net framework current dir is synced with powershell cwd
            [IO.Directory]::SetCurrentDirectory((Get-Location -PSProvider 'FileSystem').ProviderPath)
            Write-ADTLogEntry -Message "Changing shortcut [$Path]."
            If ($extension -eq '.url') {
                [String[]]$URLFile = [IO.File]::ReadAllLines($Path)
                For ($i = 0; $i -lt $URLFile.Length; $i++) {
                    $URLFile[$i] = $URLFile[$i].TrimStart()
                    If ($URLFile[$i].StartsWith('URL=') -and $targetPath) {
                        $URLFile[$i] = "URL=$targetPath"
                    }
                    ElseIf ($URLFile[$i].StartsWith('IconIndex=') -and ($null -ne $IconIndex)) {
                        $URLFile[$i] = "IconIndex=$IconIndex"
                    }
                    ElseIf ($URLFile[$i].StartsWith('IconFile=') -and $IconLocation) {
                        $URLFile[$i] = "IconFile=$IconLocation"
                    }
                }
                [IO.File]::WriteAllLines($Path, $URLFile, (New-Object -TypeName 'Text.UTF8Encoding' -ArgumentList ($false)))
            }
            Else {
                $shortcut = (Get-ADTEnvironment).Shell.CreateShortcut($Path)
                ## TargetPath
                If ($targetPath) {
                    $shortcut.TargetPath = $targetPath
                }
                ## Arguments
                If ($arguments) {
                    $shortcut.Arguments = $arguments
                }
                ## Description
                If ($description) {
                    $shortcut.Description = $description
                }
                ## Working directory
                If ($workingDirectory) {
                    $shortcut.WorkingDirectory = $workingDirectory
                }
                ## Window Style
                Switch ($windowStyle) {
                    'Normal' {
                        $windowStyleInt = 1
                    }
                    'Maximized' {
                        $windowStyleInt = 3
                    }
                    'Minimized' {
                        $windowStyleInt = 7
                    }
                    'DontChange' {
                        $windowStyleInt = 0
                    }
                    Default {
                        $windowStyleInt = 1
                    }
                }
                If ($windowStyleInt -ne 0) {
                    $shortcut.WindowStyle = $windowStyleInt
                }
                ## Hotkey
                If ($Hotkey) {
                    $shortcut.Hotkey = $Hotkey
                }
                ## Icon
                # Retrieve previous value and split the path from the index
                [String[]]$Split = $shortcut.IconLocation.Split(',')
                $TempIconLocation = $Split[0]
                $TempIconIndex = $Split[1]
                # Check whether a new icon path was specified
                If ($IconLocation) {
                    # New icon path was specified. Check whether new icon index was also specified
                    If ($null -ne $IconIndex) {
                        # Create new icon path from new icon path and new icon index
                        $IconLocation = $IconLocation + ",$IconIndex"
                    }
                    Else {
                        # No new icon index was specified as a parameter. We will keep the old one
                        $IconLocation = $IconLocation + ",$TempIconIndex"
                    }
                }
                ElseIf ($null -ne $IconIndex) {
                    # New icon index was specified, but not the icon location. Append it to the icon path from the shortcut
                    $IconLocation = $TempIconLocation + ",$IconIndex"
                }
                If ($IconLocation) {
                    $shortcut.IconLocation = $IconLocation
                }
                ## Save the changes
                $shortcut.Save()

                ## Set shortcut to run program as administrator
                If ($RunAsAdmin -eq $true) {
                    Write-ADTLogEntry -Message 'Setting shortcut to run program as administrator.'
                    [Byte[]]$filebytes = [IO.FIle]::ReadAllBytes($Path)
                    $filebytes[21] = $filebytes[21] -bor 32
                    [IO.FIle]::WriteAllBytes($Path, $filebytes)
                }
                ElseIf ($RunAsAdmin -eq $false) {
                    [Byte[]]$filebytes = [IO.FIle]::ReadAllBytes($Path)
                    Write-ADTLogEntry -Message 'Setting shortcut to not run program as administrator.'
                    $filebytes[21] = $filebytes[21] -band (-bnot 32)
                    [IO.FIle]::WriteAllBytes($Path, $filebytes)
                }
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to change the shortcut [$Path]. `r`n$(Resolve-Error)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to change the shortcut [$Path]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
