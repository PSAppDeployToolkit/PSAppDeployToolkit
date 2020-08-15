## User Interface

  - An interface to prompt the user to close specified applications that are open prior to starting the application deployment. The user is prompted to save their documents and has the option to close the programs themselves, have the toolkit close the programs, or optionally defer. Optionally, a countdown can be displayed until the applications are automatically closed.

  - The ability to allow the user to defer an installation X number of times, X number of days or until a deadline date is reached.

  - The ability to prevent the user from launching the applications that need to be closed while the application installation is in progress.

  - An indeterminate progress dialog with customizable message text that can be updated throughout the deployment.

  - A restart prompt with an option to restart later or restart now and a countdown to automatic restart.

  - The ability to notify the user if disk space requirements are not met.

  - Custom dialog boxes with options to customize title, text, buttons & icon.

  - Balloon tip notifications to indicate the beginning and end of an installation and the success or failure of an installation.

  - Branding of the above UI components using a custom logo icon and banner for your own Organization.

  - The ability to run in interactive, silent (no dialogs) or non-interactive mode (default for running SCCM task sequence or session 0).

  - The UI is localized into several languages and more can be easily added using the XML configuration file.

## Functions/Logic

  - Provides extensive logging of both the Toolkit functions and any MSI installation / uninstallation.

  - Provides the ability to execute any type of setup (MSI or EXEs) and handle the return codes.

  - Mass remove MSI applications with a partial match (e.g. remove all versions of all MSI applications which match "Office")

  - Perform SCCM actions such as Machine and User Policy Refresh, Inventory Update and Software Update

  - Supports installation of applications on Citrix/Remote Desktop Session Host Servers

  - Update Group Policy

  - Copy / Delete Files

  - Get / Set / Remove Registry Keys and Values

  - Get / Set INI File Keys and Values

  - Check File versions

  - Pin or Unpin applications to the Start Menu or Task Bar

  - Create Start Menu Shortcuts

  - Register / Unregister DLL files

  - Refresh desktop icons / environment variables

  - Test network connectivity

  - Test power connectivity

  - Check whether a PowerPoint slideshow is running in full screen presentation mode

## Integration with SCCM 

  - Handles SCCM exit codes, including time sensitive dialogs supporting SCCM's Fast Retry feature - providing more accurate SCCM Reporting (no more Failed due to timeout errors).

  - Ability to prevent reboot codes (3010) from being passed back to SCCM, which would cause a reboot prompt.

  - Supports the CM12 application model by providing an install and uninstall deployment type for every deployment script.

  - Bundle multiple application installations to overcome the supported limit of 5 applications in the CM12 application dependency chain.

  - Compared to compiled deployment packages, e.g. WiseScript, the Toolkit utilises the SCCM cache correctly and SCCM Distribution Point bandwidth more efficiently by using loose files.

## Help Console

  - A graphical console for browsing the help documentation for the toolkit functions.