#-----------------------------------------------------------------------------
#
# MARK: Get-ADTStringLanguage
#
#-----------------------------------------------------------------------------

function Private:Get-ADTStringLanguage
{
    if (![System.String]::IsNullOrWhiteSpace(($adtConfig = Get-ADTConfig).UI.LanguageOverride))
    {
        # The caller has specified a specific language.
        return $adtConfig.UI.LanguageOverride
    }
    else
    {
        # Fall back to PowerShell's.
        return [System.Threading.Thread]::CurrentThread.CurrentUICulture
    }
}
