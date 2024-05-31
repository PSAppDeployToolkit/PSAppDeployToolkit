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

Specify this parameter from the Invoke-ADTAllUsersRegistryChange function to read/edit HKCU registry settings for all users on the system.

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
        Write-ADTDebugHeader
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
                Write-ADTLogEntry -Message 'SID parameter specified but the registry hive of the key is not HKEY_CURRENT_USER.' -Severity 2
            }
        }

        If ($Key -match '^Registry::HKEY_LOCAL_MACHINE|^Registry::HKEY_CLASSES_ROOT|^Registry::HKEY_CURRENT_USER|^Registry::HKEY_USERS|^Registry::HKEY_CURRENT_CONFIG|^Registry::HKEY_PERFORMANCE_DATA') {
            ## Check for expected key string format
            If (-not $DisableFunctionLogging) {
                Write-ADTLogEntry -Message "Return fully qualified registry key path [$key]."
            }
            Write-Output -InputObject ($key)
        }
        Else {
            #  If key string is not properly formatted, throw an error
            Throw "Unable to detect target registry hive in string [$key]."
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
