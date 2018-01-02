## BEC changes
### Fixes
* -Wait parameter for `ExecuteProcessAsUser` now work on non-english OS
* `Test-PowerPoint` now works for danish OS

### New functionality
* New defer mechanism. Defer now works by creating a scheduled task, that runs the Application Deployment Evaluation Cycle
* If the installation times out, the installation will be defered. The amount is specified in the config file
* User can choose to defer 1, 2 og 4 hours
* Added `-showContinue` flag to `Show-WelcomePrompt` since the button blocks the defer dropdown
 
___
### What is the PowerShell App Deployment Toolkit?

The PowerShell App Deployment Toolkit provides a set of functions to perform common application deployment tasks and to interact with the user during a deployment. It simplifies the complex scripting challenges of deploying applications in the enterprise, provides a consistent deployment experience and improves installation success rates.

The PowerShell App Deployment Toolkit can be used to replace your WiseScript, VBScript and Batch wrapper scripts with one versatile, re-usable and extensible tool.

### What are the main features of the PowerShell App Deployment Toolkit?

* **Easy To Use** - Any PowerShell beginner can use the template and the functions provided with the Toolkit to perform application deployments.
* **Consistent** - Provides a consistent look and feel for all application deployments, regardless of complexity.
* **Powerful** - Provides a set of functions to perform common deployment tasks, such as installing or uninstalling multiple applications, prompting users to close apps, setting registry keys, copying files, etc.
* **User Interface** - Provides user interaction through customizable user interface dialogs boxes, progress dialogs and balloon tip notifications.
* **Localized** - The UI is localized in several languages and more can easily be added using the XML configuration file.
* **Integration** - Integrates well with SCCM 2007/2012; provides installation and uninstallation deployment types with options on how to handle exit codes, such as supressing reboots or returning a fast retry code.
* **Updatable** - The logic engine and functions are separated from per-application scripts, so that you can update the toolkit when a new version is released and maintain backwards compatibility with your deployment scripts.
* **Extensible** - The Toolkit can be easily extended to add custom scripts and functions.
* **Helpful** - The Toolkit provides detailed logging of all actions performed and even includes a graphical console to browse the help documentation for the Toolkit functions.

## License

The PowerShell App Deployment Tool is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or any later version.
 
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public License for more details.
