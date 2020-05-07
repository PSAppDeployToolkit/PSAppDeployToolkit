# Specify the path to scan for PSADT deployment scripts
$adtScriptsPath = 'D:\YourDeploymentScriptsPath'

# Specify the new temp path for the toolkit. Use single quotes so we write the variable, not the variable contents.
$configTempPathNew = '$envTemp'

# Get all PSADT deployment config files
$configFiles = Get-ChildItem -Recurse $adtScriptsPath | Where-Object { $_.Name -eq "AppDeployToolkitConfig.xml" }

# Iterate through each config and change as required
Foreach ($configFile in $configFiles) {
    # Load the XML
    [Xml.XmlDocument]$xmlConfigFile = Get-Content -LiteralPath $configFile.FullName
    [Xml.XmlElement]$xmlConfig = $xmlConfigFile.AppDeployToolkit_Config
    [Xml.XmlElement]$configConfigToolkitOptions = $xmlConfig.Toolkit_Options

    # Get the Temp Path
    [string]$configTempPath = $configConfigToolkitOptions.Toolkit_TempPath

    # If a change is required, do it
    If ($configTempPath -match "Users\\Public") {
        $configConfigToolkitOptions.Toolkit_TempPath = $configTempPathNew
        $xmlConfigFile.Save($configFile.FullName)
        Write-Host "Updated - [$($configFile.FullName)]"
    }
}
