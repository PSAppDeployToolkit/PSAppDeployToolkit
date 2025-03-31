@{
    BalloonText = @{
        # Text displayed in the balloon tip for successful completion of a deployment type.
        Complete = @{
            Install = 'Installation complete.'
            Repair = 'Repair complete.'
            Uninstall = 'Uninstallation complete.'
        }

        # Text displayed in the balloon tip for a failed deployment type.
        Error = @{
            Install = 'Installation failed.'
            Repair = 'Repair failed.'
            Uninstall = 'Uninstallation failed.'
        }

        # Text displayed in the balloon tip for fast retry of a deployment.
        FastRetry = @{
            Install = 'Installation not complete.'
            Repair = 'Repair not complete.'
            Uninstall = 'Uninstallation not complete.'
        }

        # Text displayed in the balloon tip for successful completion of a deployment type.
        RestartRequired = @{
            Install = 'Installation complete. A reboot is required.'
            Repair = 'Repair complete. A reboot is required.'
            Uninstall = 'Uninstallation complete. A reboot is required.'
        }

        # Text displayed in the balloon tip for the start of a deployment type.
        Start = @{
            Install = 'Installation started.'
            Repair = 'Repair started.'
            Uninstall = 'Uninstallation started.'
        }
    }

    BlockExecution = @{
        # Text displayed when prompting user that an application has been blocked.
        Message = @{
            Install = 'Launching this application has been temporarily blocked so that an installation operation can complete.'
            Repair = 'Launching this application has been temporarily blocked so that a repair operation can complete.'
            Uninstall = 'Launching this application has been temporarily blocked so that an uninstallation operation can complete.'
        }

        # The subtitle underneath the Install Title, e.g. Company Name. Only for Fluent dialogs.
        Subtitle = @{
            Install = 'PSAppDeployToolkit - App Installation'
            Repair = 'PSAppDeployToolkit - App Repair'
            Uninstall = 'PSAppDeployToolkit - App Uninstallation'
        }
    }

    DiskSpace = @{
        # Text displayed when the system does not have sufficient free disk space available to complete the installation.
        Message = @{
            Install = "You do not have enough disk space to complete the installation of:`n{0}`n`nSpace required: {1}MB`nSpace available: {2}MB`n`nPlease free up enough disk space in order to proceed with the installation."
            Repair = "You do not have enough disk space to complete the repair of:`n{0}`n`nSpace required: {1}MB`nSpace available: {2}MB`n`nPlease free up enough disk space in order to proceed with the repair."
            Uninstall = "You do not have enough disk space to complete the uninstallation of:`n{0}`n`nSpace required: {1}MB`nSpace available: {2}MB`n`nPlease free up enough disk space in order to proceed with the uninstallation."
        }
    }

    Progress = @{
        # Default message displayed in the progress bar.
        Message = @{
            Install = 'Installation in progress. Please wait...'
            Repair = 'Repair in progress. Please wait...'
            Uninstall = 'Uninstallation in progress. Please wait...'
        }

        # Default message detail displayed in the progress bar.
        MessageDetail = @{
            Install = 'This window will close automatically when the installation is complete.'
            Repair = 'This window will close automatically when the repair is complete.'
            Uninstall = 'This window will close automatically when the uninstallation is complete.'
        }

        # The subtitle underneath the Install Title, e.g. Company Name. Only for Fluent dialogs.
        Subtitle = @{
            Install = 'PSAppDeployToolkit - App Installation'
            Repair = 'PSAppDeployToolkit - App Repair'
            Uninstall = 'PSAppDeployToolkit - App Uninstallation'
        }
    }

    Prompt = @{
        # The subtitle underneath the Install Title, e.g. Company Name. Only for Fluent dialogs.
        Subtitle = @{
            Install = 'PSAppDeployToolkit - App Installation'
            Repair = 'PSAppDeployToolkit - App Repair'
            Uninstall = 'PSAppDeployToolkit - App Uninstallation'
        }
    }

    RestartPrompt = @{
        # Button text for allowing the user to restart later.
        ButtonRestartLater = 'Minimize'

        # Button text for when wanting to restart the device now.
        ButtonRestartNow = 'Restart Now'

        # Text displayed when the device requires a restart.
        Message = @{
            Install = 'In order for the installation to complete, you must restart your computer.'
            Repair = 'In order for the repair to complete, you must restart your computer.'
            Uninstall = 'In order for the uninstallation to complete, you must restart your computer.'
        }

        # Text displayed when indicating when the device will be restarted.
        MessageRestart = 'Your computer will be automatically restarted at the end of the countdown.'

        # Text displayed as a prefix to the time remaining, indicating that users should save their work, etc.
        MessageTime = 'Please save your work and restart within the allotted time.'

        # Text displayed to indicate the amount of time remaining until a restart will occur.
        TimeRemaining = 'Time remaining:'

        # Text displayed in the title of the restart prompt which helps the script identify whether there is already a restart prompt being displayed and not to duplicate it.
        Title = 'Restart Required'

        # The subtitle underneath the Install Title, e.g. Company Name. Only for Fluent dialogs.
        Subtitle = @{
            Install = 'PSAppDeployToolkit - App Installation'
            Repair = 'PSAppDeployToolkit - App Repair'
            Uninstall = 'PSAppDeployToolkit - App Uninstallation'
        }
    }

    WelcomePrompt = @{
        Classic = @{
            Close = @{
                # Text displayed on the close button when prompting to close running programs.
                ButtonClose = 'Close &Programs'

                # Text displayed on the continue button when prompting to close running programs.
                ButtonContinue = '&Continue'

                # Tooltip text displayed on the continue button when prompting to close running programs.
                ButtonContinueTooltip = 'Only select "Continue" after closing the above listed application(s).'

                # Text displayed on the defer button when prompting to close running programs.
                ButtonDefer = '&Defer'

                # Text displayed when counting down to automatically closing applications.
                CountdownMessage = 'NOTE: The program(s) will be automatically closed in:'

                # Text displayed when prompting to close running programs.
                Message = @{
                    Install = "The following programs must be closed before the installation can proceed.`n`nPlease save your work, close the programs, and then continue. Alternatively, save your work and click `"Close Programs`"."
                    Repair = "The following programs must be closed before the repair can proceed.`n`nPlease save your work, close the programs, and then continue. Alternatively, save your work and click `"Close Programs`"."
                    Uninstall = "The following programs must be closed before the uninstallation can proceed.`n`nPlease save your work, close the programs, and then continue. Alternatively, save your work and click `"Close Programs`"."
                }
            }

            Defer = @{
                # Text displayed when there is a specific deferral deadline.
                Deadline = 'Deadline:'

                # Text displayed when a deferral option is available.
                ExpiryMessage = @{
                    Install = 'You can choose to defer the installation until the deferral expires:'
                    Repair = 'You can choose to defer the repair until the deferral expires:'
                    Uninstall = 'You can choose to defer the uninstallation until the deferral expires:'
                }

                # Text displayed when there are a specific number of deferrals remaining.
                RemainingDeferrals = 'Remaining Deferrals:'

                # Text displayed after the deferral options.
                WarningMessage = 'Once the deferral has expired, you will no longer have the option to defer.'

                # Text displayed when only the deferral dialog is to be displayed and there are no applications to close.
                WelcomeMessage = @{
                    Install = 'The following application is about to be installed:'
                    Repair = 'The following application is about to be repaired:'
                    Uninstall = 'The following application is about to be uninstalled:'
                }
            }

            # The countdown message displayed at the Welcome Screen to indicate when the deployment will continue if no response from user.
            CountdownMessage = @{
                Install = 'The installation will automatically continue in:'
                Repair = 'The repair will automatically continue in:'
                Uninstall = 'The uninstallation will automatically continue in:'
            }

            # This is a custom message to display at the Welcome Screen window.
            CustomMessage = ''
        }

        Fluent = @{
            # The subtitle underneath the Install Title, e.g. Company Name. Only for Fluent dialogs.
            Subtitle = @{
                Install = 'PSAppDeployToolkit - App Installation'
                Repair = 'PSAppDeployToolkit - App Repair'
                Uninstall = 'PSAppDeployToolkit - App Uninstallation'
            }

            # This is a message to prompt users to save their work.
            DialogMessage = 'Please save your work before continuing as the following applications will be closed automatically.'

            # This is a message to when there are no running processes available.
            DialogMessageNoProcesses = @{
                Install = 'Please select Install to continue with the installation. If you have any deferrals remaining, you may also choose to delay the installation.'
                Repair = 'Please select Repair to continue with the repair. If you have any deferrals remaining, you may also choose to delay the repair.'
                Uninstall = 'Please select Uninstall to continue with the uninstallation. If you have any deferrals remaining, you may also choose to delay the uninstallation.'
            }

            # This is a word used to describe the number of deferrals left. custom message to display at the Welcome Screen window.
            TextBlockDeferralsRemaining = 'Remaining Deferrals'

            # This is a word used to describe the number of deferrals left. custom message to display at the Welcome Screen window.
            TextBlockRemain = 'remain'

            # This is a word used to describe the number of deferrals left. custom message to display at the Welcome Screen window.
            ButtonDeferRemaining = 'remain'

            # This is a word used to describe the number of deferrals left. custom message to display at the Welcome Screen window.
            TextBlockDeferralDeadline = 'Deferral Deadline'

            # This is a word used to describe the number of deferrals left. custom message to display at the Welcome Screen window.
            TextBlockAutomaticStartCountdown = 'Automatic Start Countdown'

            # This is a phrase used to describe the process of deferring a deployment.
            ButtonLeftText = 'Defer'

            # This is a phrase used to describe the process of closing applications and commencing the deployment.
            ButtonRightText = @{
                Install = 'Close Apps & Install'
                Repair = 'Close Apps & Repair'
                Uninstall = 'Close Apps & Uninstall'
            }

            # This is a phrase used to describe the process of commencing the deployment.
            ButtonRightTextNoProcesses = @{
                Install = 'Install'
                Repair = 'Repair'
                Uninstall = 'Uninstall'
            }
        }
    }
}
