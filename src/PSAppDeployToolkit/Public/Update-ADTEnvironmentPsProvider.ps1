﻿#-----------------------------------------------------------------------------
#
# MARK: Update-ADTEnvironmentPsProvider
#
#-----------------------------------------------------------------------------

function Update-ADTEnvironmentPsProvider
{
    <#
    .SYNOPSIS
        Updates the environment variables for the current PowerShell session with any environment variable changes that may have occurred during script execution.

    .DESCRIPTION
        Environment variable changes that take place during script execution are not visible to the current PowerShell session.

        Use this function to refresh the current PowerShell session with all environment variable settings.

    .PARAMETER LoadLoggedOnUserEnvironmentVariables
        If script is running in SYSTEM context, this option allows loading environment variables from the active console user. If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Update-ADTEnvironmentPsProvider

        Refreshes the current PowerShell session with all environment variable settings.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Update-ADTEnvironmentPsProvider
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [System.Obsolete("This parameter is deprecated and will be removed in PSAppDeployToolkit 4.2.0.")]
        [System.Management.Automation.SwitchParameter]$LoadLoggedOnUserEnvironmentVariables
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Determine the user SID to base things off of.
        if ($LoadLoggedOnUserEnvironmentVariables)
        {
            Write-ADTLogEntry -Message "The parameter [-LoadLoggedOnUserEnvironmentVariables] is deprecated and will be removed in PSAppDeployToolkit 4.2.0." -Severity 2
        }
        $userSid = if ($LoadLoggedOnUserEnvironmentVariables -and ($runAsActiveUser = Get-ADTClientServerUser -AllowSystemFallback))
        {
            $runAsActiveUser.SID
        }
        else
        {
            [PSADT.AccountManagement.AccountUtilities]::CallerSid
        }
    }

    process
    {
        Write-ADTLogEntry -Message 'Refreshing the environment variables for this PowerShell session.'
        try
        {
            try
            {
                # Update all session environment variables. Ordering is important here: user variables comes second so that we can override system variables.
                Get-ItemProperty -LiteralPath 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment', "Microsoft.PowerShell.Core\Registry::HKEY_USERS\$userSid\Environment" | & {
                    process
                    {
                        $_.PSObject.Properties | & {
                            process
                            {
                                if ($_.Name -notmatch '^PS((Parent)?Path|ChildName|Provider)$')
                                {
                                    Set-Item -LiteralPath "Microsoft.PowerShell.Core\Environment::$($_.Name)" -Value $_.Value
                                }
                            }
                        }
                    }
                }

                # Set PATH environment variable separately because it is a combination of the user and machine environment variables.
                Set-Item -LiteralPath Microsoft.PowerShell.Core\Environment::PATH -Value ([System.String]::Join(';', (([System.Environment]::GetEnvironmentVariable('PATH', 'Machine'), [System.Environment]::GetEnvironmentVariable('PATH', 'User')).Split(';').Trim() | & { process { if ($_) { return $_ } } } | Select-Object -Unique)))
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to refresh the environment variables for this PowerShell session."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
