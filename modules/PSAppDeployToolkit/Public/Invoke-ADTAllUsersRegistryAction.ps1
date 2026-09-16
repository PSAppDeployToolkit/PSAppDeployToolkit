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
            Set-ADTRegistryKey -SID $_.SID -LiteralPath 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord
            Set-ADTRegistryKey -SID $_.SID -LiteralPath 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'updatereliabilitydata' -Value 1 -Type DWord
        }

        Example demonstrating the setting of two values within each user's HKEY_CURRENT_USER hive.

    .EXAMPLE
        Invoke-ADTAllUsersRegistryAction {
            Set-ADTRegistryKey -SID $_.SID -LiteralPath 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord
            Set-ADTRegistryKey -SID $_.SID -LiteralPath 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'updatereliabilitydata' -Value 1 -Type DWord
        }

        As the previous example, but showing how to use ScriptBlock as a positional parameter with no name specified.

    .EXAMPLE
        Invoke-ADTAllUsersRegistryAction -UserProfiles (Get-ADTUserProfiles -ExcludeDefaultUser) -ScriptBlock {
            Set-ADTRegistryKey -SID $_.SID -LiteralPath 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord
            Set-ADTRegistryKey -SID $_.SID -LiteralPath 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'updatereliabilitydata' -Value 1 -Type DWord
        }

        As the previous example, but sending specific user profiles through to exclude the Default profile.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function supports the `-WhatIf` and `-Confirm` parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Invoke-ADTAllUsersRegistryAction

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/modules/PSAppDeployToolkit/Public/Invoke-ADTAllUsersRegistryAction.ps1
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [System.Management.Automation.ScriptBlock[]]$ScriptBlock,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [PSADT.AccountManagement.UserProfileInfo[]]$UserProfiles,

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
            $UserProfiles = Get-ADTUserProfiles
        }

        # Set up the default parameters for calling `reg.exe` via `Start-ADTProcess`.
        $regExeParams = @{
            FilePath = "$([System.Environment]::SystemDirectory)\reg.exe"
            CreateNoWindow = $true
            PassThru = $true
            SuccessExitCodes = 0
            InformationAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
            ErrorAction = [System.Management.Automation.ActionPreference]::Ignore
            WhatIf = $false
            Confirm = $false
        }
    }

    process
    {
        try
        {
            try
            {
                foreach ($UserProfile in $UserProfiles)
                {
                    # Set the path to the user's registry hive file.
                    $regHive = @{ Path = Join-Path -Path $UserProfile.ProfilePath -ChildPath 'NTUSER.DAT'; Mountpoint = "HKEY_USERS\$($UserProfile.SID)"; Mounted = $false }
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
                                if (!(Test-Path -LiteralPath $regHive.Path -PathType Leaf))
                                {
                                    $naerParams = @{
                                        Exception = [System.IO.FileNotFoundException]::new("Failed to find the registry hive file [$($regHive.Path)] for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. Continue...", $regHive.Path)
                                        Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                                        ErrorId = "$([System.IO.Path]::GetFileNameWithoutExtension($regHive.Path).ToUpperInvariant())RegistryHiveFileNotFound"
                                        TargetObject = $regHive.Path
                                        RecommendedAction = "Please confirm the state of this user profile and try again."
                                    }
                                    throw (New-ADTErrorRecord @naerParams)
                                }

                                # A native command does not throw on a bad exit code, so an unloadable hive would
                                # otherwise run the caller's scriptblock against a HKEY_USERS key that isn't there.
                                $regResult = Start-ADTProcess @regExeParams -ArgumentList LOAD, $regHive.Mountpoint, $regHive.Path
                                if ($regResult.ExitCode)
                                {
                                    $naerParams = @{
                                        Exception = [PSADT.ProcessManagement.ProcessException]::new("Failed to load the registry hive file [$($regHive.Path)] for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)] with exit code [$($regResult.ExitCode)]: $($regResult.Interleaved)", $regResult)
                                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                                        ErrorId = "$([System.IO.Path]::GetFileNameWithoutExtension($regHive.Path).ToUpperInvariant())RegistryHiveLoadFailure"
                                        TargetObject = $regResult
                                        RecommendedAction = "Please confirm the state of this user profile and try again."
                                    }
                                    throw (New-ADTErrorRecord @naerParams)
                                }
                                $regHive.Mounted = $true
                            }

                            # Invoke changes against registry.
                            Write-ADTLogEntry -Message "Executing scriptblock to modify HKCU registry settings for [$($UserProfile.NTAccount)]."
                            if ($PSCmdlet.ShouldProcess("User [$($UserProfile.NTAccount)] registry hive", 'Modify'))
                            {
                                ForEach-Object -InputObject $UserProfile -Begin $null -End $null -Process $ScriptBlock
                            }
                        }
                        catch
                        {
                            Write-Error -ErrorRecord $_
                        }
                    }
                    catch
                    {
                        Write-ADTLogEntry -Message "Failed to modify the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity Error
                    }
                    finally
                    {
                        if ($regHive.Mounted)
                        {
                            # A hive left mounted holds NTUSER.DAT open, so the profile cannot unload at logoff and
                            # the next run reads it as logged on and skips it. Not something to log and move past.
                            Write-ADTLogEntry -Message "Unloading the User [$($UserProfile.NTAccount)] registry hive in path [$($regHive.Mountpoint)]."
                            [System.GC]::Collect(); [System.GC]::WaitForPendingFinalizers()
                            $regResult = Start-ADTProcess @regExeParams -ArgumentList UNLOAD, $regHive.Mountpoint
                            if ($regResult.ExitCode)
                            {
                                $naerParams = @{
                                    Exception = [PSADT.ProcessManagement.ProcessException]::new("Failed to unload the registry hive [$($regHive.Mountpoint)] for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)] with exit code [$($regResult.ExitCode)]: $($regResult.Interleaved). The hive remains mounted.", $regResult)
                                    Category = [System.Management.Automation.ErrorCategory]::ResourceBusy
                                    ErrorId = "$([System.IO.Path]::GetFileNameWithoutExtension($regHive.Path).ToUpperInvariant())RegistryHiveUnloadFailure"
                                    TargetObject = $regResult
                                    RecommendedAction = "Please close anything holding the profile's registry hive open, then unload it with [reg.exe UNLOAD]."
                                }
                                throw (New-ADTErrorRecord @naerParams)
                            }
                        }
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
