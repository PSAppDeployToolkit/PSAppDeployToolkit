function Export-ADTModuleState
{
    # Sync all property values and export to registry.
    (Get-ADTSession).SyncPropertyValues()
    $Script:Serialisation.Hive.CreateSubKey($Script:Serialisation.Key).SetValue($Script:Serialisation.Name, [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([System.Management.Automation.PSSerializer]::Serialize((Get-ADT), [System.Int32]::MaxValue))), $Script:Serialisation.Type)
}
