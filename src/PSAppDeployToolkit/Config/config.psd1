﻿@{
    Assets = @{
        # Specify filename of the logo.
        Logo = '..\Assets\AppIcon.png'

        # Specify filename of the logo (for dark mode).
        LogoDark = '..\Assets\AppIcon.png'

        # Specify filename of the banner (Classic-only).
        Banner = '..\Assets\Banner.Classic.png'
    }

    MSI = @{
        # Installation parameters used for non-silent MSI actions.
        InstallParams = 'REBOOT=ReallySuppress /QN'

        # Logging level used for MSI logging.
        LoggingOptions = '/L*V'

        # Log path used for MSI logging. Uses the same path as Toolkit when null or empty.
        LogPath = ''

        # Log path used for MSI logging when RequireAdmin is False. Uses the same path as Toolkit when null or empty.
        LogPathNoAdminRights = ''

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

        # The name to show by default for dialog subtitles, balloon notifications, etc.
        CompanyName = 'PSAppDeployToolkit'

        # Specify if the log files should be bundled together in a compressed zip file.
        CompressLogs = $false

        # Choose from either 'Native' for native PowerShell file copy via Copy-ADTFile, or 'Robocopy' to use robocopy.exe.
        FileCopyMode = 'Native'

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

        # Specifies that logging should be to a hierarchical structure of AppVendor\AppName\AppVersion. Takes precident over "LogToSubfolder" if both are set.
        LogToHierarchy = $false

        # Specifies that a subfolder based on InstallName should be used for all log capturing.
        LogToSubfolder = $false

        # Specify if log file should be a CMTrace compatible log file or a Legacy text log file.
        LogStyle = 'CMTrace'

        # Specify if log messages should be written to the console.
        LogWriteToHost = $true

        # Specify if console log messages should bypass PowerShell's subsystems and be sent direct to stdout/stderr.
        # This only applies if "LogWriteToHost" is true, and the script is being ran in a ConsoleHost (not the ISE, or another host).
        LogHostOutputToStdStreams = $false

        # Automatically changes DeployMode to Silent during the OOBE.
        OobeDetection = $true

        # Automatically changes DeployMode when one or more processes are specified to Open-ADTSession for closure.
        ProcessDetection = $false

        # Registry key used to store toolkit information (with PSAppDeployToolkit as child registry key), e.g. deferral history.
        RegPath = 'HKLM:\SOFTWARE'

        # Same as RegPath but used when RequireAdmin is False. Bear in mind that since this Registry Key should be writable without admin permission, regular users can modify it also.
        RegPathNoAdminRights = 'HKCU:\SOFTWARE'

        # Specify if Administrator Rights are required. Note: Some functions won't work if this is set to false, such as deferral, block execution, file & registry RW access and potentially logging.
        RequireAdmin = $true

        # Automatically changes DeployMode for session zero (SYSTEM) operations.
        SessionDetection = $true

        # Path used to store temporary Toolkit files (with PSAppDeployToolkit as subdirectory), e.g. cache toolkit for cleaning up blocked apps. Normally you don't want this set to a path that is writable by regular users, this might lead to a security vulnerability. The default Temp variable for the LocalSystem account is C:\Windows\Temp.
        TempPath = '$envTemp'

        # Same as TempPath but used when RequireAdmin is False.
        TempPathNoAdminRights = '$envTemp'
    }

    UI = @{
        # Used to turn automatic balloon notifications on or off.
        BalloonNotifications = $true

        # Choose from either 'Fluent' for contemporary dialogs, or 'Classic' for PSAppDeployToolkit 3.x WinForms dialogs.
        DialogStyle = 'Fluent'

        # Specify the Accent Color in hex (with the first two characters for transparency, 00 = 0%, FF = 100%), e.g. 0xFF0078D7.
        # The value specified here should be literally typed (i.e. `FluentAccentColor = 0xFF0078D7`) and not wrapped in quotes.
        FluentAccentColor = $null

        # Exit code used when a UI prompt times out.
        DefaultExitCode = 1618

        # Time in seconds after which the prompt should be repositioned centre screen when the -PersistPrompt parameter is used. Default is 60 seconds.
        DefaultPromptPersistInterval = 60

        # Time in seconds to automatically timeout installation dialogs. Default is 55 minutes so that dialogs timeout before Intune times out.
        DefaultTimeout = 3300

        # Exit code used when a user opts to defer.
        DeferExitCode = 1602

        <# Specify a static UI language using the one of the Language Codes listed below to override the language culture detected on the system.
            Language Code    Language
            =============    ========
            AR               Arabic
            CZ               Czech
            DA               Danish
            DE               German
            EN               English
            EL               Greek
            ES               Spanish
            FI               Finnish
            FR               French
            HE               Hebrew
            HU               Hungarian
            IT               Italian
            JA               Japanese
            KO               Korean
            NL               Dutch
            NB               Norwegian (Bokmål)
            PL               Polish
            PT               Portuguese (Portugal)
            PT-BR            Portuguese (Brazil)
            RU               Russian
            SK               Slovak
            SV               Swedish
            TR               Turkish
            ZH-Hans          Chinese (Simplified)
            ZH-Hant          Chinese (Traditional)
        #>
        LanguageOverride = $null

        # Time in seconds after which to re-prompt the user to close applications in case they ignore the prompt or they cancel the application's save prompt.
        PromptToSaveTimeout = 120

        # Time in seconds after which the restart prompt should be re-displayed/repositioned when the -NoCountdown parameter is specified. Default is 600 seconds.
        RestartPromptPersistInterval = 600
    }
}
