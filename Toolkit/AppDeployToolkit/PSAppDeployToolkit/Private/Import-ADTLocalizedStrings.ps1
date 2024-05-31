function Import-ADTLocalizedStrings
{
    # Get the best language identifier.
    $Script:ADT.Language = if (![System.String]::IsNullOrWhiteSpace($Script:ADT.Config.UI.LanguageOverride))
    {
        # The caller has specified a specific language.
        $Script:ADT.Config.UI.LanguageOverride
    }
    else
    {
        # Fall back to PowerShell's.
        $PSUICulture
    }

    # Store the chosen language within this session.
    $Script:ADT.Strings = Import-LocalizedData -BaseDirectory "$Script:PSScriptRoot\Strings" -FileName strings.psd1 -UICulture $Script:ADT.Language
}
