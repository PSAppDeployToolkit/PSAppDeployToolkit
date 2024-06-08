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

Specify this parameter from the Invoke-ADTAllUsersRegistryChange function to read/edit HKCU registry settings for all users on the system.

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
        Write-ADTDebugHeader
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
                    Write-ADTLogEntry -Message "Creating registry key [$key]."
                    # No forward slash found in Key. Use New-Item cmdlet to create registry key
                    If ((($Key -split '/').Count - 1) -eq 0) {
                        $null = New-Item -Path $key -ItemType 'Registry' -Force -ErrorAction 'Stop'
                    }
                    # Forward slash was found in Key. Use REG.exe ADD to create registry key
                    Else {
                        If ((Get-ADTEnvironment).Is64BitProcess -and -not $Wow6432Node) {
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
                If (-not (Get-ItemProperty -LiteralPath $key -Name $Name -ErrorAction 'Ignore')) {
                    Write-ADTLogEntry -Message "Setting registry key value: [$key] [$name = $value]."
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
                        Write-ADTLogEntry -Message "Updating registry key value: [$key] [$name = $value]."
                        $null = Set-ItemProperty -LiteralPath $key -Name $name -Value $value -ErrorAction 'Stop'
                    }
                }
            }
        }
        Catch {
            If ($Name) {
                Write-ADTLogEntry -Message "Failed to $RegistryValueWriteAction value [$value] for registry key [$key] [$name]. `r`n$(Resolve-Error)" -Severity 3
                If (-not $ContinueOnError) {
                    Throw "Failed to $RegistryValueWriteAction value [$value] for registry key [$key] [$name]: $($_.Exception.Message)"
                }
            }
            Else {
                Write-ADTLogEntry -Message "Failed to set registry key [$key]. `r`n$(Resolve-Error)" -Severity 3
                If (-not $ContinueOnError) {
                    Throw "Failed to set registry key [$key]: $($_.Exception.Message)"
                }
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
