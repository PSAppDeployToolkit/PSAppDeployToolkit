﻿#-----------------------------------------------------------------------------
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

        This function will modify HKCU settings for all users even when executed under the SYSTEM account and can be used as an alternative to using ActiveSetup for registry settings.

        To ensure new users in the future get the registry edits, the Default User registry hive used to provision the registry for new users is modified.

        The advantage of using this function over ActiveSetup is that a user does not have to log off and log back on before the changes take effect.

    .PARAMETER ScriptBlock
        Script block which contains HKCU registry actions to be run for all users on the system.

    .PARAMETER UserProfiles
        Specify the user profiles to modify HKCU registry settings for. Default is all user profiles except for system profiles.

    .PARAMETER SkipUnloadedProfiles
        Specifies that unloaded registry hives should be skipped and not be loaded by the function.

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

        Example demonstrating the setting of two values within each user's HKEY_CURRENT_USER hive.

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
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
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
        [PSADT.Types.UserProfile[]]$UserProfiles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipUnloadedProfiles
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Set up default value for $UserProfiles if not provided.
        if (!$UserProfiles)
        {
            $UserProfiles = if ($false)
            {
                Get-ADTUserProfiles -LoadProfilePaths
            }
            else
            {
                Get-ADTUserProfiles
            }
        }
    }

    process
    {
        foreach ($UserProfile in $UserProfiles)
        {
            # Set the path to the user's registry hive file.
            $manualRegHives = $(
                @{ Path = [System.IO.Path]::Combine($UserProfile.ProfilePath, 'NTUSER.DAT'); Mountpoint = "HKEY_USERS\$($UserProfile.SID)"; Mounted = $false }
                if ($false -and !$UserProfile.SID.Equals([System.Security.Principal.SecurityIdentifier]::new([System.Security.Principal.WellKnownSidType]::NullSid, $null)))
                {
                    @{ Path = [System.IO.Path]::Combine($UserProfile.LocalAppDataPath, 'Microsoft\Windows\UsrClass.dat'); Mountpoint = "HKEY_USERS\$($UserProfile.SID)_Classes"; Mounted = $false }
                }
            )
            try
            {
                try
                {
                    # Load the User profile registry hive if it is not already loaded because the User is logged in.
                    if (!(Test-Path -LiteralPath "Microsoft.PowerShell.Core\Registry::HKEY_USERS\$($UserProfile.SID)"))
                    {
                        # Only load the profile if we've been asked to.
                        if ($SkipUnloadedProfiles)
                        {
                            Write-ADTLogEntry -Message "Skipping User [$($UserProfile.NTAccount)] as the registry hive is not loaded."
                            continue
                        }

                        # Load the User registry hive if the registry hive file exists.
                        Write-ADTLogEntry -Message "Loading the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]."
                        foreach ($regHive in $manualRegHives)
                        {
                            if (![System.IO.File]::Exists($regHive.Path))
                            {
                                $naerParams = @{
                                    Exception = [System.IO.FileNotFoundException]::new("Failed to find the registry hive file [$($regHive.Path)] for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. Continue...")
                                    Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                                    ErrorId = "$([System.IO.Path]::GetFileNameWithoutExtension($regHive.Path).ToUpper())RegistryHiveFileNotFound"
                                    TargetObject = $regHive.Path
                                    RecommendedAction = "Please confirm the state of this user profile and try again."
                                }
                                throw (New-ADTErrorRecord @naerParams)
                            }
                            $null = & "$([System.Environment]::SystemDirectory)\reg.exe" LOAD $regHive.Mountpoint $regHive.Path 2>&1
                            $regHive.Mounted = $true
                        }
                    }

                    # Invoke changes against registry.
                    Write-ADTLogEntry -Message "Executing scriptblock to modify HKCU registry settings for [$($UserProfile.NTAccount)]."
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
                [System.GC]::Collect()
                [System.GC]::WaitForPendingFinalizers()
                [System.Array]::Reverse($manualRegHives)
                foreach ($regHive in $manualRegHives)
                {
                    if ($regHive.Mounted)
                    {
                        Write-ADTLogEntry -Message "Unloading the User [$($UserProfile.NTAccount)] registry hive in path [$($regHive.Mountpoint)]."
                        try
                        {
                            $null = & "$([System.Environment]::SystemDirectory)\reg.exe" UNLOAD $regHive.Mountpoint 2>&1
                        }
                        catch
                        {
                            Write-ADTLogEntry -Message "Failed to unload the registry path [$($regHive.Mountpoint)] for User [$($UserProfile.NTAccount)]. REG.exe exit code [$Global:LASTEXITCODE]. Error message: [$($_.Exception.Message)]" -Severity 3
                        }
                    }
                }
            }
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
