@{
    BalloonText = @{
        Complete = "réussie."
        Error = "en échec."
        FastRetry = "incomplète."
        RestartRequired = "réussie. Un redémarrage est requis."
        Start = "en cours."
    }
    BlockExecution = @{
        Message = "Le lancement de cette application a été temporairement bloqué afin qu'une autre installation puisse se terminer."
    }
    ClosePrompt = @{
        ButtonClose = "Fermer Programmes"
        ButtonContinue = "Poursuivre"
        ButtonContinueTooltip = "Veuillez cliquer sur « Poursuivre » uniquement après avoir fermé la ou les application(s) ci-dessus."
        ButtonDefer = "Reporter l'installation"
        CountdownMessage = "REMARQUE: Les programmes seront automatiquement fermés dans:"
        Message = "Les programmes suivants doivent être fermés afin que l'installation s'initialise.`n`nMerci de sauvegarder votre travail, fermer tous les programmes, et continuer. Vous pouvez aussi sauvegarder votre travail puis cliquez sur « Fermer Programmes »."
    }
    DeferPrompt = @{
        Deadline = "Temps limite:"
        ExpiryMessage = "Vous pouvez choisir de reporter l'installation:"
        RemainingDeferrals = "Nombre(s) de report restant(s):"
        WarningMessage = "Quand le temps aura expiré, vous n'aurez plus la possibilité de reporter."
        WelcomeMessage = "L'application suivante est sur le point d'être installée:"
    }
    DeploymentType = @{
        Install = "Installation"
        Repair = "Réparation"
        Uninstall = "Désinstallation"
    }
    DiskSpace = @{
        Message = "Vous n'avez pas assez d'espace sur le disque pour compléter l'installation de:`n{0}`n`nEspace requis: {1}MB`nEspace disponible: {2}MB`n`nMerci de vous assurez d'avoir assez d'espace libre pour pouvoir continuer l'installation."
    }
    Progress = @{
        MessageInstall = "Installation en cours, merci de patienter..."
        MessageInstallDetail = "Cette fenêtre se fermera automatiquement lorsque l'installation sera terminée."
        MessageRepair = "Réparation en cours, merci de patienter..."
        MessageRepairDetail = "Cette fenêtre se fermera automatiquement lorsque la réparation sera terminée."
        MessageUninstall = "Désinstallation en cours, merci de patienter..."
        MessageUninstallDetail = "Cette fenêtre se fermera automatiquement lorsque la désinstallation sera terminée."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimiser"
        ButtonRestartNow = "Redémarrer Maintenant"
        Message = "Pour que l'installation soit compléte, vous devez redémarrer votre ordinateur."
        MessageRestart = "Votre ordinateur sera automatiquement redémarré à la fin du décompte."
        MessageTime = "Merci de sauvegarder votre travail et de redémarrer avant que le temps spécifié ne soit écoulé."
        TimeRemaining = "Temps restant:"
        Title = "Redémarrage Requis"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "L'{0} va continuer automatiquement:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - App {0}'
            DialogMessage = 'Veuillez sauvegarder votre travail avant de continuer, car les applications suivantes seront automatiquement fermées.'
            DialogMessageNoProcesses = "Veuillez sélectionner Installer pour poursuivre l'installation. S'il vous reste des reports, vous pouvez également choisir de retarder l'installation."
            ButtonDeferRemaining = 'rester'
            ButtonLeftText = 'Report'
            ButtonRightText = 'Fermer les applications et installer'
            ButtonRightTextNoProcesses = 'Installer'
        }
    }
}
