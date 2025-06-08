#-----------------------------------------------------------------------------
#
# MARK: Get-ADTPresentationSettingsEnabledUsers
#
#-----------------------------------------------------------------------------

function Get-ADTPresentationSettingsEnabledUsers
{
    <#
    .SYNOPSIS
        Tests whether any users have presentation mode enabled on their device.

    .DESCRIPTION
        Tests whether any users have presentation mode enabled on their device. This can be enabled via the PC's Mobility Settings, or with PresentationSettings.exe.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.UserProfile

        Returns one or more UserProfile objects of the users with presentation mode enabled on their device.

    .EXAMPLE
        Get-ADTPresentationSettingsEnabledUsers

        Checks whether any users users have presentation settings enabled on their device and returns an associated UserProfile object.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTPresentationSettingsEnabledUsers
    #>

    [CmdletBinding()]
    [OutputType([PSADT.Types.UserProfile])]
    param
    (
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        Write-ADTLogEntry -Message "Checking whether any logged on users are in presentation mode..."
        try
        {
            try
            {
                # Build out params for Invoke-ADTAllUsersRegistryAction.
                $iaauraParams = @{
                    ScriptBlock = { if (Get-ADTRegistryKey -Key Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Software\Microsoft\MobilePC\AdaptableSettings\Activity -Name Activity -SID $_.SID -ErrorAction SilentlyContinue) { return $_ } }
                    UserProfiles = Get-ADTUserProfiles -ExcludeDefaultUser -InformationAction SilentlyContinue
                }

                # Return UserProfile objects for each user with "I am currently giving a presentation" enabled.
                if ($iaauraParams.UserProfiles -and ($usersInPresentationMode = Invoke-ADTAllUsersRegistryAction @iaauraParams -SkipUnloadedProfiles -InformationAction SilentlyContinue))
                {
                    Write-ADTLogEntry -Message "The following users are currently in presentation mode: ['$([System.String]::Join("', '", $usersInPresentationMode.NTAccount))']."
                    return $usersInPresentationMode
                }
                Write-ADTLogEntry -Message "There are no logged on users in presentation mode."
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
