# Screenshots

## User Interface

### Installation Progress

The installation progress message displays an indeterminate progress ring to indicate an installation is in progress and display status messages to the end user. This is invoked using the "Show-InstallationProgress" function.

![Standard Progress Message](img/progressmessage_standard.png){: .center}

The progress message can be dynamically updated to indicate the stage of the installation or to display custom messages to the user, using the "Show-InstallationProgress" function.

![Progress Message with custom text](img/progressmessage_custom.png){: .center}

### Installation Welcome Prompt

The application welcome prompt can be used to display applications that need to be closed, an option to defer and a countdown to closing applications automatically. Use the "Show-InstallationWelcome" function to display the prompts shown below.

![Welcome Prompt](img/welcomeprompt_standard.png){: .center}

Welcome prompt with close programs option and defer option:

![Welcome Prompt with close programs and defer options](img/welcomeprompt_withcloseanddefer.png){: .center}

Welcome prompt with close programs options and countdown to automatic closing of applications:

![Welcome prompt with close programs and countdown options](img/welcomeprompt_withcloseandcountdown.png){: .center}

Welcome prompt with just a defer option:

![Welcome prompt with defer option](img/welcomeprompt_withdeferonly.png){: .center}

### Block Application Execution

If the block execution option is enabled (see Show-InstallationWelcome function), the user will be prompted that they cannot launch the specified application(s) while the installation is in progress. The application will be unblocked again once the installation has completed.

![Block Execution prompt](img/blockexecutionprompt.png){: .center}

### Disk Space Requirements

If the CheckDiskSpace parameter is used with the Show-InstallationWelcome function and the disk space requirements are not met, the following prompt will be displayed and the installation will not proceed.

![Disk Space Requirements prompt](img/diskspacerequirementsprompt.png){: .center}

### Custom Installation Prompt

A custom prompt with the toolkit branding can be used to display messages and interact with the user using the "Show-InstallationPrompt" function. The title and text is customizable and up to 3 customizable buttons can be included on the prompt as well as optional system icons, e.g.

![Custom installation prompt](img/custominstallationprompt.png){: .center}

Additionally, the prompt can be displayed asynchronously, e.g. to display a message at the end of the installation but allow the installation to return the exit code to the parent process without waiting for the user to respond to the message.

![Asynchronous installation prompt](img/asyncinstallationprompt.png){: .center}

### Installation Restart Prompt

A restart prompt can be displayed with or without a countdown to automatic restart using the "Show-InstallationRestartPrompt". Since the restart prompt is executed in a separate PowerShell session, the toolkit will still return the appropriate exit code to the parent process.

![Installation Restart prompt](img/restartprompt.png){: .center}

### Balloon tip notifications

Balloon tip notifications are displayed in the system tray automatically at the beginning and end of the installation. These can be turned off in the XML configuration.

![Balloon Tip example #1](img/balloontip1.png){: .center}

![Balloon Tip example #2](img/balloontip2.png){: .center}

![Balloon Tip example #3](img/balloontip3.png){: .center}
