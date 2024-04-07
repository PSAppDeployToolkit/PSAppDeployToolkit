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
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

		If (-not $Shell) {
			[__ComObject]$Shell = New-Object -ComObject 'WScript.Shell' -ErrorAction 'Stop'
		}
	}
	Process {
		Try {
			$extension = [IO.Path]::GetExtension($Path).ToLower()
			If ((-not $extension) -or (($extension -ne '.lnk') -and ($extension -ne '.url'))) {
				Write-Log -Message "Specified file [$Path] does not have a valid shortcut extension: .url .lnk" -Severity 3 -Source ${CmdletName}
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
			} Catch {
				Write-Log -Message "Specified path [$Path] is not valid." -Severity 3 -Source ${CmdletName}
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
					} ElseIf ($URLFile[$i].StartsWith('IconIndex=')) {
						$Output.IconIndex = $URLFile[$i].Replace('IconIndex=', '')
					} ElseIf ($URLFile[$i].StartsWith('IconFile=')) {
						$Output.IconLocation = $URLFile[$i].Replace('IconFile=', '')
					}
				}
			} Else {
				$shortcut = $shell.CreateShortcut($FullPath)
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
				If ($filebytes[21] -band 32) {
					$Output.RunAsAdmin = $true
				} Else {
					$Output.RunAsAdmin = $false
				}
			}
			Write-Output -InputObject ($Output)
		} Catch {
			Write-Log -Message "Failed to read the shortcut [$Path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to read the shortcut [$Path]: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
