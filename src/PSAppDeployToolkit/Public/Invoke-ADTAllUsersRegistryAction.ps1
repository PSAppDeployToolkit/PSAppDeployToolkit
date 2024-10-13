#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTAllUsersRegistryAction
#
#-----------------------------------------------------------------------------

function Invoke-ADTAllUsersRegistryAction
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

    .PARAMETER ScriptBlock
        Script block which contains HKCU registry actions to be run for all users on the system.

    .PARAMETER UserProfiles
        Specify the user profiles to modify HKCU registry settings for. Default is all user profiles except for system profiles.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Invoke-ADTAllUsersRegistryAction -ScriptBlock {
            Set-ADTRegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord -SID $_.SID
            Set-ADTRegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'updatereliabilitydata' -Value 1 -Type DWord -SID $_.SID
        }

    .EXAMPLE
        Invoke-ADTAllUsersRegistryAction {
            Set-ADTRegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord -SID $_.SID
            Set-ADTRegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'updatereliabilitydata' -Value 1 -Type DWord -SID $_.SID
        }

        As the previous example, but showing how to use ScriptBlock as a positional parameter with no name specified.

    .EXAMPLE
        Invoke-ADTAllUsersRegistryAction -UserProfiles (Get-ADTUserProfiles -ExcludeDefaultUser) -ScriptBlock {
            Set-ADTRegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord -SID $_.SID
            Set-ADTRegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'updatereliabilitydata' -Value 1 -Type DWord -SID $_.SID
        }

        As the previous example, but sending specific user profiles through to exclude the Default profile.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock[]]$ScriptBlock,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Types.UserProfile[]]$UserProfiles = (Get-ADTUserProfiles)
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        foreach ($UserProfile in $UserProfiles)
        {
            $ManuallyLoadedRegHive = $false
            try
            {
                try
                {
                    # Set the path to the user's registry hive file.
                    $UserRegistryHiveFile = Join-Path -Path $UserProfile.ProfilePath -ChildPath 'NTUSER.DAT'

                    # Load the User profile registry hive if it is not already loaded because the User is logged in.
                    if (!(Test-Path -LiteralPath "Microsoft.PowerShell.Core\Registry::HKEY_USERS\$($UserProfile.SID)"))
                    {
                        # Load the User registry hive if the registry hive file exists.
                        if (![System.IO.File]::Exists($UserRegistryHiveFile))
                        {
                            $naerParams = @{
                                Exception = [System.IO.FileNotFoundException]::new("Failed to find the registry hive file [$UserRegistryHiveFile] for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. Continue...")
                                Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                                ErrorId = 'UserRegistryHiveFileNotFound'
                                TargetObject = $UserRegistryHiveFile
                                RecommendedAction = "Please confirm the state of this user profile and try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }

                        Write-ADTLogEntry -Message "Loading the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]."
                        $HiveLoadResult = & "$([System.Environment]::SystemDirectory)\reg.exe" LOAD "HKEY_USERS\$($UserProfile.SID)" $UserRegistryHiveFile 2>&1
                        if ($Global:LastExitCode -ne 0)
                        {
                            $naerParams = @{
                                Exception = [System.ApplicationException]::new("Failed to load the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. Failure message [$HiveLoadResult]. Continue...")
                                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                                ErrorId = 'UserRegistryHiveLoadFailure'
                                TargetObject = $UserRegistryHiveFile
                                RecommendedAction = "Please confirm the state of this user profile and try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }
                        $ManuallyLoadedRegHive = $true
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "The user [$($UserProfile.NTAccount)] registry hive is already loaded in path [HKEY_USERS\$($UserProfile.SID)]."
                    }

                    # Invoke changes against registry.
                    Write-ADTLogEntry -Message 'Executing scriptblock to modify HKCU registry settings for all users.'
                    ForEach-Object -InputObject $UserProfile -Begin $null -End $null -Process $ScriptBlock
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                Write-ADTLogEntry -Message "Failed to modify the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity 3
            }
            finally
            {
                if ($ManuallyLoadedRegHive)
                {
                    try
                    {
                        try
                        {
                            Write-ADTLogEntry -Message "Unload the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]."
                            $HiveLoadResult = & "$([System.Environment]::SystemDirectory)\reg.exe" UNLOAD "HKEY_USERS\$($UserProfile.SID)" 2>&1
                            if ($Global:LastExitCode -ne 0)
                            {
                                Write-ADTLogEntry -Message "REG.exe failed to unload the registry hive and exited with exit code [$($Global:LastExitCode)]. Performing manual garbage collection to ensure successful unloading of registry hive." -Severity 2
                                [System.GC]::Collect()
                                [System.GC]::WaitForPendingFinalizers()
                                [System.Threading.Thread]::Sleep(5000)

                                Write-ADTLogEntry -Message "Unload the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]."
                                $HiveLoadResult = & "$([System.Environment]::SystemDirectory)\reg.exe" UNLOAD "HKEY_USERS\$($UserProfile.SID)" 2>&1
                                if ($Global:LastExitCode -ne 0)
                                {
                                    $naerParams = @{
                                        Exception = [System.ApplicationException]::new("REG.exe failed with exit code [$($Global:LastExitCode)] and result [$HiveLoadResult].")
                                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                                        ErrorId = 'UserRegistryHiveUnloadFailure'
                                        TargetObject = "HKEY_USERS\$($UserProfile.SID)"
                                        RecommendedAction = "Please confirm the state of this user profile and try again."
                                    }
                                    throw (New-ADTErrorRecord @naerParams)
                                }
                            }
                        }
                        catch
                        {
                            Write-Error -ErrorRecord $_
                        }
                    }
                    catch
                    {
                        Write-ADTLogEntry -Message "Failed to unload the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)].`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity 3
                    }
                }
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
