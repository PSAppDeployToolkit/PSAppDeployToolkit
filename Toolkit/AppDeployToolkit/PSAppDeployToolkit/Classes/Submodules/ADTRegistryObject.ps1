#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

class ADTRegistryObject
{
    [System.String]$Key
    [System.String]$Name
    [System.Object]$Value
    [Microsoft.Win32.RegistryValueKind]$Type

    [System.Collections.Hashtable] ToHashtable()
    {
        $hash = @{}
        $this.PSObject.Properties.ForEach({$hash.Add($_.Name, $_.Value)})
        return $hash
    }
}
