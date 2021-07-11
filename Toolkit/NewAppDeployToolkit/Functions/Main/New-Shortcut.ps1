#region Function New-Shortcut
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
.EXAMPLE
	New-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk" -TargetPath "$envWinDir\system32\notepad.exe" -IconLocation "$envWinDir\system32\notepad.exe" -Description 'Notepad' -WorkingDirectory "$envHomeDrive\$envHomePath"
.NOTES
	Url shortcuts only support TargetPath, IconLocation and IconIndex. Other parameters are ignored.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true, Position=0)]
		[ValidateNotNullorEmpty()]
		[string]$Path,
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$TargetPath,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$Arguments,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$IconLocation,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int]$IconIndex,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$Description,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$WorkingDirectory,
		[Parameter(Mandatory=$false)]
		[ValidateSet('Normal','Maximized','Minimized')]
		[string]$WindowStyle,
		[Parameter(Mandatory=$false)]
		[switch]$RunAsAdmin,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Hotkey,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

		If (-not $Shell) { [__comobject]$Shell = New-Object -ComObject 'WScript.Shell' -ErrorAction 'Stop' }
	}
	Process {
		Try {
			$extension = [IO.Path]::GetExtension($Path).ToLower()
			If ((-not $extension) -or (($extension -ne '.lnk') -and ($extension -ne '.url'))) {
				Write-Log -Message "Specified file [$Path] does not have a valid shortcut extension: .url .lnk" -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw
				}
				return
			}
			Try {
				# Make sure Net framework current dir is synced with powershell cwd
				[IO.Directory]::SetCurrentDirectory((Get-Location -PSProvider FileSystem).ProviderPath)
				# Get full path
				[string]$FullPath = [IO.Path]::GetFullPath($Path)
			}
			Catch {
				Write-Log -Message "Specified path [$Path] is not valid." -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw
				}
				return
			}

			Try {
				[string]$PathDirectory = [IO.Path]::GetDirectoryName($FullPath)
				If (-not $PathDirectory) {
					# The path is root or no filename supplied
					If (-not [IO.Path]::GetFileNameWithoutExtension($FullPath)) {
						# No filename supplied
						If (-not $ContinueOnError) {
							Throw
						}
						return
					}
					# Continue without creating a folder because the path is root
				} ElseIf (-not (Test-Path -LiteralPath $PathDirectory -PathType 'Container' -ErrorAction 'Stop')) {
					Write-Log -Message "Creating shortcut directory [$PathDirectory]." -Source ${CmdletName}
					$null = New-Item -Path $PathDirectory -ItemType 'Directory' -Force -ErrorAction 'Stop'
				}
			}
			Catch {
				Write-Log -Message "Failed to create shortcut directory [$PathDirectory]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				Throw
			}

			If (Test-Path -Path $FullPath -PathType Leaf) {
				Write-Log -Message "The shortcut [$FullPath] already exists. Deleting the file..." -Source ${CmdletName}
				Remove-File -Path $FullPath
			}

			Write-Log -Message "Creating shortcut [$FullPath]." -Source ${CmdletName}
			If ($extension -eq '.url') {
				[string[]]$URLFile = '[InternetShortcut]'
				$URLFile += "URL=$targetPath"
				If ($IconIndex -ne $null) { $URLFile += "IconIndex=$IconIndex" }
				If ($IconLocation) { $URLFile += "IconFile=$IconLocation" }
				[IO.File]::WriteAllLines($FullPath,$URLFile,(new-object -TypeName Text.UTF8Encoding -ArgumentList $false))
			} Else {
				$shortcut = $shell.CreateShortcut($FullPath)
				## TargetPath
				$shortcut.TargetPath = $targetPath
				## Arguments
				If ($arguments) { $shortcut.Arguments = $arguments }
				## Description
				If ($description) { $shortcut.Description = $description }
				## Working directory
				If ($workingDirectory) { $shortcut.WorkingDirectory = $workingDirectory }
				## Window Style
				Switch ($windowStyle) {
					'Normal' { $windowStyleInt = 1 }
					'Maximized' { $windowStyleInt = 3 }
					'Minimized' { $windowStyleInt = 7 }
					Default { $windowStyleInt = 1 }
				}
				$shortcut.WindowStyle = $windowStyleInt
				## Hotkey
				If ($hotkey) { $shortcut.Hotkey = $hotkey }
				## Icon
				If ($IconIndex -eq $null) {
					$IconIndex = 0
				}
				If ($IconLocation) { $shortcut.IconLocation = $IconLocation + ",$IconIndex" }
				## Save the changes
				$shortcut.Save()

				## Set shortcut to run program as administrator
				If ($RunAsAdmin) {
					Write-Log -Message 'Setting shortcut to run program as administrator.' -Source ${CmdletName}
					[byte[]]$filebytes = [IO.FIle]::ReadAllBytes($FullPath)
					$filebytes[21] = $filebytes[21] -bor 32
					[IO.FIle]::WriteAllBytes($FullPath,$filebytes)
				}
			}
		}
		Catch {
			Write-Log -Message "Failed to create shortcut [$Path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to create shortcut [$Path]: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
