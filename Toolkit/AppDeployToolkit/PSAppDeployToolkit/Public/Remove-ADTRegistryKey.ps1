#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Remove-ADTRegistryKey
{
    <#
    .SYNOPSIS
        Deletes the specified registry key or value.

    .DESCRIPTION
        This function deletes the specified registry key or value. It can handle both registry keys and values, and it supports recursive deletion of registry keys. If the SID parameter is specified, it converts HKEY_CURRENT_USER registry keys to the HKEY_USERS\$SID format, allowing for the manipulation of HKCU registry settings for all users on the system.

    .PARAMETER Key
        Path of the registry key to delete.

        Mandatory: True

    .PARAMETER Name
        Name of the registry value to delete.

        Mandatory: False

    .PARAMETER Recurse
        Delete registry key recursively.

        Mandatory: False

    .PARAMETER SID
        The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

        Specify this parameter from the Invoke-ADTAllUsersRegistryChange function to read/edit HKCU registry settings for all users on the system.

        Mandatory: False

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        # Example 1
        Remove-ADTRegistryKey -Key 'HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\RunOnce'

        Deletes the specified registry key.

    .EXAMPLE
        # Example 2
        Remove-ADTRegistryKey -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name 'RunAppInstall'

        Deletes the specified registry value.

    .EXAMPLE
        # Example 3
        Remove-ADTRegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Example' -Name '(Default)'

        Deletes the default registry value in the specified key.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
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
        [System.Management.Automation.SwitchParameter]$Recurse,

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
                    Convert-ADTRegistryPath -Key $Key -SID $SID
                }
                else
                {
                    Convert-ADTRegistryPath -Key $Key
                }

                if (!$Name)
                {
                    if (!(& $Script:CommandTable.'Test-Path' -LiteralPath $Key))
                    {
                        Write-ADTLogEntry -Message "Unable to delete registry key [$Key] because it does not exist." -Severity 2
                        return
                    }

                    if ($Recurse)
                    {
                        Write-ADTLogEntry -Message "Deleting registry key recursively [$Key]."
                        $null = & $Script:CommandTable.'Remove-Item' -LiteralPath $Key -Force -Recurse
                    }
                    elseif (!(& $Script:CommandTable.'Get-ChildItem' -LiteralPath $Key))
                    {
                        # Check if there are subkeys of $Key, if so, executing Remove-Item will hang. Avoiding this with Get-ChildItem.
                        Write-ADTLogEntry -Message "Deleting registry key [$Key]."
                        $null = & $Script:CommandTable.'Remove-Item' -LiteralPath $Key -Force
                    }
                    else
                    {
                        $naerParams = @{
                            Exception = [System.InvalidOperationException]::new("Unable to delete child key(s) of [$Key] without [-Recurse] switch.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                            ErrorId = 'SubkeyRecursionError'
                            TargetObject = $Key
                            RecommendedAction = "Please run this command again with [-Recurse]."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                }
                else
                {
                    if (!(& $Script:CommandTable.'Test-Path' -LiteralPath $Key))
                    {
                        Write-ADTLogEntry -Message "Unable to delete registry value [$Key] [$Name] because registry key does not exist." -Severity 2
                        return
                    }
                    Write-ADTLogEntry -Message "Deleting registry value [$Key] [$Name]."
                    if ($Name -eq '(Default)')
                    {
                        # Remove (Default) registry key value with the following workaround because Remove-ItemProperty cannot remove the (Default) registry key value.
                        $null = (& $Script:CommandTable.'Get-Item' -LiteralPath $Key).OpenSubKey('', 'ReadWriteSubTree').DeleteValue('')
                    }
                    else
                    {
                        $null = & $Script:CommandTable.'Remove-ItemProperty' -LiteralPath $Key -Name $Name -Force
                    }
                }
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch [System.Management.Automation.PSArgumentException]
        {
            Write-ADTLogEntry -Message "Unable to delete registry value [$Key] [$Name] because it does not exist." -Severity 2
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage $(if ($Name)
                {
                    "Failed to delete registry value [$Key] [$Name]."
                }
                else
                {
                    "Failed to delete registry key [$Key]."
                })
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
