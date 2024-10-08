#-----------------------------------------------------------------------------
#
# MARK: Test-ADTRegistryValue
#
#-----------------------------------------------------------------------------

function Test-ADTRegistryValue
{
    <#
    .SYNOPSIS
        Test if a registry value exists.

    .DESCRIPTION
        Checks a registry key path to see if it has a value with a given name. Can correctly handle cases where a value simply has an empty or null value.

    .PARAMETER Key
        Path of the registry key.

    .PARAMETER Name
        Specify the name of the value to check the existence of.

    .PARAMETER SID
        The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

        Specify this parameter from the Invoke-ADTAllUsersRegistryAction function to read/edit HKCU registry settings for all users on the system.

    .PARAMETER Wow6432Node
        Specify this switch to check the 32-bit registry (Wow6432Node) on 64-bit systems.

    .INPUTS
        System.String

        Accepts a string value for the registry key path.

    .OUTPUTS
        System.Boolean

        Returns $true if the registry value exists, $false if it does not.

    .EXAMPLE
        Test-ADTRegistryValue -Key 'HKLM:SYSTEM\CurrentControlSet\Control\Session Manager' -Name 'PendingFileRenameOperations'

        Checks if the registry value 'PendingFileRenameOperations' exists under the specified key.

    .NOTES
        An active ADT session is NOT required to use this function.

        To test if a registry key exists, use the Test-Path function like so: Test-Path -LiteralPath $Key -PathType 'Container'

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Key,

        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullOrEmpty()]
        [System.Object]$Name,

        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateNotNullOrEmpty()]
        [System.String]$SID,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Wow6432Node
    )

    begin
    {
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID.
                $Key = if ($PSBoundParameters.ContainsKey('SID'))
                {
                    & $Script:CommandTable.'Convert-ADTRegistryPath' -Key $Key -Wow6432Node:$Wow6432Node -SID $SID
                }
                else
                {
                    & $Script:CommandTable.'Convert-ADTRegistryPath' -Key $Key -Wow6432Node:$Wow6432Node
                }

                # Test whether value exists or not.
                if ((& $Script:CommandTable.'Get-Item' -LiteralPath $Key -ErrorAction Ignore | & $Script:CommandTable.'Select-Object' -ExpandProperty Property -ErrorAction Ignore) -contains $Name)
                {
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message "Registry key value [$Key] [$Name] does exist."
                    return $true
                }
                & $Script:CommandTable.'Write-ADTLogEntry' -Message "Registry key value [$Key] [$Name] does not exist."
                return $false
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}
