#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Convert-RegistryPath {
    <#
.SYNOPSIS

Converts the specified registry key path to a format that is compatible with built-in PowerShell cmdlets.

.DESCRIPTION

Converts the specified registry key path to a format that is compatible with built-in PowerShell cmdlets.

Converts registry key hives to their full paths. Example: HKLM is converted to "Registry::HKEY_LOCAL_MACHINE".

.PARAMETER Key

Path to the registry key to convert (can be a registry hive or fully qualified path)

.PARAMETER Wow6432Node

Specifies that the 32-bit registry view (Wow6432Node) should be used on a 64-bit system.

.PARAMETER SID

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

.PARAMETER DisableFunctionLogging

Disables logging of this function. Default: $true

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the converted registry key path.

.EXAMPLE

Convert-RegistryPath -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'

.EXAMPLE

Convert-RegistryPath -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Key,
        [Parameter(Mandatory = $false)]
        [Switch]$Wow6432Node = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$SID,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$DisableFunctionLogging = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## Convert the registry key hive to the full path, only match if at the beginning of the line
        If ($Key -match '^HKLM') {
            $Key = $Key -replace '^HKLM:\\', 'HKEY_LOCAL_MACHINE\' -replace '^HKLM:', 'HKEY_LOCAL_MACHINE\' -replace '^HKLM\\', 'HKEY_LOCAL_MACHINE\'
        }
        ElseIf ($Key -match '^HKCR') {
            $Key = $Key -replace '^HKCR:\\', 'HKEY_CLASSES_ROOT\' -replace '^HKCR:', 'HKEY_CLASSES_ROOT\' -replace '^HKCR\\', 'HKEY_CLASSES_ROOT\'
        }
        ElseIf ($Key -match '^HKCU') {
            $Key = $Key -replace '^HKCU:\\', 'HKEY_CURRENT_USER\' -replace '^HKCU:', 'HKEY_CURRENT_USER\' -replace '^HKCU\\', 'HKEY_CURRENT_USER\'
        }
        ElseIf ($Key -match '^HKU') {
            $Key = $Key -replace '^HKU:\\', 'HKEY_USERS\' -replace '^HKU:', 'HKEY_USERS\' -replace '^HKU\\', 'HKEY_USERS\'
        }
        ElseIf ($Key -match '^HKCC') {
            $Key = $Key -replace '^HKCC:\\', 'HKEY_CURRENT_CONFIG\' -replace '^HKCC:', 'HKEY_CURRENT_CONFIG\' -replace '^HKCC\\', 'HKEY_CURRENT_CONFIG\'
        }
        ElseIf ($Key -match '^HKPD') {
            $Key = $Key -replace '^HKPD:\\', 'HKEY_PERFORMANCE_DATA\' -replace '^HKPD:', 'HKEY_PERFORMANCE_DATA\' -replace '^HKPD\\', 'HKEY_PERFORMANCE_DATA\'
        }

        If ($Wow6432Node -and $Script:ADT.Environment.Is64BitProcess) {
            If ($Key -match '^(HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\|HKEY_CURRENT_USER\\SOFTWARE\\Classes\\|HKEY_CLASSES_ROOT\\)(AppID\\|CLSID\\|DirectShow\\|Interface\\|Media Type\\|MediaFoundation\\|PROTOCOLS\\|TypeLib\\)') {
                $Key = $Key -replace '^(HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\|HKEY_CURRENT_USER\\SOFTWARE\\Classes\\|HKEY_CLASSES_ROOT\\)(AppID\\|CLSID\\|DirectShow\\|Interface\\|Media Type\\|MediaFoundation\\|PROTOCOLS\\|TypeLib\\)', '$1Wow6432Node\$2'
            }
            ElseIf ($Key -match '^HKEY_LOCAL_MACHINE\\SOFTWARE\\') {
                $Key = $Key -replace '^HKEY_LOCAL_MACHINE\\SOFTWARE\\', 'HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\'
            }
            ElseIf ($Key -match '^HKEY_LOCAL_MACHINE\\SOFTWARE$') {
                $Key = $Key -replace '^HKEY_LOCAL_MACHINE\\SOFTWARE$', 'HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node'
            }
            ElseIf ($Key -match '^HKEY_CURRENT_USER\\Software\\Microsoft\\Active Setup\\Installed Components\\') {
                $Key = $Key -replace '^HKEY_CURRENT_USER\\Software\\Wow6432Node\\Microsoft\\Active Setup\\Installed Components\\', 'HKEY_CURRENT_USER\Software\Wow6432Node\Microsoft\Active Setup\Installed Components\'
            }
        }

        ## Append the PowerShell provider to the registry key path
        If ($key -notmatch '^Registry::') {
            [String]$key = "Registry::$key"
        }

        If ($PSBoundParameters.ContainsKey('SID')) {
            ## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
            If ($key -match '^Registry::HKEY_CURRENT_USER\\') {
                $key = $key -replace '^Registry::HKEY_CURRENT_USER\\', "Registry::HKEY_USERS\$SID\"
            }
            ElseIf (-not $DisableFunctionLogging) {
                Write-Log -Message 'SID parameter specified but the registry hive of the key is not HKEY_CURRENT_USER.' -Source ${CmdletName} -Severity 2
            }
        }

        If ($Key -match '^Registry::HKEY_LOCAL_MACHINE|^Registry::HKEY_CLASSES_ROOT|^Registry::HKEY_CURRENT_USER|^Registry::HKEY_USERS|^Registry::HKEY_CURRENT_CONFIG|^Registry::HKEY_PERFORMANCE_DATA') {
            ## Check for expected key string format
            If (-not $DisableFunctionLogging) {
                Write-Log -Message "Return fully qualified registry key path [$key]." -Source ${CmdletName}
            }
            Write-Output -InputObject ($key)
        }
        Else {
            #  If key string is not properly formatted, throw an error
            Throw "Unable to detect target registry hive in string [$key]."
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Test-RegistryValue {
    <#
.SYNOPSIS

Test if a registry value exists.

.DESCRIPTION

Checks a registry key path to see if it has a value with a given name. Can correctly handle cases where a value simply has an empty or null value.

.PARAMETER Key

Path of the registry key.

.PARAMETER Value

Specify the registry key value to check the existence of.

.PARAMETER SID

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

.PARAMETER Wow6432Node

Specify this switch to check the 32-bit registry (Wow6432Node) on 64-bit systems.

.INPUTS

System.String

Accepts a string value for the registry key path.

.OUTPUTS

System.String

Returns $true if the registry value exists, $false if it does not.

.EXAMPLE

Test-RegistryValue -Key 'HKLM:SYSTEM\CurrentControlSet\Control\Session Manager' -Value 'PendingFileRenameOperations'

.NOTES

To test if registry key exists, use Test-Path function like so:

Test-Path -Path $Key -PathType 'Container'

.LINK

https://psappdeploytoolkit.com
#>
    Param (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]$Key,
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullOrEmpty()]$Value,
        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateNotNullorEmpty()]
        [String]$SID,
        [Parameter(Mandatory = $false)]
        [Switch]$Wow6432Node = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
        Try {
            If ($PSBoundParameters.ContainsKey('SID')) {
                [String]$Key = Convert-RegistryPath -Key $Key -Wow6432Node:$Wow6432Node -SID $SID
            }
            Else {
                [String]$Key = Convert-RegistryPath -Key $Key -Wow6432Node:$Wow6432Node
            }
        }
        Catch {
            Throw
        }
        [Boolean]$IsRegistryValueExists = $false
        Try {
            If (Test-Path -LiteralPath $Key -ErrorAction 'Stop') {
                [String[]]$PathProperties = Get-Item -LiteralPath $Key -ErrorAction 'Stop' | Select-Object -ExpandProperty 'Property' -ErrorAction 'Stop'
                If ($PathProperties -contains $Value) {
                    $IsRegistryValueExists = $true
                }
            }
        }
        Catch {
        }

        If ($IsRegistryValueExists) {
            Write-Log -Message "Registry key value [$Key] [$Value] does exist." -Source ${CmdletName}
        }
        Else {
            Write-Log -Message "Registry key value [$Key] [$Value] does not exist." -Source ${CmdletName}
        }
        Write-Output -InputObject ($IsRegistryValueExists)
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-RegistryKey {
    <#
.SYNOPSIS

Retrieves value names and value data for a specified registry key or optionally, a specific value.

.DESCRIPTION

Retrieves value names and value data for a specified registry key or optionally, a specific value.

If the registry key does not exist or contain any values, the function will return $null by default. To test for existence of a registry key path, use built-in Test-Path cmdlet.

.PARAMETER Key

Path of the registry key.

.PARAMETER Value

Value to retrieve (optional).

.PARAMETER Wow6432Node

Specify this switch to read the 32-bit registry (Wow6432Node) on 64-bit systems.

.PARAMETER SID

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

.PARAMETER ReturnEmptyKeyIfExists

Return the registry key if it exists but it has no property/value pairs underneath it. Default is: $false.

.PARAMETER DoNotExpandEnvironmentNames

Return unexpanded REG_EXPAND_SZ values. Default is: $false.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the value of the registry key or value.

.EXAMPLE

Get-RegistryKey -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'

.EXAMPLE

Get-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\iexplore.exe'

.EXAMPLE

Get-RegistryKey -Key 'HKLM:Software\Wow6432Node\Microsoft\Microsoft SQL Server Compact Edition\v3.5' -Value 'Version'

.EXAMPLE

Get-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment' -Value 'Path' -DoNotExpandEnvironmentNames

Returns %ProgramFiles%\Java instead of C:\Program Files\Java

.EXAMPLE

Get-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Example' -Value '(Default)'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Key,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$Value,
        [Parameter(Mandatory = $false)]
        [Switch]$Wow6432Node = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$SID,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$ReturnEmptyKeyIfExists = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$DoNotExpandEnvironmentNames = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            ## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
            If ($PSBoundParameters.ContainsKey('SID')) {
                [String]$key = Convert-RegistryPath -Key $key -Wow6432Node:$Wow6432Node -SID $SID
            }
            Else {
                [String]$key = Convert-RegistryPath -Key $key -Wow6432Node:$Wow6432Node
            }

            ## Check if the registry key exists
            If (-not (Test-Path -LiteralPath $key -ErrorAction 'Stop')) {
                Write-Log -Message "Registry key [$key] does not exist. Return `$null." -Severity 2 -Source ${CmdletName}
                $regKeyValue = $null
            }
            Else {
                If ($PSBoundParameters.ContainsKey('Value')) {
                    Write-Log -Message "Getting registry key [$key] value [$value]." -Source ${CmdletName}
                }
                Else {
                    Write-Log -Message "Getting registry key [$key] and all property values." -Source ${CmdletName}
                }

                ## Get all property values for registry key
                $regKeyValue = Get-ItemProperty -LiteralPath $key -ErrorAction 'Stop'
                [Int32]$regKeyValuePropertyCount = $regKeyValue | Measure-Object | Select-Object -ExpandProperty 'Count'

                ## Select requested property
                If ($PSBoundParameters.ContainsKey('Value')) {
                    #  Check if registry value exists
                    [Boolean]$IsRegistryValueExists = $false
                    If ($regKeyValuePropertyCount -gt 0) {
                        Try {
                            [string[]]$PathProperties = Get-Item -LiteralPath $Key -ErrorAction 'Stop' | Select-Object -ExpandProperty 'Property' -ErrorAction 'Stop'
                            If ($PathProperties -contains $Value) {
                                $IsRegistryValueExists = $true
                            }
                        }
                        Catch {
                        }
                    }

                    #  Get the Value (do not make a strongly typed variable because it depends entirely on what kind of value is being read)
                    If ($IsRegistryValueExists) {
                        If ($DoNotExpandEnvironmentNames) {
                            #Only useful on 'ExpandString' values
                            If ($Value -like '(Default)') {
                                $regKeyValue = $(Get-Item -LiteralPath $key -ErrorAction 'Stop').GetValue($null, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
                            }
                            Else {
                                $regKeyValue = $(Get-Item -LiteralPath $key -ErrorAction 'Stop').GetValue($Value, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
                            }
                        }
                        ElseIf ($Value -like '(Default)') {
                            $regKeyValue = $(Get-Item -LiteralPath $key -ErrorAction 'Stop').GetValue($null)
                        }
                        Else {
                            $regKeyValue = $regKeyValue | Select-Object -ExpandProperty $Value -ErrorAction 'SilentlyContinue'
                        }
                    }
                    Else {
                        Write-Log -Message "Registry key value [$Key] [$Value] does not exist. Return `$null." -Source ${CmdletName}
                        $regKeyValue = $null
                    }
                }
                ## Select all properties or return empty key object
                Else {
                    If ($regKeyValuePropertyCount -eq 0) {
                        If ($ReturnEmptyKeyIfExists) {
                            Write-Log -Message "No property values found for registry key. Return empty registry key object [$key]." -Source ${CmdletName}
                            $regKeyValue = Get-Item -LiteralPath $key -Force -ErrorAction 'Stop'
                        }
                        Else {
                            Write-Log -Message "No property values found for registry key. Return `$null." -Source ${CmdletName}
                            $regKeyValue = $null
                        }
                    }
                }
            }
            Write-Output -InputObject ($regKeyValue)
        }
        Catch {
            If (-not $Value) {
                Write-Log -Message "Failed to read registry key [$key]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to read registry key [$key]: $($_.Exception.Message)"
                }
            }
            Else {
                Write-Log -Message "Failed to read registry key [$key] value [$value]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to read registry key [$key] value [$value]: $($_.Exception.Message)"
                }
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Set-RegistryKey {
    <#
.SYNOPSIS

Creates a registry key name, value, and value data; it sets the same if it already exists.

.DESCRIPTION

Creates a registry key name, value, and value data; it sets the same if it already exists.

.PARAMETER Key

The registry key path.

.PARAMETER Name

The value name.

.PARAMETER Value

The value data.

.PARAMETER Type

The type of registry value to create or set. Options: 'Binary','DWord','ExpandString','MultiString','None','QWord','String','Unknown'. Default: String.

DWord should be specified as a decimal.

.PARAMETER Wow6432Node

Specify this switch to write to the 32-bit registry (Wow6432Node) on 64-bit systems.

.PARAMETER SID

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Set-RegistryKey -Key $blockedAppPath -Name 'Debugger' -Value $blockedAppDebuggerValue

.EXAMPLE

Set-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE' -Name 'Application' -Type 'DWord' -Value '1'

.EXAMPLE

Set-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce' -Name 'Debugger' -Value $blockedAppDebuggerValue -Type String

.EXAMPLE

Set-RegistryKey -Key 'HKCU\Software\Microsoft\Example' -Name 'Data' -Value (0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x02,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x02,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x00,0x01,0x01,0x01,0x02,0x02,0x02) -Type 'Binary'

.EXAMPLE

Set-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Example' -Name '(Default)' -Value "Text"

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Key,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        $Value,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Binary', 'DWord', 'ExpandString', 'MultiString', 'None', 'QWord', 'String', 'Unknown')]
        [Microsoft.Win32.RegistryValueKind]$Type = 'String',
        [Parameter(Mandatory = $false)]
        [Switch]$Wow6432Node = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$SID,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            [String]$RegistryValueWriteAction = 'set'

            ## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
            If ($PSBoundParameters.ContainsKey('SID')) {
                [String]$key = Convert-RegistryPath -Key $key -Wow6432Node:$Wow6432Node -SID $SID
            }
            Else {
                [String]$key = Convert-RegistryPath -Key $key -Wow6432Node:$Wow6432Node
            }

            ## Create registry key if it doesn't exist
            If (-not (Test-Path -LiteralPath $key -ErrorAction 'Stop')) {
                Try {
                    Write-Log -Message "Creating registry key [$key]." -Source ${CmdletName}
                    # No forward slash found in Key. Use New-Item cmdlet to create registry key
                    If ((($Key -split '/').Count - 1) -eq 0) {
                        $null = New-Item -Path $key -ItemType 'Registry' -Force -ErrorAction 'Stop'
                    }
                    # Forward slash was found in Key. Use REG.exe ADD to create registry key
                    Else {
                        If ($Script:ADT.Environment.Is64BitProcess -and -not $Wow6432Node) {
                            $RegMode = '/reg:64'
                        }
                        Else {
                            $RegMode = '/reg:32'
                        }
                        [String]$CreateRegkeyResult = & "$env:WinDir\System32\reg.exe" Add "$($Key.Substring($Key.IndexOf('::') + 2))" /f $RegMode
                        If ($global:LastExitCode -ne 0) {
                            Throw "Failed to create registry key [$Key]"
                        }
                    }
                }
                Catch {
                    Throw
                }
            }

            If ($Name) {
                ## Set registry value if it doesn't exist
                If (-not (Get-ItemProperty -LiteralPath $key -Name $Name -ErrorAction 'SilentlyContinue')) {
                    Write-Log -Message "Setting registry key value: [$key] [$name = $value]." -Source ${CmdletName}
                    $null = New-ItemProperty -LiteralPath $key -Name $name -Value $value -PropertyType $Type -ErrorAction 'Stop'
                }
                ## Update registry value if it does exist
                Else {
                    [String]$RegistryValueWriteAction = 'update'
                    If ($Name -eq '(Default)') {
                        ## Set Default registry key value with the following workaround, because Set-ItemProperty contains a bug and cannot set Default registry key value
                        $null = $(Get-Item -LiteralPath $key -ErrorAction 'Stop').OpenSubKey('', 'ReadWriteSubTree').SetValue($null, $value)
                    }
                    Else {
                        Write-Log -Message "Updating registry key value: [$key] [$name = $value]." -Source ${CmdletName}
                        $null = Set-ItemProperty -LiteralPath $key -Name $name -Value $value -ErrorAction 'Stop'
                    }
                }
            }
        }
        Catch {
            If ($Name) {
                Write-Log -Message "Failed to $RegistryValueWriteAction value [$value] for registry key [$key] [$name]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to $RegistryValueWriteAction value [$value] for registry key [$key] [$name]: $($_.Exception.Message)"
                }
            }
            Else {
                Write-Log -Message "Failed to set registry key [$key]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to set registry key [$key]: $($_.Exception.Message)"
                }
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Remove-RegistryKey {
    <#
.SYNOPSIS

Deletes the specified registry key or value.

.DESCRIPTION

Deletes the specified registry key or value.

.PARAMETER Key

Path of the registry key to delete.

.PARAMETER Name

Name of the registry value to delete.

.PARAMETER Recurse

Delete registry key recursively.

.PARAMETER SID

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Remove-RegistryKey -Key 'HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\RunOnce'

.EXAMPLE

Remove-RegistryKey -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name 'RunAppInstall'

.EXAMPLE

Remove-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Example' -Name '(Default)'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Key,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        [Switch]$Recurse,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$SID,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            ## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
            If ($PSBoundParameters.ContainsKey('SID')) {
                [String]$Key = Convert-RegistryPath -Key $Key -SID $SID
            }
            Else {
                [String]$Key = Convert-RegistryPath -Key $Key
            }

            If (-not $Name) {
                If (Test-Path -LiteralPath $Key -ErrorAction 'Stop') {
                    If ($Recurse) {
                        Write-Log -Message "Deleting registry key recursively [$Key]." -Source ${CmdletName}
                        $null = Remove-Item -LiteralPath $Key -Force -Recurse -ErrorAction 'Stop'
                    }
                    Else {
                        If ($null -eq (Get-ChildItem -LiteralPath $Key -ErrorAction 'Stop')) {
                            ## Check if there are subkeys of $Key, if so, executing Remove-Item will hang. Avoiding this with Get-ChildItem.
                            Write-Log -Message "Deleting registry key [$Key]." -Source ${CmdletName}
                            $null = Remove-Item -LiteralPath $Key -Force -ErrorAction 'Stop'
                        }
                        Else {
                            Throw "Unable to delete child key(s) of [$Key] without [-Recurse] switch."
                        }
                    }
                }
                Else {
                    Write-Log -Message "Unable to delete registry key [$Key] because it does not exist." -Severity 2 -Source ${CmdletName}
                }
            }
            Else {
                If (Test-Path -LiteralPath $Key -ErrorAction 'Stop') {
                    Write-Log -Message "Deleting registry value [$Key] [$Name]." -Source ${CmdletName}

                    If ($Name -eq '(Default)') {
                        ## Remove (Default) registry key value with the following workaround because Remove-ItemProperty cannot remove the (Default) registry key value
                        $null = (Get-Item -LiteralPath $Key -ErrorAction 'Stop').OpenSubKey('', 'ReadWriteSubTree').DeleteValue('')
                    }
                    Else {
                        $null = Remove-ItemProperty -LiteralPath $Key -Name $Name -Force -ErrorAction 'Stop'
                    }
                }
                Else {
                    Write-Log -Message "Unable to delete registry value [$Key] [$Name] because registry key does not exist." -Severity 2 -Source ${CmdletName}
                }
            }
        }
        Catch [System.Management.Automation.PSArgumentException] {
            Write-Log -Message "Unable to delete registry value [$Key] [$Name] because it does not exist." -Severity 2 -Source ${CmdletName}
        }
        Catch {
            If (-not $Name) {
                Write-Log -Message "Failed to delete registry key [$Key]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to delete registry key [$Key]: $($_.Exception.Message)"
                }
            }
            Else {
                Write-Log -Message "Failed to delete registry value [$Key] [$Name]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to delete registry value [$Key] [$Name]: $($_.Exception.Message)"
                }
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Invoke-HKCURegistrySettingsForAllUsers {
    <#
.SYNOPSIS

Set current user registry settings for all current users and any new users in the future.

.DESCRIPTION

Set HKCU registry settings for all current and future users by loading their NTUSER.dat registry hive file, and making the modifications.

This function will modify HKCU settings for all users even when executed under the SYSTEM account.

To ensure new users in the future get the registry edits, the Default User registry hive used to provision the registry for new users is modified.

This function can be used as an alternative to using ActiveSetup for registry settings.

The advantage of using this function over ActiveSetup is that a user does not have to log off and log back on before the changes take effect.

.PARAMETER RegistrySettings

Script block which contains HKCU registry settings which should be modified for all users on the system. Must specify the -SID parameter for all HKCU settings.

.PARAMETER UserProfiles

Specify the user profiles to modify HKCU registry settings for. Default is all user profiles except for system profiles.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE
```powershell
[ScriptBlock]$HKCURegistrySettings = {
    Set-RegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord -SID $UserProfile.SID
    Set-RegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'updatereliabilitydata' -Value 1 -Type DWord -SID $UserProfile.SID
}

Invoke-HKCURegistrySettingsForAllUsers -RegistrySettings $HKCURegistrySettings
```
.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [ScriptBlock]$RegistrySettings,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [PSObject[]]$UserProfiles = (Get-UserProfiles)
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ForEach ($UserProfile in $UserProfiles) {
            Try {
                #  Set the path to the user's registry hive when it is loaded
                [String]$UserRegistryPath = "Registry::HKEY_USERS\$($UserProfile.SID)"

                #  Set the path to the user's registry hive file
                [String]$UserRegistryHiveFile = Join-Path -Path $UserProfile.ProfilePath -ChildPath 'NTUSER.DAT'

                #  Load the User profile registry hive if it is not already loaded because the User is logged in
                [Boolean]$ManuallyLoadedRegHive = $false
                If (-not (Test-Path -LiteralPath $UserRegistryPath)) {
                    #  Load the User registry hive if the registry hive file exists
                    If (Test-Path -LiteralPath $UserRegistryHiveFile -PathType 'Leaf') {
                        Write-Log -Message "Loading the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]." -Source ${CmdletName}
                        [String]$HiveLoadResult = & "$env:WinDir\System32\reg.exe" load "`"HKEY_USERS\$($UserProfile.SID)`"" "`"$UserRegistryHiveFile`""

                        If ($global:LastExitCode -ne 0) {
                            Throw "Failed to load the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. Failure message [$HiveLoadResult]. Continue..."
                        }

                        [Boolean]$ManuallyLoadedRegHive = $true
                    }
                    Else {
                        Throw "Failed to find the registry hive file [$UserRegistryHiveFile] for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. Continue..."
                    }
                }
                Else {
                    Write-Log -Message "The user [$($UserProfile.NTAccount)] registry hive is already loaded in path [HKEY_USERS\$($UserProfile.SID)]." -Source ${CmdletName}
                }

                ## Execute ScriptBlock which contains code to manipulate HKCU registry.
                #  Make sure read/write calls to the HKCU registry hive specify the -SID parameter or settings will not be changed for all users.
                #  Example: Set-RegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord -SID $UserProfile.SID
                Write-Log -Message 'Executing ScriptBlock to modify HKCU registry settings for all users.' -Source ${CmdletName}
                & $RegistrySettings
            }
            Catch {
                Write-Log -Message "Failed to modify the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)] `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            }
            Finally {
                If ($ManuallyLoadedRegHive) {
                    Try {
                        Write-Log -Message "Unload the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]." -Source ${CmdletName}
                        [String]$HiveLoadResult = & "$env:WinDir\System32\reg.exe" unload "`"HKEY_USERS\$($UserProfile.SID)`""

                        If ($global:LastExitCode -ne 0) {
                            Write-Log -Message "REG.exe failed to unload the registry hive and exited with exit code [$($global:LastExitCode)]. Performing manual garbage collection to ensure successful unloading of registry hive." -Severity 2 -Source ${CmdletName}
                            [GC]::Collect()
                            [GC]::WaitForPendingFinalizers()
                            Start-Sleep -Seconds 5

                            Write-Log -Message "Unload the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]." -Source ${CmdletName}
                            [String]$HiveLoadResult = & "$env:WinDir\System32\reg.exe" unload "`"HKEY_USERS\$($UserProfile.SID)`""
                            If ($global:LastExitCode -ne 0) {
                                Throw "REG.exe failed with exit code [$($global:LastExitCode)] and result [$HiveLoadResult]."
                            }
                        }
                    }
                    Catch {
                        Write-Log -Message "Failed to unload the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                    }
                }
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
