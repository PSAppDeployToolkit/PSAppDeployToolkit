Function Get-PendingReboot {
    <#
.SYNOPSIS

Get the pending reboot status on a local computer.

.DESCRIPTION

Check WMI and the registry to determine if the system has a pending reboot operation from any of the following:
a) Component Based Servicing (Vista, Windows 2008)
b) Windows Update / Auto Update (XP, Windows 2003 / 2008)
c) SCCM 2012 Clients (DetermineIfRebootPending WMI method)
d) App-V Pending Tasks (global based Appv 5.0 SP2)
e) Pending File Rename Operations (XP, Windows 2003 / 2008)

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSObject

Returns a custom object with the following properties
- ComputerName
- LastBootUpTime
- IsSystemRebootPending
- IsCBServicingRebootPending
- IsWindowsUpdateRebootPending
- IsSCCMClientRebootPending
- IsFileRenameRebootPending
- PendingFileRenameOperations
- ErrorMsg

.EXAMPLE

Get-PendingReboot

Returns custom object with following properties:
- ComputerName
- LastBootUpTime
- IsSystemRebootPending
- IsCBServicingRebootPending
- IsWindowsUpdateRebootPending
- IsSCCMClientRebootPending
- IsFileRenameRebootPending
- PendingFileRenameOperations
- ErrorMsg

.EXAMPLE

(Get-PendingReboot).IsSystemRebootPending

Returns boolean value determining whether or not there is a pending reboot operation.

.NOTES

