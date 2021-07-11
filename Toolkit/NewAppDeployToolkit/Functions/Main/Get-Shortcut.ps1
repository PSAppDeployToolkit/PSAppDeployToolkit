#region Function Get-Shortcut
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
.EXAMPLE
	Get-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk"
.NOTES
	Url shortcuts only support TargetPath, IconLocation and IconIndex.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true, Position=0)]
		[ValidateNotNullorEmpty()]
		[string]$Path,
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

			$Output = @{ Path = $FullPath }
			If ($extension -eq '.url') {
				[string[]]$URLFile = [IO.File]::ReadAllLines($Path)
				for($i = 0; $i -lt $URLFile.Length; $i++) {
					$URLFile[$i] = $URLFile[$i].TrimStart()
					if($URLFile[$i].StartsWith('URL=')) { $Output.TargetPath = $URLFile[$i].Replace('URL=','') }
					elseif($URLFile[$i].StartsWith('IconIndex=')) { $Output.IconIndex = $URLFile[$i].Replace('IconIndex=','') }
					elseif($URLFile[$i].StartsWith('IconFile=')) { $Output.IconLocation = $URLFile[$i].Replace('IconFile=','') }
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
					1 { $Output.WindowStyle = 'Normal'}
					3 { $Output.WindowStyle = 'Maximized'}
					7 { $Output.WindowStyle = 'Minimized'}
					Default { $Output.WindowStyle = 'Normal'}
				}
				## Hotkey
				$Output.Hotkey = $shortcut.Hotkey
				## Icon
				[string[]]$Split = $shortcut.IconLocation.Split(',')
				$Output.IconLocation = $Split[0]
				$Output.IconIndex = $Split[1]
				## Remove the variable
				$shortcut = $null
				## Run as admin
				[byte[]]$filebytes = [IO.FIle]::ReadAllBytes($FullPath)
				if ($filebytes[21] -band 32) {
					$Output.RunAsAdmin = $true
				}
				else {
					$Output.RunAsAdmin = $false
				}
			}
			Write-Output $Output
		}
		Catch {
			Write-Log -Message "Failed to read the shortcut [$Path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to read the shortcut [$Path]: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
