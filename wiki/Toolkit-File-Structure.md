### Files

The toolkit is comprised of the following files:

**Deploy-Application.ps1**

Performs the actual install / uninstall and is the only file that needs to be modified, depending on your level of customisation.

**Deploy-Application.exe**

An optional executable that can be used to launch the Deploy-Application.ps1 script without opening a PowerShell console window. Supports passing command-line parameters to the script.

**AppDeployToolkitMain.ps1**

Contains all of the functions and logic used by the installation script. By Separating the logic from the installation script, we can obfuscate away the complex code and make enhancements independently of the installation scripts that contain per-application actions.

**AppDeployToolkitConfig.xml**

Contains configurable options referenced by the AppDeployToolkitMain.ps1 script, such as MSI switches and User Interface messages, which are customizable and localized in several languages. This is intended to be a static file that is configured once, not on a per-application basis.

**AppDeployToolkitExtensions.ps1**

This is an optional PowerShell script that can be used to extend the toolkit functionality with custom functions. It is automatically dot-sourced by the AppDeployToolkitMain.ps1 script.

**AppDeployToolkitHelp.ps1**

This is a script that displays a help console to browse the functions included in the Toolkit and copy and paste examples in to your deployment script.

![](images/image2.png)

### Directories

The Root folder contains the Deploy-Application.exe and Deploy-Application.ps1 files. The Deploy-Application.ps1 file is the only file that should be modified on a per-application basis.

The directories below contain the installation files and supporting files referenced by the toolkit.

**AppDeployToolkit**

Folder containing the toolkit dependency files.

**Files**

Folder containing your main setup files, e.g. MSI

**SupportFiles**

Folder containing any supporting files such as files you need to copy to the target machine using the toolkit during deployment.
