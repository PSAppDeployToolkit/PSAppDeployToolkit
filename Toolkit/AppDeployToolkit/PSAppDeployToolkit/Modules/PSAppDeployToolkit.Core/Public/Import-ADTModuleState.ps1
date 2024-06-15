function Import-ADTModuleState
{
    # Restore the previously exported session and prepare it for asynchronous operation. The serialised state may be on-disk during BlockExecution operations.
    Set-Variable -Name ADT -Scope Script -Option ReadOnly -Force -Value $(if ([System.IO.File]::Exists(($onDiskClixml = "$((Get-ADTModuleInfo).Name).xml")))
    {
        Import-Clixml -LiteralPath $onDiskClixml
    }
    else
    {
        [System.Management.Automation.PSSerializer]::Deserialize([System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String(($regPath = $Script:Serialisation.Hive.OpenSubKey($Script:Serialisation.Key, $true)).GetValue($Script:Serialisation.Name))))
        $regPath.DeleteValue($Script:Serialisation.Name, $true)
    })

    # Create new object based on serialised state and configure for async operations.
    for ($i = 0; $i -lt ($adtData = Get-ADT).Sessions.Count; $i++)
    {
        $adtData.Sessions[$i] = [ADTSession]::new($adtData.Sessions[$i])
        $adtData.Sessions[$i].InstallPhase = 'Asynchronous'
        $adtData.Sessions[$i].Internal.CompatibilityMode = $false
    }
}
