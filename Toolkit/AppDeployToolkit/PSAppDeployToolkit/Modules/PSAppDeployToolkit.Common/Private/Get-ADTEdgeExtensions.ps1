function Get-ADTEdgeExtensions
{
    # Get the current environment.
    $adtEnv = Get-ADTEnvironment

    # Check if the ExtensionSettings registry key exists if not create it.
    if (!(Test-ADTRegistryValue -Key $adtEnv.regKeyEdgeExtensions -Value ExtensionSettings))
    {
        Set-ADTRegistryKey -Key $adtEnv.regKeyEdgeExtensions -Name ExtensionSettings -Value "" | Out-Null
        return [pscustomobject]@{}
    }
    else
    {
        $extensionSettings = Get-RegistryKey -Key $adtEnv.regKeyEdgeExtensions -Value ExtensionSettings
        Write-ADTLogEntry -Message "Configured extensions: [$($extensionSettings)]." -Severity 1
        return $extensionSettings | ConvertFrom-Json
    }
}
