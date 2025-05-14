#-----------------------------------------------------------------------------
#
# MARK: Convert-ADTRegistryPath
#
#-----------------------------------------------------------------------------

function Convert-ADTRegistryPath
{
    <#
    .SYNOPSIS
        Converts the specified registry key path to a format that is compatible with built-in PowerShell cmdlets.

    .DESCRIPTION
        Converts the specified registry key path to a format that is compatible with built-in PowerShell cmdlets.

        Converts registry key hives to their full paths. Example: HKLM is converted to "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE".

    .PARAMETER Key
        Path to the registry key to convert (can be a registry hive or fully qualified path)

    .PARAMETER Wow6432Node
        Specifies that the 32-bit registry view (Wow6432Node) should be used on a 64-bit system.

    .PARAMETER SID
        The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

        Specify this parameter from the Invoke-ADTAllUsersRegistryAction function to read/edit HKCU registry settings for all users on the system.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        Returns the converted registry key path.

    .EXAMPLE
        Convert-ADTRegistryPath -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'

        Converts the specified registry key path to a format compatible with PowerShell cmdlets.

    .EXAMPLE
        Convert-ADTRegistryPath -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'

        Converts the specified registry key path to a format compatible with PowerShell cmdlets.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Convert-ADTRegistryPath
    #>

    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Key,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$SID,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Wow6432Node
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Suppress logging output unless the caller has said otherwise.
        if (!$PSBoundParameters.ContainsKey('InformationAction'))
        {
            $InformationPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
        }
    }

    process
    {
        try
        {
            try
            {
                # Convert the registry key hive to the full path, only match if at the beginning of the line.
                $Script:Registry.PathReplacements.GetEnumerator() | . {
                    process
                    {
                        if ($Key -match $_.Key)
                        {
                            foreach ($regexMatch in ($Script:Registry.PathMatches -replace '^', $_.Key))
                            {
                                $Key = $Key -replace $regexMatch, $_.Value
                            }
                        }
                    }
                }

                # Process the WOW6432Node values if applicable.
                if ($Wow6432Node -and [System.Environment]::Is64BitProcess)
                {
                    $Script:Registry.WOW64Replacements.GetEnumerator() | . {
                        process
                        {
                            if ($Key -match $_.Key)
                            {
                                $Key = $Key -replace $_.Key, $_.Value
                            }
                        }
                    }
                }

                # Append the PowerShell provider to the registry key path.
                if ($Key -notmatch '^Microsoft\.PowerShell\.Core\\Registry::')
                {
                    $Key = "Microsoft.PowerShell.Core\Registry::$($PSCmdlet.GetUnresolvedProviderPathFromPSPath($key))"
                }

                # If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID.
                if ($PSBoundParameters.ContainsKey('SID'))
                {
                    if ($Key -match '^Microsoft\.PowerShell\.Core\\Registry::HKEY_CURRENT_USER\\')
                    {
                        $Key = $Key -replace '^Microsoft\.PowerShell\.Core\\Registry::HKEY_CURRENT_USER\\', "Microsoft.PowerShell.Core\Registry::HKEY_USERS\$SID\"
                    }
                    else
                    {
                        Write-ADTLogEntry -Message 'SID parameter specified but the registry hive of the key is not HKEY_CURRENT_USER.' -Severity 2
                        return
                    }
                }

                # Check for expected key string format.
                if ($Key -notmatch '^Microsoft\.PowerShell\.Core\\Registry::HKEY_(LOCAL_MACHINE|CLASSES_ROOT|CURRENT_USER|USERS|CURRENT_CONFIG|PERFORMANCE_DATA)')
                {
                    $naerParams = @{
                        Exception = [System.ArgumentException]::new("Unable to detect target registry hive in string [$Key].")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'RegistryKeyValueInvalid'
                        TargetObject = $Key
                        RecommendedAction = "Please confirm the supplied value is correct and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }
                Write-ADTLogEntry -Message "Return fully qualified registry key path [$Key]."
                return $Key
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
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
