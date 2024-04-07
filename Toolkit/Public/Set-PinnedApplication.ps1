Function Set-PinnedApplication {
	<#
.SYNOPSIS

Pins or unpins a shortcut to the start menu or task bar.

.DESCRIPTION

Pins or unpins a shortcut to the start menu or task bar.

This should typically be run in the user context, as pinned items are stored in the user profile.

.PARAMETER Action

Action to be performed. Options: 'PinToStartMenu','UnpinFromStartMenu','PinToTaskbar','UnpinFromTaskbar'.

.PARAMETER FilePath

Path to the shortcut file to be pinned or unpinned.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Set-PinnedApplication -Action 'PinToStartMenu' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"

.EXAMPLE

Set-PinnedApplication -Action 'UnpinFromTaskbar' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"

.NOTES

Windows 10 logic borrowed from Stuart Pearson (https://pinto10blog.wordpress.com/2016/09/10/pinto10/)

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $true)]
		[ValidateSet('PinToStartMenu', 'UnpinFromStartMenu', 'PinToTaskbar', 'UnpinFromTaskbar')]
		[String]$Action,
		[Parameter(Mandatory = $true)]
		[ValidateNotNullorEmpty()]
		[String]$FilePath
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

		#region Function Get-PinVerb
		Function Get-PinVerb {
			[CmdletBinding()]
			Param (
				[Parameter(Mandatory = $true)]
				[ValidateNotNullorEmpty()]
				[Int32]$VerbId
			)

			[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

			Write-Log -Message "Get localized pin verb for verb id [$VerbID]." -Source ${CmdletName}
			[String]$PinVerb = [PSADT.FileVerb]::GetPinVerb($VerbId)
			Write-Log -Message "Verb ID [$VerbID] has a localized pin verb of [$PinVerb]." -Source ${CmdletName}
			Write-Output -InputObject ($PinVerb)
		}
		#endregion

		#region Function Invoke-Verb
		Function Invoke-Verb {
			[CmdletBinding()]
			Param (
				[Parameter(Mandatory = $true)]
				[ValidateNotNullorEmpty()]
				[String]$FilePath,
				[Parameter(Mandatory = $true)]
				[ValidateNotNullorEmpty()]
				[String]$Verb
			)

			Try {
				[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
				$Verb = $Verb.Replace('&', '')
				$path = Split-Path -Path $FilePath -Parent -ErrorAction 'Stop'
				$folder = $shellApp.Namespace($path)
				$item = $folder.ParseName((Split-Path -Path $FilePath -Leaf -ErrorAction 'Stop'))
				$itemVerb = $item.Verbs() | Where-Object { $_.Name.Replace('&', '') -eq $Verb } -ErrorAction 'Stop'

				If ($null -eq $itemVerb) {
					Write-Log -Message "Performing action [$Verb] is not programmatically supported for this file [$FilePath]." -Severity 2 -Source ${CmdletName}
				} Else {
					Write-Log -Message "Performing action [$Verb] on [$FilePath]." -Source ${CmdletName}
					$itemVerb.DoIt()
				}
			} Catch {
				Write-Log -Message "Failed to perform action [$Verb] on [$FilePath]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
			}
		}
		#endregion

		If (([Version]$envOSVersion).Major -ge 10) {
			Write-Log -Message 'Detected Windows 10 or higher, using Windows 10 verb codes.' -Source ${CmdletName}
			[Hashtable]$Verbs = @{
				'PinToStartMenu'     = 51201
				'UnpinFromStartMenu' = 51394
				'PinToTaskbar'       = 5386
				'UnpinFromTaskbar'   = 5387
			}
		} Else {
			[Hashtable]$Verbs = @{
				'PinToStartMenu'     = 5381
				'UnpinFromStartMenu' = 5382
				'PinToTaskbar'       = 5386
				'UnpinFromTaskbar'   = 5387
			}
		}

	}
	Process {
		Try {
			Write-Log -Message "Execute action [$Action] for file [$FilePath]." -Source ${CmdletName}

			If (-not (Test-Path -LiteralPath $FilePath -PathType 'Leaf' -ErrorAction 'Stop')) {
				Throw "Path [$filePath] does not exist."
			}

			If (-not ($Verbs.$Action)) {
				Throw "Action [$Action] not supported. Supported actions are [$($Verbs.Keys -join ', ')]."
			}

			If ($Action.Contains('StartMenu')) {
				If ([Int32]$envOSVersionMajor -ge 10) {
					If ((Get-Item -Path $FilePath).Extension -ne '.lnk') {
						Throw 'Only shortcut files (.lnk) are supported on Windows 10 and higher.'
					} ElseIf (-not ($FilePath.StartsWith($envUserStartMenu, 'OrdinalIgnoreCase') -or $FilePath.StartsWith($envCommonStartMenu, 'OrdinalIgnoreCase'))) {
						Throw "Only shortcut files (.lnk) in [$envUserStartMenu] and [$envCommonStartMenu] are supported on Windows 10 and higher."
					}
				}

				[String]$PinVerbAction = Get-PinVerb -VerbId ($Verbs.$Action)
				If (-not $PinVerbAction) {
					Throw "Failed to get a localized pin verb for action [$Action]. Action is not supported on this operating system."
				}

				Invoke-Verb -FilePath $FilePath -Verb $PinVerbAction
			} ElseIf ($Action.Contains('Taskbar')) {
				If ([Int32]$envOSVersionMajor -ge 10) {
					$FileNameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
					$PinExists = Test-Path -Path "$envAppData\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\$($FileNameWithoutExtension).lnk"

					If (($Action -eq 'PinToTaskbar') -and ($PinExists)) {
						If ($(Invoke-ObjectMethod -InputObject $Shell -MethodName 'CreateShortcut' -ArgumentList "$envAppData\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\$($FileNameWithoutExtension).lnk").TargetPath -eq $FilePath) {
							Write-Log -Message "Pin [$FileNameWithoutExtension] already exists." -Source ${CmdletName}
							Return
						}
					} ElseIf (($Action -eq 'UnpinFromTaskbar') -and ($PinExists -eq $false)) {
						Write-Log -Message "Pin [$FileNameWithoutExtension] does not exist." -Source ${CmdletName}
						Return
					}

					$ExplorerCommandHandler = Get-RegistryKey -Key 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\Windows.taskbarpin' -Value 'ExplorerCommandHandler'
					$classesStarKey = (Get-Item "Registry::HKEY_USERS\$($RunasActiveUser.SID)\SOFTWARE\Classes").OpenSubKey('*', $true)
					$shellKey = $classesStarKey.CreateSubKey('shell', $true)
					$specialKey = $shellKey.CreateSubKey('{:}', $true)
					$specialKey.SetValue('ExplorerCommandHandler', $ExplorerCommandHandler)

					$Folder = Invoke-ObjectMethod -InputObject $ShellApp -MethodName 'Namespace' -ArgumentList $(Split-Path -Path $FilePath -Parent)
					$Item = Invoke-ObjectMethod -InputObject $Folder -MethodName 'ParseName' -ArgumentList $(Split-Path -Path $FilePath -Leaf)

					$Item.InvokeVerb('{:}')

					$shellKey.DeleteSubKey('{:}')
					If ($shellKey.SubKeyCount -eq 0 -and $shellKey.ValueCount -eq 0) {
						$classesStarKey.DeleteSubKey('shell')
					}
				} Else {
					[String]$PinVerbAction = Get-PinVerb -VerbId ($Verbs.$Action)
					If (-not $PinVerbAction) {
						Throw "Failed to get a localized pin verb for action [$Action]. Action is not supported on this operating system."
					}

					Invoke-Verb -FilePath $FilePath -Verb $PinVerbAction
				}
			}
		} Catch {
			Write-Log -Message "Failed to execute action [$Action]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
		} Finally {
			Try {
				If ($shellKey) {
					$shellKey.Close()
				}
			} Catch {
			}
			Try {
				If ($classesStarKey) {
					$classesStarKey.Close()
				}
			} Catch {
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
