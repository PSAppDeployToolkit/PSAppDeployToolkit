function Get-ADTEdgeExtensions
{
    # Check if the ExtensionSettings registry key exists if not create it.
    $regKeyEdgeExtensions = 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge'
    if (!(Test-RegistryValue -Key $regKeyEdgeExtensions -Value ExtensionSettings))
    {
        Set-RegistryKey -Key $regKeyEdgeExtensions -Name ExtensionSettings -Value "" | Out-Null
        return [pscustomobject]@{}
    }
    else
    {
        $extensionSettings = Get-RegistryKey -Key $regKeyEdgeExtensions -Value ExtensionSettings
        Write-ADTLogEntry -Message "Configured extensions: [$($extensionSettings)]." -Severity 1
        return $extensionSettings | ConvertFrom-Json
    }
}
