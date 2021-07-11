#region Function Set-ActiveSetup
Function Set-ActiveSetup {
<#
.SYNOPSIS
	Creates an Active Setup entry in the registry to execute a file for each user upon login.
.DESCRIPTION
	Active Setup allows handling of per-user changes registry/file changes upon login.
	A registry key is created in the HKLM registry hive which gets replicated to the HKCU hive when a user logs in.
	If the "Version" value of the Active Setup entry in HKLM is higher than the version value in HKCU, the file referenced in "StubPath" is executed.
	This Function:
	- Creates the registry entries in HKLM:SOFTWARE\Microsoft\Active Setup\Installed Components\$installName.
	- Creates StubPath value depending on the file extension of the $StubExePath parameter.
	- Handles Version value with YYYYMMDDHHMMSS granularity to permit re-installs on the same day and still trigger Active Setup after Version increase.
	- Copies/overwrites the StubPath file to $StubExePath destination path if file exists in 'Files' subdirectory of script directory.
	- Executes the StubPath file for the current user based on $ExecuteForCurrentUser (no need to logout/login to trigger Active Setup).
.PARAMETER StubExePath
	Full destination path to the file that will be executed for each user that logs in.
	If this file exists in the 'Files' subdirectory of the script directory, it will be copied to the destination path.
.PARAMETER Arguments
	Arguments to pass to the file being executed.
.PARAMETER Description
	Description for the Active Setup. Users will see "Setting up personalized settings for: $Description" at logon. Default is: $installName.
.PARAMETER Key
	Name of the registry key for the Active Setup entry. Default is: $installName.
.PARAMETER Version
	Optional. Specify version for Active setup entry. Active Setup is not triggered if Version value has more than 8 consecutive digits. Use commas to get around this limitation. Default: YYYYMMDDHHMMSS
.PARAMETER Locale
	Optional. Arbitrary string used to specify the installation language of the file being executed. Not replicated to HKCU.
.PARAMETER PurgeActiveSetupKey
	Remove Active Setup entry from HKLM registry hive. Will also load each logon user's HKCU registry hive to remove Active Setup entry. Function returns after purging.
.PARAMETER DisableActiveSetup
	Disables the Active Setup entry so that the StubPath file will not be executed. This also disables -ExecuteForCurrentUser
.PARAMETER ExecuteForCurrentUser
	Specifies whether the StubExePath should be executed for the current user. Since this user is already logged in, the user won't have the application started without logging out and logging back in. Default: $True
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Set-ActiveSetup -StubExePath 'C:\Users\Public\Company\ProgramUserConfig.vbs' -Arguments '/Silent' -Description 'Program User Config' -Key 'ProgramUserConfig' -Locale 'en'
.EXAMPLE
	Set-ActiveSetup -StubExePath "$envWinDir\regedit.exe" -Arguments "/S `"%SystemDrive%\Program Files (x86)\PS App Deploy\PSAppDeployHKCUSettings.reg`"" -Description 'PS App Deploy Config' -Key 'PS_App_Deploy_Config' -ContinueOnError $true
.EXAMPLE
	Set-ActiveSetup -Key 'ProgramUserConfig' -PurgeActiveSetupKey
	Deletes "ProgramUserConfig" active setup entry from all registry hives.
.NOTES
	Original code borrowed from: Denis St-Pierre (Ottawa, Canada), Todd MacNaught (Ottawa, Canada)
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,ParameterSetName='Create')]
		[ValidateNotNullorEmpty()]
		[string]$StubExePath,
		[Parameter(Mandatory=$false,ParameterSetName='Create')]
		[ValidateNotNullorEmpty()]
		[string]$Arguments,
		[Parameter(Mandatory=$false,ParameterSetName='Create')]
		[ValidateNotNullorEmpty()]
		[string]$Description = $installName,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Key = $installName,
		[Parameter(Mandatory=$false,ParameterSetName='Create')]
		[ValidateNotNullorEmpty()]
		[string]$Version = ((Get-Date -Format 'yyMM,ddHH,mmss').ToString()), # Ex: 1405,1515,0522
		[Parameter(Mandatory=$false,ParameterSetName='Create')]
		[ValidateNotNullorEmpty()]
		[string]$Locale,
		[Parameter(Mandatory=$false,ParameterSetName='Create')]
		[ValidateNotNullorEmpty()]
		[switch]$DisableActiveSetup = $false,
		[Parameter(Mandatory=$true,ParameterSetName='Purge')]
		[switch]$PurgeActiveSetupKey,
		[Parameter(Mandatory=$false,ParameterSetName='Create')]
		[ValidateNotNullorEmpty()]
		[boolean]$ExecuteForCurrentUser = $true,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			[string]$ActiveSetupKey = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\$Key"
			[string]$HKCUActiveSetupKey = "Registry::HKEY_CURRENT_USER\Software\Microsoft\Active Setup\Installed Components\$Key"

			## Delete Active Setup registry entry from the HKLM hive and for all logon user registry hives on the system
			If ($PurgeActiveSetupKey) {
				Write-Log -Message "Removing Active Setup entry [$ActiveSetupKey]." -Source ${CmdletName}
				Remove-RegistryKey -Key $ActiveSetupKey -Recurse

				Write-Log -Message "Removing Active Setup entry [$HKCUActiveSetupKey] for all log on user registry hives on the system." -Source ${CmdletName}
				[scriptblock]$RemoveHKCUActiveSetupKey = {
					If (Get-RegistryKey -Key $HKCUActiveSetupKey -SID $UserProfile.SID) {
						Remove-RegistryKey -Key $HKCUActiveSetupKey -SID $UserProfile.SID -Recurse
					}
				}
				Invoke-HKCURegistrySettingsForAllUsers -RegistrySettings $RemoveHKCUActiveSetupKey -UserProfiles (Get-UserProfiles -ExcludeDefaultUser)
				Return
			}

			## Verify a file with a supported file extension was specified in $StubExePath
			[string[]]$StubExePathFileExtensions = '.exe', '.vbs', '.cmd', '.ps1', '.js'
			[string]$StubExeExt = [IO.Path]::GetExtension($StubExePath)
			If ($StubExePathFileExtensions -notcontains $StubExeExt) {
				Throw "Unsupported Active Setup StubPath file extension [$StubExeExt]."
			}

			## Copy file to $StubExePath from the 'Files' subdirectory of the script directory (if it exists there)
			[string]$StubExePath = [Environment]::ExpandEnvironmentVariables($StubExePath)
			[string]$ActiveSetupFileName = [IO.Path]::GetFileName($StubExePath)
			[string]$StubExeFile = Join-Path -Path $dirFiles -ChildPath $ActiveSetupFileName
			If (Test-Path -LiteralPath $StubExeFile -PathType 'Leaf') {
				#  This will overwrite the StubPath file if $StubExePath already exists on target
				Copy-File -Path $StubExeFile -Destination $StubExePath -ContinueOnError $false
			}

			## Check if the $StubExePath file exists
			If (-not (Test-Path -LiteralPath $StubExePath -PathType 'Leaf')) { Throw "Active Setup StubPath file [$ActiveSetupFileName] is missing." }

			## Define Active Setup StubPath according to file extension of $StubExePath
			Switch ($StubExeExt) {
				'.exe' {
					[string]$CUStubExePath = $StubExePath
					[string]$CUArguments = $Arguments
					[string]$StubPath = "$CUStubExePath"
				}
				'.js' {
					[string]$CUStubExePath = "$envWinDir\system32\cscript.exe"
					[string]$CUArguments = "//nologo `"$StubExePath`""
					[string]$StubPath = "$CUStubExePath $CUArguments"
				}
				'.vbs' {
					[string]$CUStubExePath = "$envWinDir\system32\cscript.exe"
					[string]$CUArguments = "//nologo `"$StubExePath`""
					[string]$StubPath = "$CUStubExePath $CUArguments"
				}
				'.cmd' {
					[string]$CUStubExePath = "$envWinDir\system32\CMD.exe"
					[string]$CUArguments = "/C `"$StubExePath`""
					[string]$StubPath = "$CUStubExePath $CUArguments"
				}
				'.ps1' {
					[string]$CUStubExePath = "$PSHOME\powershell.exe"
					[string]$CUArguments = "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command `"&{& `\`"$StubExePath`\`"}`""
					[string]$StubPath = "$CUStubExePath $CUArguments"
				}
			}
			If ($Arguments) {
				[string]$StubPath = "$StubPath $Arguments"
				If ($StubExeExt -ne '.exe') { [string]$CUArguments = "$CUArguments $Arguments" }
			}

			[scriptblock]$TestActiveSetup = {
				Param (
					[Parameter(Mandatory=$true)]
					[ValidateNotNullorEmpty()]
					[string]$HKLMKey
					,
					[Parameter(Mandatory=$true)]
					[ValidateNotNullorEmpty()]
					[string]$HKCUKey
					,
					[Parameter(Mandatory=$false)]
					[ValidateNotNullorEmpty()]
					[string]$UserSID
				)
				If ($UserSID) {
					$HKCUProps = (Get-RegistryKey -Key $HKCUKey -SID $UserSID -ContinueOnError $true)
				} else {
					$HKCUProps = (Get-RegistryKey -Key $HKCUKey -ContinueOnError $true)
				}
				$HKLMProps = (Get-RegistryKey -Key $HKLMKey -ContinueOnError $true)
				[string]$HKCUVer = $HKCUProps.Version
				[string]$HKLMVer = $HKLMProps.Version
				[int]$HKLMInst = $HKLMProps.IsInstalled
				# HKLM entry not present. Nothing to run
				If (-not $HKLMProps) {
					Write-Log "HKLM active setup entry is not present." -Source ${CmdletName}
					return $false
				}
				# HKLM entry present, but disabled. Nothing to run
				If ($HKLMInst -eq 0) {
					Write-Log "HKLM active setup entry is present, but it is disabled (IsInstalled set to 0)." -Source ${CmdletName}
					return $false
				}
				# HKLM entry present and HKCU entry is not. Run the StubPath
				If (-not $HKCUProps) {
					Write-Log "HKLM active setup entry is present. HKCU active setup entry is not present." -Source ${CmdletName}
					return $true
				}
				# Both entries present. HKLM entry does not have Version property. Nothing to run
				If (-not $HKLMVer) {
					Write-Log "HKLM and HKCU active setup entries are present. HKLM Version property is missing." -Source ${CmdletName}
					return $false
				}
				# Both entries present. HKLM entry has Version property, but HKCU entry does not. Run the StubPath
				If (-not $HKCUVer) {
					Write-Log "HKLM and HKCU active setup entries are present. HKCU Version property is missing." -Source ${CmdletName}
					return $true
				}
				# Both entries present, with a Version property. Compare the Versions
				## Remove invalid characters from Version. Only digits and commas are allowed
				[string]$HKLMValidVer = ""
				for ($i = 0; $i -lt $HKLMVer.Length; $i++) {
					if([char]::IsDigit($HKLMVer[$i]) -or ($HKLMVer[$i] -eq ',')) {$HKLMValidVer += $HKLMVer[$i]}
				}

				[string]$HKCUValidVer = ""
				for ($i = 0; $i -lt $HKCUVer.Length; $i++) {
					if([char]::IsDigit($HKCUVer[$i]) -or ($HKCUVer[$i] -eq ',')) {$HKCUValidVer += $HKCUVer[$i]}
				}
				# After cleanup, the HKLM Version is empty. Considering it missing. HKCU is present so nothing to run.
				If (-not $HKLMValidVer) {
					Write-Log "HKLM and HKCU active setup entries are present. HKLM Version property is invalid." -Source ${CmdletName}
					return $false
				}
				# the HKCU Version property is empty while HKLM Version property is not. Run the StubPath
				If (-not $HKCUValidVer) {
					Write-Log "HKLM and HKCU active setup entries are present. HKCU Version property is invalid." -Source ${CmdletName}
					return $true
				}
				# Both Version properties are present
				# Split the version by commas
				[string[]]$SplitHKLMValidVer = $HKLMValidVer.Split(',')
				[string[]]$SplitHKCUValidVer = $HKCUValidVer.Split(',')
				# Check whether the Versions were split in the same number of strings
				If ($SplitHKLMValidVer.Count -ne $SplitHKCUValidVer.Count) {
					# The versions are different length - more commas
					If ($SplitHKLMValidVer.Count -gt $SplitHKCUValidVer.Count) {
						#HKLM is longer, Run the StubPath
						Write-Log "HKLM and HKCU active setup entries are present. Both contain Version properties, however they don't contain the same amount of sub versions. HKLM Version has more sub versions." -Source ${CmdletName}
						return $true
					} else {
						#HKCU is longer, Nothing to run
						Write-Log "HKLM and HKCU active setup entries are present. Both contain Version properties, however they don't contain the same amount of sub versions. HKCU Version has more sub versions." -Source ${CmdletName}
						return $false
					}
				}
				# The Versions have the same number of strings. Compare them
				try {
					for ($i = 0; $i -lt $SplitHKLMValidVer.Count; $i++) {
						# Parse the version is UINT64
						[uint64]$ParsedHKLMVer = [uint64]::Parse($SplitHKLMValidVer[$i])
						[uint64]$ParsedHKCUVer = [uint64]::Parse($SplitHKCUValidVer[$i])
						# The HKCU ver is lower, Run the StubPath
						If ($ParsedHKCUVer -lt $ParsedHKLMVer) {
							Write-Log "HKLM and HKCU active setup entries are present. Both Version properties are present and valid, however HKCU Version property is lower." -Source ${CmdletName}
							return $true
						}
					}
					# The HKCU version is equal or higher than HKLM version, Nothing to run
					Write-Log "HKLM and HKCU active setup entries are present. Both Version properties are present and valid, however they are either the same or HKCU Version property is higher." -Source ${CmdletName}
					return $false
				}
				catch {
					# Failed to parse strings as UInts, Run the StubPath
					Write-Log "HKLM and HKCU active setup entries are present. Both Version properties are present and valid, however parsing strings to uintegers failed." -Severity 2  -Source ${CmdletName}
					return $true
				}
			}

			## Create the Active Setup entry in the registry
			[scriptblock]$SetActiveSetupRegKeys = {
				Param (
					[Parameter(Mandatory=$true)]
					[ValidateNotNullorEmpty()]
					[string]$ActiveSetupRegKey,
					[Parameter(Mandatory=$false)]
					[ValidateNotNullorEmpty()]
					[string]$SID
				)
				If ($SID) {
					Set-RegistryKey -Key $ActiveSetupRegKey -Name '(Default)' -Value $Description -SID $SID -ContinueOnError $false
					Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Version' -Value $Version -SID $SID -ContinueOnError $false
					Set-RegistryKey -Key $ActiveSetupRegKey -Name 'StubPath' -Value $StubPath -Type 'String' -SID $SID -ContinueOnError $false
					If ($Locale) { Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Locale' -Value $Locale -SID $SID -ContinueOnError $false }
					# Only Add IsInstalled to HKLM
					If ($ActiveSetupRegKey.Contains("HKEY_LOCAL_MACHINE")) {
						If ($DisableActiveSetup) {
							Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 0 -Type 'DWord' -SID $SID -ContinueOnError $false
						} Else {
							Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 1 -Type 'DWord' -SID $SID -ContinueOnError $false
						}
					}
				} Else {
					Set-RegistryKey -Key $ActiveSetupRegKey -Name '(Default)' -Value $Description -ContinueOnError $false
					Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Version' -Value $Version -ContinueOnError $false
					Set-RegistryKey -Key $ActiveSetupRegKey -Name 'StubPath' -Value $StubPath -Type 'String' -ContinueOnError $false
					If ($Locale) { Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Locale' -Value $Locale -ContinueOnError $false }
					# Only Add IsInstalled to HKLM
					If ($ActiveSetupRegKey.Contains("HKEY_LOCAL_MACHINE")) {
						If ($DisableActiveSetup) {
							Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 0 -Type 'DWord' -ContinueOnError $false
						} Else {
							Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 1 -Type 'DWord' -ContinueOnError $false
						}
					}
				}
			}

			Write-Log -Message "Adding Active Setup Key for local machine: [$ActiveSetupKey]." -Source ${CmdletName}
			& $SetActiveSetupRegKeys -ActiveSetupRegKey $ActiveSetupKey

			## Execute the StubPath file for the current user as long as not in Session 0
			If ($ExecuteForCurrentUser) {
				If ($SessionZero) {
					If ($RunAsActiveUser) {
						# Skip if Active Setup reg key is present and Version is equal or higher
						[bool]$InstallNeeded = (& $TestActiveSetup -HKLMKey $ActiveSetupKey -HKCUKey $HKCUActiveSetupKey -UserSID $RunAsActiveUser.SID)
						If ($InstallNeeded) {
							Write-Log -Message "Session 0 detected: Executing Active Setup StubPath file for currently logged in user [$($RunAsActiveUser.NTAccount)]." -Source ${CmdletName}
							If ($CUArguments) {
								Execute-ProcessAsUser -Path $CUStubExePath -Parameters $CUArguments -Wait -ContinueOnError $true
							}
							Else {
								Execute-ProcessAsUser -Path $CUStubExePath -Wait -ContinueOnError $true
							}

							Write-Log -Message "Adding Active Setup Key for the current user: [$HKCUActiveSetupKey]." -Source ${CmdletName}
							& $SetActiveSetupRegKeys -ActiveSetupRegKey $HKCUActiveSetupKey -SID $RunAsActiveUser.SID
						} else {
							Write-Log -Message "Session 0 detected: Skipping executing Active Setup StubPath file for currently logged in user [$($RunAsActiveUser.NTAccount)]." -Source ${CmdletName} -Severity 2
						}
					}
					Else {
						Write-Log -Message 'Session 0 detected: No logged in users detected. Active Setup StubPath file will execute when users first log into their account.' -Source ${CmdletName}
					}
				}
				Else {
					# Skip if Active Setup reg key is present and Version is equal or higher
					[bool]$InstallNeeded = (& $TestActiveSetup -HKLMKey $ActiveSetupKey -HKCUKey $HKCUActiveSetupKey)
					If ($InstallNeeded) {
						Write-Log -Message 'Executing Active Setup StubPath file for the current user.' -Source ${CmdletName}
						If ($CUArguments) {
							Execute-Process -FilePath $CUStubExePath -Parameters $CUArguments -ExitOnProcessFailure $false
						}
						Else {
							Execute-Process -FilePath $CUStubExePath -ExitOnProcessFailure $false
						}

						Write-Log -Message "Adding Active Setup Key for the current user: [$HKCUActiveSetupKey]." -Source ${CmdletName}
						& $SetActiveSetupRegKeys -ActiveSetupRegKey $HKCUActiveSetupKey
					} else {
						Write-Log -Message "Skipping executing Active Setup StubPath file for current user." -Source ${CmdletName} -Severity 2
					}
				}
			}
		}
		Catch {
			Write-Log -Message "Failed to set Active Setup registry entry. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to set Active Setup registry entry: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
