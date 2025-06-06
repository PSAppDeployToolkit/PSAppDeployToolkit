#-----------------------------------------------------------------------------
#
# MARK: Get-ADTStringLanguage
#
#-----------------------------------------------------------------------------

function Private:Get-ADTStringLanguage
{
    if (![System.String]::IsNullOrWhiteSpace(($languageOverride = ($adtConfig = Get-ADTConfig).UI.LanguageOverride)))
    {
        # The caller has specified a specific language.
        return $languageOverride
    }
    elseif (($runAsActiveUser = ($adtEnv = Get-ADTEnvironmentTable).RunAsActiveUser))
    {
        # A user is logged on. If we're running as SYSTEM, the user's locale could be different so try to get theirs if we can.
        if ($runAsActiveUser.SID.Equals($adtEnv.CurrentProcessSID) -and ($userLanguage = [Microsoft.Win32.Registry]::GetValue('HKEY_CURRENT_USER\Control Panel\International\User Profile', 'Languages', $null) | Select-Object -First 1))
        {
            # We got the current user's locale from the registry.
            return $userLanguage
        }
        elseif (($userLanguage = Get-ADTRegistryKey -LiteralPath 'Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Control Panel\International\User Profile' -Name Languages -SID $runAsActiveUser.SID | Select-Object -First 1))
        {
            # We got the RunAsActiveUser's locale from the registry.
            return $userLanguage
        }
        else
        {
            # We failed all the above, so get the actual user's $PSUICulture value.
            return $((Start-ADTProcess -Username $runAsActiveUser.NTAccount -FilePath powershell.exe -ArgumentList '-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command $PSUICulture' -MsiExecWaitTime ([System.TimeSpan]::FromSeconds($adtConfig.MSI.MutexWaitTime)) -CreateNoWindow -PassThru -InformationAction SilentlyContinue).StdOut)
        }
    }
    else
    {
        # Fall back to PowerShell's for this active session.
        return $PSUICulture
    }
}
