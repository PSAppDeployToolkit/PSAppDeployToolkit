The toolkit generates extensive logging for all toolkit and MSI operations.

The toolkit will automatically place logs in the default log location for ConfigMgr or Intune. A custom default log path can be specified by setting LogPathAutoDetect to *False* and specifying the LogPath in the XML configuration file.

**Log Locations**
  - Deployed from ConfigMgr: `C:\Windows\CCM\Logs\`

  - Deployed from Intune: `C:\ProgramData\Microsoft\IntuneManagementExtension\Logs\`

  - Default: `C:\Windows\Logs\Software\`

The toolkit log file is named after the application with `_PSAppDeployToolkit` appended to the end, e.g.

Oracle_JavaRuntime_1.7.0.17_EN_01**_PSAppDeployToolkit.log**

All MSI actions are logged and the log file is named according to the MSI file used on the command line, with the action appended to the log file name. For uninstallations, the MSI product code is resolved to the MSI application name and version to keep the same log file format, e.g.

Oracle_JavaRuntimeEnvironmentx86_1.7.0.17_EN_01**_Install.log**

Oracle_JavaRuntimeEnvironmentx86_1.7.0.17_EN_01**_Repair.log**

Oracle_JavaRuntimeEnvironmentx86_1.7.0.17_EN_01**_Patch.log**

Oracle_JavaRuntimeEnvironmentx86_1.7.0.17_EN_01**_Uninstall.log**
