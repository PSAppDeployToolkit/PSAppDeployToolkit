function Import-ADTLocalizedStrings
{
    # Get the current config and root module.
    $adtData = Get-ADT
    $adtConfig = Get-ADTConfig
    $adtModule = Get-ADTModuleInfo

    # Get the best language identifier.
    $adtData.Language = if (![System.String]::IsNullOrWhiteSpace($adtConfig.UI.LanguageOverride))
    {
        # The caller has specified a specific language.
        $adtConfig.UI.LanguageOverride
    }
    else
    {
        # Fall back to PowerShell's.
        $PSUICulture
    }

    # Store the chosen language within this session.
    $adtData.Strings = Import-LocalizedData -BaseDirectory "$($adtModule.ModuleBase)\Strings" -FileName strings.psd1 -UICulture $adtData.Language
}
