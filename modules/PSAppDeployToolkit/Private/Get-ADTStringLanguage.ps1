#-----------------------------------------------------------------------------
#
# MARK: Get-ADTStringLanguage
#
#-----------------------------------------------------------------------------

function Private:Get-ADTStringLanguage
{
    [CmdletBinding()]
    [OutputType([System.Globalization.CultureInfo])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSAppDeployToolkit.Foundation.EnvironmentTable]$Environment,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Config
    )

    # Return the current language if the module is already initialised.
    if (Test-ADTModuleInitialized)
    {
        if ($PSBoundParameters.Count)
        {
            $naerParams = @{
                Exception = [System.InvalidProgramException]::new("The function [Get-ADTStringLanguage] can only be called with parameters when the module is uninitialized.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                ErrorId = 'GetStringLanguageInvalidOperation'
                RecommendedAction = "Please report this to the PSAppDeployToolkit team for further review."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
        try
        {
            return (Get-ADTModuleState).Language
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    # Calculate the language value to return, favouring a config override or user's culture over this process's culture.
    if (![System.String]::IsNullOrWhiteSpace($Config.UI.LanguageOverride))
    {
        # The caller has specified a specific language.
        return [System.Globalization.CultureInfo]$Config.UI.LanguageOverride
    }
    elseif (($runAsActiveUser = $(if (!$Environment) { [PSADT.Foundation.RunAsActiveUser]::GetAsync().ConfigureAwait($false).GetAwaiter().GetResult() } else { $Environment.RunAsActiveUser })))
    {
        # A user is logged on. If we're running as SYSTEM, the user's locale could be different so try to get theirs if we can.
        if ([PSADT.AccountManagement.AccountUtilities]::CallerSid.Equals($runAsActiveUser.SID) -and ($userLanguage = [Microsoft.Win32.Registry]::GetValue('HKEY_CURRENT_USER\Control Panel\International\User Profile', 'Languages', $null) | Select-Object -First 1))
        {
            # We got the current user's locale from the registry.
            return [System.Globalization.CultureInfo]$userLanguage
        }
        elseif (($userLanguage = Get-ADTRegistryKey -LiteralPath 'Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Control Panel\International\User Profile' -Name Languages -SID $runAsActiveUser.SID | Select-Object -First 1))
        {
            # We got the RunAsActiveUser's locale from the registry.
            return [System.Globalization.CultureInfo]$userLanguage
        }
        else
        {
            # We failed all the above, so get the actual user's $PSUICulture value.
            return [System.Globalization.CultureInfo]$((Start-ADTProcess -RunAsActiveUser $runAsActiveUser -DenyUserTermination -FilePath powershell.exe -ArgumentList '-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command $PSUICulture' -MsiExecWaitTime ([System.TimeSpan]::FromSeconds($Config.MSI.MutexWaitTime)) -CreateNoWindow -PassThru -InformationAction SilentlyContinue).StdOut)
        }
    }
    else
    {
        # Fall back to PowerShell's for this active session.
        return [System.Globalization.CultureInfo]$PSUICulture
    }
}
