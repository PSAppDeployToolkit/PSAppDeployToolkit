@{
    File = @{
        # Date this config version was released.
        Date = '05/03/2024'

        # Released config version number.
        Version = '3.91.0'
    }

    Assets = @{
        # Specify filename of the banner.
        Banner = 'AppDeployToolkitBanner.png'

        # Specify filename of the icon.
        Icon = 'AppDeployToolkitLogo.ico'

        # Specify filename of the logo.
        Logo = 'AppDeployToolkitLogo.png'
    }

    MSI = @{
        # Installation parameters used for non-silent MSI actions.
        InstallParams = 'REBOOT=ReallySuppress /QB-!'

        # Logging level used for MSI logging.
        LoggingOptions = '/L*V'

        # Log path used for MSI logging.
        LogPath = '$envWinDir\Logs\Software'

        # Log path used for MSI logging when RequireAdmin is False.
        LogPathNoAdminRights = '$envProgramData\Logs\Software'

        # The length of time in seconds to wait for the MSI installer service to become available. Default is 600 seconds (10 minutes).
        MutexWaitTime = 600

        # Installation parameters used for silent MSI actions.
        SilentParams = 'REBOOT=ReallySuppress /QN'

        # Installation parameters used for MSI uninstall actions.
        UninstallParams = 'REBOOT=ReallySuppress /QN'
    }

    Toolkit = @{
        # Specify the path for the cache folder.
        CachePath = '$envProgramData\SoftwareCache'

        # Specify if the log files should be bundled together in a compressed zip file.
        CompressLogs = $false

        # Specify if an existing log file should be appended to.
        LogAppend = $true

        # Specify if debug messages such as bound parameters passed to a function should be logged.
        LogDebugMessage = $false

        # Specify maximum number of previous log files to retain.
        LogMaxHistory = 10

        # Specify maximum file size limit for log file in megabytes (MB).
        LogMaxSize = 10

        # Log path used for Toolkit logging.
        LogPath = '$envWinDir\Logs\Software'

        # Same as LogPath but used when RequireAdmin is False.
        LogPathNoAdminRights = '$envProgramData\Logs\Software'

        # Specify if log file should be a CMTrace compatible log file or a Legacy text log file.
        LogStyle = 'CMTrace'

        # Specify if log messages should be written to the console.
        LogWriteToHost = $true

        # Automatically changes DeployMode to Silent during the OOBE.
        OobeDetection = $true

        # Registry key used to store toolkit information (with PSAppDeployToolkit as child registry key), e.g. deferral history.
        RegPath = 'HKLM:\SOFTWARE'

        # Same as RegPath but used when RequireAdmin is False. Bear in mind that since this Registry Key should be writable without admin permission, regular users can modify it also.
        RegPathNoAdminRights = 'HKCU:\SOFTWARE'

        # Specify if Administrator Rights are required. NB: Some functions won't work if this is set to false, such as deferral, blockexecution, file & registry RW access and potentially logging.
        RequireAdmin = $true

        # Automatically changes DeployMode for session zero (SYSTEM) operations.
        SessionDetection = $true

        # Path used to store temporary Toolkit files (with PSAppDeployToolkit as subdirectory), e.g. cache toolkit for cleaning up blocked apps. Normally you don't want this set to a path that is writable by regular users, this might lead to a security vulnerability. The default Temp variable for the LocalSystem account is C:\Windows\Temp.
        TempPath = '$envTemp'

        # Same as TempPath but used when RequireAdmin is False.
        TempPathNoAdminRights = '$envTemp'

        # Choose from either 'Native' for native PowerShell file copy via Copy-Item, or 'Robocopy' to use robocopy.exe.
        FileCopyMode = 'Native'
    }

    UI = @{
        # Used to turn automatic balloon notifications on or off.
        BalloonNotifications = $true

        # The name to show by default for all balloon notifications.
        BalloonTitle = 'PSAppDeployToolkit'

        # Choose from either 'Fluent' for contemporary dialogs, or 'Classic' for PSAppDeployToolkit 3.x WinForms dialogs.
        DialogStyle = 'Fluent'

        # Override dialog style when running in compatibility mode. Choose from either 'Fluent' for contemporary dialogs, or 'Classic' for PSAppDeployToolkit 3.x WinForms dialogs.
        DialogStyleCompatMode  = 'Classic'

        # Exit code used when a UI prompt times out or the user opts to defer.
        DefaultExitCode = 1618

        # Time in seconds after which the prompt should be repositioned centre screen when the -PersistPrompt parameter is used. Default is 60 seconds.
        DefaultPromptPersistInterval = 60

        # Time in seconds to automatically timeout installation dialogs. Default is 1 hour and 55 minutes so that dialogs timeout before SCCM times out.
        DefaultTimeout = 6900

        # Exit code used when a user opts to defer.
        DeferExitCode = 60012

        # Specify whether to re-enumerate running processes dynamically while displaying Show-ADTInstallationWelcome.
        # If the CloseApps processes were not running when the prompt was displayed, and are subsequently detected to be running, the prompt will be updated with the apps to close.
        # If the CloseApps processes were running when the prompt was displayed and are subsequently detected not to be running then the installation will automatically continue if deferral is not available.
        # If the running applications change (new CloseApps processes launched or running processes closed), the list box will dynamically update to reflect the currently running applications.
        DynamicProcessEvaluation = $true

        # Time in seconds after which to re-enumerate running processes while displaying the Show-ADTInstallationWelcome prompt. Default is 2 seconds.
        DynamicProcessEvaluationInterval = 2

        <# Specify a static UI language using the one of the Language Codes listed below to override the language culture detected on the system.
            Language Code    Language       |       Language Code    Language
            =============    ========       |       =============    ========
            AR               Arabic         |       KO               Korean
            CZ               Czech          |       NL               Dutch
            DA               Danish         |       NB               Norwegian (Bokmål)
            DE               German         |       PL               Polish
            EN               English        |       PT               Portuguese (Portugal)
            EL               Greek          |       PT-BR            Portuguese (Brazil)
            ES               Spanish        |       RU               Russian
            FI               Finnish        |       SK               Slovak
            FR               French         |       SV               Swedish
            HE               Hebrew         |       TR               Turkish
            HU               Hungarian      |       ZH-Hans          Chinese (Simplified)
            IT               Italian        |       ZH-Hant          Chinese (Traditional)
            JA               Japanese       |
        #>
        LanguageOverride = $null

        # Time in seconds after which to re-prompt the user to close applications in case they ignore the prompt or they cancel the application's save prompt.
        PromptToSaveTimeout = 120

        # Time in seconds after which the restart prompt should be re-displayed/repositioned when the -NoCountdown parameter is specified. Default is 600 seconds.
        RestartPromptPersistInterval = 600
    }
}
