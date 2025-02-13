#-----------------------------------------------------------------------------
#
# MARK: Get-ADTRegistryKey
#
#-----------------------------------------------------------------------------

function Get-ADTRegistryKey
{
    <#
    .SYNOPSIS
        Retrieves value names and value data for a specified registry key or optionally, a specific value.

    .DESCRIPTION
        Retrieves value names and value data for a specified registry key or optionally, a specific value. If the registry key does not exist or contain any values, the function will return $null by default.

        To test for existence of a registry key path, use built-in Test-Path cmdlet.

    .PARAMETER Path
        Path of the registry key, wildcards permitted.

    .PARAMETER LiteralPath
        Literal path of the registry key.

    .PARAMETER Name
        Value name to retrieve (optional).

    .PARAMETER Wow6432Node
        Specify this switch to read the 32-bit registry (Wow6432Node) on 64-bit systems.

    .PARAMETER SID
        The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

        Specify this parameter from the Invoke-ADTAllUsersRegistryAction function to read/edit HKCU registry settings for all users on the system.

    .PARAMETER ReturnEmptyKeyIfExists
        Return the registry key if it exists but it has no property/value pairs underneath it.

    .PARAMETER DoNotExpandEnvironmentNames
        Return unexpanded REG_EXPAND_SZ values.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        Returns the value of the registry key or value.

    .EXAMPLE
        Get-ADTRegistryKey -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'

        This example retrieves all value names and data for the specified registry key.

    .EXAMPLE
        Get-ADTRegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\iexplore.exe'

        This example retrieves all value names and data for the specified registry key.

    .EXAMPLE
        Get-ADTRegistryKey -Key 'HKLM:Software\Wow6432Node\Microsoft\Microsoft SQL Server Compact Edition\v3.5' -Name 'Version'

        This example retrieves the 'Version' value data for the specified registry key.

    .EXAMPLE
        Get-ADTRegistryKey -Key 'HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment' -Name 'Path' -DoNotExpandEnvironmentNames

        This example retrieves the 'Path' value data without expanding environment variables.

    .EXAMPLE
        Get-ADTRegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Example' -Name '(Default)'

        This example retrieves the default value data for the specified registry key.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTRegistryKey
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [ValidateNotNullOrEmpty()]
        [SupportsWildcards()]
        [System.String]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullOrEmpty()]
        [Alias('Key')]
        [System.String]$LiteralPath,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Wow6432Node,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$SID,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ReturnEmptyKeyIfExists,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DoNotExpandEnvironmentNames
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
        $pathParam = @{ $PSCmdlet.ParameterSetName = Get-Variable -Name $PSCmdlet.ParameterSetName -ValueOnly }
    }

    process
    {
        try
        {
            try
            {
                # If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID.
                $pathParam.($PSCmdlet.ParameterSetName) = if ($PSBoundParameters.ContainsKey('SID'))
                {
                    Convert-ADTRegistryPath -Key $pathParam.($PSCmdlet.ParameterSetName) -Wow6432Node:$Wow6432Node -SID $SID
                }
                else
                {
                    Convert-ADTRegistryPath -Key $pathParam.($PSCmdlet.ParameterSetName) -Wow6432Node:$Wow6432Node
                }

                # Check if the registry key exists before continuing.
                if (!(Test-Path @pathParam))
                {
                    Write-ADTLogEntry -Message "Registry key [$($pathParam.($PSCmdlet.ParameterSetName))] does not exist. Return `$null." -Severity 2
                    return
                }

                if ($PSBoundParameters.ContainsKey('Name'))
                {
                    Write-ADTLogEntry -Message "Getting registry key [$($pathParam.($PSCmdlet.ParameterSetName))] value [$Name]."
                }
                else
                {
                    Write-ADTLogEntry -Message "Getting registry key [$($pathParam.($PSCmdlet.ParameterSetName))] and all property values."
                }

                # Get all property values for registry key and enumerate.
                Get-Item @pathParam | & {
                    process
                    {
                        # Select requested property.
                        if (![System.String]::IsNullOrWhiteSpace($Name))
                        {
                            # Get the Value (do not make a strongly typed variable because it depends entirely on what kind of value is being read)
                            if ($_.Property -notcontains $Name)
                            {
                                Write-ADTLogEntry -Message "Registry key value [$($_.PSPath)] [$Name] does not exist. Return `$null."
                                return
                            }
                            if ($DoNotExpandEnvironmentNames)
                            {
                                return $_.GetValue($(if ($Name -ne '(Default)') { $Name }), $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
                            }
                            elseif ($Name -like '(Default)')
                            {
                                return $_.GetValue($null)
                            }
                            else
                            {
                                return Get-ItemProperty -LiteralPath $_.PSPath | Select-Object -ExpandProperty $Name
                            }
                        }
                        elseif ($_.Property.Count -eq 0)
                        {
                            # Select all properties or return empty key object.
                            if ($ReturnEmptyKeyIfExists)
                            {
                                Write-ADTLogEntry -Message "No property values found for [$($_.PSPath)]. Return empty registry key object."
                                return $_
                            }
                            else
                            {
                                Write-ADTLogEntry -Message "No property values found for [$($_.PSPath)]. Return `$null."
                                return
                            }
                        }

                        # Return the populated registry key to the caller.
                        return Get-ItemProperty -LiteralPath $_.PSPath
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to read registry key [$($pathParam.($PSCmdlet.ParameterSetName))]$(if ($Name) {" value [$Name]"})."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
