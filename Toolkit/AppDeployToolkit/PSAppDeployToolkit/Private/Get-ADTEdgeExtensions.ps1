function Get-ADTEdgeExtensions
{
    # Check if the ExtensionSettings registry key exists if not create it.
    if (!(Test-RegistryValue -Key $Script:ADT.Environment.regKeyEdgeExtensions -Value ExtensionSettings))
    {
        Set-RegistryKey -Key $Script:ADT.Environment.regKeyEdgeExtensions -Name ExtensionSettings -Value "" | Out-Null
        return [pscustomobject]@{}
    }
    else
    {
        $extensionSettings = Get-RegistryKey -Key $Script:ADT.Environment.regKeyEdgeExtensions -Value ExtensionSettings
        Write-ADTLogEntry -Message "Configured extensions: [$($extensionSettings)]." -Severity 1
        return $extensionSettings | ConvertFrom-Json
    }
}
