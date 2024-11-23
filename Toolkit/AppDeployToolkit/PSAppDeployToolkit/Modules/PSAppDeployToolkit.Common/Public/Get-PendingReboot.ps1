function Get-ADTPendingReboot
{
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
    None. You cannot pipe objects to this function.

    .OUTPUTS
    PSObject. Returns a custom object with the following properties:
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
    Get-ADTPendingReboot

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
    (Get-ADTPendingReboot).IsSystemRebootPending

    Returns boolean value determining whether or not there is a pending reboot operation.

    .NOTES
    ErrorMsg only contains something if an error occurred

    .LINK
    https://psappdeploytoolkit.com

    #>

    begin {
        # Initialize variables,
        $PendRebootErrorMsg = [System.Collections.Generic.List[System.String]]::new()
        $adtEnv = Get-ADTEnvironment
        Write-ADTDebugHeader
    }
    Process {
        # Get the date/time that the system last booted up.
        Write-ADTLogEntry -Message "Getting the pending reboot status on the local computer [$($adtEnv.envComputerNameFQDN)]."
        $LastBootUpTime = [System.DateTime]::Now - [System.TimeSpan]::FromMilliseconds([System.Math]::Abs([System.Environment]::TickCount))

        # Determine if a Windows Vista/Server 2008 and above machine has a pending reboot from a Component Based Servicing (CBS) operation.
        $IsCBServicingRebootPending = Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending'

        # Determine if there is a pending reboot from a Windows Update.
        $IsWindowsUpdateRebootPending = Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired'

        # Determine if there is a pending reboot from an App-V global Pending Task. (User profile based tasks will complete on logoff/logon).
        $IsAppVRebootPending = Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Software\Microsoft\AppV\Client\PendingTasks'

        # Get the value of PendingFileRenameOperations.
        $PendingFileRenameOperations = if ($IsFileRenameRebootPending = Test-ADTRegistryValue -Key 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager' -Value 'PendingFileRenameOperations')
        {
            try
            {
                Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager' | Select-Object -ExpandProperty PendingFileRenameOperations
            }
            catch
            {
                Write-ADTLogEntry -Message "Failed to get PendingFileRenameOperations.`n$(Resolve-ADTError)" -Severity 3
                $PendRebootErrorMsg.Add("Failed to get PendingFileRenameOperations: $($_.Exception.Message)")
            }
        }

        # Determine SCCM 2012 Client reboot pending status.
        $IsSCCMClientRebootPending = try
        {
            if (($SCCMClientRebootStatus = Invoke-CimMethod -Namespace ROOT\CCM\ClientSDK -ClassName CCM_ClientUtilities -Name DetermineIfRebootPending).ReturnValue -eq 0)
            {
                $SCCMClientRebootStatus.IsHardRebootPending -or $SCCMClientRebootStatus.RebootPending
            }
        }
        catch
        {
            Write-ADTLogEntry -Message "Failed to get IsSCCMClientRebootPending.`n$(Resolve-ADTError)" -Severity 3
            $PendRebootErrorMsg.Add("Failed to get IsSCCMClientRebootPending: $($_.Exception.Message)")
        }

        # Create a custom object containing pending reboot information for the system.
        [PSADT.Types.RebootInfo]$PendingRebootInfo = @{
            ComputerName                 = $adtEnv.envComputerNameFQDN
            LastBootUpTime               = $LastBootUpTime
            IsSystemRebootPending        = $IsCBServicingRebootPending -or $IsWindowsUpdateRebootPending -or $IsFileRenameRebootPending -or $IsSCCMClientRebootPending
            IsCBServicingRebootPending   = $IsCBServicingRebootPending
            IsWindowsUpdateRebootPending = $IsWindowsUpdateRebootPending
            IsSCCMClientRebootPending    = $IsSCCMClientRebootPending
            IsAppVRebootPending          = $IsAppVRebootPending
            IsFileRenameRebootPending    = $IsFileRenameRebootPending
            PendingFileRenameOperations  = $PendingFileRenameOperations
            ErrorMsg                     = $PendRebootErrorMsg
        }
        Write-ADTLogEntry -Message "Pending reboot status on the local computer [$($adtEnv.envComputerNameFQDN)]:`n$($PendingRebootInfo | Format-List | Out-String)"
        return $PendingRebootInfo
    }

    end {
        Write-ADTDebugFooter
    }
}
