#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function New-Shortcut {
    <#
.SYNOPSIS

Creates a new .lnk or .url type shortcut

.DESCRIPTION

Creates a new shortcut .lnk or .url file, with configurable options

.PARAMETER Path

Path to save the shortcut

.PARAMETER TargetPath

Target path or URL that the shortcut launches

.PARAMETER Arguments

Arguments to be passed to the target path

.PARAMETER IconLocation

Location of the icon used for the shortcut

.PARAMETER IconIndex

The index of the icon. Executables, DLLs, ICO files with multiple icons need the icon index to be specified. This parameter is an Integer. The first index is 0.

.PARAMETER Description

Description of the shortcut

.PARAMETER WorkingDirectory

Working Directory to be used for the target path

.PARAMETER WindowStyle

Windows style of the application. Options: Normal, Maximized, Minimized. Default is: Normal.

.PARAMETER RunAsAdmin

Set shortcut to run program as administrator. This option will prompt user to elevate when executing shortcut.

.PARAMETER Hotkey

Create a Hotkey to launch the shortcut, e.g. "CTRL+SHIFT+F"

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None. This function does not return any output.

.EXAMPLE

New-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk" -TargetPath "$envWinDir\System32\notepad.exe" -IconLocation "$envWinDir\System32\notepad.exe" -Description 'Notepad' -WorkingDirectory "$envHomeDrive\$envHomePath"

.NOTES

