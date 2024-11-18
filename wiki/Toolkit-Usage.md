The Deploy-Application.ps1 script is the only script you need to modify to deploy your application.

The Deploy-Application.ps1 is broken down into the following sections:

**Initialization** e.g. Variables such as App Vendor, App Name, App Version

**Pre-Installation** e.g. Close applications, uninstall or clean-up previous versions

**Installation** e.g. Install the primary application, or components of the application

**Post-Installation** e.g. Drop additional files, registry tweaks

**Uninstallation** e.g. Uninstall/rollback the changes performed in the install section.

## Launching the Toolkit

### Overview

There are two ways to launch the toolkit for deployment of applications.

1.  Launch "Deploy-Application.ps1" PowerShell script as administrator.

2.  Launch “Deploy-Application.exe” as administrator. This will launch the “Deploy-Application.ps1” PowerShell script without opening a PowerShell command window. Note, if the x86 PowerShell is required (for example, if CAPICOM or another x86 library is needed), launch **Deploy-Application.exe /32**

#### Examples:

> **Deploy-Application.ps1**
>
> *Deploy an application for installation*
>
> **Deploy-Application.ps1 -DeploymentType "Uninstall" -DeployMode "Silent"**
>
> *Deploy an application for uninstallation in silent mode*
>
> **Deploy-Application.exe /32 -DeploymentType "Uninstall" -DeployMode "Silent"**
>
> *Deploy an application for uninstallation using PowerShell x86, supressing the PowerShell console window and deploying in silent mode.*
>
> **Deploy-Application.exe -AllowRebootPassThru**
>
> *Deploy an application for installation, supressing the PowerShell console window and allowing reboot codes to be returned to the parent process.*
>
> **Deploy-Application.exe "Custom-Script.ps1"**
>
> *Deploy an application with a custom name instead of Deploy-Application.ps1.*
>
> **Deploy-Application.exe -Command "C:\\Testing\\Custom-Script.ps1" -DeploymentType "Uninstall"**
>
> *Deploy an application with a custom name and custom location for the script file.*

### Toolkit Parameters

The following parameters are accepted by Deploy-Application.ps1:

**-DeploymentType** "Install" | "Uninstall" (default is install)

Specify whether to install or uninstall the application.

**-DeployMode** "Interactive" | "Silent" | "NonInteractive" (default is interactive)

Specify whether the installation should be run in Interactive, Silent or NonInteractive mode.

Interactive = Shows dialogs

Silent = No dialogs (progress and balloon tip notifications are supressed)

NonInteractive = Very silent, i.e. no blocking apps. NonInteractive mode is automatically set if it is detected that the process is not user interactive.

**-AllowRebootPassThru** $true | $false (default is false)

Specify whether to allow the 3010 exit code (reboot required) to be passed back to the parent process (e.g. SCCM) if detected during an installation. If a 3010 code is passed to SCCM, the SCCM client will display a reboot prompt. If set to false, the 3010 return code will be replaced by a “0” (successful, no restart required).

**-TerminalServerMode** $true | $false (default is false)

Changes to user install mode and back to user execute mode for installing/uninstalling applications on Remote Desktop Session Host/Citrix servers

**-DisableLogging** (switch parameter, default is false)

Disables logging to file for the script.

## Customizing the Toolkit

Aside from customizing the “Deploy-Application.ps1” script to deploy your application, no configuration is necessary out of the box. The following components can be configured as required:

**AppDeployToolkitConfig.xml** - Configure the default UI messages, MSI parameters, log file location, whether Admin rights should be required, whether log files should be compressed, log style (CMTrace or Legacy), max log size, whether debug messages should be logged, whether log entries should be written to the console, whether toolkit should re-launch as elevated logged-on console user when in SYSTEM context, whether toolkit should fall back to SYSTEM context if failure to launch toolkit as user, and whether toolkit should attempt to launch as a non-console logged on user (e.g. user logged on via terminal services) when in SYSTEM context.

**AppDeployToolkitLogo.ico** - To brand the balloon notifications and UI window title bars with your own custom/corporate logo, replace the AppDeployToolkitLogo.ico file with your own .ico file (retaining the file name)

**AppDeployToolkitBanner.png** - To brand the toolkit UI prompts with your own custom/corporate banner, replace the AppDeployToolkitBanner.png file with your own .png file (retaining the file name). The file must be in PNG format and must be 450 x 50 in size.

**CompressLogs (option in AppDeployToolkitConfig.xml)** - One of the Toolkit Options in the AppDeployToolkitConfig.xml file is CompressLogs. Enabling this option will create a temporary logging folder where you can save all of the log files you want to include in the single ZIP file that will be created from this folder.

To enable the CompressLogs, set the following option in AppDeployToolkitConfig.xml to True:

*<Toolkit_CompressLogs>True</Toolkit_CompressLogs>*

When set to True, the following happens:

  - Both toolkit and MSI logs are temporally placed in $envTemp\\$installName which gets cleaned up at the end of the install.

  - At the end of the install / uninstall, the logs are compressed into a new zip file which is placed in the LogFolder location in the config file.

  - The Zip file name indicates whether it is an Install / Uninstall and has the timestamp in the filename so previous logs do not get overwritten.

  - If your package creates other log files, you can send them to the temporary logging FOLDER at $envTemp\\$installName.

**Log Location Auto Detection (option in AppDeployToolkitConfig.xml)** - One of the Toolkit Options in the AppDeployToolkitConfig.xml file is to automatically detect the log location based on the deployment method so the logs can be collected with Diagnostics data collection from ConfigMgr or Intune.

To disable the Log Location Auto Detection, set the following option in AppDeployToolkitConfig.xml to True:

*<Toolkit_LogPathAutoDetect>False</Toolkit_LogPathAutoDetect>*

When set to True, the following happens:

  - Deployed from ConfigMgr: Logs are placed within `C:\Windows\CCM\Logs\`

  - Deployed from Intune: Logs are placed within `C:\ProgramData\Microsoft\IntuneManagementExtension\Logs\`

When set to False, the following happens:

  - Logs are placed within `C:\Windows\Logs\Software\`

## Example Deployments

- [Building an Adobe Reader installation with the PowerShell App Deployment Toolkit](building-an-adobe-reader-installation-with-the-powershell-app-deployment-toolkit)
- [Deploy the Adobe Reader installation using SCCM 2007 / SCCM 2012 package](deploy-the-adobe-reader-installation-using-sccm-2007-sccm-2012-package)
- [Deploy the Adobe Reader installation using SCCM 2012 Application Model](deploy-the-adobe-reader-installation-using-sccm-2012-application-model)
- [An advanced Office 2013 SP1 installation with the PowerShell App Deployment Toolkit](an-advanced-office-2013-sp1-installation-with-the-powershell-app-deployment-toolkit)