ErrorMsg only contains something if an error occurred

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        $adtEnv = Get-ADTEnvironment
        Write-ADTDebugHeader

        ## Initialize variables
        [String]$private:ComputerName = $adtEnv.envComputerNameFQDN
        $PendRebootErrorMsg = $null
    }
    Process {
        Write-ADTLogEntry -Message "Getting the pending reboot status on the local computer [$ComputerName]."

        ## Get the date/time that the system last booted up
        Try {
            [Nullable[DateTime]]$LastBootUpTime = (Get-Date -ErrorAction 'Stop') - ([Timespan]::FromMilliseconds([Math]::Abs([Environment]::TickCount)))
        }
        Catch {
            [Nullable[DateTime]]$LastBootUpTime = $null
            [String[]]$PendRebootErrorMsg += "Failed to get LastBootUpTime: $($_.Exception.Message)"
            Write-ADTLogEntry -Message "Failed to get LastBootUpTime.`n$(Resolve-ADTError)" -Severity 3
        }

        ## Determine if a Windows Vista/Server 2008 and above machine has a pending reboot from a Component Based Servicing (CBS) operation
        Try {
            If ($adtEnv.envOSVersionMajor -ge 5) {
                If (Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending' -ErrorAction 'Stop') {
                    [Nullable[Boolean]]$IsCBServicingRebootPending = $true
                }
                Else {
                    [Nullable[Boolean]]$IsCBServicingRebootPending = $false
                }
            }
        }
        Catch {
            [Nullable[Boolean]]$IsCBServicingRebootPending = $null
            [String[]]$PendRebootErrorMsg += "Failed to get IsCBServicingRebootPending: $($_.Exception.Message)"
            Write-ADTLogEntry -Message "Failed to get IsCBServicingRebootPending.`n$(Resolve-ADTError)" -Severity 3
        }

        ## Determine if there is a pending reboot from a Windows Update
        Try {
            If (Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired' -ErrorAction 'Stop') {
                [Nullable[Boolean]]$IsWindowsUpdateRebootPending = $true
            }
            Else {
                [Nullable[Boolean]]$IsWindowsUpdateRebootPending = $false
            }
        }
        Catch {
            [Nullable[Boolean]]$IsWindowsUpdateRebootPending = $null
            [String[]]$PendRebootErrorMsg += "Failed to get IsWindowsUpdateRebootPending: $($_.Exception.Message)"
            Write-ADTLogEntry -Message "Failed to get IsWindowsUpdateRebootPending.`n$(Resolve-ADTError)" -Severity 3
        }

        ## Determine if there is a pending reboot from a pending file rename operation
        [Boolean]$IsFileRenameRebootPending = $false
        $PendingFileRenameOperations = $null
        If (Test-ADTRegistryValue -Key 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager' -Value 'PendingFileRenameOperations') {
            #  If PendingFileRenameOperations value exists, set $IsFileRenameRebootPending variable to $true
            [Boolean]$IsFileRenameRebootPending = $true
            #  Get the value of PendingFileRenameOperations
            Try {
                [String[]]$PendingFileRenameOperations = Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager' -ErrorAction 'Stop' | Select-Object -ExpandProperty 'PendingFileRenameOperations' -ErrorAction 'Stop'
            }
            Catch {
                [String[]]$PendRebootErrorMsg += "Failed to get PendingFileRenameOperations: $($_.Exception.Message)"
                Write-ADTLogEntry -Message "Failed to get PendingFileRenameOperations.`n$(Resolve-ADTError)" -Severity 3
            }
        }

        ## Determine SCCM 2012 Client reboot pending status
        Try {
            [Boolean]$IsSccmClientNamespaceExists = $false
            [PSObject]$SCCMClientRebootStatus = Invoke-WmiMethod -ComputerName $ComputerName -Namespace 'ROOT\CCM\ClientSDK' -Class 'CCM_ClientUtilities' -Name 'DetermineIfRebootPending' -ErrorAction 'Stop'
            [Boolean]$IsSccmClientNamespaceExists = $true
            If ($SCCMClientRebootStatus.ReturnValue -ne 0) {
                Throw "'DetermineIfRebootPending' method of 'ROOT\CCM\ClientSDK\CCM_ClientUtilities' class returned error code [$($SCCMClientRebootStatus.ReturnValue)]"
            }
            Else {
                Write-ADTLogEntry -Message 'Successfully queried SCCM client for reboot status.'
                [Nullable[Boolean]]$IsSCCMClientRebootPending = $false
                If ($SCCMClientRebootStatus.IsHardRebootPending -or $SCCMClientRebootStatus.RebootPending) {
                    [Nullable[Boolean]]$IsSCCMClientRebootPending = $true
                    Write-ADTLogEntry -Message 'Pending SCCM reboot detected.'
                }
                Else {
                    Write-ADTLogEntry -Message 'Pending SCCM reboot not detected.'
                }
            }
        }
        Catch [System.Management.ManagementException] {
            [Nullable[Boolean]]$IsSCCMClientRebootPending = $null
            [Boolean]$IsSccmClientNamespaceExists = $false
            Write-ADTLogEntry -Message 'Failed to get IsSCCMClientRebootPending. Failed to detect the SCCM client WMI class.' -Severity 3
        }
        Catch {
            [Nullable[Boolean]]$IsSCCMClientRebootPending = $null
            [String[]]$PendRebootErrorMsg += "Failed to get IsSCCMClientRebootPending: $($_.Exception.Message)"
            Write-ADTLogEntry -Message "Failed to get IsSCCMClientRebootPending.`n$(Resolve-ADTError)" -Severity 3
        }

        ## Determine if there is a pending reboot from an App-V global Pending Task. (User profile based tasks will complete on logoff/logon)
        Try {
            If (Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Software\Microsoft\AppV\Client\PendingTasks' -ErrorAction 'Stop') {
                [Nullable[Boolean]]$IsAppVRebootPending = $true
            }
            Else {
                [Nullable[Boolean]]$IsAppVRebootPending = $false
            }
        }
        Catch {
            [Nullable[Boolean]]$IsAppVRebootPending = $null
            [String[]]$PendRebootErrorMsg += "Failed to get IsAppVRebootPending: $($_.Exception.Message)"
            Write-ADTLogEntry -Message "Failed to get IsAppVRebootPending.`n$(Resolve-ADTError)" -Severity 3
        }

        ## Determine if there is a pending reboot for the system
        [Boolean]$IsSystemRebootPending = $false
        If ($IsCBServicingRebootPending -or $IsWindowsUpdateRebootPending -or $IsSCCMClientRebootPending -or $IsFileRenameRebootPending) {
            [Boolean]$IsSystemRebootPending = $true
        }

        ## Create a custom object containing pending reboot information for the system
        [PSObject]$PendingRebootInfo = New-Object -TypeName 'PSObject' -Property @{
            ComputerName                 = $ComputerName
            LastBootUpTime               = $LastBootUpTime
            IsSystemRebootPending        = $IsSystemRebootPending
            IsCBServicingRebootPending   = $IsCBServicingRebootPending
            IsWindowsUpdateRebootPending = $IsWindowsUpdateRebootPending
            IsSCCMClientRebootPending    = $IsSCCMClientRebootPending
            IsAppVRebootPending          = $IsAppVRebootPending
            IsFileRenameRebootPending    = $IsFileRenameRebootPending
            PendingFileRenameOperations  = $PendingFileRenameOperations
            ErrorMsg                     = $PendRebootErrorMsg
        }
        Write-ADTLogEntry -Message "Pending reboot status on the local computer [$ComputerName]:`n$($PendingRebootInfo | Format-List | Out-String)"
    }
    End {
        Write-Output -InputObject ($PendingRebootInfo | Select-Object -Property 'ComputerName', 'LastBootUpTime', 'IsSystemRebootPending', 'IsCBServicingRebootPending', 'IsWindowsUpdateRebootPending', 'IsSCCMClientRebootPending', 'IsAppVRebootPending', 'IsFileRenameRebootPending', 'PendingFileRenameOperations', 'ErrorMsg')

        Write-ADTDebugFooter
    }
}
