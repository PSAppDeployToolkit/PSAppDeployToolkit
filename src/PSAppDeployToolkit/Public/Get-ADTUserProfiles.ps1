#-----------------------------------------------------------------------------
#
# MARK: Get-ADTUserProfiles
#
#-----------------------------------------------------------------------------

function Get-ADTUserProfiles
{
    <#
    .SYNOPSIS
        Get the User Profile Path, User Account SID, and the User Account Name for all users that log onto the machine and also the Default User.

    .DESCRIPTION
        Get the User Profile Path, User Account SID, and the User Account Name for all users that log onto the machine and also the Default User (which does not log on).

        Please note that the NTAccount property may be empty for some user profiles but the SID and ProfilePath properties will always be populated.

    .PARAMETER ExcludeNTAccount
        Specify NT account names in DOMAIN\username format to exclude from the list of user profiles.

    .PARAMETER IncludeSystemProfiles
        Include system profiles: SYSTEM, LOCAL SERVICE, NETWORK SERVICE.

    .PARAMETER IncludeServiceProfiles
        Include service (NT SERVICE) profiles.

    .PARAMETER IncludeIISAppPoolProfiles
        Include IIS AppPool profiles. Excluded by default as they don't parse well.

    .PARAMETER ExcludeDefaultUser
        Exclude the Default User.

    .PARAMETER LoadProfilePaths
        Load additional profile paths for each user profile.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.UserProfile

        Returns a PSADT.Types.UserProfile object with the following properties:
        - NTAccount
        - SID
        - ProfilePath

    .EXAMPLE
        Get-ADTUserProfiles

        Return the following properties for each user profile on the system: NTAccount, SID, ProfilePath.

    .EXAMPLE
        Get-ADTUserProfiles -ExcludeNTAccount CONTOSO\Robot,CONTOSO\ntadmin

        Return the following properties for each user profile on the system, except for 'Robot' and 'ntadmin': NTAccount, SID, ProfilePath.

    .EXAMPLE
        [string[]]$ProfilePaths = Get-ADTUserProfiles | Select-Object -ExpandProperty ProfilePath

        Return the user profile path for each user on the system. This information can then be used to make modifications under the user profile on the filesystem.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTUserProfiles
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ExcludeNTAccount', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    [OutputType([PSADT.Types.UserProfile])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Security.Principal.NTAccount[]]$ExcludeNTAccount,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeSystemProfiles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeServiceProfiles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeIISAppPoolProfiles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExcludeDefaultUser,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$LoadProfilePaths
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $userProfileListRegKey = 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList'
        $excludedSids = "^S-1-5-($([System.String]::Join('|', $(
            if (!$IncludeSystemProfiles)
            {
                18  # System (or LocalSystem)
                19  # NT Authority (LocalService)
                20  # Network Service
            }
            if (!$IncludeServiceProfiles)
            {
                80  # NT Service
            }
            if (!$IncludeIISAppPoolProfiles)
            {
                82  # IIS AppPool
            }
        ))))"
    }

    process
    {
        Write-ADTLogEntry -Message 'Getting the User Profile Path, User Account SID, and the User Account Name for all users that log onto the machine.'
        try
        {
            try
            {
                # Get the User Profile Path, User Account SID, and the User Account Name for all users that log onto the machine.
                Get-ItemProperty -Path "$userProfileListRegKey\*" | & {
                    process
                    {
                        # Return early if the SID is to be excluded.
                        if ($_.PSChildName -match $excludedSids)
                        {
                            return
                        }

                        # Return early for accounts that have a null NTAccount.
                        if (!($ntAccount = ConvertTo-ADTNTAccountOrSID -SID $_.PSChildName -InformationAction SilentlyContinue))
                        {
                            return
                        }

                        # Return early for excluded accounts.
                        if ($ExcludeNTAccount -contains $ntAccount)
                        {
                            return
                        }

                        # Establish base profile.
                        $userProfile = [PSADT.Types.UserProfile]::new(
                            $ntAccount,
                            $_.PSChildName,
                            $_.ProfileImagePath
                        )

                        # Append additional info if requested.
                        if ($LoadProfilePaths)
                        {
                            $userProfile = Invoke-ADTAllUsersRegistryAction -UserProfiles $userProfile -InformationAction SilentlyContinue -ScriptBlock {
                                [PSADT.Types.UserProfile]::new(
                                    $_.NTAccount,
                                    $_.SID,
                                    $_.ProfilePath,
                                    $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -Name 'AppData' -SID $_.SID -DoNotExpandEnvironmentNames) -replace '%USERPROFILE%', $_.ProfilePath),
                                    $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -Name 'Local AppData' -SID $_.SID -DoNotExpandEnvironmentNames) -replace '%USERPROFILE%', $_.ProfilePath),
                                    $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -Name 'Desktop' -SID $_.SID -DoNotExpandEnvironmentNames) -replace '%USERPROFILE%', $_.ProfilePath),
                                    $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -Name 'Personal' -SID $_.SID -DoNotExpandEnvironmentNames) -replace '%USERPROFILE%', $_.ProfilePath),
                                    $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -Name 'Start Menu' -SID $_.SID -DoNotExpandEnvironmentNames) -replace '%USERPROFILE%', $_.ProfilePath),
                                    $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Environment' -Name 'TEMP' -SID $_.SID -DoNotExpandEnvironmentNames) -replace '%USERPROFILE%', $_.ProfilePath),
                                    $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Environment' -Name 'OneDrive' -SID $_.SID -DoNotExpandEnvironmentNames) -replace '%USERPROFILE%', $_.ProfilePath),
                                    $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Environment' -Name 'OneDriveCommercial' -SID $_.SID -DoNotExpandEnvironmentNames) -replace '%USERPROFILE%', $_.ProfilePath)
                                )
                            }
                        }

                        # Write out the object to the pipeline.
                        if ($userProfile)
                        {
                            return $userProfile
                        }
                    }
                }

                # Create a custom object for the Default User profile. Since the Default User is not an actual user account, it does not have a username or a SID.
                # We will make up a SID and add it to the custom object so that we have a location to load the default registry hive into later on.
                if (!$ExcludeDefaultUser)
                {
                    # The path to the default profile is stored in the default string value for the key.
                    $defaultUserProfilePath = (Get-ItemProperty -LiteralPath $userProfileListRegKey).Default

                    # Retrieve additional information if requested.
                    if ($LoadProfilePaths)
                    {
                        return [PSADT.Types.UserProfile]::new(
                            'Default',
                            [System.Security.Principal.SecurityIdentifier]::new([System.Security.Principal.WellKnownSidType]::NullSid, $null),
                            $defaultUserProfilePath,
                            $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\.DEFAULT\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -Name 'AppData' -DoNotExpandEnvironmentNames -InformationAction SilentlyContinue) -replace '%USERPROFILE%', $defaultUserProfilePath),
                            $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\.DEFAULT\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -Name 'Local AppData' -DoNotExpandEnvironmentNames -InformationAction SilentlyContinue) -replace '%USERPROFILE%', $defaultUserProfilePath),
                            $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\.DEFAULT\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -Name 'Desktop' -DoNotExpandEnvironmentNames -InformationAction SilentlyContinue) -replace '%USERPROFILE%', $defaultUserProfilePath),
                            $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\.DEFAULT\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -Name 'Personal' -DoNotExpandEnvironmentNames -InformationAction SilentlyContinue) -replace '%USERPROFILE%', $defaultUserProfilePath),
                            $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\.DEFAULT\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -Name 'Start Menu' -DoNotExpandEnvironmentNames -InformationAction SilentlyContinue) -replace '%USERPROFILE%', $defaultUserProfilePath),
                            $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\.DEFAULT\Environment' -Name 'TEMP' -DoNotExpandEnvironmentNames -InformationAction SilentlyContinue) -replace '%USERPROFILE%', $defaultUserProfilePath),
                            $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\.DEFAULT\Environment' -Name 'OneDrive' -DoNotExpandEnvironmentNames -InformationAction SilentlyContinue) -replace '%USERPROFILE%', $defaultUserProfilePath),
                            $((Get-ADTRegistryKey -Key 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\.DEFAULT\Environment' -Name 'OneDriveCommercial' -DoNotExpandEnvironmentNames -InformationAction SilentlyContinue) -replace '%USERPROFILE%', $defaultUserProfilePath)
                        )
                    }
                    return [PSADT.Types.UserProfile]::new(
                        'Default',
                        'S-1-0-0',
                        $defaultUserProfilePath
                    )
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
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
