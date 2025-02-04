#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTRegistryKey
#
#-----------------------------------------------------------------------------

function Remove-ADTRegistryKey
{
    <#
    .SYNOPSIS
        Deletes the specified registry key or value.

    .DESCRIPTION
        This function deletes the specified registry key or value. It can handle both registry keys and values, and it supports recursive deletion of registry keys. If the SID parameter is specified, it converts HKEY_CURRENT_USER registry keys to the HKEY_USERS\$SID format, allowing for the manipulation of HKCU registry settings for all users on the system.

    .PARAMETER Path
        Path of the registry key to delete, wildcards permitted.

    .PARAMETER LiteralPath
        Literal path of the registry key to delete.

    .PARAMETER Name
        Name of the registry value to delete.

    .PARAMETER Wow6432Node
        Specify this switch to read the 32-bit registry (Wow6432Node) on 64-bit systems.

    .PARAMETER Recurse
        Delete registry key recursively.

    .PARAMETER SID
        The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

        Specify this parameter from the Invoke-ADTAllUsersRegistryAction function to read/edit HKCU registry settings for all users on the system.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Remove-ADTRegistryKey -Key 'HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\RunOnce'

        Deletes the specified registry key.

    .EXAMPLE
        Remove-ADTRegistryKey -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name 'RunAppInstall'

        Deletes the specified registry value.

    .EXAMPLE
        Remove-ADTRegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Example' -Name '(Default)'

        Deletes the default registry value in the specified key.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
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
        [System.Management.Automation.SwitchParameter]$Recurse,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$SID
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

                if (!$Name)
                {
                    if (!(Test-Path @pathParam))
                    {
                        Write-ADTLogEntry -Message "Unable to delete registry key [$($pathParam.($PSCmdlet.ParameterSetName))] because it does not exist." -Severity 2
                        return
                    }

                    if ($Recurse)
                    {
                        Write-ADTLogEntry -Message "Deleting registry key recursively [$($pathParam.($PSCmdlet.ParameterSetName))]."
                        $null = Remove-Item @pathParam -Force -Recurse
                    }
                    elseif (!(Get-ChildItem @pathParam))
                    {
                        # Check if there are subkeys of the path, if so, executing Remove-Item will hang. Avoiding this with Get-ChildItem.
                        Write-ADTLogEntry -Message "Deleting registry key [$($pathParam.($PSCmdlet.ParameterSetName))]."
                        $null = Remove-Item @pathParam -Force
                    }
                    else
                    {
                        $naerParams = @{
                            Exception = [System.InvalidOperationException]::new("Unable to delete child key(s) of [$($pathParam.($PSCmdlet.ParameterSetName))] without [-Recurse] switch.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                            ErrorId = 'SubKeyRecursionError'
                            TargetObject = $pathParam.($PSCmdlet.ParameterSetName)
                            RecommendedAction = "Please run this command again with [-Recurse]."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                }
                else
                {
                    if (!(Test-Path @pathParam))
                    {
                        Write-ADTLogEntry -Message "Unable to delete registry value [$($pathParam.($PSCmdlet.ParameterSetName))] [$Name] because registry key does not exist." -Severity 2
                        return
                    }
                    Write-ADTLogEntry -Message "Deleting registry value [$($pathParam.($PSCmdlet.ParameterSetName))] [$Name]."
                    if ($Name -eq '(Default)')
                    {
                        # Remove (Default) registry key value with the following workaround because Remove-ItemProperty cannot remove the (Default) registry key value.
                        $null = (Get-Item @pathParam).OpenSubKey('', 'ReadWriteSubTree').DeleteValue('')
                    }
                    else
                    {
                        $null = Remove-ItemProperty @pathParam -Name $Name -Force
                    }
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch [System.Management.Automation.PSArgumentException]
        {
            Write-ADTLogEntry -Message "Unable to delete registry value [$($pathParam.($PSCmdlet.ParameterSetName))] [$Name] because it does not exist." -Severity 2
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to delete registry $(("key [$($pathParam.($PSCmdlet.ParameterSetName))]", "value [$($pathParam.($PSCmdlet.ParameterSetName))] [$Name]")[!!$Name])."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
