function Remove-ADTRegistryKey
{
    <#

    .SYNOPSIS
    Deletes the specified registry key or value.

    .DESCRIPTION
    Deletes the specified registry key or value.

    .PARAMETER Key
    Path of the registry key to delete.

    .PARAMETER Name
    Name of the registry value to delete.

    .PARAMETER Recurse
    Delete registry key recursively.

    .PARAMETER SID
    The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

    Specify this parameter from the Invoke-ADTAllUsersRegistryChange function to read/edit HKCU registry settings for all users on the system.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    Remove-ADTRegistryKey -Key 'HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\RunOnce'

    .EXAMPLE
    Remove-ADTRegistryKey -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name 'RunAppInstall'

    .EXAMPLE
    Remove-ADTRegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Example' -Name '(Default)'

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    Param (
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

    begin {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction Continue
    }

    process {
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
                if (!(Test-Path -LiteralPath $Key))
                {
                    Write-ADTLogEntry -Message "Unable to delete registry key [$Key] because it does not exist." -Severity 2
                    return
                }

                if ($Recurse)
                {
                    Write-ADTLogEntry -Message "Deleting registry key recursively [$Key]."
                    [System.Void](Remove-Item -LiteralPath $Key -Force -Recurse)
                }
                elseif (!(Get-ChildItem -LiteralPath $Key))
                {
                    # Check if there are subkeys of $Key, if so, executing Remove-Item will hang. Avoiding this with Get-ChildItem.
                    Write-ADTLogEntry -Message "Deleting registry key [$Key]."
                    [System.Void](Remove-Item -LiteralPath $Key -Force)
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
                    Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
                }
            }
            else
            {
                if (!(Test-Path -LiteralPath $Key))
                {
                    Write-ADTLogEntry -Message "Unable to delete registry value [$Key] [$Name] because registry key does not exist." -Severity 2
                    return
                }
                Write-ADTLogEntry -Message "Deleting registry value [$Key] [$Name]."
                if ($Name -eq '(Default)')
                {
                    # Remove (Default) registry key value with the following workaround because Remove-ItemProperty cannot remove the (Default) registry key value.
                    [System.Void]((Get-Item -LiteralPath $Key).OpenSubKey('', 'ReadWriteSubTree').DeleteValue(''))
                }
                else
                {
                    [System.Void](Remove-ItemProperty -LiteralPath $Key -Name $Name -Force)
                }
            }
        }
        catch [System.Management.Automation.PSArgumentException]
        {
            Write-ADTLogEntry -Message "Unable to delete registry value [$Key] [$Name] because it does not exist." -Severity 2
        }
        catch
        {
            if ($Name)
            {
                Write-ADTLogEntry -Message "Failed to delete registry value [$Key] [$Name].`n$(Resolve-ADTError)" -Severity 3
            }
            else
            {
                Write-ADTLogEntry -Message "Failed to delete registry key [$Key].`n$(Resolve-ADTError)" -Severity 3
            }
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
