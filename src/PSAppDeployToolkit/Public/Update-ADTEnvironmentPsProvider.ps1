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
        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$LoadLoggedOnUserEnvironmentVariables
    )

    begin
    {
        # Perform initial setup.
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Determine the user SID to base things off of.
        $userSid = if ($LoadLoggedOnUserEnvironmentVariables -and ($runAsActiveUser = & $Script:CommandTable.'Get-ADTRunAsActiveUser'))
        {
            $runAsActiveUser.SID
        }
        else
        {
            [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
        }
    }

    process
    {
        & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Refreshing the environment variables for this PowerShell session.'
        try
        {
            try
            {
                # Update all session environment variables. Ordering is important here: user variables comes second so that we can override system variables.
                $null = & $Script:CommandTable.'Get-ItemProperty' -LiteralPath 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment', "Microsoft.PowerShell.Core\Registry::HKEY_USERS\$userSid\Environment" | & {
                    process
                    {
                        $_.PSObject.Properties | & {
                            process
                            {
                                if ($_.Name -notmatch '^PS((Parent)?Path|ChildName|Provider)$')
                                {
                                    & $Script:CommandTable.'Set-Item' -LiteralPath "Env:$($_.Name)" -Value $_.Value
                                }
                            }
                        }
                    }
                }

                # Set PATH environment variable separately because it is a combination of the user and machine environment variables.
                & $Script:CommandTable.'Set-Item' -LiteralPath Env:PATH -Value ([System.String]::Join(';', (('Machine', 'User' | & { process { [System.Environment]::GetEnvironmentVariable('PATH', $_) } }).Split(';') | & { process { if ($_) { return $_ } } } | & $Script:CommandTable.'Select-Object' -Unique)))
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to refresh the environment variables for this PowerShell session."
        }
    }

    end
    {
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}
