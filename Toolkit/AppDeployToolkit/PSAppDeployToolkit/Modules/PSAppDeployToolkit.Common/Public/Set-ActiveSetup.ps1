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

Use this parameter to specify the destination path of the file that will be executed upon user login.

Note: Place the file you want users to execute in the '\Files' subdirectory of the script directory and the toolkit will install it to the path specificed in this parameter.

.PARAMETER Arguments

Arguments to pass to the file being executed.

.PARAMETER Description

Description for the Active Setup. Users will see "Setting up personalized settings for: $Description" at logon. Default is: $installName.

.PARAMETER Key

Name of the registry key for the Active Setup entry. Default is: $installName.

.PARAMETER Wow6432Node

Specify this switch to use Active Setup entry under Wow6432Node on a 64-bit OS. Default is: $false.

.PARAMETER Version

Optional. Specify version for Active setup entry. Active Setup is not triggered if Version value has more than 8 consecutive digits. Use commas to get around this limitation. Default: YYYYMMDDHHMMSS

Note:
    - Do not use this parameter if it is not necessary. PSADT will handle this parameter automatically using the time of the installation as the version number.
    - In Windows 10, Scripts and EXEs might be blocked by AppLocker. Ensure that the path given to -StubExePath will permit end users to run Scripts and EXEs unelevated.

.PARAMETER Locale

Optional. Arbitrary string used to specify the installation language of the file being executed. Not replicated to HKCU.

.PARAMETER PurgeActiveSetupKey

Remove Active Setup entry from HKLM registry hive. Will also load each logon user's HKCU registry hive to remove Active Setup entry. Function returns after purging.

.PARAMETER DisableActiveSetup

Disables the Active Setup entry so that the StubPath file will not be executed. This also disables -ExecuteForCurrentUser

.PARAMETER ExecuteForCurrentUser

Specifies whether the StubExePath should be executed for the current user. Since this user is already logged in, the user won't have the application started without logging out and logging back in. Default: $true

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Boolean

