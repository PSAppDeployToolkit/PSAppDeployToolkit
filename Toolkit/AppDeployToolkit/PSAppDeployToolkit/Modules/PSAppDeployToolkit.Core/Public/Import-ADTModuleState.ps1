function Import-ADTModuleState
{
    # Get the root module's info.
    $adtModule = Get-ADTModuleInfo

    # Restore the previously exported session and prepare it for asynchronous operation. The serialised state may be on-disk during BlockExecution operations.
    if ([System.IO.File]::Exists(($onDiskClixml = "$($adtModule.ModuleBase)\$($adtModule.Name).xml")))
    {
        $adtData = (Set-Variable -Name ADT -Scope Script -Option ReadOnly -Force -PassThru -Value (Import-Clixml -LiteralPath $onDiskClixml)).Value
    }
    else
    {
        $adtData = (Set-Variable -Name ADT -Scope Script -Option ReadOnly -Force -PassThru -Value ([System.Management.Automation.PSSerializer]::Deserialize([System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String(($regPath = $Script:Serialisation.Hive.OpenSubKey($Script:Serialisation.Key, $true)).GetValue($Script:Serialisation.Name)))))).Value
        $regPath.DeleteValue($Script:Serialisation.Name, $true)
    }

    # Create new object based on serialised state and configure for async operations.
    for ($i = 0; $i -lt $adtData.Sessions.Count; $i++)
    {
        $adtData.Sessions[$i] = [ADTSession]$adtData.Sessions[$i]
        $adtData.Sessions[$i].InstallPhase = 'Asynchronous'
        $adtData.Sessions[$i].CompatibilityMode = $false
    }
}
