@{
    BalloonTip = @{
        Start = @{
            Install = "L'installation a commencé."
            Repair = 'Réparation commencée.'
            Uninstall = 'Désinstallation commencée.'
        }
        Complete = @{
            Install = 'Installation terminée.'
            Repair = 'Réparation terminée.'
            Uninstall = 'Désinstallation terminée.'
        }
        RestartRequired = @{
            Install = 'Installation terminée. Un redémarrage est nécessaire.'
            Repair = 'Réparation terminée. Un redémarrage est nécessaire.'
            Uninstall = 'Désinstallation terminée. Un redémarrage est nécessaire.'
        }
        FastRetry = @{
            Install = "L'installation n'est pas terminée."
            Repair = 'Réparation non terminée.'
            Uninstall = 'Désinstallation non terminée.'
        }
        Error = @{
            Install = 'Installation échouée.'
            Repair = 'Échec de la réparation.'
            Uninstall = 'La désinstallation a échoué.'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = "Le lancement de cette application a été temporairement bloqué pour permettre l'achèvement d'une opération d'installation."
            Repair = "Le lancement de cette application a été temporairement bloqué pour permettre la réalisation d'une opération de réparation."
            Uninstall = "Le lancement de cette application a été temporairement bloqué afin qu'une opération de désinstallation puisse être menée à bien."
        }
        Subtitle = @{
            Install = "{Toolkit\CompanyName} - Installation de l'application"
            Repair = "{Toolkit\CompanyName} - Réparation de l'application"
            Uninstall = "{Toolkit\CompanyName} - Désinstallation de l'application"
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "Vous n'avez pas assez d'espace disque pour terminer l'installation de:`n{0}`n`space requis : {1}MB`n espace disponible : {2}MB`n`nVeuillez libérer suffisamment d'espace disque pour poursuivre l'installation."
            Repair = "Vous n'avez pas assez d'espace disque pour terminer la réparation de:`n{0}`n`space requis : {1}MB`nEspace disponible : {2}MB`n`nVeuillez libérer suffisamment d'espace disque pour procéder à la réparation."
            Uninstall = "Vous n'avez pas assez d'espace disque pour terminer la désinstallation de:`n{0}`n`n`Espace requis : {1}MB`nEspace disponible : {2}MB`n`nVeuillez libérer suffisamment d'espace disque pour procéder à la désinstallation."
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = "{Toolkit\CompanyName} - Installation de l'application"
            Repair = "{Toolkit\CompanyName} - Réparation de l'application"
            Uninstall = "{Toolkit\CompanyName} - Désinstallation de l'application"
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'Installation en cours. Veuillez patienter…'
            Repair = 'Réparation en cours. Veuillez patienter…'
            Uninstall = 'Désinstallation en cours. Veuillez patienter…'
        }
        MessageDetail = @{
            Install = "Cette fenêtre se fermera automatiquement lorsque l'installation sera terminée."
            Repair = "Cette fenêtre se fermera automatiquement lorsque la réparation sera terminée."
            Uninstall = "Cette fenêtre se fermera automatiquement lorsque la désinstallation sera terminée."
        }
        Subtitle = @{
            Install = "{Toolkit\CompanyName} - Installation de l'application"
            Repair = "{Toolkit\CompanyName} - Réparation de l'application"
            Uninstall = "{Toolkit\CompanyName} - Désinstallation de l'application"
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Réduire'
        ButtonRestartNow = 'Redémarrer maintenant'
        Message = @{
            Install = "Pour que l'installation soit terminée, vous devez redémarrer votre ordinateur."
            Repair = "Pour que la réparation soit terminée, vous devez redémarrer votre ordinateur."
            Uninstall = "Pour que la désinstallation soit terminée, vous devez redémarrer votre ordinateur."
        }
        CustomMessage = ''
        MessageRestart = 'Votre ordinateur sera automatiquement redémarré à la fin du compte à rebours.'
        MessageTime = 'Veuillez sauvegarder votre travail et redémarrer dans le temps imparti.'
        TimeRemaining = 'Temps restant:'
        Title = 'Redémarrage requis'
        Subtitle = @{
            Install = "{Toolkit\CompanyName} - Installation de l'application"
            Repair = "{Toolkit\CompanyName} - Réparation de l'application"
            Uninstall = "{Toolkit\CompanyName} - Désinstallation de l'application"
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = "L'application suivante est sur le point d'être installée:"
                Repair = "L'application suivante est sur le point d'être réparée:"
                Uninstall = "L'application suivante est sur le point d'être désinstallée:"
            }
            CloseAppsMessage = @{
                Install = "Les programmes suivants doivent être fermés avant que l'installation ne puisse avoir lieu.`n`nVeuillez enregistrer votre travail, fermer les programmes, puis continuer. Vous pouvez également enregistrer votre travail et cliquer sur « Fermer les programmes »."
                Repair = "The following programs must be closed before the repair can proceed.`n`nPlease save your work, close the programs, and then continue. Vous pouvez également enregistrer votre travail et cliquer sur « Fermer les programmes »."
                Uninstall = "Les programmes suivants doivent être fermés pour que la désinstallation puisse avoir lieu.`n`n Veuillez enregistrer votre travail, fermer les programmes, puis continuer. Vous pouvez également enregistrer votre travail et cliquer sur « Fermer les programmes »."
            }
            ExpiryMessage = @{
                Install = "Vous pouvez choisir de différer l'installation jusqu'à l'expiration du délai:"
                Repair = "Vous pouvez choisir de différer la réparation jusqu'à l'expiration du délai:"
                Uninstall = "Vous pouvez choisir de différer la désinstallation jusqu'à l'expiration du délai:"
            }
            DeferralsRemaining = 'Reports restants:'
            DeferralDeadline = 'Date limite:'
            ExpiryWarning = "Une fois le report expiré, vous n'aurez plus la possibilité de le différer."
            CountdownDefer = @{
                Install = "L'installation se poursuivra automatiquement dans:"
                Repair = "La réparation se poursuivra automatiquement dans:"
                Uninstall = "La désinstallation se poursuivra automatiquement dans :"
            }
            CountdownClose = 'NOTE: Le(s) programme(s) sera(ont) automatiquement fermé(s) dans:'
            ButtonClose = 'Fermer &Programmes'
            ButtonDefer = '&Report'
            ButtonContinue = '&Continuer'
            ButtonContinueTooltip = "Ne sélectionnez « Continuer » qu'après avoir fermé la ou les applications listées ci-dessus."
        }
        Fluent = @{
            DialogMessage = 'Veuillez sauvegarder votre travail avant de continuer car les applications suivantes seront fermées automatiquement.'
            DialogMessageNoProcesses = @{
                Install = "Veuillez sélectionner Installer pour poursuivre l'installation. S'il vous reste des reports, vous pouvez également choisir de retarder l'installation."
                Repair = "Veuillez sélectionner Réparer pour poursuivre la réparation. S'il vous reste des reports, vous pouvez également choisir de retarder la réparation."
                Uninstall = "Veuillez sélectionner Désinstaller pour poursuivre la désinstallation. S'il vous reste des reports, vous pouvez également choisir de retarder la désinstallation."
            }
            AutomaticStartCountdown = 'Compte à rebours de démarrage automatique'
            DeferralsRemaining = 'Reports restants'
            DeferralDeadline = 'Date limite de report'
            ButtonLeftText = @{
                Install = 'Fermer les applications et installer'
                Repair = 'Fermer les applications et réparer'
                Uninstall = 'Fermer les applications et désinstaller'
            }
            ButtonLeftNoProcessesText = @{
                Install = 'Installer'
                Repair = 'Réparer'
                Uninstall = 'Désinstaller'
            }
            ButtonRightText = 'Différer'
            Subtitle = @{
                Install = "{Toolkit\CompanyName} - Installation de l'application"
                Repair = "{Toolkit\CompanyName} - Réparation de l'application"
                Uninstall = "{Toolkit\CompanyName} - Désinstallation de l'application"
            }
        }
        CustomMessage = ''
    }
}