Returns $true if Active Setup entry was created or updated, $false if Active Setup entry was not created or updated.

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

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$StubExePath,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$Arguments,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$Description = (Get-ADTSession).GetPropertyValue('installName'),
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Key = (Get-ADTSession).GetPropertyValue('installName'),
        [Parameter(Mandatory = $false)]
        [Switch]$Wow6432Node = $false,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$Version = ((Get-Date -Format 'yyMM,ddHH,mmss').ToString()), # Ex: 1405,1515,0522
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$Locale,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [Switch]$DisableActiveSetup = $false,
        [Parameter(Mandatory = $true, ParameterSetName = 'Purge')]
        [Switch]$PurgeActiveSetupKey,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [Boolean]$ExecuteForCurrentUser = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        $adtEnv = Get-ADTEnvironment
        Write-ADTDebugHeader
    }
    Process {
        Try {
            if ($Wow6432Node -and $adtEnv.Is64Bit) {
                [String]$ActiveSetupKey = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\$Key"
                [String]$HKCUActiveSetupKey = "Registry::HKEY_CURRENT_USER\Software\Wow6432Node\Microsoft\Active Setup\Installed Components\$Key"
            }
            else {
                [String]$ActiveSetupKey = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\$Key"
                [String]$HKCUActiveSetupKey = "Registry::HKEY_CURRENT_USER\Software\Microsoft\Active Setup\Installed Components\$Key"
            }

            ## Delete Active Setup registry entry from the HKLM hive and for all logon user registry hives on the system
            If ($PurgeActiveSetupKey) {
                Write-ADTLogEntry -Message "Removing Active Setup entry [$ActiveSetupKey]."
                Remove-RegistryKey -Key $ActiveSetupKey -Recurse

                Write-ADTLogEntry -Message "Removing Active Setup entry [$HKCUActiveSetupKey] for all log on user registry hives on the system."
                [ScriptBlock]$RemoveHKCUActiveSetupKey = {
                    If (Get-RegistryKey -Key $HKCUActiveSetupKey -SID $adtEnv.RunAsActiveUser.SID) {
                        Remove-RegistryKey -Key $HKCUActiveSetupKey -SID $adtEnv.RunAsActiveUser.SID -Recurse
                    }
                }
                Invoke-ADTAllUsersRegistryChange -RegistrySettings $RemoveHKCUActiveSetupKey -UserProfiles (Get-ADTUserProfiles -ExcludeDefaultUser)
                Return
            }

            ## Verify a file with a supported file extension was specified in $StubExePath
            [String[]]$StubExePathFileExtensions = '.exe', '.vbs', '.cmd', '.ps1', '.js'
            [String]$StubExeExt = [IO.Path]::GetExtension($StubExePath)
            If ($StubExePathFileExtensions -notcontains $StubExeExt) {
                Throw "Unsupported Active Setup StubPath file extension [$StubExeExt]."
            }

            ## Copy file to $StubExePath from the 'Files' subdirectory of the script directory (if it exists there)
            [String]$StubExePath = [Environment]::ExpandEnvironmentVariables($StubExePath)
            [String]$ActiveSetupFileName = [IO.Path]::GetFileName($StubExePath)
            [String]$StubExeFile = Join-Path -Path (Get-ADTSession).GetPropertyValue('dirFiles') -ChildPath $ActiveSetupFileName
            If (Test-Path -LiteralPath $StubExeFile -PathType 'Leaf') {
                #  This will overwrite the StubPath file if $StubExePath already exists on target
                Copy-File -Path $StubExeFile -Destination $StubExePath -ContinueOnError $false
            }

            ## Check if the $StubExePath file exists
            If (-not (Test-Path -LiteralPath $StubExePath -PathType 'Leaf')) {
                Throw "Active Setup StubPath file [$ActiveSetupFileName] is missing."
            }

            ## Define Active Setup StubPath according to file extension of $StubExePath
            Switch ($StubExeExt) {
                '.exe' {
                    [String]$CUStubExePath = "$StubExePath"
                    [String]$CUArguments = $Arguments
                    [String]$StubPath = "`"$CUStubExePath`""
                }
                '.js' {
                    [String]$CUStubExePath = "$env:WinDir\System32\cscript.exe"
                    [String]$CUArguments = "//nologo `"$StubExePath`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
                '.vbs' {
                    [String]$CUStubExePath = "$env:WinDir\System32\cscript.exe"
                    [String]$CUArguments = "//nologo `"$StubExePath`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
                '.cmd' {
                    [String]$CUStubExePath = "$env:WinDir\System32\cmd.exe"
                    [String]$CUArguments = "/C `"$StubExePath`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
                '.ps1' {
                    [String]$CUStubExePath = $adtEnv.envPSProcessPath
                    [String]$CUArguments = "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command `"& {& `\`"$StubExePath`\`"}`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
            }
            If ($Arguments) {
                [String]$StubPath = "$StubPath $Arguments"
                If ($StubExeExt -ne '.exe') {
                    [String]$CUArguments = "$CUArguments $Arguments"
                }
            }

            [ScriptBlock]$TestActiveSetup = {
                Param (
                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullorEmpty()]
                    [String]$HKLMKey,
                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullorEmpty()]
                    [String]$HKCUKey,
                    [Parameter(Mandatory = $false)]
                    [ValidateNotNullorEmpty()]
                    [String]$UserSID
                )
                If ($UserSID) {
                    $HKCUProps = (Get-RegistryKey -Key $HKCUKey -SID $UserSID -ContinueOnError $true)
                }
                Else {
                    $HKCUProps = (Get-RegistryKey -Key $HKCUKey -ContinueOnError $true)
                }

                $HKLMProps = (Get-RegistryKey -Key $HKLMKey -ContinueOnError $true)
                [String]$HKCUVer = $HKCUProps.Version
                [String]$HKLMVer = $HKLMProps.Version
                [Int32]$HKLMInst = $HKLMProps.IsInstalled

                # HKLM entry not present. Nothing to run.
                If (-not $HKLMProps) {
                    Write-ADTLogEntry 'HKLM active setup entry is not present.'
                    Return ($false)
                }
                # HKLM entry present, but disabled. Nothing to run.
                If ($HKLMInst -eq 0) {
                    Write-ADTLogEntry 'HKLM active setup entry is present, but it is disabled (IsInstalled set to 0).'
                    Return ($false)
                }
                # HKLM entry present and HKCU entry is not. Run the StubPath.
                If (-not $HKCUProps) {
                    Write-ADTLogEntry 'HKLM active setup entry is present. HKCU active setup entry is not present.'
                    Return ($true)
                }
                # Both entries present. HKLM entry does not have Version property. Nothing to run.
                If (-not $HKLMVer) {
                    Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. HKLM Version property is missing.'
                    Return ($false)
                }
                # Both entries present. HKLM entry has Version property, but HKCU entry does not. Run the StubPath.
                If (-not $HKCUVer) {
                    Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. HKCU Version property is missing.'
                    Return ($true)
                }

                # Remove invalid characters from Version property. Only digits and commas are allowed.
                [String]$HKLMValidVer = ''
                For ($i = 0; $i -lt $HKLMVer.Length; $i++) {
                    If ([Char]::IsDigit($HKLMVer[$i]) -or ($HKLMVer[$i] -eq ',')) {
                        $HKLMValidVer += $HKLMVer[$i]
                    }
                }

                [String]$HKCUValidVer = ''
                For ($i = 0; $i -lt $HKCUVer.Length; $i++) {
                    If ([Char]::IsDigit($HKCUVer[$i]) -or ($HKCUVer[$i] -eq ',')) {
                        $HKCUValidVer += $HKCUVer[$i]
                    }
                }

                # After cleanup, the HKLM Version property is empty. Considering it missing. HKCU is present so nothing to run.
                If (-not $HKLMValidVer) {
                    Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. HKLM Version property is invalid.'
                    Return ($false)
                }

                # After cleanup, the HKCU Version property is empty while HKLM Version property is not. Run the StubPath.
                If (-not $HKCUValidVer) {
                    Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. HKCU Version property is invalid.'
                    Return ($true)
                }

                ## Both entries present, with a Version property. Compare the Versions.
                # Convert the version property to Version type and compare
                [Version]$VersionHKLMValidVer = $null
                [Version]$VersionHKCUValidVer = $null
                Try {
                    [Version]$VersionHKLMValidVer = [Version]$HKLMValidVer.Replace(',','.')
                    [Version]$VersionHKCUValidVer = [Version]$HKCUValidVer.Replace(',','.')

                    If ($VersionHKLMValidVer -gt $VersionHKCUValidVer) {
                        # HKLM is greater, run the StubPath.
                        Write-ADTLogEntry "HKLM and HKCU active setup entries are present. Both contain Version properties, and the HKLM Version is greater."
                        Return ($true)
                    }
                    Else {
                        # The HKCU version is equal or higher than HKLM version, Nothing to run
                        Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. Both contain Version properties. However, they are either the same or the HKCU Version property is higher.'
                        Return ($false)
                    }
                }
                Catch {
                    # Failed to convert version property to Version type.
                }

                # Check whether the Versions were split into the same number of strings
                # Split the version by commas
                [String[]]$SplitHKLMValidVer = $HKLMValidVer.Split(',')
                [String[]]$SplitHKCUValidVer = $HKCUValidVer.Split(',')
                If ($SplitHKLMValidVer.Count -ne $SplitHKCUValidVer.Count) {
                    # The versions are different length - more commas
                    If ($SplitHKLMValidVer.Count -gt $SplitHKCUValidVer.Count) {
                        # HKLM is longer, Run the StubPath
                        Write-ADTLogEntry "HKLM and HKCU active setup entries are present. Both contain Version properties. However, the HKLM Version has more version fields."
                        Return ($true)
                    }
                    Else {
                        # HKCU is longer, Nothing to run
                        Write-ADTLogEntry "HKLM and HKCU active setup entries are present. Both contain Version properties. However, the HKCU Version has more version fields."
                        Return ($false)
                    }
                }

                # The Versions have the same number of strings. Compare them
                Try {
                    For ($i = 0; $i -lt $SplitHKLMValidVer.Count; $i++) {
                        # Parse the version is UINT64
                        [UInt64]$ParsedHKLMVer = [UInt64]::Parse($SplitHKLMValidVer[$i])
                        [UInt64]$ParsedHKCUVer = [UInt64]::Parse($SplitHKCUValidVer[$i])
                        # The HKCU ver is lower, Run the StubPath
                        If ($ParsedHKCUVer -lt $ParsedHKLMVer) {
                            Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. Both Version properties are present and valid. However, HKCU Version property is lower.'
                            Return ($true)
                        }
                    }
                    # The HKCU version is equal or higher than HKLM version, Nothing to run
                    Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. Both Version properties are present and valid. However, they are either the same or HKCU Version property is higher.'
                    Return ($false)
                }
                Catch {
                    # Failed to parse strings as UInt64, Run the StubPath
                    Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. Both Version properties are present and valid. However, parsing string numerics to 64-bit integers failed.' -Severity 2
                    Return ($true)
                }
            }

            ## Create the Active Setup entry in the registry
            [ScriptBlock]$SetActiveSetupRegKeys = {
                Param (
                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullorEmpty()]
                    [String]$ActiveSetupRegKey,
                    [Parameter(Mandatory = $false)]
                    [ValidateNotNullorEmpty()]
                    [String]$SID
                )
                If ($SID) {
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name '(Default)' -Value $Description -SID $SID -ContinueOnError $false
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Version' -Value $Version -SID $SID -ContinueOnError $false
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name 'StubPath' -Value $StubPath -Type 'String' -SID $SID -ContinueOnError $false
                    If ($Locale) {
                        Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Locale' -Value $Locale -SID $SID -ContinueOnError $false
                    }
                    # Only Add IsInstalled to HKLM
                    If ($ActiveSetupRegKey.Contains('HKEY_LOCAL_MACHINE')) {
                        If ($DisableActiveSetup) {
                            Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 0 -Type 'DWord' -SID $SID -ContinueOnError $false
                        }
                        Else {
                            Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 1 -Type 'DWord' -SID $SID -ContinueOnError $false
                        }
                    }
                }
                Else {
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name '(Default)' -Value $Description -ContinueOnError $false
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Version' -Value $Version -ContinueOnError $false
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name 'StubPath' -Value $StubPath -Type 'String' -ContinueOnError $false
                    If ($Locale) {
                        Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Locale' -Value $Locale -ContinueOnError $false
                    }
                    # Only Add IsInstalled to HKLM
                    If ($ActiveSetupRegKey.Contains('HKEY_LOCAL_MACHINE')) {
                        If ($DisableActiveSetup) {
                            Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 0 -Type 'DWord' -ContinueOnError $false
                        }
                        Else {
                            Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 1 -Type 'DWord' -ContinueOnError $false
                        }
                    }
                }
            }

            Write-ADTLogEntry -Message "Adding Active Setup Key for local machine: [$ActiveSetupKey]."
            & $SetActiveSetupRegKeys -ActiveSetupRegKey $ActiveSetupKey

            ## Execute the StubPath file for the current user as long as not in Session 0
            If ($ExecuteForCurrentUser) {
                If ($adtEnv.SessionZero) {
                    If ($adtEnv.RunAsActiveUser) {
                        # Skip if Active Setup reg key is present and Version is equal or higher
                        [Boolean]$InstallNeeded = (& $TestActiveSetup -HKLMKey $ActiveSetupKey -HKCUKey $HKCUActiveSetupKey -UserSID $adtEnv.RunAsActiveUser.SID)
                        If ($InstallNeeded) {
                            Write-ADTLogEntry -Message "Session 0 detected: Executing Active Setup StubPath file for currently logged in user [$($adtEnv.RunAsActiveUser.NTAccount)]."
                            If ($CUArguments) {
                                Execute-ProcessAsUser -Path $CUStubExePath -Parameters $CUArguments -Wait -ContinueOnError $true
                            }
                            Else {
                                Execute-ProcessAsUser -Path $CUStubExePath -Wait -ContinueOnError $true
                            }

                            Write-ADTLogEntry -Message "Adding Active Setup Key for the current user: [$HKCUActiveSetupKey]."
                            & $SetActiveSetupRegKeys -ActiveSetupRegKey $HKCUActiveSetupKey -SID $adtEnv.RunAsActiveUser.SID
                        }
                        Else {
                            Write-ADTLogEntry -Message "Session 0 detected: Skipping executing Active Setup StubPath file for currently logged in user [$($adtEnv.RunAsActiveUser.NTAccount)]." -Severity 2
                        }
                    }
                    Else {
                        Write-ADTLogEntry -Message 'Session 0 detected: No logged in users detected. Active Setup StubPath file will execute when users first log into their account.'
                    }
                }
                Else {
                    # Skip if Active Setup reg key is present and Version is equal or higher
                    [Boolean]$InstallNeeded = (& $TestActiveSetup -HKLMKey $ActiveSetupKey -HKCUKey $HKCUActiveSetupKey)
                    If ($InstallNeeded) {
                        Write-ADTLogEntry -Message 'Executing Active Setup StubPath file for the current user.'
                        If ($CUArguments) {
                            Execute-Process -FilePath $CUStubExePath -Parameters $CUArguments -ExitOnProcessFailure $false
                        }
                        Else {
                            Execute-Process -FilePath $CUStubExePath -ExitOnProcessFailure $false
                        }

                        Write-ADTLogEntry -Message "Adding Active Setup Key for the current user: [$HKCUActiveSetupKey]."
                        & $SetActiveSetupRegKeys -ActiveSetupRegKey $HKCUActiveSetupKey
                    }
                    Else {
                        Write-ADTLogEntry -Message 'Skipping executing Active Setup StubPath file for current user.' -Severity 2
                    }
                }
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to set Active Setup registry entry.`n$(Resolve-ADTError)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to set Active Setup registry entry: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
