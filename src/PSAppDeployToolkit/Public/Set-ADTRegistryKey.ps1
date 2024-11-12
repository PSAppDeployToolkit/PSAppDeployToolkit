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

    .PARAMETER Key
        The registry key path.

    .PARAMETER Name
        The value name.

    .PARAMETER Value
        The value data.

    .PARAMETER Type
        The type of registry value to create or set. Options: 'Binary','DWord','ExpandString','MultiString','None','QWord','String','Unknown'. Default: String.

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
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Key,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [System.Object]$Value,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Binary', 'DWord', 'ExpandString', 'MultiString', 'None', 'QWord', 'String', 'Unknown')]
        [Microsoft.Win32.RegistryValueKind]$Type = 'String',

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
                $Key = if ($PSBoundParameters.ContainsKey('SID'))
                {
                    Convert-ADTRegistryPath -Key $Key -Wow6432Node:$Wow6432Node -SID $SID
                }
                else
                {
                    Convert-ADTRegistryPath -Key $Key -Wow6432Node:$Wow6432Node
                }

                # Create registry key if it doesn't exist.
                if (!(Test-Path -LiteralPath $Key))
                {
                    Write-ADTLogEntry -Message "Creating registry key [$Key]."
                    if (($Key.Split('/').Count - 1) -eq 0)
                    {
                        # No forward slash found in Key. Use New-Item cmdlet to create registry key.
                        $null = New-Item -Path $Key -ItemType Registry -Force
                    }
                    else
                    {
                        # Forward slash was found in Key. Use REG.exe ADD to create registry key
                        $RegMode = if ([System.Environment]::Is64BitProcess -and !$Wow6432Node)
                        {
                            '/reg:64'
                        }
                        else
                        {
                            '/reg:32'
                        }
                        $CreateRegKeyResult = & "$([System.Environment]::SystemDirectory)\reg.exe" ADD "$($Key.Substring($Key.IndexOf('::') + 2))" /f $RegMode 2>&1
                        if ($Global:LASTEXITCODE -ne 0)
                        {
                            $naerParams = @{
                                Exception = [System.ApplicationException]::new("Failed to create registry key [$Key]")
                                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                                ErrorId = 'RegKeyCreationFailure'
                                TargetObject = $CreateRegKeyResult
                                RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }
                    }
                }

                if ($Name)
                {
                    if (!(Get-ItemProperty -LiteralPath $Key -Name $Name -ErrorAction Ignore))
                    {
                        # Set registry value if it doesn't exist.
                        Write-ADTLogEntry -Message "Setting registry key value: [$Key] [$Name = $Value]."
                        $null = New-ItemProperty -LiteralPath $Key -Name $Name -Value $Value -PropertyType $Type
                    }
                    else
                    {
                        # Update registry value if it does exist.
                        if ($Name -eq '(Default)')
                        {
                            # Set Default registry key value with the following workaround, because Set-ItemProperty contains a bug and cannot set Default registry key value.
                            $null = (Get-Item -LiteralPath $Key).OpenSubKey('', 'ReadWriteSubTree').SetValue($null, $Value)
                        }
                        else
                        {
                            Write-ADTLogEntry -Message "Updating registry key value: [$Key] [$Name = $Value]."
                            $null = Set-ItemProperty -LiteralPath $Key -Name $Name -Value $Value
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to $(("set registry key [$Key]", "update value [$Value] for registry key [$Key] [$Name]")[!!$Name])."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
