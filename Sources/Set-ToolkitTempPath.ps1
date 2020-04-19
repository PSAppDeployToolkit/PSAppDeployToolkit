# Specify the new temp path for the toolkit
$configTempPathNew = '$envTemp'

$configFiles = Get-ChildItem -Recurse | Where {$_.Name -eq "AppDeployToolkitConfig.xml"}

Foreach ($configFile in $configFiles) {

    [Xml.XmlDocument]$xmlConfigFile = Get-Content -LiteralPath $configFile 
    [Xml.XmlElement]$xmlConfig = $xmlConfigFile.AppDeployToolkit_Config
    [Xml.XmlElement]$configConfigToolkitOptions = $xmlConfig.Toolkit_Options
    [string]$configTempPath = $configConfigToolkitOptions.Toolkit_TempPath

    If ($configTempPath -match "Users\\Public") {
        $configConfigToolkitOptions.Toolkit_TempPath = $configTempPathNew
    }

$xmlConfigFile.Save($configFile.FullName)

}
