#region Function Set-PinnedApplication
Function Set-PinnedApplication {
<#
.SYNOPSIS
	Pins or unpins a shortcut to the start menu or task bar.
.DESCRIPTION
	Pins or unpins a shortcut to the start menu or task bar.
	This should typically be run in the user context, as pinned items are stored in the user profile.
.PARAMETER Action
	Action to be performed. Options: 'PintoStartMenu','UnpinfromStartMenu','PintoTaskbar','UnpinfromTaskbar'.
.PARAMETER FilePath
	Path to the shortcut file to be pinned or unpinned.
.EXAMPLE
	Set-PinnedApplication -Action 'PintoStartMenu' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"
.EXAMPLE
	Set-PinnedApplication -Action 'UnpinfromTaskbar' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"
.NOTES
	Windows 10 logic borrowed from Stuart Pearson (https://pinto10blog.wordpress.com/2016/09/10/pinto10/)
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateSet('PintoStartMenu','UnpinfromStartMenu','PintoTaskbar','UnpinfromTaskbar')]
		[string]$Action,
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$FilePath
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

		#region Function Get-PinVerb
		Function Get-PinVerb {
			[CmdletBinding()]
			Param (
				[Parameter(Mandatory=$true)]
				[ValidateNotNullorEmpty()]
				[int32]$VerbId
			)

			[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

			Write-Log -Message "Get localized pin verb for verb id [$VerbID]." -Source ${CmdletName}
			[string]$PinVerb = [PSADT.FileVerb]::GetPinVerb($VerbId)
			Write-Log -Message "Verb ID [$VerbID] has a localized pin verb of [$PinVerb]." -Source ${CmdletName}
			Write-Output -InputObject $PinVerb
		}
		#endregion

		#region Function Invoke-Verb
		Function Invoke-Verb {
			[CmdletBinding()]
			Param (
				[Parameter(Mandatory=$true)]
				[ValidateNotNullorEmpty()]
				[string]$FilePath,
				[Parameter(Mandatory=$true)]
				[ValidateNotNullorEmpty()]
				[string]$Verb
			)

			Try {
				[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
				$verb = $verb.Replace('&','')
				$path = Split-Path -Path $FilePath -Parent -ErrorAction 'Stop'
				$folder = $shellApp.Namespace($path)
				$item = $folder.ParseName((Split-Path -Path $FilePath -Leaf -ErrorAction 'Stop'))
				$itemVerb = $item.Verbs() | Where-Object { $_.Name.Replace('&','') -eq $verb } -ErrorAction 'Stop'

				If ($null -eq $itemVerb) {
					Write-Log -Message "Performing action [$verb] is not programmatically supported for this file [$FilePath]." -Severity 2 -Source ${CmdletName}
				}
				Else {
					Write-Log -Message "Performing action [$verb] on [$FilePath]." -Source ${CmdletName}
					$itemVerb.DoIt()
				}
			}
			Catch {
				Write-Log -Message "Failed to perform action [$verb] on [$FilePath]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
			}
		}
		#endregion

		If (([version]$envOSVersion).Major -ge 10) {
			Write-Log -Message "Detected Windows 10 or higher, using Windows 10 verb codes." -Source ${CmdletName}
			[hashtable]$Verbs = @{
				'PintoStartMenu' = 51201
				'UnpinfromStartMenu' = 51394
				'PintoTaskbar' = 5386
				'UnpinfromTaskbar' = 5387
			}
		}
		Else {
			[hashtable]$Verbs = @{
			'PintoStartMenu' = 5381
			'UnpinfromStartMenu' = 5382
			'PintoTaskbar' = 5386
			'UnpinfromTaskbar' = 5387
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

			If ($Action.Contains("StartMenu"))
			{
				If ([int]$envOSVersionMajor -ge 10)	{
					If ((Get-Item -Path $FilePath).Extension -ne '.lnk') {
						Throw "Only shortcut files (.lnk) are supported on Windows 10 and higher."
					}
					ElseIf (-not ($FilePath.StartsWith($envUserStartMenu, 'CurrentCultureIgnoreCase') -or $FilePath.StartsWith($envCommonStartMenu, 'CurrentCultureIgnoreCase'))) {
						Throw "Only shortcut files (.lnk) in [$envUserStartMenu] and [$envCommonStartMenu] are supported on Windows 10 and higher."
					}
				}

				[string]$PinVerbAction = Get-PinVerb -VerbId $Verbs.$Action
				If (-not ($PinVerbAction)) {
					Throw "Failed to get a localized pin verb for action [$Action]. Action is not supported on this operating system."
				}

				Invoke-Verb -FilePath $FilePath -Verb $PinVerbAction
			}
			ElseIf ($Action.Contains("Taskbar")) {
				If ([int]$envOSVersionMajor -ge 10) {
					$FileNameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
					$PinExists = Test-Path -Path "$envAppData\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\$($FileNameWithoutExtension).lnk"

					If ($Action -eq 'PintoTaskbar' -and $PinExists) {
						If($(Invoke-ObjectMethod -InputObject $Shell -MethodName 'CreateShortcut' -ArgumentList "$envAppData\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\$($FileNameWithoutExtension).lnk").TargetPath -eq $FilePath) {
							Write-Log -Message "Pin [$FileNameWithoutExtension] already exists." -Source ${CmdletName}
							return
						}
					}
					ElseIf ($Action -eq 'UnpinfromTaskbar' -and $PinExists -eq $false) {
						Write-Log -Message "Pin [$FileNameWithoutExtension] does not exist." -Source ${CmdletName}
						return
					}

					$ExplorerCommandHandler = Get-RegistryKey -Key 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\Windows.taskbarpin' -Value 'ExplorerCommandHandler'
					$classesStarKey = (Get-Item "Registry::HKEY_USERS\$($RunasActiveUser.SID)\SOFTWARE\Classes").OpenSubKey("*", $true)
					$shellKey = $classesStarKey.CreateSubKey("shell", $true)
					$specialKey = $shellKey.CreateSubKey("{:}", $true)
					$specialKey.SetValue("ExplorerCommandHandler", $ExplorerCommandHandler)

					$Folder = Invoke-ObjectMethod -InputObject $ShellApp -MethodName 'Namespace' -ArgumentList $(Split-Path -Path $FilePath -Parent)
					$Item = Invoke-ObjectMethod -InputObject $Folder -MethodName 'ParseName' -ArgumentList $(Split-Path -Path $FilePath -Leaf)

					$Item.InvokeVerb("{:}")

					$shellKey.DeleteSubKey("{:}")
					If ($shellKey.SubKeyCount -eq 0 -and $shellKey.ValueCount -eq 0) {
						$classesStarKey.DeleteSubKey("shell")
					}
				}
				Else {
					[string]$PinVerbAction = Get-PinVerb -VerbId $Verbs.$Action
					If (-not ($PinVerbAction)) {
						Throw "Failed to get a localized pin verb for action [$Action]. Action is not supported on this operating system."
					}

					Invoke-Verb -FilePath $FilePath -Verb $PinVerbAction
				}
			}
		}
		Catch {
			Write-Log -Message "Failed to execute action [$Action]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
		}
		Finally {
			Try { If ($shellKey) { $shellKey.Close() } } Catch { }
			Try { If ($classesStarKey) { $classesStarKey.Close() } } Catch { }
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
