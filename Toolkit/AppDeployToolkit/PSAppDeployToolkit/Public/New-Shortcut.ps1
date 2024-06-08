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
        Write-ADTDebugHeader
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
                $shortcut = (Get-ADTEnvironment).Shell.CreateShortcut($FullPath)
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
        Write-ADTDebugFooter
    }
}
