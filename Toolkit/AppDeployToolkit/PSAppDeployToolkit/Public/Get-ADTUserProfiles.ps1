#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Get-ADTUserProfiles
{
    <#

    .SYNOPSIS
    Get the User Profile Path, User Account SID, and the User Account Name for all users that log onto the machine and also the Default User (which does not log on).

    .DESCRIPTION
    Get the User Profile Path, User Account SID, and the User Account Name for all users that log onto the machine and also the Default User (which does  not log on).

    Please note that the NTAccount property may be empty for some user profiles but the SID and ProfilePath properties will always be populated.

    .PARAMETER ExcludeNTAccount
    Specify NT account names in DOMAIN\username format to exclude from the list of user profiles.

    .PARAMETER IncludeSystemProfiles
    Include system profiles: SYSTEM, LOCAL SERVICE, NETWORK SERVICE. Default is: $false.

    .PARAMETER IncludeServiceProfiles
    Include service profiles where NTAccount begins with NT SERVICE. Default is: $false.

    .PARAMETER ExcludeDefaultUser
    Exclude the Default User. Default is: $false.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    PSADT.Types.UserProfile. Returns a PSADT.Types.UserProfile object with the following properties: NTAccount, SID, ProfilePath

    .EXAMPLE
    # Return the following properties for each user profile on the system: NTAccount, SID, ProfilePath
    Get-ADTUserProfiles

    .EXAMPLE
    # Return the following properties for each user profile on the system, except for 'Robot' and 'ntadmin': NTAccount, SID, ProfilePath
    Get-ADTUserProfiles -ExcludeNTAccount CONTOSO\Robot,CONTOSO\ntadmin

    .EXAMPLE
    # Return the user profile path for each user on the system. This information can then be used to make modifications under the user profile on the filesystem.
    [string[]]$ProfilePaths = Get-ADTUserProfiles | Select-Object -ExpandProperty ProfilePath

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    [OutputType([PSADT.Types.UserProfile])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ExcludeNTAccount,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeSystemProfiles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeServiceProfiles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExcludeDefaultUser
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $userProfileListRegKey = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList'
        $excludedSids = "^($([System.String]::Join('|', $(if (!$IncludeSystemProfiles) {'S-1-5-18', 'S-1-5-19', 'S-1-5-20'}; 'S-1-5-82'))))"
    }

    process
    {
        Write-ADTLogEntry -Message 'Getting the User Profile Path, User Account SID, and the User Account Name for all users that log onto the machine.'
        try
        {
            try
            {
                # Get the User Profile Path, User Account SID, and the User Account Name for all users that log onto the machine.
                foreach ($profileListing in (& $Script:CommandTable.'Get-ItemProperty' -Path "$userProfileListRegKey\*" | & $Script:CommandTable.'Where-Object' {$_.PSChildName -notmatch $excludedSids}))
                {
                    # Return early for accounts that have a null NTAccount.
                    if (!($ntAccount = ConvertTo-ADTNTAccountOrSID -SID $profileListing.PSChildName | & $Script:CommandTable.'Select-Object' -ExpandProperty Value))
                    {
                        return
                    }

                    # Exclude early for excluded accounts.
                    if (($ExcludeNTAccount -contains $ntAccount) -or (!$IncludeServiceProfiles -and $ntAccount.StartsWith('NT SERVICE\')))
                    {
                        return
                    }

                    # Write out the object to the pipeline.
                    [PSADT.Types.UserProfile]@{
                        NTAccount = $ntAccount
                        SID = $profileListing.PSChildName
                        ProfilePath = $profileListing.ProfileImagePath
                    }
                }

                # Create a custom object for the Default User profile. Since the Default User is not an actual user account, it does not have a username or a SID.
                # We will make up a SID and add it to the custom object so that we have a location to load the default registry hive into later on.
                if (!$ExcludeDefaultUser)
                {
                    [PSADT.Types.UserProfile]@{
                        NTAccount = 'Default User'
                        SID = 'S-1-5-21-Default-User'
                        ProfilePath = (& $Script:CommandTable.'Get-ItemProperty' -LiteralPath $userProfileListRegKey).Default
                    }
                }
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
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
