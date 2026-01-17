#-----------------------------------------------------------------------------
#
# MARK: ConvertTo-ADTNTAccountOrSID
#
#-----------------------------------------------------------------------------

function ConvertTo-ADTNTAccountOrSID
{
    <#

    .SYNOPSIS
        Convert between NT Account names and their security identifiers (SIDs).

    .DESCRIPTION
        Specify either the NT Account name or the SID and get the other. Can also convert well known sid types.

    .PARAMETER AccountName
        The Windows NT Account name specified in <domain>\<username> format.

        Use fully qualified account names (e.g., <domain>\<username>) instead of isolated names (e.g, <username>) because they are unambiguous and provide better performance.

    .PARAMETER SID
        The Windows NT Account SID.

    .PARAMETER WellKnownSIDName
        Specify the Well Known SID name translate to the actual SID (e.g., LocalServiceSid).

        To get all well known SIDs available on system: [Enum]::GetNames([Security.Principal.WellKnownSidType])

    .PARAMETER WellKnownToNTAccount
        Convert the Well Known SID to an NTAccount name.

    .PARAMETER LocalHost
        Avoids a costly domain check when only converting local accounts.

    .PARAMETER LdapUri
        Allows specification of the LDAP URI to use, either `LDAP://` or `LDAPS://`.

    .INPUTS
        System.String

        Accepts a string containing the NT Account name or SID.

    .OUTPUTS
        System.String

        Returns the NT Account name or SID.

    .EXAMPLE
        ConvertTo-ADTNTAccountOrSID -AccountName 'CONTOSO\User1'

        Converts a Windows NT Account name to the corresponding SID.

    .EXAMPLE
        ConvertTo-ADTNTAccountOrSID -SID 'S-1-5-21-1220945662-2111687655-725345543-14012660'

        Converts a Windows NT Account SID to the corresponding NT Account Name.

    .EXAMPLE
        ConvertTo-ADTNTAccountOrSID -WellKnownSIDName 'NetworkServiceSid'

        Converts a Well Known SID name to a SID.

    .NOTES
        An active ADT session is NOT required to use this function.

        The conversion can return an empty result if the user account does not exist anymore or if translation fails Refer to: http://blogs.technet.com/b/askds/archive/2011/07/28/troubleshooting-sid-translation-failures-from-the-obvious-to-the-not-so-obvious.aspx

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/ConvertTo-ADTNTAccountOrSID

    .LINK
        http://msdn.microsoft.com/en-us/library/system.security.principal.wellknownsidtype(v=vs.110).aspx

    #>

    [CmdletBinding()]
    [OutputType([System.Security.Principal.SecurityIdentifier])]
    [OutputType([System.Security.Principal.NTAccount])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'NTAccountToSID', ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Security.Principal.NTAccount]$AccountName,

        [Parameter(Mandatory = $true, ParameterSetName = 'SIDToNTAccount', ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Security.Principal.SecurityIdentifier]$SID,

        [Parameter(Mandatory = $true, ParameterSetName = 'WellKnownName', ValueFromPipelineByPropertyName = $true)]
        [Parameter(Mandatory = $true, ParameterSetName = 'WellKnownNameLdap', ValueFromPipelineByPropertyName = $true)]
        [Parameter(Mandatory = $true, ParameterSetName = 'WellKnownNameLocalHost', ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Security.Principal.WellKnownSidType]$WellKnownSIDName,

        [Parameter(Mandatory = $false, ParameterSetName = 'WellKnownName')]
        [Parameter(Mandatory = $false, ParameterSetName = 'WellKnownNameLdap')]
        [Parameter(Mandatory = $false, ParameterSetName = 'WellKnownNameLocalHost')]
        [System.Management.Automation.SwitchParameter]$WellKnownToNTAccount,

        [Parameter(Mandatory = $true, ParameterSetName = 'WellKnownNameLocalHost')]
        [System.Management.Automation.SwitchParameter]$LocalHost,

        [Parameter(Mandatory = $true, ParameterSetName = 'WellKnownNameLdap')]
        [ValidateSet('LDAP://', 'LDAPS://')]
        [System.String]$LdapUri = 'LDAP://'
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

        # Internal worker function for SID to NTAccount translation.
        function Convert-ADTSIDToNTAccount
        {
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.Security.Principal.SecurityIdentifier]$TargetSid
            )

            # Try a regular translation first.
            try
            {
                return $TargetSid.Translate([System.Security.Principal.NTAccount])
            }
            catch
            {
                # Device likely is off the domain network and had no line of sight to a domain controller.
                # Attempt to rummage through the group policy cache and see what's available to us.
                # Failing this, throw out the original error as there's not much we can do otherwise.
                if (!($TargetNtAccount = [PSADT.AccountManagement.GroupPolicyAccountInfo]::Get() | & { process { if ($_.SID.Equals($TargetSid)) { return $_.Username } } } | Select-Object -First 1))
                {
                    throw
                }
                return $TargetNtAccount
            }
        }

        # Internal worker function for SID to NTAccount translation.
        function Convert-ADTNTAccountToSID
        {
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.Security.Principal.NTAccount]$TargetNtAccount
            )

            # Try a regular translation first.
            try
            {
                return $TargetNtAccount.Translate([System.Security.Principal.SecurityIdentifier])
            }
            catch
            {
                # Device likely is off the domain network and had no line of sight to a domain controller.
                # Attempt to rummage through the group policy cache and see what's available to us.
                # Failing this, throw out the original error as there's not much we can do otherwise.
                if (!($TargetSid = [PSADT.AccountManagement.GroupPolicyAccountInfo]::Get() | & { if ($_.Username.Equals($TargetNtAccount)) { return $_.SID } } | Select-Object -First 1))
                {
                    throw
                }
                return $TargetSid
            }
        }

        # Pre-calculate the domain SID.
        $DomainSid = if ($PSCmdlet.ParameterSetName.StartsWith('WellKnownName') -and !$LocalHost)
        {
            try
            {
                [System.Security.Principal.SecurityIdentifier]::new([System.DirectoryServices.DirectoryEntry]::new("$LdapUri$((Get-CimInstance -ClassName Win32_ComputerSystem).Domain.ToLowerInvariant())").ObjectSid[0], 0)
            }
            catch
            {
                Write-ADTLogEntry -Message 'Unable to get Domain SID from Active Directory. Setting Domain SID to $null.' -Severity 2
            }
        }
    }

    process
    {
        try
        {
            try
            {
                switch -regex ($PSCmdlet.ParameterSetName)
                {
                    '^SIDToNTAccount'
                    {
                        Write-ADTLogEntry -Message "Converting $(($msg = "the SID [$SID] to an NT Account name"))."
                        return (Convert-ADTSIDToNTAccount -TargetSid $SID)
                    }
                    '^NTAccountToSID'
                    {
                        Write-ADTLogEntry -Message "Converting $(($msg = "the NT Account [$AccountName] to a SID"))."
                        return (Convert-ADTNTAccountToSID -TargetNtAccount $AccountName)
                    }
                    '^WellKnownName'
                    {
                        Write-ADTLogEntry -Message "Converting $(($msg = "the Well Known SID Name [$WellKnownSIDName] to a $(('SID', 'NTAccount')[!!$WellKnownToNTAccount])"))."
                        $NTAccountSID = [System.Security.Principal.SecurityIdentifier]::new($WellKnownSIDName, $DomainSid)
                        if ($WellKnownToNTAccount)
                        {
                            return (Convert-ADTSIDToNTAccount -TargetSid $NTAccountSID)
                        }
                        return $NTAccountSID
                    }
                }
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Error ensures the correct PositionMessage is used.
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to convert $msg. It may not be a valid account anymore or there is some other problem."
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