Url shortcuts only support TargetPath, IconLocation and IconIndex. Other parameters are ignored.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullorEmpty()]
        [String]$Path,
        [Parameter(Mandatory = $true)]
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
        [Int32]$IconIndex,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$Description,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$WorkingDirectory,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Normal', 'Maximized', 'Minimized')]
        [String]$WindowStyle,
        [Parameter(Mandatory = $false)]
        [Switch]$RunAsAdmin,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Hotkey,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        Write-DebugHeader
    }
    Process {
        Try {
            $extension = [IO.Path]::GetExtension($Path).ToLower()
            If ((-not $extension) -or (($extension -ne '.lnk') -and ($extension -ne '.url'))) {
                Write-ADTLogEntry -Message "Specified file [$Path] does not have a valid shortcut extension: .url .lnk" -Severity 3
                If (-not $ContinueOnError) {
                    Throw
                }
                Return
            }
            Try {
                # Make sure Net framework current dir is synced with powershell cwd
                [IO.Directory]::SetCurrentDirectory((Get-Location -PSProvider 'FileSystem').ProviderPath)
                # Get full path
                [String]$FullPath = [IO.Path]::GetFullPath($Path)
            }
            Catch {
                Write-ADTLogEntry -Message "Specified path [$Path] is not valid." -Severity 3
                If (-not $ContinueOnError) {
                    Throw
                }
                Return
            }

            Try {
                [String]$PathDirectory = [IO.Path]::GetDirectoryName($FullPath)
                If (-not $PathDirectory) {
                    # The path is root or no filename supplied
                    If (-not [IO.Path]::GetFileNameWithoutExtension($FullPath)) {
                        # No filename supplied
                        If (-not $ContinueOnError) {
                            Throw
                        }
                        Return
                    }
                    # Continue without creating a folder because the path is root
                }
                ElseIf (-not (Test-Path -LiteralPath $PathDirectory -PathType 'Container' -ErrorAction 'Stop')) {
                    Write-ADTLogEntry -Message "Creating shortcut directory [$PathDirectory]."
                    $null = New-Item -Path $PathDirectory -ItemType 'Directory' -Force -ErrorAction 'Stop'
                }
            }
            Catch {
                Write-ADTLogEntry -Message "Failed to create shortcut directory [$PathDirectory]. `r`n$(Resolve-Error)" -Severity 3
                Throw
            }

            If (Test-Path -Path $FullPath -PathType 'Leaf') {
                Write-ADTLogEntry -Message "The shortcut [$FullPath] already exists. Deleting the file..."
                Remove-ADTFile -Path $FullPath
            }

            Write-ADTLogEntry -Message "Creating shortcut [$FullPath]."
            If ($extension -eq '.url') {
                [String[]]$URLFile = '[InternetShortcut]'
                $URLFile += "URL=$targetPath"
                If ($null -ne $IconIndex) {
                    $URLFile += "IconIndex=$IconIndex"
                }
                If ($IconLocation) {
                    $URLFile += "IconFile=$IconLocation"
                }
                [IO.File]::WriteAllLines($FullPath, $URLFile, (New-Object -TypeName 'Text.UTF8Encoding' -ArgumentList ($false)))
            }
            Else {
                $shortcut = $Script:ADT.Environment.Shell.CreateShortcut($FullPath)
                ## TargetPath
                $shortcut.TargetPath = $targetPath
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
                    Default {
                        $windowStyleInt = 1
                    }
                }
                $shortcut.WindowStyle = $windowStyleInt
                ## Hotkey
                If ($Hotkey) {
                    $shortcut.Hotkey = $Hotkey
                }
                ## Icon
                If ($null -eq $IconIndex) {
                    $IconIndex = 0
                }
                If ($IconLocation) {
                    $shortcut.IconLocation = $IconLocation + ",$IconIndex"
                }
                ## Save the changes
                $shortcut.Save()

                ## Set shortcut to run program as administrator
                If ($RunAsAdmin) {
                    Write-ADTLogEntry -Message 'Setting shortcut to run program as administrator.'
                    [Byte[]]$filebytes = [IO.FIle]::ReadAllBytes($FullPath)
                    $filebytes[21] = $filebytes[21] -bor 32
                    [IO.FIle]::WriteAllBytes($FullPath, $filebytes)
                }
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to create shortcut [$Path]. `r`n$(Resolve-Error)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to create shortcut [$Path]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-DebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

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
        Write-DebugHeader
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
                $shortcut = $Script:ADT.Environment.Shell.CreateShortcut($Path)
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
        Write-DebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-Shortcut {
    <#
.SYNOPSIS

Get information from a new .lnk or .url type shortcut

.DESCRIPTION

Get information from a new .lnk or .url type shortcut. Returns a hashtable.

.PARAMETER Path

Path to the shortcut to get information from

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Collections.Hashtable.

Returns a hashtable with the following keys
- TargetPath
- Arguments
- Description
- WorkingDirectory
- WindowStyle
- Hotkey
- IconLocation
- IconIndex
- RunAsAdmin

.EXAMPLE

Get-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk"

.NOTES

Url shortcuts only support TargetPath, IconLocation and IconIndex.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullorEmpty()]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        Write-DebugHeader
    }
    Process {
        Try {
            $extension = [IO.Path]::GetExtension($Path).ToLower()
            If ((-not $extension) -or (($extension -ne '.lnk') -and ($extension -ne '.url'))) {
                Write-ADTLogEntry -Message "Specified file [$Path] does not have a valid shortcut extension: .url .lnk" -Severity 3
                If (-not $ContinueOnError) {
                    Throw
                }
                Return
            }
            Try {
                # Make sure Net framework current dir is synced with powershell cwd
                [IO.Directory]::SetCurrentDirectory((Get-Location -PSProvider 'FileSystem').ProviderPath)
                # Get full path
                [String]$FullPath = [IO.Path]::GetFullPath($Path)
            }
            Catch {
                Write-ADTLogEntry -Message "Specified path [$Path] is not valid." -Severity 3
                If (-not $ContinueOnError) {
                    Throw
                }
                Return
            }

            $Output = @{ Path = $FullPath }
            If ($extension -eq '.url') {
                [String[]]$URLFile = [IO.File]::ReadAllLines($Path)
                For ($i = 0; $i -lt $URLFile.Length; $i++) {
                    $URLFile[$i] = $URLFile[$i].TrimStart()
                    If ($URLFile[$i].StartsWith('URL=')) {
                        $Output.TargetPath = $URLFile[$i].Replace('URL=', '')
                    }
                    ElseIf ($URLFile[$i].StartsWith('IconIndex=')) {
                        $Output.IconIndex = $URLFile[$i].Replace('IconIndex=', '')
                    }
                    ElseIf ($URLFile[$i].StartsWith('IconFile=')) {
                        $Output.IconLocation = $URLFile[$i].Replace('IconFile=', '')
                    }
                }
            }
            Else {
                $shortcut = $Script:ADT.Environment.Shell.CreateShortcut($FullPath)
                ## TargetPath
                $Output.TargetPath = $shortcut.TargetPath
                ## Arguments
                $Output.Arguments = $shortcut.Arguments
                ## Description
                $Output.Description = $shortcut.Description
                ## Working directory
                $Output.WorkingDirectory = $shortcut.WorkingDirectory
                ## Window Style
                Switch ($shortcut.WindowStyle) {
                    1 {
                        $Output.WindowStyle = 'Normal'
                    }
                    3 {
                        $Output.WindowStyle = 'Maximized'
                    }
                    7 {
                        $Output.WindowStyle = 'Minimized'
                    }
                    Default {
                        $Output.WindowStyle = 'Normal'
                    }
                }
                ## Hotkey
                $Output.Hotkey = $shortcut.Hotkey
                ## Icon
                [String[]]$Split = $shortcut.IconLocation.Split(',')
                $Output.IconLocation = $Split[0]
                $Output.IconIndex = $Split[1]
                ## Remove the variable
                $shortcut = $null
                ## Run as admin
                [Byte[]]$filebytes = [IO.FIle]::ReadAllBytes($FullPath)
                $Output.RunAsAdmin = [Boolean]($filebytes[21] -band 32)
            }
            Write-Output -InputObject ($Output)
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to read the shortcut [$Path]. `r`n$(Resolve-Error)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to read the shortcut [$Path]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-DebugFooter
    }
}
