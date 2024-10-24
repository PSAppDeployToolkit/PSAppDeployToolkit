@{
    BalloonText = @{
        # Text displayed in the balloon tip for successful completion of a deployment type.
        Complete = 'complete.'

        # Text displayed in the balloon tip for a failed deployment type.
        Error = 'failed.'

        # Text displayed in the balloon tip for fast retry of a deployment.
        FastRetry = 'not complete.'

        # Text displayed in the balloon tip for successful completion of a deployment type.
        RestartRequired = 'complete. A reboot is required.'

        # Text displayed in the balloon tip for the start of a deployment type.
        Start = 'started.'
    }

    BlockExecution = @{
        # Text displayed when prompting user that an application has been blocked.
        Message = 'Launching this application has been temporarily blocked so that an installation operation can complete.'
    }

    ClosePrompt = @{
        # Text displayed on the close button when prompting to close running programs.
        ButtonClose = 'Close &Programs'

        # Text displayed on the continue button when prompting to close running programs.
        ButtonContinue = '&Continue'

        # Tooltip text displayed on the continue button when prompting to close running programs.
        ButtonContinueTooltip = "Only select `"Continue`" after closing the above listed application(s)."

        # Text displayed on the defer button when prompting to close running programs.
        ButtonDefer = '&Defer'

        # Text displayed when counting down to automatically closing applications.
        CountdownMessage = 'NOTE: The program(s) will be automatically closed in:'

        # Text displayed when prompting to close running programs.
        Message = "The following programs must be closed before the installation can proceed.`n`nPlease save your work, close the programs, and then continue. Alternatively, save your work and click `"Close Programs`"."
    }

    DeferPrompt = @{
        # Text displayed when there is a specific deferral deadline.
        Deadline = 'Deadline:'

        # Text displayed when a deferral option is available.
        ExpiryMessage = 'You can choose to defer the installation until the deferral expires:'

        # Text displayed when there are a specific number of deferrals remaining.
        RemainingDeferrals = 'Remaining Deferrals:'

        # Text displayed after the deferral options.
        WarningMessage = 'Once the deferral has expired, you will no longer have the option to defer.'

        # Text displayed when only the deferral dialog is to be displayed and there are no applications to close.
        WelcomeMessage = 'The following application is about to be installed:'
    }

    DeploymentType = @{
        # Name displayed in UI for installation deployment type.
        Install = 'Installation'

        # Name displayed in UI for repair deployment type.
        Repair = 'Repairing'

        # Name displayed in UI for Uninstallation deployment type.
        Uninstall = 'Uninstallation'
    }

    DiskSpace = @{
        # Text displayed when the system does not have sufficient free disk space available to complete the installation.
        Message = "You do not have enough disk space to complete the installation of:`n{0}`n`nSpace required: {1}MB`nSpace available: {2}MB`n`nPlease free up enough disk space in order to proceed with the installation."
    }

    Progress = @{
        # Default text displayed in the progress bar for installations.
        MessageInstall = 'Installation in progress. Please wait...'

        # Default text displayed in the progress bar for installations.
        MessageInstallDetail = 'This window will close automatically when the installation is complete.'

        # Default text displayed in the progress bar for repairs.
        MessageRepair = 'Repair in progress. Please wait...'

        # Default text displayed in the progress bar for repairs.
        MessageRepairDetail = 'This window will close automatically when the repair is complete.'

        # Default text displayed in the progress bar for Uninstallations.
        MessageUninstall = 'Uninstallation in progress. Please wait...'

        # Default text displayed in the progress bar for Uninstallations.
        MessageUninstallDetail = 'This window will close automatically when the uninstallation is complete.'
    }

    RestartPrompt = @{
        # Button text for allowing the user to restart later.
        ButtonRestartLater = 'Minimize'

        # Button text for when wanting to restart the device now.
        ButtonRestartNow = 'Restart Now'

        # Text displayed when the device requires a restart.
        Message = 'In order for the installation to complete, you must restart your computer.'

        # Text displayed when indicating when the device will be restarted.
        MessageRestart = 'Your computer will be automatically restarted at the end of the countdown.'

        # Text displayed as a prefix to the time remaining, indicating that users should save their work, etc.
        MessageTime = 'Please save your work and restart within the allotted time.'

        # Text displayed to indicate the amount of time remaining until a restart will occur.
        TimeRemaining = 'Time remaining:'

        # Text displayed in the title of the restart prompt which helps the script identify whether there is already a restart prompt being displayed and not to duplicate it.
        Title = 'Restart Required'
    }

    WelcomePrompt = @{
        Classic = @{
            # The countdown message displayed at the Welcome Screen to indicate when the install will continue if no response from user.
            CountdownMessage = 'The {0} will automatically continue in:'

            # This is a custom message to display at the Welcome Screen window.
            CustomMessage = ''
        }
        Fluent = @{
            # The subtitle underneath the Application Title, e.g. Company Name. Using {0} will insert the Application Type, e.g. App "Install"
            Subtitle = 'PSAppDeployToolkit - App {0}'

            # This is a message to prompt users to save their work.
            DialogMessage = 'Please save your work before continuing as the following applications will be closed automatically.'

            # This is a message to when there are no running processes available.
            DialogMessageNoProcesses = 'Please select Install to continue with the installation. If you have any deferrals remaining, you may also choose to delay the installation.'

            # This is a word used to describe the number of deferrals left. custom message to display at the Welcome Screen window.
            ButtonDeferRemaining = 'remain'

            # This is a phrase used to describe the process of deferring an application installation.
            ButtonLeftText = 'Defer'

            # This is a phrase used to describe the process of closing applications and installing the application.
            ButtonRightText = 'Close Apps & Install'

            # This is a phrase used to describe the process of installing the application.
            ButtonRightTextNoProcesses = 'Install'
        }
    }
}
