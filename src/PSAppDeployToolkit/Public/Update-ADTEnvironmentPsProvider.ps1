#-----------------------------------------------------------------------------
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
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Determine the user SID to base things off of.
        $userSid = if (($runAsActiveUser = Get-ADTClientServerUser -AllowSystemFallback))
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
                Set-Item -LiteralPath Microsoft.PowerShell.Core\Environment::PATH -Value ([System.String]::Join(';', (([System.Environment]::GetEnvironmentVariable('PATH', 'Machine'), [System.Environment]::GetEnvironmentVariable('PATH', 'User')).Split(';', [System.StringSplitOptions]::RemoveEmptyEntries).Trim() | Select-Object -Unique)))
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
