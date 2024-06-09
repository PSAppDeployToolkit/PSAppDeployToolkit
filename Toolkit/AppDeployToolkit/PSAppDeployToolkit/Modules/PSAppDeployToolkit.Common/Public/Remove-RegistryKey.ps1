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
        Write-ADTDebugHeader
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
                        Write-ADTLogEntry -Message "Deleting registry key recursively [$Key]."
                        $null = Remove-Item -LiteralPath $Key -Force -Recurse -ErrorAction 'Stop'
                    }
                    Else {
                        If ($null -eq (Get-ChildItem -LiteralPath $Key -ErrorAction 'Stop')) {
                            ## Check if there are subkeys of $Key, if so, executing Remove-Item will hang. Avoiding this with Get-ChildItem.
                            Write-ADTLogEntry -Message "Deleting registry key [$Key]."
                            $null = Remove-Item -LiteralPath $Key -Force -ErrorAction 'Stop'
                        }
                        Else {
                            Throw "Unable to delete child key(s) of [$Key] without [-Recurse] switch."
                        }
                    }
                }
                Else {
                    Write-ADTLogEntry -Message "Unable to delete registry key [$Key] because it does not exist." -Severity 2
                }
            }
            Else {
                If (Test-Path -LiteralPath $Key -ErrorAction 'Stop') {
                    Write-ADTLogEntry -Message "Deleting registry value [$Key] [$Name]."

                    If ($Name -eq '(Default)') {
                        ## Remove (Default) registry key value with the following workaround because Remove-ItemProperty cannot remove the (Default) registry key value
                        $null = (Get-Item -LiteralPath $Key -ErrorAction 'Stop').OpenSubKey('', 'ReadWriteSubTree').DeleteValue('')
                    }
                    Else {
                        $null = Remove-ItemProperty -LiteralPath $Key -Name $Name -Force -ErrorAction 'Stop'
                    }
                }
                Else {
                    Write-ADTLogEntry -Message "Unable to delete registry value [$Key] [$Name] because registry key does not exist." -Severity 2
                }
            }
        }
        Catch [System.Management.Automation.PSArgumentException] {
            Write-ADTLogEntry -Message "Unable to delete registry value [$Key] [$Name] because it does not exist." -Severity 2
        }
        Catch {
            If (-not $Name) {
                Write-ADTLogEntry -Message "Failed to delete registry key [$Key]. `r`n$(Resolve-Error)" -Severity 3
                If (-not $ContinueOnError) {
                    Throw "Failed to delete registry key [$Key]: $($_.Exception.Message)"
                }
            }
            Else {
                Write-ADTLogEntry -Message "Failed to delete registry value [$Key] [$Name]. `r`n$(Resolve-Error)" -Severity 3
                If (-not $ContinueOnError) {
                    Throw "Failed to delete registry value [$Key] [$Name]: $($_.Exception.Message)"
                }
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
