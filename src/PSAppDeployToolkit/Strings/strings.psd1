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
        Message = 'Launching this application has been temporarily blocked so that an {0} operation can complete.'
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
        Message = "The following programs must be closed before the {0} can proceed.`n`nPlease save your work, close the programs, and then continue. Alternatively, save your work and click `"Close Programs`"."
    }

    DeferPrompt = @{
        # Text displayed when there is a specific deferral deadline.
        Deadline = 'Deadline:'

        # Text displayed when a deferral option is available.
        ExpiryMessage = 'You can choose to defer the {0} until the deferral expires:'

        # Text displayed when there are a specific number of deferrals remaining.
        RemainingDeferrals = 'Remaining Deferrals:'

        # Text displayed after the deferral options.
        WarningMessage = 'Once the deferral has expired, you will no longer have the option to defer.'

        # Text displayed when only the deferral dialog is to be displayed and there are no applications to close.
        WelcomeMessage = 'The following application {0} is about to begin:'
    }

    DeploymentType = @{
        # Name displayed in UI for install deployment type.
        Install = 'Install'

        # Name displayed in UI for repair deployment type.
        Repair = 'Repair'

        # Name displayed in UI for uninstall deployment type.
        Uninstall = 'Uninstall'
    }

    DiskSpace = @{
        # Text displayed when the system does not have sufficient free disk space available to complete the deployment.
        Message = "You do not have enough disk space to complete the {3} of:`n{0}`n`nSpace required: {1}MB`nSpace available: {2}MB`n`nPlease free up enough disk space in order for the {3} to proceed."
    }

    Progress = @{
        # Default text displayed in the progress bar for deployments.
        Message = '{0} in progress. Please wait...'

        # Default subtext displayed in the progress bar for deployment.
        MessageDetail = 'This window will close automatically when the {0} is complete.'
    }

    RestartPrompt = @{
        # Button text for allowing the user to restart later.
        ButtonRestartLater = 'Minimize'

        # Button text for when wanting to restart the device now.
        ButtonRestartNow = 'Restart Now'

        # Text displayed when the device requires a restart.
        Message = 'In order for the {0} to complete, you must restart your computer.'

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
            # The countdown message displayed at the Welcome Screen to indicate when the deployment will continue if no response from user.
            CountdownMessage = 'The {0} will automatically continue in:'

            # This is a custom message to display at the Welcome Screen window.
            CustomMessage = ''
        }
        Fluent = @{
            # The subtitle underneath the Application Title, e.g. Company Name. Using {0} will insert the Application Type, e.g. App "Install".
            Subtitle = 'PSAppDeployToolkit - App {0}'

            # This is a message to prompt users to save their work.
            DialogMessage = 'Please save your work before continuing as the following applications will be closed automatically.'

            # This is a message to when there are no running processes available.
            DialogMessageNoProcesses = 'Please select {0} to continue. If you have any deferrals remaining, you may also choose to delay the {1}.'

            # This is a word used to describe the number of deferrals left. custom message to display at the Welcome Screen window.
            ButtonDeferRemaining = 'remain'

            # This is a phrase used to describe the process of deferring a deployment.
            ButtonLeftText = 'Defer'

            # This is a phrase used to describe the process of closing applications and commencing the deployment.
            ButtonRightText = 'Close Apps & {0}'
        }
    }
}
