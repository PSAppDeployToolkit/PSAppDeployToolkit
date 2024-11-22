function Invoke-ADTAllUsersRegistryChange
{
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
    Script block which contains HKCU registry settings which should be modified for all users on the system.

    .PARAMETER UserProfiles
    Specify the user profiles to modify HKCU registry settings for. Default is all user profiles except for system profiles.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    ```powershell
    [ScriptBlock]$HKCURegistrySettings = {
        Set-RegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord -SID $_.SID
        Set-RegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'updatereliabilitydata' -Value 1 -Type DWord -SID $_.SID
    }

    Invoke-ADTAllUsersRegistryChange -RegistrySettings $HKCURegistrySettings
    ```

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock[]]$RegistrySettings,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSObject[]]$UserProfiles = (Get-ADTUserProfiles)
    )

    begin {
        Write-ADTDebugHeader
    }

    process {
        foreach ($UserProfile in $UserProfiles)
        {
            try
            {
                # Set the path to the user's registry hive when it is loaded.
                [String]$UserRegistryPath = "Registry::HKEY_USERS\$($UserProfile.SID)"

                # Set the path to the user's registry hive file.
                [String]$UserRegistryHiveFile = Join-Path -Path $UserProfile.ProfilePath -ChildPath 'NTUSER.DAT'

                # Load the User profile registry hive if it is not already loaded because the User is logged in
                [Boolean]$ManuallyLoadedRegHive = $false
                if (!(Test-Path -LiteralPath $UserRegistryPath))
                {
                    # Load the User registry hive if the registry hive file exists
                    if (Test-Path -LiteralPath $UserRegistryHiveFile -PathType 'Leaf')
                    {
                        Write-ADTLogEntry -Message "Loading the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]."
                        [String]$HiveLoadResult = & "$env:WinDir\System32\reg.exe" load "`"HKEY_USERS\$($UserProfile.SID)`"" "`"$UserRegistryHiveFile`""

                        if ($Global:LastExitCode -ne 0)
                        {
                            throw "Failed to load the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. Failure message [$HiveLoadResult]. Continue..."
                        }

                        [Boolean]$ManuallyLoadedRegHive = $true
                    }
                    else
                    {
                        throw "Failed to find the registry hive file [$UserRegistryHiveFile] for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. Continue..."
                    }
                }
                else
                {
                    Write-ADTLogEntry -Message "The user [$($UserProfile.NTAccount)] registry hive is already loaded in path [HKEY_USERS\$($UserProfile.SID)]."
                }

                # Invoke changes against registry.
                Write-ADTLogEntry -Message 'Executing scriptblock to modify HKCU registry settings for all users.'
                ForEach-Object -InputObject $UserProfile -Begin $null -End $null -Process $RegistrySettings
            }
            catch
            {
                Write-ADTLogEntry -Message "Failed to modify the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]`n$(Resolve-ADTError)" -Severity 3
            }
            finally
            {
                if ($ManuallyLoadedRegHive)
                {
                    try
                    {
                        Write-ADTLogEntry -Message "Unload the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]."
                        [String]$HiveLoadResult = & "$env:WinDir\System32\reg.exe" unload "`"HKEY_USERS\$($UserProfile.SID)`""

                        if ($Global:LastExitCode -ne 0)
                        {
                            Write-ADTLogEntry -Message "REG.exe failed to unload the registry hive and exited with exit code [$($Global:LastExitCode)]. Performing manual garbage collection to ensure successful unloading of registry hive." -Severity 2
                            [GC]::Collect()
                            [GC]::WaitForPendingFinalizers()
                            Start-Sleep -Seconds 5

                            Write-ADTLogEntry -Message "Unload the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]."
                            [String]$HiveLoadResult = & "$env:WinDir\System32\reg.exe" unload "`"HKEY_USERS\$($UserProfile.SID)`""
                            if ($Global:LastExitCode -ne 0)
                            {
                                throw "REG.exe failed with exit code [$($Global:LastExitCode)] and result [$HiveLoadResult]."
                            }
                        }
                    }
                    catch
                    {
                        Write-ADTLogEntry -Message "Failed to unload the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)].`n$(Resolve-ADTError)" -Severity 3
                    }
                }
            }
        }
    }

    end {
        Write-ADTDebugFooter
    }
}
