#-----------------------------------------------------------------------------
#
# MARK: Set-ADTRegistryKey
#
#-----------------------------------------------------------------------------

function Set-ADTRegistryKey
{
    <#
    .SYNOPSIS
        Creates or sets a registry key name, value, and value data.

    .DESCRIPTION
        Creates a registry key name, value, and value data; it sets the same if it already exists. This function can also handle registry keys for specific user SIDs and 32-bit registry on 64-bit systems.

    .PARAMETER LiteralPath
        The registry key path.

    .PARAMETER Name
        The value name.

    .PARAMETER Value
        The value data.

    .PARAMETER Type
        The type of registry value to create or set.

        DWord should be specified as a decimal.

    .PARAMETER Wow6432Node
        Specify this switch to write to the 32-bit registry (Wow6432Node) on 64-bit systems.

    .PARAMETER SID
        The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

        Specify this parameter from the Invoke-ADTAllUsersRegistryAction function to read/edit HKCU registry settings for all users on the system.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Set-ADTRegistryKey -Key $blockedAppPath -Name 'Debugger' -Value $blockedAppDebuggerValue

        Creates or sets the 'Debugger' value in the specified registry key.

    .EXAMPLE
        Set-ADTRegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE' -Name 'Application' -Type 'DWord' -Value '1'

        Creates or sets a DWord value in the specified registry key.

    .EXAMPLE
        Set-ADTRegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce' -Name 'Debugger' -Value $blockedAppDebuggerValue -Type String

        Creates or sets a String value in the specified registry key.

    .EXAMPLE
        Set-ADTRegistryKey -Key 'HKCU\Software\Microsoft\Example' -Name 'Data' -Value (0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x02,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x02,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x00,0x01,0x01,0x01,0x02,0x02,0x02) -Type 'Binary'

        Creates or sets a Binary value in the specified registry key.

    .EXAMPLE
        Set-ADTRegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Example' -Name '(Default)' -Value "Text"

        Creates or sets the default value in the specified registry key.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: © 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTRegistryKey
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, HelpMessage = 'New/Set-ItemProperty parameter')]
        [ValidateNotNullOrEmpty()]
        [Alias('Key')]
        [System.String]$LiteralPath,

        [Parameter(Mandatory = $false, HelpMessage = 'New/Set-ItemProperty parameter')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false, HelpMessage = 'New/Set-ItemProperty parameter')]
        [System.Object]$Value,

        [Parameter(Mandatory = $false, HelpMessage = 'New/Set-ItemProperty parameter')]
        [ValidateNotNullOrEmpty()]
        [Microsoft.Win32.RegistryValueKind]$Type,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Wow6432Node,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$SID
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        try
        {
            try
            {
                # If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID.
                $PSBoundParameters.LiteralPath = $LiteralPath = if ($PSBoundParameters.ContainsKey('SID'))
                {
                    Convert-ADTRegistryPath -Key $LiteralPath -Wow6432Node:$Wow6432Node -SID $SID
                }
                else
                {
                    Convert-ADTRegistryPath -Key $LiteralPath -Wow6432Node:$Wow6432Node
                }

                # Create registry key if it doesn't exist.
                if (!(Test-Path -LiteralPath $LiteralPath))
                {
                    Write-ADTLogEntry -Message "Creating registry key [$LiteralPath]."
                    $provider, $subkey = [System.Text.RegularExpressions.Regex]::Matches($LiteralPath, '^(.+::[a-zA-Z_]+)\\(.+)$').Groups[1..2].Value
                    $regKey = Get-Item -LiteralPath $provider
                    $null = $regKey.CreateSubKey($subkey)
                    $regKey.Close()
                }

                # If a name was provided, set the appropriate ItemProperty up.
                if ($PSBoundParameters.ContainsKey('Name'))
                {
                    # Build out ItemProperty parameters.
                    $ipParams = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -HelpMessage 'New/Set-ItemProperty parameter'

                    # Set registry value if it doesn't exist, otherwise update the property.
                    $null = if (!(Get-ItemProperty -LiteralPath $LiteralPath -Name $Name -ErrorAction Ignore))
                    {
                        Write-ADTLogEntry -Message "Setting registry key value: [$LiteralPath] [$Name = $Value]."
                        New-ItemProperty @ipParams
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Updating registry key value: [$LiteralPath] [$Name = $Value]."
                        if (!$ipParams.ContainsKey('Value')) { $ipParams.Add('Value', $null) }
                        Set-ItemProperty @ipParams -Force
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to $(("set registry key [$LiteralPath]", "update value [$Value] for registry key [$LiteralPath] [$Name]")[!!$Name])."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
