#-----------------------------------------------------------------------------
#
# MARK: Get-ADTStringLanguage
#
#-----------------------------------------------------------------------------

function Private:Get-ADTStringLanguage
{
    if (![System.String]::IsNullOrWhiteSpace(($languageOverride = (Get-ADTConfig).UI.LanguageOverride)))
    {
        # The caller has specified a specific language.
        return $languageOverride
    }
    elseif ($Script:ADT.Environment.RunAsActiveUser)
    {
        # Get the actual user's $PSUICulture value. If we're running as SYSTEM, the user's locale could be different.
        return $((Start-ADTProcess -Username $Script:ADT.Environment.RunAsActiveUser.NTAccount -FilePath powershell.exe -ArgumentList '-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command $PSUICulture' -MsiExecWaitTime ([System.TimeSpan]::FromSeconds($Script:ADT.Config.MSI.MutexWaitTime)) -CreateNoWindow -PassThru -InformationAction SilentlyContinue).StdOut)
    }
    else
    {
        # Fall back to PowerShell's for this active session.
        return $PSUICulture
    }
}
